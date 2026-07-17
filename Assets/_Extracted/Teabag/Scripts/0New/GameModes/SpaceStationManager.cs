using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.GameMode;
using Teabag.Networking;
using UnityEngine;

/// <summary>
/// Discovers TrainController and TrainWagon scene objects, routes local player boarding events.
/// When the train departs, dispatches players to WaitingZone sessions based on their wagon's match type.
/// </summary>
public class SpaceStationManager : NetworkBehaviour
{
    private int _localBoardedWagonIndex = -1;
    private INetworkManager _networkManager;
    private IGorillaService _gorillaService;
    private GameLoopService _gameLoopService;
    private ITeleportService _teleportService;
    private CancellationTokenSource _joinCts;
    private CancellationTokenSource _spawnTeleportCts;
    private bool _localDepartureStarted;
    private MatchType _pendingMatchType;
    private int _pendingWagonIndex;

    private const float SpawnTeleportRigTimeoutSeconds = 10f;
    private const float PostTeleportSettleSeconds = 0.15f;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    /// <summary>
    /// Transform of the wagon the local player is currently boarded on, or null if not boarded.
    /// Used by MovingPlatformLocomotion as the "head-inside-zone" platform fallback so that
    /// players who are inside the wagon's TrainWagonZone (but not necessarily standing on a
    /// floor collider) still inherit the wagon's translation and yaw.
    /// </summary>
    public Transform LocalWagonTransform
    {
        get
        {
            if (_localBoardedWagonIndex < 0)
                return null;
            var wagon = _gameLoopService?.GetWagonByIndex(_localBoardedWagonIndex);
            return wagon != null ? wagon.transform : null;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        _gameLoopService = ServiceLocator.Get<GameLoopService>();

        if (_gameLoopService?.SpaceStationManager != null && _gameLoopService.SpaceStationManager != this)
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

        GameServices.GorillaGameManagerExists = () => _gameLoopService?.SpaceStationManager != null;
        GameServices.SuppressRespawnOnSceneLoad = () => _gameLoopService?.SpaceStationManager != null;

        _networkManager = ServiceLocator.Get<INetworkManager>();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        _teleportService = ServiceLocator.Get<ITeleportService>();

        // Claim the post-spawn teleport handshake so JoinGameExAsync waits for our
        // teleport + settle to finish before releasing the rig hold. Without this
        // claim, JoinGameExAsync skips the wait (which is correct for empty-gameMode
        // joins but would let the rig get released mid-fall here).
        _networkManager?.ClaimSpawnTeleport();

        _spawnTeleportCts?.Cancel();
        _spawnTeleportCts?.Dispose();
        _spawnTeleportCts = new CancellationTokenSource();
        TeleportToStationSpawnAsync(_spawnTeleportCts.Token);
    }

    /// <summary>
    /// Async: waits for the local rig to exist, then teleports it to the registered
    /// station spawnpoint. SpaceStationManager.Spawned() fires inside GorillaRunner.OnSceneLoadDone
    /// right after the local Gorilla is spawned but before Fusion has ticked — the rig's
    /// HardwareRig may not be registered with IRigInfoService yet. AvatarXRSpawnpointRegistry
    /// scans via FindObjectsByType (including inactive), so no separate spawnpoint wait is needed.
    ///
    /// Freezing the rig (kinematic, no gravity, zero velocity) is owned by NetworkManager for
    /// the duration of the join — it polls TryHoldLocalRig every frame so the rig is held the
    /// instant Fusion spawns it. This method just teleports + settles, then signals
    /// INetworkManager.SignalSpawnTeleportComplete so the rig is released into its final pose.
    /// </summary>
    private async void TeleportToStationSpawnAsync(CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested) return;

            DateTime rigDeadline = DateTime.UtcNow.AddSeconds(SpawnTeleportRigTimeoutSeconds);
            while (LocalHardwareRig == null && DateTime.UtcNow < rigDeadline && !ct.IsCancellationRequested)
            {
                await UniTask.Yield();
            }

            if (ct.IsCancellationRequested) return;

            if (LocalHardwareRig == null)
            {
                GameLogger.Error($"[SpaceStationManager] Rig not ready after {SpawnTeleportRigTimeoutSeconds}s — station teleport skipped; rig hold owned by NetworkManager will be released by JoinGameExAsync timeout");
                return;
            }

            // Use the rig-root teleport so the root Transform lands exactly on the spawn
            // point (no headset-offset compensation). This avoids the player appearing
            // offset by whatever lateral headset drift accumulated during loading.
            if (TryResolveStationSpawnPoint(out Vector3 spawnPos, out Quaternion spawnRot))
            {
                LocalHardwareRig.Teleport(spawnPos, spawnRot);
                GameLogger.Info($"[SpaceStationManager] Teleported rig root to station spawn {spawnPos}");
            }
            else
            {
                GameLogger.Error("[SpaceStationManager] No AvatarXRSpawnpoint found in any loaded scene — station teleport skipped; rig will be released at origin by NetworkManager");
                return;
            }

            // Hold the rig a few physics ticks so collisions resolve before gravity resumes.
            // The rig stays kinematic for this window via the NetworkManager-owned hold.
            await UniTask.Delay(TimeSpan.FromSeconds(PostTeleportSettleSeconds), cancellationToken: ct);
        }
        catch (OperationCanceledException) { /* expected on despawn */ }
        catch (Exception e)
        {
            GameLogger.Error($"[SpaceStationManager] TeleportToStationSpawnAsync failed: {e}");
        }
        finally
        {
            // Always signal — success, failure, or cancellation. NetworkManager's
            // JoinGameExAsync is awaiting this handshake to release the rig hold and
            // a missing signal would block the join until the 12s safety timeout.
            _networkManager?.SignalSpawnTeleportComplete();
        }
    }

    private static bool TryResolveStationSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        var all = AvatarXRSpawnpointRegistry.Active;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var sp = all[i];
            if (sp == null) continue;
            // Guard against the scene-unload race: destroyed scene objects can linger
            // in the registry for a frame, and during cross-fades a WaitingZone
            // spawnpoint from the outgoing scene could be picked. Mirrors the filter
            // used in DefaultSpawnPointStrategy.
            var scene = sp.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded) continue;
            position = sp.transform.position;
            rotation = sp.transform.rotation;
            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private void Update()
    {
        // Nothing to do if not boarded and no departure in progress
        if (_localBoardedWagonIndex < 0 && !_localDepartureStarted)
            return;

        var train = GetTrain();
        if (!train)
            return;

        // Step 1: Capture pending data the frame the train starts departing
        if (!_localDepartureStarted && _localBoardedWagonIndex >= 0 && train.PhaseType == TrainPhaseType.Departing)
        {
            CaptureDepartureData(train);
            _localDepartureStarted = true;
        }

        // Step 2: Dispatch when train crosses the LoadGameMode spline marker (or OffScreen as fallback)
        if (_localDepartureStarted && (train.LoadPointReached || train.PhaseType == TrainPhaseType.OffScreen))
        {
            _localDepartureStarted = false;
            _localBoardedWagonIndex = -1;
            DispatchToWaitingZone();
        }
    }

    private void CaptureDepartureData(TrainController train)
    {
        var wagon = _gameLoopService?.GetWagonByIndex(_localBoardedWagonIndex);
        if (!wagon)
            return;

        _pendingMatchType = wagon.WagonMatchType;
        _pendingWagonIndex = _localBoardedWagonIndex;

        _networkManager.PendingMatchType = (int)_pendingMatchType;
        _networkManager.PendingShuttleIndex = _pendingWagonIndex;

        // Save player position relative to their wagon for arrival positioning in WaitingZone.
        // Using wagon.transform (not train root) so the offset stays correct on curved splines
        // where each wagon has a different orientation.
        var gorillaRig = LocalHardwareRig;

        if (gorillaRig?.LocomotionController?.PlayerRigidbody != null)
        {
            Vector3 rigRootPos = gorillaRig.GetRigRootPosition();
            float rigYaw = gorillaRig.GetRigRootRotation().eulerAngles.y;

            _networkManager.PendingShuttleLocalOffset =
                wagon.transform.InverseTransformPoint(rigRootPos);

            float wagonYaw = wagon.transform.eulerAngles.y;
            _networkManager.PendingShuttleRotation = Quaternion.Euler(0f, rigYaw - wagonYaw, 0f);
        }

        // GameLogger.Info($"[SpaceStation] Departure captured — wagon {_pendingWagonIndex}, matchType={_pendingMatchType}");
    }

    private TrainController GetTrain()
    {
        return _gameLoopService?.TrainController;
    }

    private void DispatchToWaitingZone()
    {
        // GameLogger.Info($"[SpaceStation] Dispatching to WaitingZone (matchType={_pendingMatchType})");

        var isFirstPassenger = IsLowestPlayerRef();
        if (isFirstPassenger)
        {
            // GameLogger.Info($"[SpaceStation] First passenger — creating WaitingZone session");
            _networkManager.JoinGameEx(NetworkGameModeIds.WaitingZone, "", true);
        }
        else
        {
            JoinAfterScoutAsync();
        }
    }

    private bool IsLowestPlayerRef()
    {
        if (!Runner)
            return true;

        int localId = Runner.LocalPlayer.PlayerId;
        foreach (var player in Runner.ActivePlayers)
        {
            if (player.PlayerId < localId)
                return false;
        }
        return true;
    }

    private async void JoinAfterScoutAsync()
    {
        _joinCts?.Cancel();
        _joinCts?.Dispose();
        _joinCts = new CancellationTokenSource();
        try
        {
            _networkManager.NetworkState = State.JOINING;

            const int scoutWaitMilliseconds = 3000;
            GameLogger.Info($"[SpaceStation] Waiting {scoutWaitMilliseconds}ms for scout, then FillRoom into WaitingZone (matchType={_pendingMatchType})");
            await UniTask.Delay(scoutWaitMilliseconds, cancellationToken: _joinCts.Token);
            _networkManager.PendingMatchType = (int)_pendingMatchType;
            _networkManager.PendingShuttleIndex = _pendingWagonIndex;
            _networkManager.JoinGameEx(NetworkGameModeIds.WaitingZone, "", true);
        }
        catch (OperationCanceledException)
        {
            GameLogger.Info("[SpaceStation] JoinAfterScoutAsync cancelled");
        }
    }

    /// <summary>
    /// Called by TrainWagonZone when local player enters a wagon.
    /// Routes the boarding RPC to the correct TrainWagon.
    /// </summary>
    public void OnLocalPlayerBoarded(int wagonIndex, int matchType)
    {
        if (_localBoardedWagonIndex >= 0)
        {
            GameLogger.Info($"[SpaceStation] OnLocalPlayerBoarded ignored — already in wagon {_localBoardedWagonIndex}");
            return;
        }

        _localBoardedWagonIndex = wagonIndex;
        var wagon = _gameLoopService?.GetWagonByIndex(wagonIndex);
        if (wagon)
        {
            GameLogger.Info($"[SpaceStation] OnLocalPlayerBoarded: wagon={wagonIndex} matchType={matchType} — sending RPC");
            wagon.RPC_PlayerBoarded(Runner.LocalPlayer);
        }
    }

    /// <summary>
    /// Called by TrainWagonZone when local player exits a wagon.
    /// </summary>
    public void OnLocalPlayerUnboarded(int wagonIndex)
    {
        if (_localBoardedWagonIndex != wagonIndex) return;
        _localBoardedWagonIndex = -1;
        var wagon = _gameLoopService?.GetWagonByIndex(wagonIndex);
        if (wagon)
            wagon.RPC_PlayerUnboarded(Runner.LocalPlayer);
    }


    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _joinCts?.Cancel();
        _joinCts?.Dispose();
        _joinCts = null;

        _spawnTeleportCts?.Cancel();
        _spawnTeleportCts?.Dispose();
        _spawnTeleportCts = null;

        base.Despawned(runner, hasState);

        _gameLoopService?.Unregister(this);
        GameServices.GorillaGameManagerExists = null;
        GameServices.SuppressRespawnOnSceneLoad = null;
    }
}
