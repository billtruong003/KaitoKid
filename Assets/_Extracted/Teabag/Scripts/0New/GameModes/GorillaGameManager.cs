using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking;
using Teabag.Player;
using UnityEngine;

public class GorillaGameManager : NetworkBehaviour
{
    private const double CountdownTimeoutBufferSeconds = 10d;

    public static GorillaGameManager instance;

    public GameLoop gameLoop;

    [Networked, OnChangedRender(nameof(OnGameStateChanged))]
    public GameState gameState { get; set; }

    [Header("Start Options")]
    public int requiredPlayers = 2;
    public int requiredTeams;
    public int countdownSeconds = 30;
    public int gameSeconds;

    public int RequiredPlayers
    {
        get
        {
            if (IsSinglePlayer) return 1;
            return requiredPlayers;
        }
    }

    public INetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = ServiceLocator.Get<INetworkManager>();
            }
            return _networkManager;
        }
    }

    [Header("Components")]
    public MonoBehaviour teamManager;

    [Header("Game Info")]
    [NonSerialized]
    public GameState lastGameState;

    [Networked]
    public long startTime { get; set; }

    [Networked]
    public long endTime { get; set; }

    [Networked]
    public long waitingForPlayersEnteredTicks { get; set; }

    [Networked]
    public long waitingForMasterEnteredTicks { get; set; }

    [Networked]
    public long startingEnteredTicks { get; set; }

    public DateTime StartTime
    {
        get
        {
            return new DateTime(startTime);
        }
        set
        {
            startTime = value.Ticks;
        }
    }
    public DateTime EndTime
    {
        get
        {
            return new DateTime(endTime);
        }
        set
        {
            endTime = value.Ticks;
        }
    }

    public DateTime WaitingForPlayersEnteredAt
    {
        get
        {
            return waitingForPlayersEnteredTicks > 0 ? new DateTime(waitingForPlayersEnteredTicks) : DateTime.MinValue;
        }
        set
        {
            waitingForPlayersEnteredTicks = value.Ticks;
        }
    }

    public DateTime WaitingForMasterEnteredAt
    {
        get
        {
            return waitingForMasterEnteredTicks > 0 ? new DateTime(waitingForMasterEnteredTicks) : DateTime.MinValue;
        }
        set
        {
            waitingForMasterEnteredTicks = value.Ticks;
        }
    }

    public DateTime StartingEnteredAt
    {
        get
        {
            return startingEnteredTicks > 0 ? new DateTime(startingEnteredTicks) : DateTime.MinValue;
        }
        set
        {
            startingEnteredTicks = value.Ticks;
        }
    }

    protected bool IsSinglePlayer => NetworkManager.Runner &&
        (NetworkManager.Runner.IsSinglePlayer || NetworkManager.Runner.GameMode == Fusion.GameMode.Single);

    private const int EndGameDespawnDelayMs = 5000;

    private bool _isValid = true;
    private INetworkManager _networkManager;
    private IGorillaService _gorillaService;
    private CancellationTokenSource _endCts;

    public override void Spawned()
    {
        base.Spawned();
        _gorillaService = ServiceLocator.Get<IGorillaService>();

        // Make sure there's only one Game Manager instance
        if (instance != null && HasStateAuthority)
        {
            if (instance.HasStateAuthority)
            {
                GameLogger.Warning($"[GGM] Duplicate detected — despawning OLD instance (type={instance.GetType().Name})");
                instance._isValid = false;
                Runner.Despawn(instance.Object);
            }
            else
            {
                GameLogger.Warning($"[GGM] Duplicate detected — despawning THIS new instance (type={GetType().Name})");
                _isValid = false;
                Runner.Despawn(Object);
                return;
            }
        }

        instance = this;

        // Wire GameServices bridges
        Teabag.Core.GameServices.OnPlayerDeath = (dead, killer) => OnDeath((Gorilla)dead, (Gorilla)killer);
        Teabag.Core.GameServices.GorillaGameManagerExists = () => instance != null;

        if (gameLoop == GameLoop.None)
            gameState = GameState.Running;

        OnGameStateChanged();
    }

    public void FixedUpdate()
    {
        if (!Object || !Runner)
            return;

        if (HasStateAuthority)
        {
            switch (gameLoop)
            {
                case GameLoop.None:
                    break;
                case GameLoop.StartEnd:
                    StartEndLoop();
                    break;
                default:
                    GameLogger.Error("Invalid game loop");
                    break;
            }
        }

        if (gameState == GameState.Running)
            FixedGameUpdate();
        lastGameState = gameState;
    }

    public void StartEndLoop()
    {
        switch (gameState)
        {
            case GameState.None:
                EnterWaitingForPlayers();
                break;
            case GameState.WaitingForPlayers:
                if (HasMinimumPlayersAndTeams())
                    EnterWaitingForMaster();

                break;
            case GameState.WaitingForMaster:
                if (!HasMinimumPlayersAndTeams())
                {
                    EnterWaitingForPlayers();
                    break;
                }

                if (ShouldAdvanceFromWaitingForMaster())
                    EnterStarting();

                break;
            case GameState.Starting:
                if (!HasMinimumPlayersAndTeams())
                {
                    EnterWaitingForPlayers();
                    break;
                }

                EnsureCountdownStateIsValid();

                if (SyncedTime.Now >= StartTime)
                {
                    EnterRunning();
                    break;
                }

                if (StartingEnteredAt != DateTime.MinValue)
                {
                    double startingElapsedSeconds = (SyncedTime.Now - StartingEnteredAt).TotalSeconds;
                    double countdownTimeoutSeconds = Math.Max(0d, countdownSeconds) + CountdownTimeoutBufferSeconds;
                    if (startingElapsedSeconds >= countdownTimeoutSeconds)
                        EnterRunning();
                }

                break;
            case GameState.Running:
                if (SyncedTime.Now >= EndTime && endTime > 0)
                    EnterEnded();

                break;
            case GameState.Ended: // If this (or a default) is not here the code will error
                break;
        }
    }

    /// <summary>
    /// Revalidates the state machine when a player leaves so countdown and waiting states
    /// can recover immediately instead of relying on the next natural transition.
    /// </summary>
    public void HandlePlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        switch (gameState)
        {
            case GameState.WaitingForMaster:
            case GameState.Starting:
                if (!HasMinimumPlayersAndTeams())
                {
                    EnterWaitingForPlayers();
                    return;
                }

                if (gameState == GameState.WaitingForMaster && ShouldAdvanceFromWaitingForMaster())
                    EnterStarting();

                break;
            case GameState.Running:
                if (!HasMinimumPlayersAndTeams())
                {
                    EnterEnded();
                    return;
                }

                break;
        }
    }
    public virtual void OnDeath(Gorilla gorilla, Gorilla killer) { }

    public virtual void OnGameStarted() { }

    public virtual void OnGameStarted(bool joinedLate) { }

    public virtual async UniTaskVoid OnGameEnded()
    {
        _endCts?.Cancel();
        _endCts = new CancellationTokenSource();
        try
        {
            await UniTask.Delay(EndGameDespawnDelayMs, cancellationToken: _endCts.Token);

            if (HasStateAuthority && Runner && Object && Object.IsValid)
                Runner.Despawn(Object);
        }
        catch (OperationCanceledException) { }
    }

    public virtual void OnReset() { }

    public virtual void FixedGameUpdate() { }

    public virtual void OnGameStateChanged(NetworkBehaviourBuffer previous = default)
    {
        var previousValue = GameState.None;
        if (previous)
            previousValue = GetPropertyReader<GameState>(nameof(gameState)).Read(previous);

        Debug.Log($"Game state changed: {gameState} (Previous: {previousValue})");
        switch (gameState)
        {
            case GameState.Running:
                OnGameStarted(previousValue == GameState.None);
                break;
            case GameState.Ended:
                OnGameEnded();
                break;
        }
    }

    public virtual bool Won()
    {
        var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
        if (localGorilla != null && localGorilla.health)
            return !localGorilla.health.isDead;

        return false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _endCts?.Cancel();
        _endCts?.Dispose();
        _endCts = null;

        base.Despawned(runner, hasState);

        if (instance == this)
            instance = null;

        if (runner.IsShutdown || !_isValid)
            return;

        OnReset();
    }


    /// <summary>
    /// Returns the current live player count
    /// </summary>
    protected int GetCurrentPlayerCount() => _gorillaService?.GorillaCount ?? 0;

    /// <summary>
    /// Returns the active team count for team-based modes, or zero for free-for-all
    /// modes where team validation is not required.
    /// </summary>
    protected int GetCurrentTeamCount() => teamManager ? (GameServices.GetActiveTeamCount?.Invoke() ?? 0) : 0;

    protected bool HasMinimumPlayersAndTeams()
    {
        return GetCurrentPlayerCount() >= RequiredPlayers && GetCurrentTeamCount() >= requiredTeams;
    }

    /// <summary>
    /// Determines whether the authoritative instance is allowed to leave
    /// <see cref="GameState.WaitingForMaster"/> and enter the countdown.
    /// This keeps solo, public, and private room gate rules in one place.
    /// </summary>
    protected bool ShouldAdvanceFromWaitingForMaster()
    {
        if (!HasStateAuthority)
            return false;

        if (IsSinglePlayer)
            return true;

        if (NetworkManager.CurrentRoom == null)
            return false;

        if (!NetworkManager.CurrentRoomSafe.IsPrivate)
            return true;

        return true; // private room currently has no extra readiness gate in this repo
    }


    /// <summary>
    /// Resets countdown and timer data, then returns the match to the pre-start
    /// waiting state so player and team requirements can be evaluated again.
    /// </summary>
    protected void EnterWaitingForPlayers()
    {
        gameState = GameState.WaitingForPlayers;
        WaitingForPlayersEnteredAt = SyncedTime.Now;
        WaitingForMasterEnteredAt = DateTime.MinValue;
        StartingEnteredAt = DateTime.MinValue;
        startTime = 0;
        endTime = 0;
    }

    /// <summary>
    /// Enters the short gate between meeting the player requirements and starting
    /// the countdown. Room-type and authority checks are resolved here.
    /// </summary>
    protected void EnterWaitingForMaster()
    {
        gameState = GameState.WaitingForMaster;
        WaitingForMasterEnteredAt = SyncedTime.Now;
        StartingEnteredAt = DateTime.MinValue;
        startTime = 0;
        endTime = 0;
    }

    /// <summary>
    /// Starts the countdown phase and stamps the authoritative start time used by
    /// the round timer and Battle Royale start-dependent systems.
    /// </summary>
    protected void EnterStarting()
    {
        gameState = GameState.Starting;
        StartingEnteredAt = SyncedTime.Now;
        StartTime = SyncedTime.Now.AddSeconds(IsSinglePlayer ? 0 : countdownSeconds);
        endTime = 0;
    }

    /// <summary>
    /// Moves the match into live play and initializes <see cref="EndTime"/> for
    /// timed modes so the running phase can end cleanly.
    /// </summary>
    protected void EnterRunning()
    {
        gameState = GameState.Running;
        if (gameSeconds > 0)
            EndTime = SyncedTime.Now.AddSeconds(gameSeconds);
        else
            endTime = 0;
    }

    /// <summary>
    /// Marks the match as finished so end-of-round callbacks, cleanup, and despawn
    /// flow can run through <see cref="OnGameEnded"/>.
    /// </summary>
    protected void EnterEnded()
    {
        gameState = GameState.Ended;
    }

    /// <summary>
    /// Repairs countdown timestamps if <see cref="GameState.Starting"/> was entered
    /// with invalid or missing time data, preventing the countdown from stalling.
    /// </summary>
    protected void EnsureCountdownStateIsValid()
    {
        if (StartingEnteredAt == DateTime.MinValue)
        {
            StartingEnteredAt = SyncedTime.Now;
            GameLogger.Warning("StartingEnteredAt was missing; repaired countdown state-entry timestamp");
        }

        if (startTime > 0)
            return;

        DateTime countdownBaseTime = StartingEnteredAt != DateTime.MinValue ? StartingEnteredAt : SyncedTime.Now;
        StartTime = countdownBaseTime.AddSeconds(IsSinglePlayer ? 0 : countdownSeconds);
        GameLogger.Warning($"StartTime was missing during Starting; repaired countdown target to {StartTime:O}");
    }
}
