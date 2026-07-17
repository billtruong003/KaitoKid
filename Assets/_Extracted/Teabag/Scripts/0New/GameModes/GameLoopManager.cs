using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.GameMode;
using Teabag.Networking;
using Squido.JungleXRKit.Avatar;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestrates the FreeForAll game loop:
///   SubwayBoarding -> SubwayDeparting -> SubwayDropping ->
///   BattleRoyale -> PostMatch -> return to SpaceStation.
///
/// Players arrive here from WaitingZone (which handles the pre-game lobby).
/// Spawned as GameManagerPrefab on the FreeForAll GameModeSo.
/// </summary>
public class GameLoopManager : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnPhaseChanged))]
    public GameLoopPhase phase { get; set; }

    public event Action<GameLoopPhase> PhaseChanged;

    [Networked]
    public long transitionTime { get; set; }

    [Networked]
    public MatchType matchType { get; set; }

    [Header("Subway Settings")]
    [SerializeField] private float _subwayBoardingTimeoutSeconds = 120f;
    [Tooltip("Max seconds to wait for player to exit subway before force-transitioning to BattleRoyale.")]
    [SerializeField] private float _maxSubwayExitWaitSeconds = 30f;
    [SerializeField] private float _boardingForceTeleportLeadSeconds = 5f;
    [SerializeField] private float _sessionLockLeadSeconds = 5f;

    [Header("Post-Match")]
    [SerializeField] private float _postMatchDurationSeconds = 8f;

    [Header("Prefabs")]
    [SerializeField] private NetworkObject _battleRoyaleManagerPrefab;

    private INetworkManager _networkManager;
    private IGorillaService _gorillaService;
    private ITeleportService _teleportService;
    private GameLoopService _gameLoopService;
    private bool _sceneSwapInProgress;
    private bool _battleRoyaleSpawned;
    private bool _returnTriggered;
    private bool _startingBattleRoyale;
    private bool _waitingForSubwayExit;
    private const float SubwayRegistrationTimeoutSeconds = 15f;
    private const float RigSpawnTimeoutSeconds = 10f;
    private const float MinOffsetSqrMagnitude = 0.001f;

    private float _subwayExitElapsed;
    private float _battleRoyalePhaseElapsed;
    private CancellationTokenSource _cts;
    private GameLoopPhase _lastProcessedPhase;
    private bool _staleScenesCleared;
    private bool _wasAuthority;
    private bool _hasRunSetup;
    private bool _rigFrozen;
    private float _setupWaitTime;
    private bool _boardingForceTeleported;
    private bool _boardingPositioned;
    private bool _insideSubway;
    private Rigidbody _cachedPlayerRb;
    private SubwayDropVehicle _subscribedSubway;
    private bool _boardedOnce; // true after the single boarding teleport has run for this manager instance

    public DateTime TransitionTime
    {
        get => new DateTime(transitionTime);
        set => transitionTime = value.Ticks;
    }

    private bool IsSinglePlayer => _networkManager?.Runner != null && (_networkManager.Runner.IsSinglePlayer || _networkManager.Runner.GameMode == Fusion.GameMode.Single);
    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        _cts = new CancellationTokenSource();

        _networkManager = ServiceLocator.Get<INetworkManager>();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        _teleportService = ServiceLocator.Get<ITeleportService>();
        _gameLoopService = ServiceLocator.Get<GameLoopService>();
        _gameLoopService?.Register(this);
        _gameLoopService?.InvokeManagerChanged();
        GameServices.GorillaGameManagerExists = () => _gameLoopService?.HasManager == true;
        GameServices.SuppressSceneCleanup = () => _gameLoopService?.HasManager == true;
        GameServices.SuppressRespawnOnSceneLoad = () => _gameLoopService?.HasManager == true;

        // Claim the post-spawn teleport handshake so JoinGameExAsync waits for our
        // subway positioning to finish before releasing the rig hold. Without this
        // claim, the rig would be released into pre-position pose and gravity would
        // tunnel it through the subway floor.
        _networkManager?.ClaimSpawnTeleport();

        if (HasStateAuthority)
        {
            ParseMatchTypeFromSession();
            transitionTime = 0;
            phase = GameLoopPhase.SubwayBoarding;

            // Lock session immediately — players arrived from WaitingZone, no new joins
            LockSession("arrived from WaitingZone");
        }

        _lastProcessedPhase = phase;
        _wasAuthority = HasStateAuthority;
        _hasRunSetup = false;

        // Always position player inside subway on spawn, regardless of current phase.
        // This covers normal flow (SubwayBoarding) AND late joiners (SubwayDropping, BattleRoyale, etc.).
        // HandleSubwayBoarding is async and waits for subway — this is the single entry point.
        PositionPlayerInSubwayOnSpawn(_cts.Token);

        CleanupStaleScenesAsync(_cts.Token);
    }

    private void ReWireGameManagerExistsBridge()
    {
        GameServices.GorillaGameManagerExists = () => (_gameLoopService?.HasManager == true) || GorillaGameManager.instance != null;
    }

    private async void CleanupStaleScenesAsync(CancellationToken ct)
    {
        // Fade is owned by PositionPlayerInSubwayOnSpawn — don't double-fade here.
        try
        {
            var gameModeData = _networkManager?.GetGameMode(NetworkGameModeIds.FreeForAll);
            if (gameModeData != null)
            {
                var validScenes = gameModeData.SceneIndices;

                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.buildIndex <= 0) continue;
                    if (validScenes.Contains(scene.buildIndex)) continue;
                    if (_networkManager.IsSceneIgnoredForUnload(scene.path)) continue;

                    if (SceneManager.sceneCount > 1)
                    {
                        ct.ThrowIfCancellationRequested();
                        var op = SceneManager.UnloadSceneAsync(scene.buildIndex);
                        while (op != null && !op.isDone)
                            await UniTask.Yield();
                    }
                }
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception e)
        {
            Debug.LogError($"[GameLoopManager] CleanupStaleScenesAsync failed: {e}");
        }

        _hasRunSetup = true;
        _staleScenesCleared = true;
    }

    private void ParseMatchTypeFromSession()
    {
        var room = _networkManager.CurrentRoom;
        if (room != null)
        {
            matchType = (MatchType)room.MatchType;
            return;
        }
        matchType = (int)MatchType.FreeForAll;
    }

    private void Update()
    {
        if (!_hasRunSetup)
        {
            _setupWaitTime += Time.deltaTime;

            const float setupTimeoutSeconds = 20f;
            if (_setupWaitTime > setupTimeoutSeconds)
            {
                GameLogger.Error("[GameLoopManager] Setup timed out — forcing unfreeze");
                _hasRunSetup = true;
                _staleScenesCleared = true;
                _boardingPositioned = true;
            }
        }

        // Track when the player leaves the subway so we can reset locomotion state.
        // NOTE: The old per-frame reposition loop has been removed — MovingPlatformLocomotion
        // now handles carrying the player with the subway via the TrainFloor foot raycast.
        // We only need to detect the exit event for the locomotion reset.
        if (_insideSubway)
        {
            var subway = _gameLoopService?.SubwayDropVehicle;
            if (subway)
            {
                if (subway.IsRouteStarted && !subway.PlayerIsInside)
                {
                    _insideSubway = false;
                    LocalHardwareRig?.LocomotionController.Reset();
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && !_wasAuthority)
        {
            _wasAuthority = true;
            RecoverAuthorityState();
        }
        else if (!HasStateAuthority)
        {
            _wasAuthority = false;
        }

        if (HasStateAuthority)
            DrivePhaseTransitions();

        if (phase == GameLoopPhase.SubwayBoarding && transitionTime != 0 && !_boardingForceTeleported)
        {
            var secondsLeft = (TransitionTime - SyncedTime.Now).TotalSeconds;
            if (secondsLeft <= _boardingForceTeleportLeadSeconds)
            {
                _boardingForceTeleported = true;
                ForceTeleportIntoSubwayIfOutside();
            }
        }
    }

    private void RecoverAuthorityState()
    {
        // GameLogger.Warning($"[GameLoopManager] Authority acquired at phase={phase} — recovering state");
        _battleRoyaleSpawned = GorillaGameManager.instance != null;
        _startingBattleRoyale = false;
        _sceneSwapInProgress = false;
    }

    private void DrivePhaseTransitions()
    {
        switch (phase)
        {
            case GameLoopPhase.SubwayBoarding:
                UpdateSubwayBoarding();
                break;
            case GameLoopPhase.SubwayDeparting:
                break;
            case GameLoopPhase.SubwayDropping:
                UpdateSubwayDropping();
                break;
            case GameLoopPhase.BattleRoyale:
                _battleRoyalePhaseElapsed += Runner.DeltaTime;

                const float battleRoyaleGracePeriodSeconds = 5f;
                if (_battleRoyaleSpawned && _battleRoyalePhaseElapsed > battleRoyaleGracePeriodSeconds && GorillaGameManager.instance == null)
                {
                    GameLogger.Warning($"[GLM] BattleRoyaleManager gone without OnMatchComplete — forcing PostMatch (elapsed={_battleRoyalePhaseElapsed:F1}s)");
                    HandleMatchComplete();
                }
                break;
            case GameLoopPhase.PostMatch:
                UpdatePostMatch();
                break;
        }
    }

    private void LockSession(string reason)
    {
        _networkManager.CurrentRoom?.SetIsVisible(false);
        if (_networkManager.CurrentRoom != null)
            _networkManager.CurrentRoom.IsRunning = true;
    }

    /// <summary>
    /// Called once from Spawned(). Async: waits for subway, positions player, unfreezes.
    /// Replaces the old HandleSubwayBoarding which was only triggered by OnPhaseChanged.
    /// </summary>
    private async void PositionPlayerInSubwayOnSpawn(CancellationToken ct)
    {
        if (_sceneSwapInProgress)
            return;

        _sceneSwapInProgress = true;

        try
        {
            if (ct.IsCancellationRequested || Runner == null || Runner.IsShutdown)
                return;

            // Wait for SubwayDropVehicle to register
            DateTime timeout = DateTime.UtcNow.AddSeconds(SubwayRegistrationTimeoutSeconds);
            while (_gameLoopService?.SubwayDropVehicle == null && DateTime.UtcNow < timeout && !ct.IsCancellationRequested)
                await UniTask.Yield();

            var subway = _gameLoopService?.SubwayDropVehicle;
            if (subway == null)
            {
                Debug.LogWarning($"[GameLoopManager] SubwayDropVehicle not found after {SubwayRegistrationTimeoutSeconds}s — player will not be positioned");
            }
            else
            {
                SubscribeToSubwayRouteStarted(subway);
                // Wait for the local gorilla rig to exist before teleporting.
                // The rig is spawned by NetworkObjectsManager and may not be ready yet.
                _gorillaService ??= ServiceLocator.Get<IGorillaService>();
                var rigTimeout = DateTime.UtcNow.AddSeconds(RigSpawnTimeoutSeconds);
                while (DateTime.UtcNow < rigTimeout && !ct.IsCancellationRequested)
                {
                    var rig = LocalHardwareRig;
                    if (rig != null)
                        break;

                    await UniTask.Yield();
                }

                if (LocalHardwareRig == null)
                {
                    Debug.LogError($"[GameLoopManager] Rig not spawned after {RigSpawnTimeoutSeconds}s — skipping position");
                }
                else
                {
                    PositionPlayerInSubway(subway);
                }
            }

            _boardingPositioned = true;
            _boardingForceTeleported = false;

            if (HasStateAuthority && phase == GameLoopPhase.SubwayBoarding)
            {
                TransitionTime = SyncedTime.Now.AddSeconds(_subwayBoardingTimeoutSeconds);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameLoopManager] PositionPlayerInSubwayOnSpawn failed: {e}");
            _boardingPositioned = true;
        }
        finally
        {
            _sceneSwapInProgress = false;
            CheckForMissedPhaseChange();

            // Always signal — success, missing-rig fallback, missing-subway fallback,
            // or thrown exception. NetworkManager.JoinGameExAsync is awaiting this to
            // release the rig hold; a missing signal would block the join until the
            // 12s safety timeout.
            _networkManager?.SignalSpawnTeleportComplete();
        }
    }

    private void PositionPlayerInSubway(SubwayDropVehicle subway)
    {
        // If the WaitingZoneManager already boarded this rig and MovingPlatformLocomotion is
        // carrying it, skip the re-teleport — it would land on a different spline-pose than
        // the waiting-zone teleport and compound the visible offset.
        if (_boardedOnce)
        {
            _insideSubway = true;
            // Still clear pending values so they don't leak to the next boarding.
            if (_networkManager != null)
            {
                _networkManager.PendingShuttleLocalOffset = Vector3.zero;
                _networkManager.PendingShuttleRotation = Quaternion.identity;
            }
            return;
        }

        // Fallback
        var spawnPos = subway.spawnPoints[0].position;
        var spawnRot = subway.spawnPoints[0].rotation;
        TeleportLocalPlayer(spawnPos, spawnRot);

        // Track player inside subway continuously until they jump or are ejected
        _insideSubway = true;
        _boardedOnce = true;

        // Clear pending values so they are not reused
        if (_networkManager != null)
        {
            _networkManager.PendingShuttleLocalOffset = Vector3.zero;
            _networkManager.PendingShuttleRotation = Quaternion.identity;
        }
    }

    private void UpdateSubwayBoarding()
    {
        if (transitionTime != 0 && SyncedTime.Now >= TransitionTime)
        {
            transitionTime = 0;
            phase = GameLoopPhase.SubwayDeparting;
        }
    }

    private void ForceTeleportIntoSubwayIfOutside()
    {
        var vehicle = _gameLoopService?.SubwayDropVehicle;
        var player = LocalHardwareRig;
        if (vehicle == null || player == null || player.Headset == null) return;

        if (!vehicle.IsInsideInterior(player.Headset.Position))
        {
            var localIndex = GetLocalPlayerIndex();
            TeleportLocalPlayer(vehicle.GetSpawnPosition(localIndex), vehicle.GetSpawnRotation(localIndex));
            // GameLogger.Warning($"[GameLoopManager] Force-teleported player into subway ({_boardingForceTeleportLeadSeconds}s before timeout)");
        }
    }

    private void HandleSubwayDeparting()
    {
        var subwayVehicle = _gameLoopService?.SubwayDropVehicle;
        if (subwayVehicle == null)
        {
            GameLogger.Error("[GameLoopManager] SubwayDropVehicle not found!");
            if (HasStateAuthority)
                phase = GameLoopPhase.SubwayDropping;
            return;
        }

        // Authority snaps the subway to spline-knot-0 synchronously inside StartRoute.
        // Non-authority receives the snap via Rpc_StartRoute -> InitLocalRouteState.
        // Both paths fire SubwayDropVehicle.RouteStarted, which is subscribed in
        // PositionPlayerInSubwayOnSpawn to re-teleport the local player into the
        // now-snapped interior (the transform delta exceeds MovingPlatformLocomotion's carry cap).
        if (HasStateAuthority)
            subwayVehicle.StartRoute();

        _waitingForSubwayExit = false;

        if (HasStateAuthority)
            phase = GameLoopPhase.SubwayDropping;
    }

    private void SubscribeToSubwayRouteStarted(SubwayDropVehicle subway)
    {
        if (_subscribedSubway == subway) return;
        UnsubscribeFromSubwayRouteStarted();

        _subscribedSubway = subway;
        subway.RouteStarted += HandleRouteStarted;

        // Late joiner: route already started — reposition immediately into the snapped subway.
        if (subway.IsRouteStarted)
            RepositionLocalPlayerInSubway(subway);
    }

    private void UnsubscribeFromSubwayRouteStarted()
    {
        if (_subscribedSubway == null) return;
        _subscribedSubway.RouteStarted -= HandleRouteStarted;
        _subscribedSubway = null;
    }

    private void HandleRouteStarted()
    {
        var subway = _gameLoopService?.SubwayDropVehicle;
        if (subway == null) return;
        RepositionLocalPlayerInSubway(subway);
    }

    private void RepositionLocalPlayerInSubway(SubwayDropVehicle subway)
    {
        // Boarding is single-shot per manager instance. If WaitingZoneManager (or a prior
        // PositionPlayerInSubway) already teleported us, MovingPlatformLocomotion is
        // already carrying the rig — a second teleport would visibly snap to a different
        // spline-pose than the first and produce the compounding-offset symptom.
        if (_boardedOnce) return;

        var player = LocalHardwareRig;
        if (player == null || player.Headset == null) return;

        Vector3 spawnPos;
        Quaternion spawnRot;

            int localIndex = GetLocalPlayerIndex();
            spawnPos = subway.GetSpawnPosition(localIndex);
            spawnRot = subway.GetSpawnRotation(localIndex);

        TeleportLocalPlayer(spawnPos, spawnRot);
        _boardedOnce = true;
    }

    private void UpdateSubwayDropping()
    {
        if (_startingBattleRoyale) return;

        var subway = _gameLoopService?.SubwayDropVehicle;
        if (subway == null) return;

        if (!subway.IsRouteStarted) return;

        if (!_waitingForSubwayExit)
        {
            _waitingForSubwayExit = true;
            _subwayExitElapsed = 0f;
        }
        _subwayExitElapsed += Time.deltaTime;

        // Start BR only when the player has actually left the subway,
        // the route is complete (forced eject), or the wait timed out.
        // Do NOT start on doorsOpen alone — the subway is still in the air
        // and the storm would kill players before they can jump.
        bool playerJumped = !subway.PlayerIsInside && subway.doorsOpen;
        bool routeDone = subway.IsRouteComplete;
        bool timedOut = _subwayExitElapsed >= _maxSubwayExitWaitSeconds;

        if (playerJumped || routeDone || timedOut)
        {
            if (timedOut)
                GameLogger.Warning($"[GameLoopManager] Subway exit wait timed out after {_maxSubwayExitWaitSeconds}s");

            _startingBattleRoyale = true;
            _waitingForSubwayExit = false;
            StartBattleRoyalePhase();
        }
    }

    private void StartBattleRoyalePhase()
    {
        if (Runner == null || Runner.IsShutdown) return;

        if (!_battleRoyaleSpawned && _battleRoyaleManagerPrefab != null)
        {
            var spawnedObj = Runner.Spawn(_battleRoyaleManagerPrefab);
            _battleRoyaleSpawned = true;
            GameLogger.Warning($"[GLM] BattleRoyaleManager spawned (obj={spawnedObj != null}, prefab={_battleRoyaleManagerPrefab.name})");
            ReWireGameManagerExistsBridge();
        }
        else if (_battleRoyaleManagerPrefab == null)
        {
            GameLogger.Error("[GLM] battleRoyaleManagerPrefab is NULL — cannot spawn BattleRoyaleManager!");
        }

        _battleRoyalePhaseElapsed = 0f;
        phase = GameLoopPhase.BattleRoyale;
    }

    public void HandleMatchComplete()
    {
        if (!HasStateAuthority)
            return;

        // GameLogger.Warning("[GLM] HandleMatchComplete -> PostMatch");
        _returnTriggered = false;
        TransitionTime = SyncedTime.Now.AddSeconds(_postMatchDurationSeconds);
        phase = GameLoopPhase.PostMatch;
    }

    private void UpdatePostMatch()
    {
        if (!_returnTriggered && SyncedTime.Now >= TransitionTime)
        {
            _returnTriggered = true;
            HandleReturnToStationAsync();
        }
    }

    public void OnPhaseChanged()
    {
        _lastProcessedPhase = phase;

        switch (phase)
        {
            case GameLoopPhase.SubwayBoarding:
                // Player positioning is handled by PositionPlayerInSubwayOnSpawn() from Spawned().
                // No action needed here.
                break;
            case GameLoopPhase.SubwayDeparting:
                HandleSubwayDeparting();
                break;
            case GameLoopPhase.PostMatch:
                if (!HasStateAuthority)
                    _ = WaitAndReturnToStationAsync();
                break;
        }
        PhaseChanged?.Invoke(phase);
    }

    private void CheckForMissedPhaseChange()
    {
        if (phase != _lastProcessedPhase)
        {
            OnPhaseChanged();
        }
    }

    private async UniTask WaitAndReturnToStationAsync()
    {
        float waitTime = _postMatchDurationSeconds;
        if (transitionTime != 0)
        {
            var remaining = TransitionTime - SyncedTime.Now;
            if (remaining.TotalSeconds > 0)
                waitTime = (float)remaining.TotalSeconds;
        }

        // No cancellation token — must survive GameLoopManager despawn
        await UniTask.Delay((int)(waitTime * 1000));

        if (_returnTriggered) return;
        _returnTriggered = true;
        HandleReturnToStationAsync();
    }

    public async void HandleReturnToStationAsync()
    {
        // Clean up ragdolls before leaving — they're local GameObjects that
        // survive scene transitions and would carry into SpaceStation.
        BattleRoyaleManager.CleanUpRagdolls();

        // Capture ref — _networkManager may be nulled during despawn
        var nm = _networkManager ?? ServiceLocator.Get<INetworkManager>();

        try
        {
            var gameModeData = nm?.GetGameMode(NetworkGameModeIds.FreeForAll);
            if (gameModeData)
            {
                var ffaScenes = gameModeData.SceneIndices;
                for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded || scene.buildIndex <= 0 || !ffaScenes.Contains(scene.buildIndex))
                        continue;

                    if (SceneManager.sceneCount <= 1)
                        break;

                    var op = SceneManager.UnloadSceneAsync(scene.buildIndex);
                    if (op != null)
                    {
                        while (!op.isDone)
                            await UniTask.Yield();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameLoopManager] Pre-return scene cleanup failed: {e}");
        }

        // Always attempt to join SpaceStation even if cleanup had errors
        nm?.JoinGameEx(NetworkGameModeIds.SpaceStation, "", true);
    }

    private void TeleportLocalPlayer(Vector3 position, Quaternion rotation)
    {
        if (_teleportService == null)
        {
            GameLogger.Warning("[GameLoopManager] TeleportLocalPlayer: no teleport service");
            return;
        }

        var yaw = rotation.eulerAngles.y;
        var yawRot = Quaternion.Euler(0f, yaw, 0f);

        _teleportService.TeleportToPosition(position, yawRot);
    }

    private int GetLocalPlayerIndex()
    {
        if (Runner == null) return 0;
        var localId = Runner.LocalPlayer.PlayerId;
        var index = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            if (p.PlayerId < localId)
                index++;
        }
        return index;
    }

    [ContextMenu("Debug/Force Launch Subway + Game")]
    public void DebugForceLaunchSubway()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GameLoopManager] Debug launch requires Play mode");
            return;
        }

        if (HasStateAuthority)
        {
            DebugForceLaunchSubway_Internal();
        }
        else if (Object != null && Runner != null)
        {
            Rpc_DebugForceLaunchSubway();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_DebugForceLaunchSubway()
    {
        DebugForceLaunchSubway_Internal();
    }

    private void DebugForceLaunchSubway_Internal()
    {
        _staleScenesCleared = true;
        _hasRunSetup = true;

        var subway = _gameLoopService?.SubwayDropVehicle;
        if (subway != null)
        {
            subway.StartRoute();
        }
        else
        {
            Debug.LogWarning("[GameLoopManager] DEBUG: SubwayDropVehicle not found");
        }

        _waitingForSubwayExit = false;
        phase = GameLoopPhase.SubwayDropping;
    }

    [ContextMenu("Debug/Skip to BattleRoyale")]
    public void DebugSkipToBattleRoyale()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[GameLoopManager] Debug skip requires Play mode");
            return;
        }

        if (HasStateAuthority)
        {
            DebugSkipToBattleRoyale_Internal();
        }
        else if (Object != null && Runner != null)
        {
            Rpc_DebugSkipToBattleRoyale();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_DebugSkipToBattleRoyale()
    {
        DebugSkipToBattleRoyale_Internal();
    }

    private void DebugSkipToBattleRoyale_Internal()
    {
        _staleScenesCleared = true;
        _hasRunSetup = true;
        _startingBattleRoyale = true;
        StartBattleRoyalePhase();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        UnsubscribeFromSubwayRouteStarted();

        _cachedPlayerRb = null;
        _boardedOnce = false;

        _gameLoopService?.Unregister(this);

        GameServices.GorillaGameManagerExists = null;
        GameServices.SuppressSceneCleanup = null;
        GameServices.SuppressRespawnOnSceneLoad = null;

        if (_networkManager != null)
        {
            _networkManager.PendingShuttleLocalOffset = Vector3.zero;
            _networkManager.PendingShuttleRotation = Quaternion.identity;
        }
    }
}
