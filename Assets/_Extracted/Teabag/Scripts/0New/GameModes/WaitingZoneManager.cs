using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.GameMode;
using Teabag.Networking;
using Teabag.Core;
using UnityEngine;

/// <summary>
/// Networked manager for the WaitingZone game mode.
/// Set as GameManagerPrefab on GameMode_LOOP_SpaceStation_WaitingZone.
///
/// Flow:
///   1. Arrival train arrives with passengers from SpaceStation
///   2. Offboarding: players exit / forced off train after timer
///   3. Train departs empty (visual), goes Idle
///   4. PreGameLobby: wait for enough players (late joiners spawn on platform)
///   5. When lobby full: session locked, countdown
///   6. BoardingDeparture: all players teleported into SubwayDropVehicle
///   7. Transitioning: scene transition to FreeForAll (players stay inside subway)
///
/// The departure vehicle is the SubwayDropVehicle (same vehicle that continues
/// in FreeForAll for the BR drop ride), NOT a second train.
/// </summary>
public class WaitingZoneManager : NetworkBehaviour
{
    private const float _minOffsetSqrMagnitude = 0.001f;
    private const int _authorityJoinDelayMs = 500;
    private const int _clientJoinDelayMs = 2500;

    [Header("Lobby Settings")]
    [SerializeField] private int _maxPlayers = 16;
    [SerializeField] private int _requiredPlayersFFA = 2;
    [SerializeField] private int _requiredPlayersDuo = 4;
    [SerializeField] private int _requiredPlayersSquads = 8;
    [SerializeField] private int _lobbyCountdownSeconds = 30;
    [SerializeField] private int _departureCountdownSeconds = 10;

    [Header("Offboarding")]
    [Tooltip("Seconds before players are force-teleported out of the arrival train.")]
    [SerializeField] private float _forceOffboardSeconds = 5f;

    [Header("Scene References")]
    [Tooltip("Optional reference to the TrainController in this scene. Used to check if a train exists on spawn without FindObjectOfType.")]
    [SerializeField] private TrainController _sceneTrainController;

    [Networked, OnChangedRender(nameof(OnPhaseChanged))]
    public int WaitingPhase { get; set; }

    [Networked]
    public long CurrentTransitionTime { get; set; }

    [Networked, OnChangedRender(nameof(OnMatchTypeChanged))]
    public MatchType MatchType { get; set; }

    // Stores only the random 4-char code, not the full prefixed name. The prefix
    // is reconstructed locally on each client via NetworkManager.GetSessionNamePrefix()
    // so the _8 capacity is sufficient regardless of prefix length.
    [Networked, Capacity(8)]
    public NetworkString<_8> FfaSessionName { get; set; }

    public WaitingZonePhase Phase
    {
        get => (WaitingZonePhase)WaitingPhase;
        set => WaitingPhase = (int)value;
    }

    public DateTime TransitionTime
    {
        get => new DateTime(CurrentTransitionTime);
        set => CurrentTransitionTime = value.Ticks;
    }

    public int RequiredPlayers => MatchType switch
    {
        MatchType.Duo => _requiredPlayersDuo,
        MatchType.Squads => _requiredPlayersSquads,
        _ => _requiredPlayersFFA
    };

    public event Action<MatchType> MatchTypeChanged;

    private bool IsSinglePlayer => _networkManager?.Runner != null && (_networkManager.Runner.IsSinglePlayer || _networkManager.Runner.GameMode == Fusion.GameMode.Single);

    private INetworkManager _networkManager;
    private IGorillaService _gorillaService;
    private GameLoopService _gameLoopService;
    private ITeleportService _teleportService;
    private CancellationTokenSource _cts;

    private bool _sessionLocked;
    private bool _transitionTriggered;
    private bool _trackingArrivalTrain;
    private TrainWagon _trackedWagon; // the specific wagon the player is riding on
    private bool _departTeleportDone; // true once we've teleported player off departing arrival train
    private bool _insideWagonZone;   // true while local player's head is inside a TrainWagonZone
    private bool _boardedSubway;
    private bool _departureCountdownStarted;
    private bool _subwayRouteStarted;
    private bool _pendingTrainPosition; // true when we need to position on train but it wasn't ready yet
    private float _pendingTrainTimeout;  // fallback timer so we don't wait forever

    private SubwayDropVehicle _subscribedSubway;
    private bool _boardedOnce; // true after the single boarding teleport has run for this manager instance
    private Rigidbody _cachedPlayerRb;
    private bool _rigBodyWasKinematic;
    private bool _rigBodyWasUsingGravity;
    private bool _rigBodyFrozen; // tracks whether we applied the kinematic/gravity hold

    // True after we've called INetworkManager.SignalSpawnTeleportComplete once.
    // The signal can come from ParentRigToWagon or TeleportToWaitingZonePlatform
    // depending on the entry path; the flag prevents a second signal on later
    // teleports (which would no-op anyway, but keeps log noise down).
    private bool _spawnTeleportSignaled;

    private int _previousPlayerCount;

    public override void Spawned()
    {
        base.Spawned();

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _gameLoopService = ServiceLocator.Get<GameLoopService>();

        if (_gameLoopService?.WaitingZoneManager != null && _gameLoopService.WaitingZoneManager != this)
        {
            if (HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
            // Non-authority: the winning instance has already wired GameServices bridges
            // and registered with _gameLoopService. Falling through here would overwrite
            // those delegates and then, when THIS (loser) instance is despawned, null
            // them — leaving the winner with broken bridges. Return unconditionally.
            return;
        }

        _gameLoopService?.Register(this);
        _gameLoopService?.InvokeManagerChanged();

        _networkManager = ServiceLocator.Get<INetworkManager>();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        _teleportService = ServiceLocator.Get<ITeleportService>();
        GameServices.GorillaGameManagerExists = () => _gameLoopService?.WaitingZoneManager != null;
        GameServices.SuppressRespawnOnSceneLoad = () => _gameLoopService?.WaitingZoneManager != null;

        // Claim the post-spawn teleport handshake so JoinGameExAsync waits for our
        // first rig-positioning call (ParentRigToWagon or TeleportToWaitingZonePlatform)
        // to complete before releasing the rig hold.
        _networkManager?.ClaimSpawnTeleport();
        _spawnTeleportSignaled = false;
        SignalSpawnTeleportFallbackAsync(_cts.Token).Forget();

        if (HasStateAuthority)
        {
            ParseMatchTypeFromSession();
            CurrentTransitionTime = 0;
            Phase = WaitingZonePhase.Arriving;
        }
    }

    private void ParentRigToWagon(TrainController train, TrainWagon wagon, Vector3 localOffset)
    {
        if (_teleportService == null)
            return;

        // Snap all wagon transforms to the exact networked splineProgress before reading position.
        // Update() runs before Render(), so transforms may still be at the old location.
        train.SnapSplinePosition();

        // Use the wagon's transform (not train root) so offset is correct on curved splines.
        // Fall back to train root if wagon not found.
        var refTransform = wagon ? wagon.transform : train.transform;

        var worldPos = refTransform.TransformPoint(localOffset);
        var pendingRot = _networkManager?.PendingShuttleRotation ?? Quaternion.identity;
        var refYaw = refTransform.eulerAngles.y;
        var rigYaw = refYaw + pendingRot.eulerAngles.y;

        _teleportService.TeleportToPosition(worldPos, Quaternion.Euler(0f, rigYaw, 0f));

        _trackingArrivalTrain = true;
        _trackedWagon = wagon;
        _departTeleportDone = false;

        TrySignalSpawnTeleportComplete();
    }

    private void TeleportToWaitingZonePlatform(bool clearPendingOffset = false)
    {
        // DefaultSpawnPointStrategy uses AvatarXRSpawnpoint's static active-registry
        // (last-enabled wins), so the incoming WaitingZone scene's spawnpoint is picked
        // deterministically even while the outgoing Lobby scene is still loaded.
        _teleportService?.TeleportToSpawnPoint();
        _trackingArrivalTrain = false;

        TrySignalSpawnTeleportComplete();
    }

    private void TrySignalSpawnTeleportComplete()
    {
        if (_spawnTeleportSignaled) return;
        _spawnTeleportSignaled = true;
        _networkManager?.SignalSpawnTeleportComplete();
    }

    // Safety net: if neither ParentRigToWagon nor TeleportToWaitingZonePlatform fires
    // within a couple seconds (e.g. UpdateArriving skips straight to PreGameLobby
    // because no train is in the scene yet), signal anyway so JoinGameExAsync can
    // release the rig instead of waiting on the 12s safety timeout.
    private async UniTaskVoid SignalSpawnTeleportFallbackAsync(CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: ct);
            TrySignalSpawnTeleportComplete();
        }
        catch (OperationCanceledException) { /* expected on despawn */ }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (Phase)
        {
            case WaitingZonePhase.Arriving:
                UpdateArriving();
                break;
            case WaitingZonePhase.Offboarding:
                UpdateOffboarding();
                break;
            case WaitingZonePhase.PreGameLobby:
                UpdatePreGameLobby();
                break;
            case WaitingZonePhase.Countdown:
                UpdateCountdown();
                break;
            case WaitingZonePhase.BoardingDeparture:
                UpdateBoardingDeparture();
                break;
        }
    }

    private void LateUpdate()
    {
        // Deferred train positioning: train wasn't ready or was in Idle at Spawned(), retry now
        if (_pendingTrainPosition)
        {
            // Timeout fallback — don't wait forever
            if (Time.time >= _pendingTrainTimeout)
            {
                _pendingTrainPosition = false;
                TeleportToWaitingZonePlatform();
            }
            else
            {
                var train = GetTrain();
                if (train)
                {
                    var pendingOffset = Vector3.zero;
                    var hasOffset = pendingOffset.sqrMagnitude > _minOffsetSqrMagnitude;

                    // Train is in a rideable phase — position the player on it
                    if (hasOffset && (train.PhaseType == TrainPhaseType.Arriving
                            || train.PhaseType == TrainPhaseType.Docked
                            || train.PhaseType == TrainPhaseType.Offboarding))
                    {
                        _pendingTrainPosition = false;
                        var wagonIndex = _networkManager?.PendingShuttleIndex ?? 0;
                        var wagon = _gameLoopService?.GetWagonByIndex(wagonIndex);
                        ParentRigToWagon(train, wagon, pendingOffset);
                    }
                    // Train is Idle/OffScreen — keep waiting for authority to trigger arrival cycle
                    else if (train.PhaseType == TrainPhaseType.Idle || train.PhaseType == TrainPhaseType.OffScreen)
                    {
                        // Keep _pendingTrainPosition = true, wait for next frame
                    }
                    // Train is Departing — we missed it, go to platform
                    else if (train.PhaseType == TrainPhaseType.Departing)
                    {
                        _pendingTrainPosition = false;
                        TeleportToWaitingZonePlatform();
                    }
                }
                // If train still null, keep waiting (next frame)
            }
        }

        // Release the player from the vehicle when the train phase changes.
        // Player.Update() handles continuous positioning via vehicle delta — no
        // per-frame repositioning needed here.
        if (_trackingArrivalTrain)
        {
            var train = GetTrain();
            if (!train
                || train.PhaseType == TrainPhaseType.Departing
                || train.PhaseType == TrainPhaseType.OffScreen
                || train.PhaseType == TrainPhaseType.Idle)
            {
                _trackingArrivalTrain = false;
                _trackedWagon = null;
            }
            else if (train.PhaseType == TrainPhaseType.Offboarding)
            {
                // Doors open — player can walk off naturally
                _trackingArrivalTrain = false;
                _trackedWagon = null;
            }
        }

        // Force-teleport to platform when arrival train departs, but only if the player
        // is still inside a wagon zone. Players who walked off during offboarding are fine.
        if (!_departTeleportDone && !_boardedSubway && _insideWagonZone)
        {
            var arrivalTrain = GetTrain();
            if (arrivalTrain && arrivalTrain.PhaseType == TrainPhaseType.Departing)
            {
                _trackingArrivalTrain = false;
                _departTeleportDone = true;
                _insideWagonZone = false;
                TeleportToWaitingZonePlatform(clearPendingOffset: true);
            }
        }

        if (_boardedSubway)
        {
            var subway = _gameLoopService?.SubwayDropVehicle;
            if (subway == null || subway.IsRouteComplete)
            {
                _boardedSubway = false;
            }
        }

        // Every client (authority AND non-authority) must subscribe to the subway's RouteStarted
        // event so the post-snap re-teleport fires. FixedUpdateNetwork returns early on clients,
        // so we can't rely on UpdateBoardingDeparture to wire this up.
        var subwayForSub = _gameLoopService?.SubwayDropVehicle;
        if (subwayForSub != null && _subscribedSubway != subwayForSub)
            SubscribeToSubwayRouteStarted(subwayForSub);
    }

    private void UpdateArriving()
    {
        var train = GetTrain();

        if (train == null)
        {
            // No train in scene — skip directly to lobby
            Phase = WaitingZonePhase.PreGameLobby;
            return;
        }

        if (train.PhaseType == TrainPhaseType.Docked)
        {
            Phase = WaitingZonePhase.Offboarding;
        }
    }

    private void UpdateOffboarding()
    {
        // TrainController handles its own Offboarding phase (doors open, timer, close, depart).
        // WaitingZoneManager waits for the train to leave before moving to lobby.
        var train = GetTrain();
        if (train == null
            || train.PhaseType == TrainPhaseType.Departing
            || train.PhaseType == TrainPhaseType.OffScreen
            || train.PhaseType == TrainPhaseType.Idle)
        {
            Phase = WaitingZonePhase.PreGameLobby;
            _previousPlayerCount = _gorillaService?.GorillaCount ?? 0;
        }
    }

    private void UpdatePreGameLobby()
    {
        var playerCount = _gorillaService?.GorillaCount ?? 0;
        var required = IsSinglePlayer ? 1 : RequiredPlayers;
        var max = IsSinglePlayer ? 1 : _maxPlayers;

        // Check for new player arrivals — trigger visual arrival train if train is idle
        if (playerCount > _previousPlayerCount)
        {
            var newPlayers = playerCount - _previousPlayerCount;
            TryTriggerArrivalTrainForNewPlayers(newPlayers);
        }
        _previousPlayerCount = playerCount;

        // Lock session at max
        if (playerCount >= max && !_sessionLocked)
            LockSession("max players reached");

        // Start countdown when minimum met
        if (playerCount >= required && CurrentTransitionTime == 0)
        {
            TransitionTime = SyncedTime.Now.AddSeconds(IsSinglePlayer ? 3 : _lobbyCountdownSeconds);
            Phase = WaitingZonePhase.Countdown;
        }
    }

    private void TryTriggerArrivalTrainForNewPlayers(int newPlayers)
    {
        if (newPlayers <= 0) return;

        var train = GetTrain();
        if (train == null) return;

        // Only trigger new arrival if train is idle (no train visible)
        if (train.PhaseType == TrainPhaseType.Idle)
        {
            train.TriggerArrivalCycle();
        }
    }

    private void UpdateCountdown()
    {
        var playerCount = _gorillaService?.GorillaCount ?? 0;
        var required = IsSinglePlayer ? 1 : RequiredPlayers;

        // Cancel if players dropped below minimum
        if (playerCount < required)
        {
            CurrentTransitionTime = 0;
            Phase = WaitingZonePhase.PreGameLobby;
            return;
        }

        // Lock session when close to launch
        if (!_sessionLocked)
        {
            var secondsLeft = (TransitionTime - SyncedTime.Now).TotalSeconds;
            if (secondsLeft <= 5 || playerCount >= _maxPlayers)
                LockSession("countdown imminent");
        }

        if (SyncedTime.Now >= TransitionTime)
        {
            if (!_sessionLocked)
                LockSession("countdown complete");

            // Move to boarding — players will be teleported into the subway
            Phase = WaitingZonePhase.BoardingDeparture;
            _departureCountdownStarted = false;
        }
    }

    private void UpdateBoardingDeparture()
    {
        var subway = _gameLoopService?.SubwayDropVehicle;
        if (subway == null)
        {
            // No subway in scene — transition directly
            Phase = WaitingZonePhase.Transitioning;
            BeginTransitionToFreeForAll();
            return;
        }

        // NOTE: SubscribeToSubwayRouteStarted used to be called here, but FixedUpdateNetwork
        // returns early on non-authority, so clients never subscribed — and they were the ones
        // most likely to read pre-snap spawn positions. The subscription is now driven from
        // LateUpdate (runs on every client) so both authority and clients re-teleport after
        // the subway snaps to spline-knot-0.

        // Single-tick boarding kickoff:
        //   1) StartRoute() on authority — snaps subway to spline-knot-0 and fires RouteStarted
        //      locally; Rpc_StartRoute propagates the snap (and the RouteStarted fire) to clients.
        //   2) RPC_BoardSubway() — broadcasts boarding state only (the teleport itself lives in
        //      RepositionLocalPlayerInSubway on the RouteStarted event, guarded by _boardedOnce).
        if (!_subwayRouteStarted)
        {
            _subwayRouteStarted = true;
            subway.StartRoute();
            // Fallback timeout in case StationEnd key is missing or subway gets stuck
            TransitionTime = SyncedTime.Now.AddSeconds(_departureCountdownSeconds);
            RPC_BoardSubway();
        }

        // Transition when subway reaches StationEnd (or fallback timeout)
        var reachedEnd = subway.HasReachedStationEnd;
        var timedOut = SyncedTime.Now >= TransitionTime;
        if (reachedEnd || timedOut)
        {
            Phase = WaitingZonePhase.Transitioning;
            BeginTransitionToFreeForAll();
        }
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
        // Reset so a re-spawned subway (e.g. after scene-swap cancel) gets re-subscribed.
        _subwayRouteStarted = false;
    }

    private void HandleRouteStarted()
    {
        var subway = _gameLoopService?.SubwayDropVehicle;
        if (subway == null) return;
        RepositionLocalPlayerInSubway(subway);
    }

    private void RepositionLocalPlayerInSubway(SubwayDropVehicle subway)
    {
        if (_teleportService == null) return;
        if (_boardedOnce) return; // single-shot: RouteStarted + late-joiner paths converge here

        var spawnPos = subway.spawnPoints[0].position;
        var spawnRotFull = subway.spawnPoints[0].rotation;

        // Strip to yaw-only so the rig faces forward in the cabin instead of picking up the
        // 90° Y bias from the prefab's intermediate SpawnPoints container.
        Quaternion spawnRot = Quaternion.Euler(0f, spawnRotFull.eulerAngles.y, 0f);


        _teleportService.TeleportToPosition(spawnPos, spawnRot);

        _boardedSubway = true;
        _trackingArrivalTrain = false;
        _boardedOnce = true;
    }

    /// <summary>
    /// Broadcast boarding state to all clients. The teleport itself is handled by
    /// RepositionLocalPlayerInSubway on the RouteStarted event — keeping teleport and
    /// state in a single deterministic path avoids the compounding-offset bug that
    /// happened when both RPC_BoardSubway and HandleRouteStarted teleported.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BoardSubway()
    {
        var subway = _gameLoopService?.SubwayDropVehicle;
        if (subway == null) return;

        // If the RouteStarted event hasn't fired yet on this client (RPC arrived before
        // the snap propagated), force the single-shot teleport here. _boardedOnce keeps
        // this idempotent with a later HandleRouteStarted from Rpc_StartRoute.
        if (!_boardedOnce && subway.IsRouteStarted)
            RepositionLocalPlayerInSubway(subway);

        _boardedSubway = true;
        _trackingArrivalTrain = false;
    }

    /// <summary>
    /// Authority-only: resolves the session name, sends an RPC to non-authority
    /// clients, then delays before calling JoinGameEx itself.
    ///
    /// The RPC carries the session name so non-authority clients don't depend on
    /// the networked ffaSessionName property (which may not sync before the Runner
    /// shuts down). The authority MUST delay its own JoinGameEx so the Runner
    /// stays alive long enough for the RPC to be flushed to the relay server.
    /// </summary>
    private void BeginTransitionToFreeForAll()
    {
        // FfaSessionName carries only the random code. The full session name
        // (prefix + code) is reconstructed locally so all clients of the same
        // build land in the same FFA room.
        var code = FfaSessionName.ToString();
        if (string.IsNullOrEmpty(code))
        {
            code = _networkManager is NetworkManager networkManager
                ? networkManager.GenerateSessionCode()
                : Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

            GameLogger.Warning($"[WaitingZoneManager] ffaSessionName was empty — fallback code: {code}");
        }

        string fullSession = SessionNaming.BuildFullSessionName(code);

        // Send RPC so non-authority clients get the session code
        RPC_TransitionToFreeForAll(code);

        // Authority joins after a short delay so the RPC flushes first.
        // Capture networkManager ref — object may despawn during the delay.
        _transitionTriggered = true;
        var capturedNm = _networkManager;
        if (capturedNm != null) capturedNm.PendingSessionName = fullSession;
        JoinAfterDelayAsync(capturedNm, fullSession, _authorityJoinDelayMs, "Authority");
    }


    /// <summary>
    /// RPC received by non-authority clients only. The authority skips this
    /// because it handles its own transition in BeginTransitionToFreeForAll.
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TransitionToFreeForAll(NetworkString<_8> session)
    {
        if (HasStateAuthority)
            return;

        if (_transitionTriggered)
            return;

        _transitionTriggered = true;

        var code = session.ToString();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("[WaitingZoneManager] RPC: session code is empty");
            return;
        }

        string fullSession = SessionNaming.BuildFullSessionName(code);

        // Capture refs before despawn kills them
        var capturedNm = _networkManager;
        if (capturedNm != null) capturedNm.PendingSessionName = fullSession;
        JoinAfterDelayAsync(capturedNm, fullSession, _clientJoinDelayMs, "Client");
    }

    /// <summary>
    /// Fire-and-forget join that does NOT depend on _cts or any instance fields.
    /// All state is captured in parameters so the method survives WaitingZoneManager
    /// despawn (which happens when the authority's Runner shuts down and the
    /// non-authority gets disconnected from the WaitingZone session).
    ///
    /// Intentionally omits CancellationToken: this MUST survive despawn so the
    /// client can transition to FreeForAll even after the WaitingZone session ends.
    /// The null-conditional on networkManager is the safety valve.
    /// </summary>
    private static async UniTaskVoid JoinAfterDelayAsync(INetworkManager networkManager, string session, int delayMs, string tag)
    {
        try
        {
            await UniTask.Delay(delayMs);
            networkManager?.JoinGameEx(NetworkGameModeIds.FreeForAll, session, true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WaitingZoneManager] {tag} JoinAfterDelay failed: {e}");
        }
    }

    private void ParseMatchTypeFromSession()
    {
        var room = _networkManager?.CurrentRoom;
        if (room != null)
            MatchType = (MatchType)room.MatchType;
        else
            MatchType = MatchType.FreeForAll;

        MatchTypeChanged?.Invoke(MatchType);
    }

    private void LockSession(string reason)
    {
        _sessionLocked = true;
        _networkManager?.CurrentRoom?.SetIsVisible(false);
        if (_networkManager?.CurrentRoom != null)
            _networkManager.CurrentRoom.IsRunning = true;

        // Pre-generate FFA session code (not the full name) so all clients can read it
        // before transition. The prefix is applied locally via SessionNaming.BuildFullSessionName.
        if (HasStateAuthority && string.IsNullOrEmpty(FfaSessionName.ToString()))
        {
            var generatedCode = _networkManager is Teabag.Networking.NetworkManager nm
                ? nm.GenerateSessionCode()
                : Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            FfaSessionName = generatedCode;
        }
    }

    public void OnPhaseChanged()
    {
        switch (Phase)
        {
            case WaitingZonePhase.Offboarding:
                // Doors are open — players walk off naturally.
                // Force-teleport only happens in Update() when train enters Departing phase.
                break;

            case WaitingZonePhase.Transitioning:
                // Primary transition is via RPC_TransitionToFreeForAll.
                // This fallback catches the rare case where the networked phase
                // arrives but the RPC was somehow lost.
                if (!HasStateAuthority && !_transitionTriggered)
                {
                    var code = FfaSessionName.ToString();
                    if (!string.IsNullOrEmpty(code))
                    {
                        // FfaSessionName carries only the random code; reconstruct the
                        // full prefixed session name so every client joins the same room
                        // as the authority, which uses SessionNaming.BuildFullSessionName(code).
                        string fullSession = SessionNaming.BuildFullSessionName(code);
                        _transitionTriggered = true;
                        var capturedNm = _networkManager;
                        if (capturedNm != null) capturedNm.PendingSessionName = fullSession;
                        JoinAfterDelayAsync(capturedNm, fullSession, _clientJoinDelayMs, "Client-fallback");
                    }
                    else
                    {
                        GameLogger.Warning("[WaitingZoneManager] OnPhaseChanged(Transitioning): ffaSessionName empty, waiting for RPC");
                    }
                }
                break;
        }
    }

    public void OnMatchTypeChanged()
    {
        MatchTypeChanged?.Invoke(MatchType);
    }

    public void OnLocalPlayerBoarded(int wagonIndex, int matchType)
    {
        _insideWagonZone = true;
    }

    public void OnLocalPlayerUnboarded(int wagonIndex)
    {
        _insideWagonZone = false;
    }

    private TrainController GetTrain()
    {
        return _gameLoopService?.TrainController;
    }


    [ContextMenu("Debug/Force Launch Subway")]
    public void DebugForceLaunchSubway()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[WaitingZoneManager] Debug launch requires Play mode");
            return;
        }

        if (HasStateAuthority)
        {
            DebugForceLaunchSubway_Internal();
        }
        else if (Object != null && Runner != null)
        {
            Rpc_DebugForceLaunchSubway();
            // GameLogger.Info("[WaitingZoneManager] DEBUG: Sent RPC to authority to force launch subway");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_DebugForceLaunchSubway()
    {
        // GameLogger.Info("[WaitingZoneManager] DEBUG: Received RPC — force launching subway");
        DebugForceLaunchSubway_Internal();
    }

    private void DebugForceLaunchSubway_Internal()
    {
        if (!_sessionLocked)
            LockSession("debug force launch");

        // Jump straight to BoardingDeparture — this boards players into the
        // SubwayDropVehicle, starts its route, and transitions to FreeForAll.
        _departureCountdownStarted = false;
        _subwayRouteStarted = false;
        Phase = WaitingZonePhase.BoardingDeparture;
        // GameLogger.Info("[WaitingZoneManager] DEBUG: Forced BoardingDeparture phase");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        UnsubscribeFromSubwayRouteStarted();
        _boardedOnce = false;
        _cachedPlayerRb = null;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _gameLoopService?.Unregister(this);
        GameServices.GorillaGameManagerExists = null;
        GameServices.SuppressRespawnOnSceneLoad = null;
    }
}
