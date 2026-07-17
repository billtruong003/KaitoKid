using System;
using System.Collections;
using Eflatun.SceneReference;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
// things may break using this, however this is just a test!
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.GameMode;
using Teabag.Networking.Extensions;
using Teabag.Player;
using Teabag.Player.Rig;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using IAudioService = Teabag.Core.IAudioService;
using Random = UnityEngine.Random;


namespace Teabag.Networking
{
    public class NetworkManager : MonoBehaviour, INetworkManager
    {
        private const float UPDATE_PLAYER_PROPERTIES_COOLDOWN = 2f;

        // ── Serialized fields ──
        public NetworkRunner runnerPrefab;
        public NetworkObject defaultGorillaPrefab;
        public Material defaultSkybox;
        public NetworkObject userPropertyManagerPrefab;
        public GameModeDb gameModeDb;
        [SerializeField] internal NetworkConfig _networkConfig;
        [SerializeField] private ScriptableStringEvent _fadeScreenMessageEvent;

        [Header("Scene References")]
        [SerializeField] private SceneReference[] _ignoredScenesForUnload;

        // ── Private timing fields ──
        private float _lastUpdatePlayerPropertiesTime;
        private float _lastSendTelemetryTime;

        // ── INetworkManager implementation ──

        bool INetworkManager.HasRunner => Runner;

        public NetworkRunner Runner { get; set; }

        public event Action<StartGameResult> OnJoinRoom;

        public bool IsLaggyConnection => Runner != null && Runner.GetNetworkStats().currentRTT > 0.5;

        public NetworkObject GorillaPrefab
        {
            get
            {
                if (gameModeDb && gameModeDb.TryGet(CurrentGameMode, out var gm) && gm.GorillaPrefab)
                    return gm.GorillaPrefab;
                return defaultGorillaPrefab;
            }
        }

        public GorillaRoomInfo CurrentRoom
        {
            get
            {
                if (!Runner || Runner.SessionInfo == null || Runner.IsSinglePlayer || Runner.GameMode == Fusion.GameMode.Single)
                    return null;
                return new GorillaRoomInfo(Runner.SessionInfo);
            }
        }

        public GorillaRoomInfo CurrentRoomSafe
            => CurrentRoom ?? new GorillaRoomInfo();

        public string CurrentGameMode
        {
            get
            {
                var roomGameMode = CurrentRoomSafe.GameMode;
                if (!string.IsNullOrEmpty(roomGameMode))
                    return roomGameMode;

                // In single-player mode CurrentRoom is null, fall back to last join request
                return LastJoinRequest.gameMode ?? string.Empty;
            }
        }
        public bool IsMaster
        {
            get
            {
                if (Runner && Runner.GameMode == Fusion.GameMode.Shared)
                    return Runner.IsSharedModeMasterClient;
                return true;
            }
        }

        public bool InRoom => Runner?.IsRunning ?? false;

        public bool InNetworkedRoom
        {
            get
            {
                if (Runner && Runner.SessionInfo != null)
                    return Runner.GameMode == Fusion.GameMode.Shared;
                return false;
            }
        }

        public TimeSpan TimeSpentInRoom => (DateTime.UtcNow - TimeJoinedRoom);
        public bool IsConnected => Runner != null;

        public int PlayerCount { get; set; }
        public int PendingMatchType { get; set; }
        public int PendingShuttleIndex { get; set; }

        /// <summary>Player's local-space offset from the shuttle transform at departure time. Used to restore relative position after transition.</summary>
        public Vector3 PendingShuttleLocalOffset { get; set; }
        /// <summary>Source shuttle rotation at departure time. Used to compute rotation delta for player orientation.</summary>
        public Quaternion PendingShuttleRotation { get; set; }
        public string PendingSessionName { get; set; }
        public State NetworkState { get; set; } = State.NONE;
        public StartGameResult LastResult { get; set; }
        public DateTime TimeJoinedRoom { get; set; }

        public (string gameMode, string sessionName, bool online)
            LastJoinRequest
        { get; private set; }

        public bool IsLoading
        {
            get
            {
                if (NetworkState == State.JOINING || NetworkState == State.LEAVING)
                    return true;
                return Runner && Runner.IsSceneManagerBusy;
            }
        }

        public NetworkObject UserPropertyManagerPrefab => userPropertyManagerPrefab;
        public Material DefaultSkybox => defaultSkybox;
        public GameModeDb GameModeDatabase => gameModeDb;

        private bool _joinInProgress;
        private int _currentRetryAttempt;
        private bool _isRetrying;

#if UNITY_EDITOR
        internal bool SimulateTimeout;
        internal bool SimulateFailure;
        internal int SimulateFailCount;
#endif

        public bool JoinInProgress => _joinInProgress;
        public bool IsRetrying => _isRetrying;
        public int CurrentRetryAttempt => _currentRetryAttempt;

        private int MaxJoinRetries =>
            _networkConfig ? _networkConfig.MaxRetries : 4;

        private TimeSpan ConnectionTimeout =>
            TimeSpan.FromSeconds(_networkConfig ? _networkConfig.ConnectionTimeoutSeconds : 30f);


        private IGorillaService _gorillaService = null;


        /// <summary>
        /// The game mode to return to when leaving a match (typically MainLobby).
        /// Assign via inspector — used by LeaveGameAsync and offline recovery.
        /// </summary>
        [SerializeField] private GameModeSo _defaultGameMode;
        public string DefaultGameModeId => _defaultGameMode != null ? _defaultGameMode.Id : string.Empty;

        [SerializeField]
        private SceneReference _mainSceneReference;


        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        // Rig freeze state held across JoinGameExAsync so the local VR rig does not
        // free-fall from its current pose while scenes are (un)loading. Mirrors the
        // FadeOut/FadeIn pairing: hold right after fade-out, release in the outer
        // finally after fade-in (or on any failure path).
        private Rigidbody _joinHeldRigidbody;
        private bool _joinRigPrevKinematic;
        private bool _joinRigPrevUseGravity;

        // Polls TryHoldLocalRig every frame until the rig appears, so a join that
        // starts BEFORE Fusion has spawned the local rig (the bootstrap path) still
        // catches it on its very first frame of life — closing the gravity window
        // that otherwise lets the rig free-fall before SpaceStationManager teleports.
        private CancellationTokenSource _joinHoldPollCts;

        // Signalled by the active game-mode manager (SpaceStationManager etc.) when
        // its post-spawn teleport + settle window is complete. JoinGameExAsync awaits
        // this on the success path before returning, so the rig is released into its
        // final pose — and the bootstrap loader's reveal-fade only runs after the rig
        // is positioned. Only awaited when SpawnTeleportClaimed is true so empty-gameMode
        // / offline preliminary joins don't waste time on the safety timeout.
        // Recreated per join.
        public UniTaskCompletionSource SpawnTeleportTcs { get; private set; }
        public bool SpawnTeleportClaimed { get; private set; }

        // Bound: SpaceStationManager rig-wait (10s) + settle (0.15s) + slack. Long
        // enough that a slow rig spawn in editor still completes under the timeout;
        // only fires when a manager has claimed the handshake but never signalled.
        private const float SpawnTeleportWaitTimeoutSeconds = 12f;

        public void ClaimSpawnTeleport()
        {
            SpawnTeleportClaimed = true;
        }

        public void SignalSpawnTeleportComplete()
        {
            SpawnTeleportTcs?.TrySetResult();
        }

        private void TryHoldLocalRig()
        {
            if (_joinHeldRigidbody != null) return;
            var rb = LocalHardwareRig?.LocomotionController?.PlayerRigidbody;
            if (rb == null) return;

            _joinHeldRigidbody = rb;
            _joinRigPrevKinematic = rb.isKinematic;
            _joinRigPrevUseGravity = rb.useGravity;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        private void ReleaseLocalRig()
        {
            if (_joinHeldRigidbody == null) return;

            _joinHeldRigidbody.isKinematic = _joinRigPrevKinematic;
            _joinHeldRigidbody.useGravity = _joinRigPrevUseGravity;
            _joinHeldRigidbody.linearVelocity = Vector3.zero;
            _joinHeldRigidbody.angularVelocity = Vector3.zero;
            _joinHeldRigidbody = null;
        }

        private async UniTaskVoid PollLocalRigHoldAsync(CancellationToken ct)
        {
            // Idempotent: TryHoldLocalRig early-returns once it has grabbed the rig,
            // so this loop becomes a per-frame no-op for the rest of the join.
            while (!ct.IsCancellationRequested && _joinHeldRigidbody == null)
            {
                TryHoldLocalRig();
                if (_joinHeldRigidbody != null) return;
                await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow();
            }
        }


        private void Start()
        {
#if UNITY_EDITOR
            if (!GetComponent<NetworkManagerDebugGUI>())
                gameObject.AddComponent<NetworkManagerDebugGUI>();
#endif

            if (gameModeDb)
                gameModeDb.Initialize();

            // Wire GameServices bridges for named assemblies
            Teabag.Core.GameServices.GetRunner = () => Runner;
            Teabag.Core.GameServices.GetCurrentGameMode = () => CurrentGameMode;
            Teabag.Core.GameServices.IsInRoom = () => InRoom;
            Teabag.Core.GameServices.IsInNetworkedRoom = () => InNetworkedRoom;
            Teabag.Core.GameServices.IsLoading = () => IsLoading;
            Teabag.Core.GameServices.JoinGameWithCode = (gm, sn) => JoinGameEx(gm, sn, true);

            // If XRKit bootstrap is running, skip self-initialization.
            // FusionNetworkService will handle Photon config + room join.
            if (BootstrapSettingsAsset.InstanceAsset != null
                && BootstrapSettingsAsset.InstanceAsset.Settings.ExecuteBootstrapSequence)
            {
                return;
            }

            // set our values
            PhotonAppSettings.Global.AppSettings.AppVersion = Application.version;
            const string photonRegion = "us";
            PhotonAppSettings.Global.AppSettings.FixedRegion = photonRegion;

            // simple one-liner
            PermissionInterface.AttemptRequestPermission(Permission.Microphone);

            // load player count and set onLogin callback
            _ = GetPlayerCountAsync();
            PlayerData.OnLogin += OnLogin; // this will be our que to join Bootcamp or a default room

            // join default room (blimp)
            JoinGameEx(string.Empty, string.Empty, false);
        }

        public async UniTask<int> GetPlayerCountAsync()
        {
            if (GameServices.GetPlayerCountAsync == null)
                return (PlayerCount = 0);

            int result = await SafeNetworkOperations.ExecuteSafe(GameServices.GetPlayerCountAsync);
            PlayerCount = result;
            return result;
        }

        private void OnLogin()
        {
            var persistence = ServiceLocator.Get<IDataPersistenceService>();
            if ((persistence?.LoadData<int>(SettingKeyNames.CompletedTutorial, 0) ?? 0) < 1)
            {
                _ = JoinGameExAsync(NetworkGameModeIds.Bootcamp, string.Empty, false);
                persistence?.TrySaveData(SettingKeyNames.CompletedTutorial, 1);
            }
        }

        public void JoinGame(GameModeSo gameMode, string sessionName = "")
        {
            if (!gameMode)
            {
                GameLogger.Error($"{nameof(gameMode)}");
                return;
            }
            _ = JoinGameAsync(gameMode.Id, sessionName);
        }

        public void JoinGame(string gameMode, string sessionName = "")
            => _ = JoinGameAsync(gameMode, sessionName);

        public async UniTask<StartGameResult> JoinGameAsync(string gameMode, string sessionName = "")
            => await JoinGameExAsync(gameMode, sessionName, true);

        public void JoinGameEx(string gameMode, string sessionName = "", bool online = true)
            => _ = JoinGameExAsync(gameMode, sessionName, online);

        public void JoinGameEx(GameModeSo gameMode, string sessionName = "", bool online = true)
        {
            if (!gameMode)
            {
                GameLogger.Error($"{nameof(gameMode)}");
                return;
            }
            JoinGameEx(gameMode.Id, sessionName, online);
        }

        public async UniTask<StartGameResult> JoinGameExAsync(GameModeSo gameMode, string sessionName = "", bool online = true, bool loadScene = true, bool fade = true)
        {
            if (!gameMode)
            {
                GameLogger.Error($"{nameof(gameMode)}");
                return null;
            }

            return await JoinGameExAsync(gameMode != null ? gameMode.Id : string.Empty, sessionName, online, loadScene, fade);
        }

        // BUG: client always creates a new room with no one in it
        //      could be 'enableClientSessionCreation' ?
        //      check: https://discord.com/channels/1163138896872349767/1383319471724298341/1383319471724298341
        //public async UniTask<StartGameResult> JoinGameExAsync(string gameMode, string sessionName = "", bool online = true, bool enableClientSessionCreation = true)
        public async UniTask<StartGameResult> JoinGameExAsync(string gameMode, string sessionName = "", bool online = true, bool loadScene = true, bool fade = true)
        {
#if OFFLINE_MODE
            // OFFLINE_MODE define is set by the editor toggle — force all joins to single-player.
            online = false;
#endif

            if (_joinInProgress is true)
            {
                GameLogger.Warning("JoinGameExAsync: join already in progress, ignoring");
                return null;
            }

            _joinInProgress = true;
            _currentRetryAttempt = 0;
            _isRetrying = false;

            if (fade)
                await CameraFade.FadeOutAsync(startingValue: 0f, colour: Color.black);

            // Freeze the local rig (kinematic, no gravity) while the connection and
            // scene load are in flight. Without this, the rig free-falls from its
            // current pose (or from Vector3.zero if it just spawned) while the game-
            // mode manager's teleport is still racing to catch it. Released in the
            // outer finally so every exit path restores the rig.
            TryHoldLocalRig();

            // On the bootstrap path, the call above is a no-op because Fusion hasn't
            // spawned the local rig yet. Run a per-frame poll so the rig is held the
            // instant it appears mid-load, instead of leaving a gravity window that
            // SpaceStationManager.Spawned() can't close fast enough.
            _joinHoldPollCts?.Cancel();
            _joinHoldPollCts?.Dispose();
            _joinHoldPollCts = new CancellationTokenSource();
            PollLocalRigHoldAsync(_joinHoldPollCts.Token).Forget();

            // Coordination handshake with the active game-mode manager: created here,
            // claimed by the manager's Spawned() (e.g. SpaceStationManager), resolved by
            // SignalSpawnTeleportComplete() after teleport + settle, awaited just before we
            // exit the success path. Joins without a claiming manager (offline preliminary
            // FillRoom, empty gameMode) skip the await entirely.
            SpawnTeleportTcs = new UniTaskCompletionSource();
            SpawnTeleportClaimed = false;

            // Offline joins get exactly 1 attempt; online joins retry up to MaxJoinRetries times.
            int totalAttempts = online is true ? MaxJoinRetries + 1 : 1;

            try
            {
                for (int attempt = 0; attempt < totalAttempts; attempt++)
                {
                    _currentRetryAttempt = attempt;
                    LastJoinRequest = (gameMode, sessionName, online);

                    GameLogger.Info($"Joining room " +
                                    $"(GameMode={(string.IsNullOrEmpty(gameMode) ? "None" : gameMode)}, " +
                                    $"Attempt={attempt + 1}/{totalAttempts}, " +
                                    $"Online={online})");

                    // Exponential backoff delay before retries (skip on first attempt).
                    if (attempt > 0)
                    {
                        float delay = _networkConfig
                            ? _networkConfig.GetRetryDelay(attempt - 1)
                            : Mathf.Min(10f * Mathf.Pow(2f, attempt - 1), 60f);
                        int delaySeconds = Mathf.CeilToInt(delay);
                        string failReason = GetCurrentFailReason();
                        GameLogger.Warning($"JoinGameExAsync: Retrying in {delaySeconds}s ({attempt}/{MaxJoinRetries})");
                        _isRetrying = true;
                        try
                        {
                            for (int remaining = delaySeconds; remaining > 0; remaining--)
                            {
                                //RaiseFadeMessage($"Retrying in {remaining}s ({attempt}/{MaxJoinRetries}) - {failReason}");
                                await UniTask.Delay(1000);
                            }
                        }
                        finally
                        {
                            _isRetrying = false;
                        }
                    }

                    // Fade + close doors only on first attempt; screen is already black on retries.
                    if (attempt == 0)
                    {
                        GameServices.CloseBlimpDoors?.Invoke();
                        NetworkState = State.JOINING;
                        RaiseFadeMessage("Connecting...");

                        var audioService = ServiceLocator.Get<IAudioService>();
                        audioService?.StopAll();
                    }
                    else
                    {
                        NetworkState = State.JOINING;
                    }

                    // Destroy + recreate runner for a clean Fusion state each attempt.
                    await DestroyRunners();
                    CreateRunner();

                    // buildPrefix acts as a matchmaking filter: FillRoom only joins rooms
                    // whose 'buildPrefix' property equals ours, isolating per-build pools
                    // alongside Photon AppVersion. Only set when non-empty — an empty
                    // string is NOT the same as "property absent" in Fusion FillRoom
                    // filtering, which would break matching against pre-change rooms or
                    // between clients with the setting disabled.
                    string buildPrefix = GetSessionNamePrefix();

                    Dictionary<string, SessionProperty> sessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { "gameMode", gameMode },
                        { "running", false },
                        { "modded", GameServices.JoinModded }
                    };

                    if (!string.IsNullOrEmpty(buildPrefix))
                        sessionProperties["buildPrefix"] = buildPrefix;

                    // Add matchType so Fusion FillRoom groups same match types together.
                    // WaitingZone needs it to keep FFA/Duo/Squads in separate lobbies.
                    // FreeForAll needs it so the BR session matches the lobby's match type.
                    if (gameMode == NetworkGameModeIds.FreeForAll || gameMode == NetworkGameModeIds.WaitingZone)
                    {
                        sessionProperties["matchType"] = PendingMatchType;
                        // Only reset for FreeForAll — WaitingZone preserves it for the later FFA dispatch
                        if (gameMode == NetworkGameModeIds.FreeForAll)
                            PendingMatchType = (int)MatchType.FreeForAll;
                    }

                    // Use PendingSessionName if set (e.g. WaitingZone → FreeForAll transition
                    // ensures all players from the same lobby join the same FFA room).
                    if (!string.IsNullOrEmpty(PendingSessionName) && string.IsNullOrEmpty(sessionName))
                    {
                        sessionName = PendingSessionName;
                        GameLogger.Info($"[Matchmaking] Using PendingSessionName='{sessionName}'");
                    }
                    PendingSessionName = null;

                    // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                    GameModeSo gameModeData = GetGameMode(gameMode);
                    int maxPlayers = gameModeData != null ? gameModeData.MaxPlayers : 16;
                    TimeSpan connectionTimeout = ConnectionTimeout;

                    var photonSettings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings;
                    GameLogger.Info($"[Matchmaking] StartGame: SessionName='{sessionName ?? "(null)"}' " +
                                   $"IsVisible={string.IsNullOrEmpty(sessionName)} GameMode={gameMode} " +
                                   $"FillRoom=true PlayerCount={maxPlayers} " +
                                   $"FixedRegion='{photonSettings.FixedRegion}' AppVersion='{photonSettings.AppVersion}' " +
                                   $"BuildPrefix={(string.IsNullOrEmpty(buildPrefix) ? "(disabled)" : $"'{buildPrefix}'")}");
                    StartGameResult result = await SafeNetworkOperations.ExecuteSafe(async () =>
                    {
#if UNITY_EDITOR
                        if (SimulateTimeout is true)
                        {
                            GameLogger.Warning("[SimulateTimeout] Delaying past connection timeout");
                            await UniTask.Delay(connectionTimeout + TimeSpan.FromSeconds(2));
                            return ReflectionHelper.CreateInstance<StartGameResult>(
                                ShutdownReason.ConnectionTimeout,
                                "Simulated timeout",
                                null
                            );
                        }

                        if (SimulateFailure is true && SimulateFailCount > 0)
                        {
                            SimulateFailCount--;
                            if (SimulateFailCount <= 0)
                                SimulateFailure = false;
                            GameLogger.Warning($"[SimulateFailure] Returning ConnectionRefused ({SimulateFailCount} remaining)");
                            return ReflectionHelper.CreateInstance<StartGameResult>(
                                ShutdownReason.ConnectionRefused,
                                "Simulated failure",
                                null
                            );
                        }
#endif

                        UniTask<StartGameResult> startGameTask = Runner.StartGame(new StartGameArgs()
                        {
                            GameMode = online ? Fusion.GameMode.Shared : Fusion.GameMode.Single,
                            SessionNameGenerator = GenerateSessionName,
                            SessionName = string.IsNullOrEmpty(sessionName) ? null : sessionName,
                            SessionProperties = sessionProperties,
                            MatchmakingMode = MatchmakingMode.FillRoom,
                            PlayerCount = maxPlayers,
                            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                            SceneManager = Runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                            // Always visible so FillRoom from other shuttles can find this room.
                            // GameLoopManager sets IsVisible=false when the lobby countdown ends.
                            IsVisible = true,
                            AuthValues = null
                        }).AsUniTask();

                        if (!await TaskUtils.WithTimeoutAsync(startGameTask, connectionTimeout))
                        {
                            GameLogger.Debug("StartGame() timed out");
                            return ReflectionHelper.CreateInstance<StartGameResult>(
                                ShutdownReason.ConnectionTimeout,
                                "Internal Connection Timeout",
                                null
                            );
                        }

                        return await startGameTask;
                    }, default, (int)(connectionTimeout.TotalMilliseconds + 5000));

                    GameLogger.Debug("Runner started game");
                    LastResult = result;

                    if (result != null && result.Ok is true)
                    {
                        var si = Runner.SessionInfo;
                        GameLogger.Info($"[Matchmaking] Joined session: '{si?.Name}' " +
                                       $"players={si?.PlayerCount}/{si?.MaxPlayers} " +
                                       $"isMaster={Runner.IsSharedModeMasterClient} " +
                                       $"region={si?.Region}");

                        // Pin the region after first successful online join so all
                        // subsequent joins land on the same regional server.
                        // Without this, auto-detect can pick different regions per
                        // StartGame call when latencies are close (e.g. us=32ms vs cae=39ms).
                        if (online && !string.IsNullOrEmpty(si?.Region))
                        {
                            var appSettings = Fusion.Photon.Realtime.PhotonAppSettings.Global.AppSettings;
                            if (string.IsNullOrEmpty(appSettings.FixedRegion))
                            {
                                appSettings.FixedRegion = si.Region;
                                GameLogger.Info($"[Matchmaking] Pinned FixedRegion to '{si.Region}'");
                            }
                        }

                        if (Runner.IsSharedModeMasterClient)
                            CurrentRoom.IsRunning = false;

                        TimeJoinedRoom = DateTime.UtcNow;

                        // Suppress respawn/teleport during scene loading for ALL clients
                        // (not just scene authority). Non-authority clients get scenes synced
                        // by Fusion — OnSceneLoadDone fires before GameLoopManager.Spawned()
                        // can set its own suppress delegate. Without this, Respawn() fires
                        // and teleports the player to the blimp/default position.
                        //
                        // Hold the installed delegate in a local so we can detect takeover
                        // by direct reference compare below — comparing delegate.Target.GetType
                        // is brittle because compiler-generated closures don't have Target
                        // typed as System.Object.
                        var prevSuppress = GameServices.SuppressRespawnOnSceneLoad;
                        Func<bool> localSuppress = () => true;
                        GameServices.SuppressRespawnOnSceneLoad = localSuppress;

                        if (loadScene && (Runner.IsSceneAuthority || Runner.GameMode == Fusion.GameMode.Single))
                        {
                            GameLogger.Debug("Game mode: " + gameMode);
                            await LoadGameModeAsync(gameMode, fade);
                            if (Runner.GameMode != Fusion.GameMode.Single)
                                CurrentRoom.IsRunning = false;
                        }
                        else if (loadScene)
                        {
                            // Non-authority: wait for game scenes to be synced by Fusion AND
                            // for GameLoopManager.Spawned() to take over the suppress delegate.
                            GameModeSo joinedGameMode = GetGameMode(gameMode);
                            HashSet<int> expectedScenes = joinedGameMode != null
                                ? joinedGameMode.SceneIndices
                                : new HashSet<int>();

                            DateTime sceneTimeout = DateTime.UtcNow.AddSeconds(30);
                            DateTime lastLog = DateTime.MinValue;
                            bool scenesLoaded = false;
                            bool delegateTakenOver = false;

                            while (DateTime.UtcNow < sceneTimeout)
                            {
                                // Check if all expected game mode scenes are loaded
                                if (!scenesLoaded && expectedScenes.Count > 0)
                                    scenesLoaded = expectedScenes.All(idx => IsSceneLoaded(idx));
                                else if (expectedScenes.Count == 0)
                                    scenesLoaded = true;

                                // Check if GameLoopManager.Spawned() replaced our delegate.
                                // Direct reference compare against the local we installed —
                                // matches the pattern used in LoadGameModeAsync (tempSuppress).
                                if (!delegateTakenOver)
                                {
                                    delegateTakenOver = GameServices.SuppressRespawnOnSceneLoad != null
                                        && GameServices.SuppressRespawnOnSceneLoad != localSuppress;
                                }

                                if (scenesLoaded && delegateTakenOver) break;

                                // Once scenes are loaded, give delegate takeover a few more
                                // seconds but don't block forever (covers game modes without
                                // a GameLoopManager, e.g. SpaceStation).
                                if (scenesLoaded)
                                {
                                    // Wait at most 5 extra seconds for delegate after scenes load
                                    if (!delegateTakenOver)
                                    {
                                        DateTime delegateDeadline = DateTime.UtcNow.AddSeconds(5);
                                        while (DateTime.UtcNow < delegateDeadline)
                                        {
                                            delegateTakenOver = GameServices.SuppressRespawnOnSceneLoad != null
                                                && GameServices.SuppressRespawnOnSceneLoad != localSuppress;
                                            if (delegateTakenOver) break;
                                            await UniTask.Yield();
                                        }
                                    }
                                    break;
                                }

                                // Periodic progress log
                                if ((DateTime.UtcNow - lastLog).TotalSeconds >= 5)
                                {
                                    lastLog = DateTime.UtcNow;
                                    GameLogger.Info($"[JoinGameExAsync] Waiting for scenes: " +
                                        $"expected=[{string.Join(",", expectedScenes)}] " +
                                        $"loaded={scenesLoaded} delegate={delegateTakenOver}");
                                }

                                await UniTask.Yield();
                            }

                            if (!scenesLoaded)
                            {
                                // Loud error — this path silently bounces the player back to
                                // the default game mode, which is easy to confuse with a normal
                                // game-mode transition. Include the full matchmaking context so
                                // we can see what the joined-into room actually looked like.
                                var joinedSi = Runner != null ? Runner.SessionInfo : null;
                                GameLogger.Error($"[JoinGameExAsync] Scene wait TIMED OUT after 30s — " +
                                    $"bouncing to DefaultGameModeId='{DefaultGameModeId}'. " +
                                    $"GameMode={gameMode} " +
                                    $"JoinedSession='{joinedSi?.Name}' " +
                                    $"Players={joinedSi?.PlayerCount}/{joinedSi?.MaxPlayers} " +
                                    $"Region='{joinedSi?.Region}' " +
                                    $"IsSceneAuthority={Runner?.IsSceneAuthority} " +
                                    $"Expected=[{string.Join(",", expectedScenes)}] " +
                                    $"DelegateTakenOver={delegateTakenOver}");

                                // Recovery: kick back to SpaceStation instead of leaving the
                                // player on a black screen. Use try/finally so prevSuppress
                                // is always restored even if the fade message or delay throws.
                                try
                                {
                                    _joinInProgress = false;
                                    _isRetrying = false;
                                    NetworkState = State.NONE;
                                    RaiseFadeMessage("Scene sync failed — returning to lobby");
                                    await UniTask.Delay(TimeSpan.FromSeconds(1));

                                    // Wrap the fire-and-forget join in an awaited local helper
                                    // so an auth failure / session full / throw inside
                                    // JoinGameExAsync doesn't get swallowed and leave the user
                                    // stuck on a black screen.
                                    _ = TryRecoverToDefaultGameModeAsync();
                                }
                                finally
                                {
                                    GameServices.SuppressRespawnOnSceneLoad = prevSuppress;
                                }
                                return null;
                            }

                            // Late-joiner success path: we faded out at the top of
                            // JoinGameExAsync but this branch doesn't go through
                            // LoadGameModeAsync (which owns the matching FadeInAsync for
                            // the authority path). Fade in here so late joiners don't
                            // stay on a black screen after scene sync completes. Skipped
                            // when fade == false so the caller (e.g. the initial bootstrap
                            // post-load) can own the reveal after its own cleanup.
                            if (fade)
                                await CameraFade.FadeInAsync(startingValue: 1f, colour: Color.black);
                        }

                        // If GameLoopManager didn't take over (delegate is still our local
                        // or has been cleared), restore the previous delegate.
                        bool gameLoopActive = GameServices.SuppressRespawnOnSceneLoad != null
                            && GameServices.SuppressRespawnOnSceneLoad != localSuppress
                            && GameServices.SuppressRespawnOnSceneLoad != prevSuppress;
                        if (!gameLoopActive)
                            GameServices.SuppressRespawnOnSceneLoad = prevSuppress;

                        _currentRetryAttempt = 0;
                        _isRetrying = false;
                        NetworkState = State.IN_ROOM;
                        string mode = (Runner.GameMode == Fusion.GameMode.Single) ? "Single Player" : "Online";
                        RaiseFadeMessage($"Connected ({mode})");
                        OnJoinRoom?.Invoke(result);

                        // Block here until the active game-mode manager finishes its
                        // post-spawn teleport + settle. The rig remains kinematic for
                        // this whole window (held by the poll task), so the bootstrap
                        // loader's reveal-fade reveals a stationary, correctly-positioned
                        // rig. Only waits when a manager has claimed the handshake — empty
                        // gameMode / offline preliminary joins skip this entirely. Timeout
                        // protects against a manager that claims but never signals.
                        if (SpawnTeleportClaimed && SpawnTeleportTcs != null)
                        {
                            try
                            {
                                await UniTask.WhenAny(
                                    SpawnTeleportTcs.Task,
                                    UniTask.Delay(TimeSpan.FromSeconds(SpawnTeleportWaitTimeoutSeconds)));
                            }
                            catch (Exception ex)
                            {
                                GameLogger.Warning($"[JoinGameExAsync] SpawnTeleportTcs wait threw: {ex.Message} — releasing rig anyway");
                            }
                        }

                        return result;
                    }

                    GameLogger.Error($"Failed to join room " +
                                     $"(Attempt={attempt + 1}/{totalAttempts}, " +
                                     $"Error={result?.ShutdownReason}, " +
                                     $"ErrorMessage={result?.ErrorMessage})");
                }

                // All attempts exhausted.
                if (online is true)
                {
                    GameLogger.Error($"JoinGameExAsync: All {totalAttempts} retries EXHAUSTED for GameMode='{gameMode}' — " +
                                    $"falling back to offline single-player on DefaultGameModeId='{DefaultGameModeId}'. " +
                                    $"LastShutdownReason='{LastResult?.ShutdownReason}' LastError='{LastResult?.ErrorMessage}'");
                    _currentRetryAttempt = 0;
                    _isRetrying = false;
                    RaiseFadeMessage("Connection failed, going offline");
                    await UniTask.Delay(TimeSpan.FromSeconds(2));
#if UNITY_EDITOR
                    SimulateTimeout = false;
                    SimulateFailure = false;
                    SimulateFailCount = 0;
#endif
                    _joinInProgress = false;
                    return await JoinGameExAsync(DefaultGameModeId, string.Empty, false);
                }

                _currentRetryAttempt = 0;
                _isRetrying = false;
                NetworkState = State.NONE;
                return null;
            }
            finally
            {
                _joinInProgress = false;
                _isRetrying = false;

                _joinHoldPollCts?.Cancel();
                _joinHoldPollCts?.Dispose();
                _joinHoldPollCts = null;

                // Capture before we reset — used to gate the release below.
                bool wasClaimed = SpawnTeleportClaimed;

                // Resolve any unsignalled TCS so a recursive/follow-up join doesn't
                // observe a stale, never-completing handshake.
                SpawnTeleportTcs?.TrySetResult();
                SpawnTeleportTcs = null;
                SpawnTeleportClaimed = false;

                // Per-join freeze/unfreeze contract: only release the rig when this
                // specific join had a claiming manager that signalled completion (i.e.
                // a teleport actually happened and the rig is in its final pose).
                // Unclaimed joins (the offline FillRoom that runs before the SpaceStation
                // join, any reconnect that doesn't spawn a manager) keep the rig held
                // so the next claimed join finds it already frozen — no gap, no fall.
                if (wasClaimed)
                    ReleaseLocalRig();
            }
        }

        private async UniTask<bool> WaitForSceneManagerAsync(TimeSpan timeout)
        {
            if (Runner == null) return false;

            // Give Fusion a small window to mark the scene manager as busy (especially for clients)
            DateTime startWindow = DateTime.UtcNow.AddSeconds(2);
            while (Runner != null && !Runner.IsSceneManagerBusy && DateTime.UtcNow < startWindow)
                await UniTask.Yield();

            DateTime startTime = DateTime.UtcNow;
            while (Runner != null && Runner.IsSceneManagerBusy)
            {
                if (DateTime.UtcNow - startTime > timeout)
                    return false;
                await UniTask.Delay(100);
            }
            return Runner != null && !Runner.IsShutdown && Runner.IsRunning;
        }

        private async UniTask LoadGameModeAsync(string gameMode, bool fade = true)
        {
            if (Runner == null || Runner.IsShutdown || !Runner.IsRunning)
            {
                GameLogger.Warning("LoadGameModeAsync aborted: no active runner.");
                return;
            }

            TimeSpan loadTimeout = TimeSpan.FromSeconds(15);
            async UniTask<bool> WaitWithTimeout(NetworkSceneAsyncOp op, string operationLabel)
            {
                DateTime startTime = DateTime.UtcNow;
                while (!op.IsDone)
                {
                    if (DateTime.UtcNow - startTime > loadTimeout)
                        return false;
                    await UniTask.Delay(100);
                }
                if (op.Error != null)
                {
                    GameLogger.Warning($"{operationLabel} failed: {op.Error}");
                    return false;
                }
                return true;
            }

            if (CurrentRoom != null)
                CurrentRoom.GameMode = gameMode;

            GameLogger.Info($"Loading game scene '{(string.IsNullOrEmpty(gameMode) ? "None" : gameMode)}'");

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            GameModeSo gameModeData = GetGameMode(gameMode);
            HashSet<int> targetScenes = gameModeData ? gameModeData.SceneIndices : new HashSet<int>();

            GameLogger.Debug($"LoadGameModeAsync: target scene indices = [{string.Join(", ", targetScenes)}]");

            if (targetScenes.Count == 0)
            {
                // Fallback: _defaultGameMode may not be configured yet (new field).
                // Use _mainSceneReference if available, otherwise legacy scene index 1.
                // This matches pre-refactor behaviour where empty game mode loaded scene 1.
                if (_mainSceneReference != null && _mainSceneReference.BuildIndex > 0)
                {
                    GameLogger.Warning($"LoadGameModeAsync: no scenes for '{gameMode}', " +
                        $"falling back to _mainSceneReference (index={_mainSceneReference.BuildIndex}).");
                    targetScenes = new HashSet<int> { _mainSceneReference.BuildIndex };
                }
                else if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    GameLogger.Warning($"LoadGameModeAsync: no scenes for '{gameMode}', falling back to scene index 1.");
                    targetScenes = new HashSet<int> { 1 };
                }
                else
                {
                    GameLogger.Warning($"LoadGameModeAsync: no scenes defined for game mode '{gameMode}'.");
                    return;
                }
            }

            if (!await WaitForSceneManagerAsync(loadTimeout))
            {
                GameLogger.Warning("LoadGameModeAsync aborted: scene manager busy timeout.");
                return;
            }

            // Temporarily suppress respawn/teleport during scene loading so that
            // scene Awake() callbacks (MapInfo.Awake → Load → Teleport, BananaBlimp.Start)
            // don't move the player before GameLoopManager takes ownership.
            // GameLoopManager.Spawned() will overwrite this with its own delegate;
            // if no GameLoopManager exists we clear it after loading completes.
            var previousSuppressDelegate = GameServices.SuppressRespawnOnSceneLoad;
            Func<bool> tempSuppress = () => true;
            GameServices.SuppressRespawnOnSceneLoad = tempSuppress;
            bool suppressRestored = false;

            // Load target scenes FIRST, then unload old ones.
            // This avoids the "can't unload last scene" problem and ensures
            // the target scene is available before cleanup.
            var isFirstScene = true;
            foreach (int targetIndex in targetScenes)
            {
                if (IsSceneLoaded(targetIndex))
                {
                    // Scene is orphaned from a previous runner — unload it via Unity
                    // so the current runner can reload it and OnSceneLoadDone fires.
                    GameLogger.Debug($"LoadGameModeAsync: scene index {targetIndex} already loaded, unloading first.");
                    var preUnloadOp = SceneManager.UnloadSceneAsync(targetIndex);
                    while (preUnloadOp != null && !preUnloadOp.isDone)
                        await UniTask.Yield();

                    // Extra frames for Unity to fully tear down the scene and let
                    // NetworkSceneManagerDefault recognise the scene is gone.
                    await UniTask.Yield();
                    await UniTask.Yield();
                }

                GameLogger.Debug($"LoadGameModeAsync: loading scene index {targetIndex}...");
                await UniTask.Yield();

                bool loadSucceeded = await SafeNetworkOperations.ExecuteSafe(() =>
                {
                    if (Runner == null || Runner.IsShutdown || !Runner.IsRunning)
                    {
                        Debug.LogWarning("[Network] Runner invalid.");
                        return UniTask.FromResult(false);
                    }

                    SceneRef sceneRef = SceneRef.FromIndex(targetIndex);
                    NetworkSceneAsyncOp loadOp = Runner.LoadScene(sceneRef, LoadSceneMode.Additive);
                    return WaitWithTimeout(loadOp, $"Scene load (index={targetIndex})");
                });

                // Set the first successfully loaded scene as active so that
                // any objects spawned by Fusion callbacks (e.g. OnSceneLoadDone)
                // are placed in a game mode scene instead of the bootstrap scene.
                if (loadSucceeded && isFirstScene)
                {
                    var loaded = SceneManager.GetSceneByBuildIndex(targetIndex);
                    if (loaded.isLoaded)
                        SceneManager.SetActiveScene(loaded);

                    isFirstScene = false;
                }

                if (!loadSucceeded)
                {
                    GameLogger.Warning($"Scene load timed out (index={targetIndex}).");
                    return;
                }
            }

            // Unload any loaded scene (buildIndex > 0) that isn't in the target set.
            // Done AFTER loading so there's always a valid scene and no "last scene" issue.
            // Iterate backwards: SceneManager indices shift when scenes are removed.
            bool isSingleMode = Runner.GameMode == Fusion.GameMode.Single;
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.buildIndex > 0 && !targetScenes.Contains(scene.buildIndex))
                {
                    if (IsSceneIgnoredForUnload(scene.path)) continue;

                    int buildIndex = scene.buildIndex;
                    GameLogger.Debug($"LoadGameModeAsync: unloading non-target scene index {buildIndex}");

                    // In Single mode, the runner only tracks scenes IT loaded in this
                    // session.  Orphaned scenes from a previous (destroyed) runner can't
                    // be unloaded through Fusion — Runner.UnloadScene would hang until
                    // the 15-second timeout.  Go straight to Unity SceneManager instead.
                    if (isSingleMode)
                    {
                        if (SceneManager.sceneCount > 1)
                        {
                            var directOp = SceneManager.UnloadSceneAsync(buildIndex);
                            while (directOp != null && !directOp.isDone)
                                await UniTask.Yield();
                        }
                        continue;
                    }

                    bool unloadSucceeded = await SafeNetworkOperations.ExecuteSafe(() =>
                    {
                        if (Runner == null || Runner.IsShutdown || !Runner.IsRunning)
                        {
                            Debug.LogWarning("[Network] Runner invalid.");
                            return UniTask.FromResult(false);
                        }

                        NetworkSceneAsyncOp unloadOp = Runner.UnloadScene(SceneRef.FromIndex(buildIndex));
                        return WaitWithTimeout(unloadOp, $"Scene unload (index={buildIndex})");
                    });

                    if (!unloadSucceeded)
                    {
                        GameLogger.Warning($"Scene unload via Runner failed (index={buildIndex}), falling back to SceneManager.");
                        if (SceneManager.sceneCount > 1)
                        {
                            var fallbackOp = SceneManager.UnloadSceneAsync(buildIndex);
                            while (fallbackOp != null && !fallbackOp.isDone)
                                await UniTask.Yield();
                        }
                    }
                }
            }

            // ---- Gorilla spawn safety net ----
            // OnSceneLoadDone should spawn the gorilla, but in Single mode there are
            // edge-cases where the Fusion callback timing prevents it.  Give a few
            // frames for any pending OnSceneLoadDone callbacks to fire, then check.
            await UniTask.Yield();
            await UniTask.Yield();

            var gorillaService = ServiceLocator.Get<IGorillaService>();
            bool hasGorilla = gorillaService != null
                              && gorillaService.HasLocalGorilla
                              && gorillaService.LocalGorilla is UnityEngine.Object gorillaObj
                              && gorillaObj != null;

            if (!hasGorilla && !Runner.IsShutdown && Runner.IsRunning)
            {
                GameLogger.Warning("LoadGameModeAsync: gorilla not found after scene load, spawning as safety net.");
                GorillaRunner.SpawnGorilla();

                // Also ensure UserPropertyManager exists
                if (!UserPropertyManager.Instance && UserPropertyManagerPrefab)
                    Runner.Spawn(UserPropertyManagerPrefab);
            }

            // If GameLoopManager.Spawned() took ownership it replaced our temp delegate.
            // If the delegate is still ours, no GameLoopManager exists — restore previous.
            bool gameLoopTookOver = GameServices.SuppressRespawnOnSceneLoad != tempSuppress;
            GameLogger.Debug($"LoadGameModeAsync: gameLoopTookOver={gameLoopTookOver}");
            if (!gameLoopTookOver)
                GameServices.SuppressRespawnOnSceneLoad = previousSuppressDelegate;
            suppressRestored = true;

            // Player positioning after a game-mode load is owned by SpawnPointService
            // (SceneManager.sceneLoaded → AvatarXRSpawnpointRegistry) + the game-mode
            // manager's Spawned() hook. The previous "restore VR rig to pre-switch
            // position" step was removed because it raced with those teleports:
            // if the game-mode manager hadn't Spawned() yet when LoadGameModeAsync
            // reached it, the restore would snap the player back to the OLD scene
            // position and clobber the correct spawnpoint teleport.

            // Safety: if an exception prevented normal restore, clear the temp delegate
            if (!suppressRestored && GameServices.SuppressRespawnOnSceneLoad == tempSuppress)
                GameServices.SuppressRespawnOnSceneLoad = previousSuppressDelegate;

            if (fade)
                await CameraFade.FadeInAsync(startingValue: 1f, colour: Color.black);
        }

        public bool IsSceneLoaded(int sceneIndex)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.buildIndex == sceneIndex)
                    return true;
            }
            return false;
        }

        public bool IsSceneIgnoredForUnload(string scenePath)
        {
            if (_ignoredScenesForUnload == null) return false;
            foreach (var ignoredScene in _ignoredScenesForUnload)
            {
                if (ignoredScene != null && ignoredScene.Path == scenePath)
                {
                    return true;
                }
            }
            return false;
        }

        public GameModeSo GetGameMode(string gameMode)
        {
            if (gameModeDb)
                return gameModeDb.Get(gameMode);
            return null;
        }

        public GameModeSo CurrentGameModeData => GetGameMode(CurrentGameMode);

        public bool IsBattleRoyale => CurrentGameModeData?.IsBattleRoyale ?? false;
        public bool IsBootcamp => CurrentGameModeData?.IsBootcamp ?? false;
        public bool IsShop => CurrentGameModeData?.IsShop ?? false;

        private void CreateRunner()
        {
            if (runnerPrefab == null)
            {
                GameLogger.Error("RunnerPrefab is null");
                return;
            }
            GameLogger.Info("Creating runner");
            Runner = Instantiate(runnerPrefab);
            DontDestroyOnLoad(Runner.gameObject);
            GameLogger.Info("Creating runner success");
        }

        public void LeaveGame() => _ = LeaveGameAsync();
        public async UniTask LeaveGameAsync()
        {
            //RaiseFadeMessage(string.Empty);

            // close our doors
            GameServices.CloseBlimpDoors?.Invoke();

            // set our state and fade out
            NetworkState = State.LEAVING;

            // Stay in LEAVING state — JoinGameExAsync will set JOINING.
            // Setting NONE here creates a window where the health check reports
            // unhealthy and the FSM triggers bootstrap recovery mid-transition.

            // If a previous join is still in progress (e.g. interrupted by this leave),
            // reset the flag so the rejoin below isn't silently dropped.
            _joinInProgress = false;

            // Return to the default game mode (Main lobby / blimp).
            // Using the configured DefaultGameModeId ensures LoadGameModeAsync
            // has actual scene indices to load — empty string has none, which
            // leaves the player on a black screen in offline / Single mode.
            string returnMode = DefaultGameModeId;
            if (string.IsNullOrEmpty(returnMode))
                GameLogger.Warning("LeaveGameAsync: _defaultGameMode not assigned — scene load will be skipped.");

            await JoinGameExAsync(returnMode, string.Empty, false);
        }

        private async UniTask DestroyRunners()
        {
            // loop through all NetworkRunners and shut them down, then
            // delete them from the scene they're in
            foreach (NetworkRunner r in FindObjectsOfType<NetworkRunner>())
            {
                if (r != null && !r.IsShutdown)
                    await SafeNetworkOperations.ExecuteSafe(async () => await r.Shutdown());
                if (r != null && r.gameObject != null)
                    Destroy(r.gameObject);
            }

            // Yield one frame so Unity processes Destroy calls and clears
            // any pending state from the old runner before we create a new one.
            await UniTask.Yield();
        }

        public string GenerateSessionName()
        {
            string prefix = GetSessionNamePrefix();
            string code = GenerateSessionCode();
            return string.IsNullOrEmpty(prefix) ? code : $"{prefix}_{code}";
        }

        /// <summary>
        /// Returns just the random 4-char code portion of a session name (no prefix).
        /// Used for WaitingZone → FFA RPC payloads where the NetworkString capacity
        /// is too small to carry the full prefixed name; clients reconstruct the
        /// full name locally using the shared SessionNamePrefix setting.
        /// </summary>
        public string GenerateSessionCode()
        {
            const int LENGTH = 4;
            const string CHARSET = "ACBDEFGHIJKLMNOPQRSTUVWXYZ";
            const int MAX_ATTEMPTS = 10;

            var sb = new StringBuilder(LENGTH);
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                sb.Clear();
                for (int i = 0; i < LENGTH; i++)
                    sb.Append(CHARSET[Random.Range(0, CHARSET.Length)]);

                string candidate = sb.ToString();
                if (GameServices.CheckBadWord?.Invoke(candidate) is true)
                    continue;

                return candidate;
            }

            string fallback = Guid.NewGuid().ToString("N").Substring(0, LENGTH).ToUpper();
            Debug.LogWarning($"[NetworkManager] GenerateSessionCode exhausted {MAX_ATTEMPTS} attempts. Using fallback: {fallback}");
            return fallback;
        }

        /// <summary>
        /// Returns the sanitized session-name prefix from FusionSettings, or empty if unset.
        /// Drops any character that isn't a letter, digit, '-', or '_' so Player Settings
        /// values like "CurlyBlue - Teabag" become safe as a room-name prefix and
        /// SessionProperty value.
        /// </summary>
        public static string GetSessionNamePrefix()
        {
            var raw = FusionSettingsAsset.InstanceAsset != null
                ? FusionSettingsAsset.InstanceAsset.Settings?.SessionNamePrefix
                : null;
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    sb.Append(c);
            }
            return sb.ToString();
        }


        public void ClearFadeMessage()
        {
            RaiseFadeMessage(string.Empty);
        }

        private void RaiseFadeMessage(string message)
        {
            if (_fadeScreenMessageEvent is null)
                return;
            _fadeScreenMessageEvent.Raise(message);
        }

        /// <summary>
        /// Awaits <see cref="JoinGameExAsync"/> on the default (lobby) game mode and surfaces
        /// any exception via the fade-message event. Used as a recovery path when scene sync
        /// times out — swallowing the exception there would leave the user stuck on a black
        /// screen with no indication of failure.
        /// </summary>
        private async UniTaskVoid TryRecoverToDefaultGameModeAsync()
        {
            try
            {
                await JoinGameExAsync(DefaultGameModeId, string.Empty, true);
            }
            catch (Exception ex)
            {
                GameLogger.Error($"[NetworkManager] Recovery JoinGameEx failed: {ex}");
                RaiseFadeMessage("Cannot return to lobby, please restart the app.");
            }
        }

        public string GetCurrentFailReason()
        {
            if (LastResult == null || LastResult.Ok)
                return string.Empty;
            switch (LastResult.ShutdownReason)
            {
                case ShutdownReason.GameIsFull: return "Room is full";
                case ShutdownReason.ConnectionTimeout: return "Timed out";
                case ShutdownReason.ConnectionRefused: return "Connection refused";
                default: return LastResult.ShutdownReason.ToString();
            }
        }

        private void Update()
        {
            _gorillaService ??= ServiceLocator.TryGet<IGorillaService>(out var gorillaService) ? gorillaService : null;
            if (_gorillaService != null)
            {
                var localGorilla = _gorillaService.LocalGorilla as Gorilla;
                if (NetworkState != State.IN_ROOM || localGorilla == null || !Runner || !Runner.IsRunning)
                    return;

                if ((Time.time - _lastUpdatePlayerPropertiesTime) > UPDATE_PLAYER_PROPERTIES_COOLDOWN)
                {
                    _lastUpdatePlayerPropertiesTime = Time.time;
                    localGorilla.Object.StateAuthority.TrySetUserProperty(
                        "ping",
                        Mathf.RoundToInt(Mathf.Abs(((float)Runner.GetPlayerRtt(Runner.LocalPlayer) * 1000f)))
                    );
                }
            }
        }



    }

    public enum State
    {
        NONE,
        JOINING,
        IN_ROOM,
        LEAVING,
        GHOST
    }

    public class GorillaRoomInfo
    {
        public string Name => m_Info?.Name ?? string.Empty;

        public string FriendlyName
        {
            get
            {
                if (m_Info == null)
                    return string.Empty;
                if (!string.IsNullOrEmpty(GameMode))
                    return Name.Replace(GameMode, string.Empty);
                return Name;
            }
        }

        public string GameMode
        {
            get
            {
                if (m_Info == null || m_Info.Properties == null)
                    return string.Empty;
                if (m_Info.Properties.TryGetValue("gameMode", out SessionProperty gameModeProperty))
                    return (string)gameModeProperty.PropertyValue;
                return string.Empty;
            }
            set => SetGameMode(value);
        }

        public int PlayerCount => m_Info?.PlayerCount ?? 0;

        public int MaxPlayers => m_Info?.MaxPlayers ?? 0;

        public bool IsRunning
        {
            get => GetBool("running");
            set => SetRunning(value);
        }

        public bool IsModded
        {
            get => GetBool("modded");
            set => SetModded(value);
        }

        public int MatchType
        {
            get
            {
                if (m_Info?.Properties == null) return 0;
                return m_Info.Properties.TryGetValue("matchType", out SessionProperty p) ? (int)p.PropertyValue : 0;
            }
        }

        public bool IsPrivate => !m_Info?.IsVisible ?? false;
        public bool IsValid => m_Info?.IsValid ?? false;

        private readonly SessionInfo m_Info;

        public GorillaRoomInfo() { }
        public GorillaRoomInfo(SessionInfo info) : this()
        {
            if (info == null || !info.IsValid)
                return;
            m_Info = info;
        }

        public void SetGameMode(string gameMode)
        {
            if (m_Info == null)
                return;
            Dictionary<string, SessionProperty> properties = new Dictionary<string, SessionProperty>()
            {
                { "gameMode", gameMode },
                { "running", false }
            };
            m_Info.UpdateCustomProperties(properties);
        }

        public void SetRunning(bool isRunning) => SetBool("running", isRunning);
        public void SetModded(bool isModded) => SetBool("modded", isModded);

        private bool GetBool(string name)
        {
            if (m_Info == null || m_Info.Properties == null)
                return false;
            if (m_Info.Properties.TryGetValue(name, out SessionProperty runningProperty))
                return (bool)runningProperty.PropertyValue;
            GameLogger.Error("Couldn't find session property: " + name);
            return false;
        }

        private void SetBool(string name, bool value)
        {
            if (m_Info == null)
                return;
            m_Info.UpdateCustomProperties(new Dictionary<string, SessionProperty>()
            {
                { name, SessionProperty.Convert(value) }
            });
        }

        public void SetIsVisible(bool isVisible)
        {
            var nm = ServiceLocator.Get<INetworkManager>();
            if (nm == null || !nm.Runner.IsSharedModeMasterClient)
            {
                GameLogger.Warning("Cannot SetIsVisible() because you are not the Master of this room.");
                return;
            }

            if (m_Info != null)
                m_Info.IsVisible = isVisible;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("GorillaRoomInfo(");
            foreach (var field in GetType().GetProperties())
                stringBuilder.Append($"{field.Name}={field.GetValue(this)}, ");
            stringBuilder.Remove(stringBuilder.Length - 2, 2);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }
    }
}
