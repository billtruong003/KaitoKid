using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Teabag.Authentication;
using Teabag.Core;
using Teabag.GameMode;
using Teabag.Networking;
using Teabag.Services;
using UnityEngine;
using Teabag.Player;

public class BattleRoyaleManager : GorillaGameManager
{
    public static new BattleRoyaleManager instance;

    [Header("Spawn Airdrop - Supply")]
    [SerializeField] DropSupplyData chest;
    [SerializeField] float airdropTime = 2f;
    [SerializeField] DropSupplyData firstAirdrop;
    [SerializeField] DropSupplyData defaultAirdrop;
    [field: SerializeField] public Collider AirDropArea { private set; get; }
    public KilledInfo killedInfo;
    public int kills;
    public int teabagRips;
    DateTime spawn;
    int supplyTurn;
    float startedAt;
    [Networked] public float _gameRunningElapsed { get; private set; }
    private bool _matchHadEnoughPlayers;
    [Networked] private TickTimer _blimpEjectTimer { get; set; }
    [Networked, OnChangedRender(nameof(OnBlimpEjectTriggeredChanged))] private NetworkBool _blimpEjectTriggered { get; set; }
    [Networked, OnChangedRender(nameof(OnDoorsOpenedThisRoundChanged))] private NetworkBool _doorsOpenedThisRound { get; set; }
    private int _doorsOpenScheduleVersion;
    private HashSet<NetworkId> _roundVictims = new HashSet<NetworkId>();

    private const float BlimpEjectTimerSeconds = 60f;
    private const float LateJoinThresholdSeconds = 60f;
    private const float MinGameRunningBeforeEndSeconds = 10f;
    private const float BlimpEjectCountdownWarningSeconds = 5f;
    private const int ZoneStartDelaySeconds = 15;
    private const int PostMatchDisplayDelayMs = 3000;
    private const int DeathScreenDelayMs = 3000;
    private const int WinnerStatDelayMs = 500;
    private const int PostEndRespawnDelayMs = 2500;
    private const int ChestSpawnChancePercent = 25;
    private const int ChallengeTypeWin = 3;
    private const int ChallengeTypeGame = 6;

    private IGorillaService _gorillaService;
    private IGameLoopService _gameLoopService;
    private CancellationTokenSource _cts;
    private readonly Dictionary<int, int> _teamsBuffer = new();
    private bool _statsSubmitted;

    [Networked, OnChangedRender(nameof(OnAlivePlayerChanged))]
    private int playersAlive
    {
        get; set;
    }

    public Action<int> OnPlayerAmountChanged;

    private void OnAlivePlayerChanged()
    {
        OnPlayerAmountChanged?.Invoke(playersAlive);
    }

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private Gorilla LocalGorilla
    {
        get
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            return _gorillaService?.LocalGorilla as Gorilla;
        }
    }

    private void Awake()
    {
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        _gameLoopService = ServiceLocator.Get<IGameLoopService>();
    }

    public override async void Spawned()
    {
        // In new game loop, skip the StartEnd waiting loop — players already waited in WaitingZone
        _gameLoopService ??= ServiceLocator.Get<IGameLoopService>();
        if (_gameLoopService?.HasManager == true)
        {
            gameLoop = GameLoop.None;
            // Set StartTime BEFORE base.Spawned() triggers OnGameStarted.
            // Without this, StartTime stays at DateTime.MinValue and the
            // late-joiner check thinks the match has been running for centuries,
            // killing every player instantly.
            if (HasStateAuthority)
                StartTime = SyncedTime.Now;
        }

        base.Spawned();
        instance = this;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        // Prevent immediate airdrop spawn — default DateTime is MinValue which
        // makes (UtcNow - spawn).TotalMinutes >= 2 true on the first FixedGameUpdate.
        spawn = DateTime.UtcNow;

        // Wire GameServices bridges. The player-stats callbacks stay on GameServices because
        // direct service references from here can run into asmdef boundary issues.
        Teabag.Core.GameServices.GetKillCount = () => kills;
        Teabag.Core.GameServices.GetTeabagRipCount = () => teabagRips;
        Teabag.Core.GameServices.IncrementTeabagRipCount = () => teabagRips++;
        Teabag.Core.GameServices.GetMatchStartTime = () => StartTime;
        Teabag.Core.GameServices.BattleRoyaleManagerExists = () => instance != null;
        Teabag.Core.GameServices.GetAlivePlayerCount = () =>
        {
            int count = 0;
            var service = Squido.JungleXRKit.Core.ServiceLocator.Get<Teabag.Core.IGorillaService>();
            if (service != null && service.Gorillas != null)
            {
                foreach (var g in service.Gorillas)
                {
                    var gorilla = (Gorilla)g;
                    // Count as alive if there is no health script OR if health is not dead
                    if (gorilla && (!gorilla.health || !gorilla.health.isDead))
                    {
                        count++;
                    }
                }
            }
            return count;
        };

        var networkManager = ServiceLocator.Get<INetworkManager>();
        while (networkManager?.Runner != null && networkManager.Runner.IsSceneManagerBusy)
            await UniTask.Yield();

        // After async wait, the object may have been despawned or runner shut down
        if (!Object || !Runner || Runner.IsShutdown)
            return;

        CleanUp();

        OnDoorsOpenedThisRoundChanged();
        OnBlimpEjectTriggeredChanged();

        var hardwareRig = LocalHardwareRig;
        if (hardwareRig != null)
        {
            var mapService = ServiceLocator.Get<IMapService>();
            var spawnPoint = mapService?.OnGetSpawnPoint?.Invoke();
            if (spawnPoint.HasValue)
                hardwareRig.Teleport(spawnPoint.Value, Quaternion.identity);
        }
    }

    public override void OnGameStarted(bool joinedLate)
    {
        base.OnGameStarted();

        startedAt = Time.time;
        _matchHadEnoughPlayers = true;
        if (HasStateAuthority)
        {
            SpawnChests();
            _blimpEjectTimer = TickTimer.CreateFromSeconds(Runner, BlimpEjectTimerSeconds);
            _blimpEjectTriggered = false;


            // from the subway before the damage zone activates. Without this,
            // players are killed instantly because they spawn outside the zone
            // boundary while still riding the subway.
            _gameLoopService ??= ServiceLocator.Get<IGameLoopService>();
            if (_gameLoopService?.HasManager == true)
                DelayedStartZonePhases(ZoneStartDelaySeconds);
            else
                ServiceLocator.Get<IZoneService>()?.OnStartZonePhases?.Invoke();

            if (NetworkManager.CurrentRoom != null)
                NetworkManager.CurrentRoom.IsRunning = true;

        }

        // Only kill a late joiner if they join well into the match (after 60s).
        // Joining within the first 60s is considered normal — don't kill them.
        // Skip entirely in GameLoop flow: joinedLate is always true there because
        // gameState goes None->Running in Spawned(), so previousValue is always None.
        // GameLoopManager handles the full player lifecycle — no late-join kills needed.
        if (joinedLate && _gameLoopService?.HasManager != true)
        {
            bool wellIntoMatch = (SyncedTime.Now - StartTime).TotalSeconds > LateJoinThresholdSeconds;
            if (wellIntoMatch)
            {
                if (_gorillaService.HasLocalGorilla)
                {
                    KillLocalGorilla();
                }
                else
                {
                    _gorillaService.OnGorillaSpawned -= OnLateJoinGorillaSpawned;
                    _gorillaService.OnGorillaSpawned += OnLateJoinGorillaSpawned;
                }
            }
        }

    }

    private async void DelayedStartZonePhases(int delaySeconds)
    {
        try
        {
            await UniTask.Delay(delaySeconds * 1000);
            if (!Object || !Runner || Runner.IsShutdown) return;
            ServiceLocator.Get<IZoneService>()?.OnStartZonePhases?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[BR] DelayedStartZonePhases failed: {e}");
        }
    }

    public override async UniTaskVoid OnGameEnded()
    {
        base.OnGameEnded();

        if (_gorillaService != null)
            _gorillaService.OnGorillaSpawned -= OnLateJoinGorillaSpawned;
        if (HasStateAuthority)
        {
            _blimpEjectTimer = TickTimer.None;
            _blimpEjectTriggered = false;
            _doorsOpenedThisRound = false;
            _doorsOpenScheduleVersion++;
        }
        GameServices.CloseBlimpDoors?.Invoke();

        var zoneService = ServiceLocator.Get<IZoneService>();
        zoneService?.OnDamageZoneGameEnded?.Invoke();
    }

    private void OnLateJoinGorillaSpawned(IGorilla gorilla)
    {
        _gorillaService.OnGorillaSpawned -= OnLateJoinGorillaSpawned;
        KillLocalGorilla();
    }

    private void KillLocalGorilla()
    {
        var lateLocal = _gorillaService.LocalGorilla as Gorilla;
        lateLocal?.health?.Damage(byte.MaxValue, HitType.Head);
    }

    public void CleanUp()
    {
        for (int i = Grabbable.allGrabbables.Count - 1; i >= 0; i--)
        {
            var grabbable = Grabbable.allGrabbables[i];
            try
            {
                bool isBackpack = GameServices.IsBackpackType?.Invoke(grabbable) ?? false;
                var mapService = ServiceLocator.Get<IMapService>();
                bool isMap = mapService?.IsMapType?.Invoke(grabbable) ?? false;
                bool isLCK = GameServices.IsLCKGrabbableType?.Invoke(grabbable) ?? false;

                if (grabbable == null || grabbable.Object == null)
                {
                    continue;
                }

                if (grabbable.Object.HasStateAuthority && !isBackpack && !isMap && !isLCK)
                    Runner.Despawn(grabbable.Object);
                else if (grabbable.Object.HasStateAuthority && isLCK)
                    grabbable.transform.position = GameServices.GetBlimpTransform?.Invoke()?.position ?? Vector3.zero;
            }
            catch (Exception e)
            {
                GameLogger.Error($"Error while cleaning up grabbables: {e}");
            }
        }

        CleanUpRagdolls();

        if (Runner != null && !Runner.IsShutdown)
            GameServices.DestroyAllChests?.Invoke(Runner);
    }

    public static void CleanUpRagdolls()
    {
        if (GorillaRagdoll.activeRagdolls.Count == 0) return;

        foreach (var ragdoll in GorillaRagdoll.activeRagdolls.Values)
        {
            if (ragdoll)
                Destroy(ragdoll.gameObject);
        }
        GorillaRagdoll.activeRagdolls.Clear();
    }

    public override void OnGameStateChanged(NetworkBehaviourBuffer previous = default)
    {
        GameState previousValue = GameState.None;
        if (previous)
            previousValue = GetPropertyReader<GameState>(nameof(gameState)).Read(previous);

        base.OnGameStateChanged(previous);
        if (gameState == GameState.Ended && LocalGorilla && LocalGorilla.health)
        {
            // Move the game end sequence to a separate async method to avoid NetworkBehaviourBuffer compilation error
            _ = HandleGameEndSequence();
        }

        if (!HasStateAuthority)
            return;

        if (gameState == GameState.Starting && previousValue != GameState.Starting)
        {
            _doorsOpenScheduleVersion++;
            _doorsOpenedThisRound = false;
            _ = WaitAndOpenDoorsDuringStartingAsync(_doorsOpenScheduleVersion);
        }
    }

    private async UniTask HandleGameEndSequence()
    {
        if (LocalGorilla == null || LocalGorilla.health == null) return;

        bool isWinner = !LocalGorilla.health.isDead;

        // Capture networkManager early — the instance may be despawned after an await.
        var networkManager = NetworkManager ?? ServiceLocator.Get<INetworkManager>();

        if (isWinner)
        {
            // Wait a short time (0.5s) for any pending death RPCs to arrive and update the kill count.
            // This is crucial on the Host, as it might detect the game end before receiving the last kill RPC.
            await UniTask.Delay(500);

            if (LocalGorilla == null) return;

            var authenticationService = ServiceLocator.Get<IAuthenticationService>();
            string authPlayFabId = authenticationService?.PlayFabId;
            string playerDataPlayFabId = PlayerData.playFabId;
            string crownKey = !string.IsNullOrEmpty(authPlayFabId)
                ? authPlayFabId
                : !string.IsNullOrEmpty(playerDataPlayFabId)
                    ? playerDataPlayFabId
                    : string.Empty;

            ServiceLocator.Get<ICrownService>()?.Grant(crownKey);
            LocalGorilla.hasCrown = true;

            killedInfo = new KilledInfo()
            {
                killerName = "None",
                place = 1
            };
            GameServices.EnableDrone?.Invoke();
            GameServices.OpenSettingsScreen?.Invoke("GameOver");
        }

        // Wait for the remaining time before respawning (approx 3s total delay from game end)
        await UniTask.Delay(2500);

        // Respawn all players on the blimp when the match concludes.
        // Guard: object may have been despawned during the delay.
        if (LocalGorilla && LocalGorilla.health)
            _ = LocalGorilla.health.Respawn();

        if (isWinner && !IsSinglePlayer && networkManager?.CurrentRoomSafe != null
            && !networkManager.CurrentRoomSafe.IsPrivate)
        {
            _statsSubmitted = true;
            AuthenticationUtils.SubmitKillsAsync(kills, true);
            _ = GameServices.ScoreChallengeAsync?.Invoke(ChallengeTypeWin);
            _ = GameServices.ScoreChallengeAsync?.Invoke(ChallengeTypeGame);
        }

        // Wait so the player sees the victory/death screen before scene transition
        await UniTask.Delay(PostMatchDisplayDelayMs);

        // Use the same return path as eliminated players so FreeForAll scenes are
        // unloaded consistently before rejoining SpaceStation.
        var gameLoopService = _gameLoopService ?? ServiceLocator.Get<IGameLoopService>();
        if (gameLoopService?.HasManager == true)
        {
            gameLoopService.ReturnToStation();
            return;
        }

        networkManager ??= ServiceLocator.Get<INetworkManager>();
        if (networkManager != null)
        {
            networkManager.JoinGameEx(NetworkGameModeIds.SpaceStation, "", true);
        }
        else
        {
            Debug.LogError("[BR] HandleGameEndSequence: no return path available — player stuck");
        }
    }

    /*private async UniTask HandleGameEndSequence()
    {
        if (LocalGorilla == null || LocalGorilla.health == null) return;

        bool isWinner = !LocalGorilla.health.isDead;
        if (isWinner)
        {
            // Wait a short time (0.5s) for any pending death RPCs to arrive and update the kill count.
            // This is crucial on the Host, as it might detect the game end before receiving the last kill RPC.
            await UniTask.Delay(WinnerStatDelayMs);

            if (LocalGorilla == null) return;

            ServiceLocator.Get<ICrownService>()?.Grant(PlayerData.playFabId);
            LocalGorilla.hasCrown = true;

            killedInfo = new KilledInfo()
            {
                killerName = "None",
                place = 1
            };
            GameServices.EnableDrone?.Invoke();
            GameServices.OpenSettingsScreen?.Invoke("GameOver");
        }

        await UniTask.Delay(PostEndRespawnDelayMs);

        // Respawn all players on the blimp when the match concludes
        if (LocalGorilla && LocalGorilla.health)
        {
            _ = LocalGorilla.health.Respawn();
        }
    }*/


    public override void OnReset()
    {
        base.OnReset();

        GameServices.CloseBlimpDoors?.Invoke();

        var zoneService2 = ServiceLocator.Get<IZoneService>();
        zoneService2?.OnDamageZoneGameEnded?.Invoke();

        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        var resetLocal = _gorillaService?.LocalGorilla as Gorilla;

       if (!_statsSubmitted && resetLocal?.health != null)
       {
           if (resetLocal.health.isDead || gameState == GameState.Ended)
           {
               bool won = !resetLocal.health.isDead && gameState == GameState.Ended;
               GameServices.RecordMatchResult?.Invoke(kills, killedInfo.place, teabagRips);

               if (!IsSinglePlayer && !NetworkManager.CurrentRoomSafe.IsPrivate)
               {
                   _ = AuthenticationUtils.SubmitMatchResultAsync(kills, won, teabagRips, killedInfo.place);

                    if (won)
                    {
                        GameServices.ScoreChallengeAsync?.Invoke(ChallengeTypeWin);
                        ServiceLocator.Get<IQuestService>()?.ReportProgressAsync("win_public_game");
                    }

                    GameServices.ScoreChallengeAsync?.Invoke(ChallengeTypeGame);
                    ServiceLocator.Get<IQuestService>()?.ReportProgressAsync("play_one_game");
                }
            }
        }
        _statsSubmitted = false;

        if (_gameLoopService?.HasManager == true)
        {
            _gameLoopService.NotifyMatchComplete();
        }
        else if (IsSinglePlayer)
        {
            GorillaRunner.SpawnGameManager();
        }
        else if (HasStateAuthority && NetworkManager.CurrentRoomSafe.IsPrivate)
        {
            GorillaRunner.SpawnGameManager();
            if (NetworkManager.CurrentRoom != null)
                NetworkManager.CurrentRoom.IsRunning = false;
        }
        else
        {
            _gameLoopService?.NotifyMatchComplete();
        }

        var health = resetLocal?.health;
        if (health != null)
            health.Respawn().Forget();

        startedAt = 0;
        // Reset kills counter so it doesn't carry over to the next round
        kills = 0;
        teabagRips = 0;
        _roundVictims.Clear();

        CleanUp();
    }

    public override void OnDeath(Gorilla gorilla, Gorilla killer)
    {
        base.OnDeath(gorilla, killer);

        if (gorilla != null && gorilla.HasStateAuthority)
        {
            int aliveCount = 0;
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            var deathGorillas = _gorillaService?.Gorillas;
            if (deathGorillas != null)
            {
                foreach (var gorillaEntry in deathGorillas)
                {
                    var candidate = (Gorilla)gorillaEntry;
                    if (candidate.health && !candidate.health.isDead)
                        aliveCount++;
                }
            }

            killedInfo = new KilledInfo()
            {
                killerName = killer != null ? killer.playerName : "Unknown",
                killer = killer,
                place = aliveCount
            };

            // Capture refs — BattleRoyaleManager may be despawned (_cts cancelled)
            // before the delay completes if the authority disconnects.
            var capturedNetworkManager = NetworkManager;
            var capturedGls = _gameLoopService ?? ServiceLocator.Get<IGameLoopService>();
            _ = ReturnToStationAfterDeath(capturedNetworkManager, capturedGls);
        }

        // Credit kill if the killer is the local player (controlled by this client)
        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        bool isLocalKiller = (killer != null && killer == LocalGorilla);

        // (though Gorilla.Local should be consistent on each client)
        if (!isLocalKiller && killer != null && killer.Object != null && killer.Object.IsValid)
        {
            isLocalKiller = (killer.Object.StateAuthority == Runner.LocalPlayer);
        }

        if (isLocalKiller && gorilla != killer && gorilla.Object != null)
        {
            if (!_roundVictims.Contains(gorilla.Object.Id))
            {
                _roundVictims.Add(gorilla.Object.Id);
                kills++;
            }
        }
    }

    public override void FixedGameUpdate()
    {
        base.FixedGameUpdate();
        SpawnAirdrop();

        if (HasStateAuthority && !_doorsOpenedThisRound)
        {
            if (gameState == GameState.Running)
            {
                _doorsOpenedThisRound = true;
            }
        }

        if (HasStateAuthority && !_blimpEjectTriggered && _blimpEjectTimer.Expired(Runner))
        {
            _blimpEjectTriggered = true;
            _blimpEjectTimer = TickTimer.None;
        }

        if (HasStateAuthority)
        {
            _gameRunningElapsed += Runner.DeltaTime;

            _teamsBuffer.Clear();
            var teams = _teamsBuffer;
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            var fixedGorillas = _gorillaService?.Gorillas;
            int totalGorillas = 0;
            int nullHealth = 0;
            int deadCount = 0;
            int aliveCount = 0;
            if (fixedGorillas != null)
            {
                foreach (var gorillaEntry in fixedGorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    totalGorillas++;
                    if (!gorilla.health)
                    {
                        nullHealth++;
                        continue;
                    }
                    if (gorilla.health.isDead)
                    {
                        deadCount++;
                        continue;
                    }
                    aliveCount++;
                    int team = 0;
                    if (gorilla.team)
                        team = gorilla.team.team;

                    if (!teams.TryAdd(team, 1))
                        teams[team]++;
                }
                if(aliveCount != playersAlive)
                {
                    playersAlive = aliveCount;
                }
            }

            int remaining = 0;
            if (teamManager)
            {
                remaining = teams.Count;
            }
            else
            {
                foreach (var kvp in teams)
                    remaining += kvp.Value;
            }

            if (remaining >= 2)
                _matchHadEnoughPlayers = true;

            // Guard: do not check win conditions until we have seen at least
            // requiredPlayers gorillas. This prevents a false game-over when the
            // second player just joined and their gorilla hasn't synced yet.
            if ((_gorillaService?.GorillaCount ?? 0) < RequiredPlayers)
                return;

            if (remaining <= 1 && !IsSinglePlayer && _gameRunningElapsed > MinGameRunningBeforeEndSeconds && _matchHadEnoughPlayers)
            {
                GameLogger.Warning($"[BR] ENDING GAME: remaining={remaining} elapsed={_gameRunningElapsed:F1}s gorillas={totalGorillas} alive={aliveCount} dead={deadCount} nullHP={nullHealth}");
                gameState = GameState.Ended;
            }
        }
    }

    /// <summary>
    /// Static fire-and-forget: returns dead player to SpaceStation.
    /// Does NOT depend on _cts or instance fields — survives BattleRoyaleManager despawn.
    /// </summary>
    private static async UniTask ReturnToStationAfterDeath(INetworkManager networkManager, IGameLoopService gameLoopService)
    {
        try
        {
            await UniTask.Delay(DeathScreenDelayMs);

            CleanUpRagdolls();

            if (gameLoopService?.HasManager == true)
                gameLoopService.ReturnToStation();
            else if (networkManager != null)
                networkManager.JoinGameEx(NetworkGameModeIds.SpaceStation, "", true);
            else
                Debug.LogError("[BR] ReturnToStationAfterDeath: no return path");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BR] ReturnToStationAfterDeath failed: {e}");

            // Last-resort: try to return to station even after an error
            try
            {
                networkManager?.JoinGameEx(NetworkGameModeIds.SpaceStation, "", true);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"[BR] ReturnToStationAfterDeath last-resort also failed: {e2}");
            }
        }
    }

    private void OnDoorsOpenedThisRoundChanged()
    {
        if (!_doorsOpenedThisRound)
            return;

        if (GameServices.BlimpExists?.Invoke() ?? false)
            GameServices.OpenBlimpDoors?.Invoke();
    }

    private async UniTask WaitAndOpenDoorsDuringStartingAsync(int scheduleVersion)
    {
        while (Object && Runner && !Runner.IsShutdown && HasStateAuthority &&
               scheduleVersion == _doorsOpenScheduleVersion && !_doorsOpenedThisRound &&
               gameState == GameState.Starting)
        {
            var secondsToStart = (StartTime - SyncedTime.Now).TotalSeconds;

            if (secondsToStart <= 15d && secondsToStart > 0d)
            {
                _doorsOpenedThisRound = true;
                return;
            }

            await UniTask.Yield();
        }
    }

    private void OnBlimpEjectTriggeredChanged()
    {
        if (!_blimpEjectTriggered)
            return;

        var isInBlimp = GameServices.GetBlimpIsInBlimp?.Invoke() ?? false;
        GameServices.SetBlimpEjecting?.Invoke(isInBlimp);
    }

    [ContextMenu("Spawn Air Drop")]
    public void SpawnAirdrop()
    {
        if ((DateTime.UtcNow - spawn).TotalMinutes < airdropTime)
            return;

        var networkManager = ServiceLocator.Get<INetworkManager>();
        var runner = networkManager?.Runner;
        if (!runner || runner.IsShutdown)
            return;

        // Only authority should spawn network objects
        if (!Object || !Object.HasStateAuthority)
            return;

        var drop = supplyTurn < 1 ? firstAirdrop : defaultAirdrop;
        if (!drop?.DropSupplyPrefab)
        {
            Debug.LogWarning($"[BattleRoyaleManager] Airdrop prefab is null (supplyTurn={supplyTurn})");
            supplyTurn++;
            spawn = DateTime.UtcNow;
            return;
        }

        for (int i = 0; i < drop.DropSupplyAmount; i++)
        {
            try
            {
                runner.SpawnAsync(drop.DropSupplyPrefab);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BattleRoyaleManager] SpawnAsync failed for airdrop: {e.Message}");
                break;
            }
        }

        supplyTurn++;
        spawn = DateTime.UtcNow;
    }

    public void SpawnChests()
    {
        var networkManager = ServiceLocator.Get<INetworkManager>();
        foreach (var m in Mark.FindObjectsWithMark("ChestSpawn"))
        {
            if (UnityEngine.Random.Range(0, 100) < ChestSpawnChancePercent)
            {
                if (networkManager?.Runner != null)
                {
                    var obj = networkManager.Runner.Spawn(chest.DropSupplyPrefab, m.transform.position, m.transform.rotation);
                    GameServices.SetupSpawnedChest?.Invoke(obj, m.transform);
                }
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_gorillaService != null)
            _gorillaService.OnGorillaSpawned -= OnLateJoinGorillaSpawned;

        if (instance == this)
            instance = null;

        GameServices.GetKillCount = null;
        GameServices.GetMatchStartTime = null;
        GameServices.BattleRoyaleManagerExists = null;
    }
}

public class KilledInfo
{
    public string killerName;
    public Gorilla killer;
    public int place;
}

[Serializable]
public class DropSupplyData
{
    [field: SerializeField] public NetworkObject DropSupplyPrefab { private set; get; }
    [field: SerializeField] public int DropSupplyAmount { private set; get; }
}
