using System;
using System.Collections.Generic;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Networked train controller that drives all wagons along a spline path.
/// All wagons are children of this GameObject and move via parenting.
///
/// Supports two modes:
///   StationMode       — Full boarding cycle: Arriving -> Docked -> Countdown -> Departing -> OffScreen -> loop
///   WaitingZoneMode   — Arrival only:  Arriving -> Docked -> Offboarding -> Departing -> OffScreen -> Idle
///                        WaitingZoneManager calls TriggerArrivalCycle() for subsequent arrivals.
///                        Departure from WaitingZone uses SubwayDropVehicle (not this train).
///
/// Replaces the individual shuttle departure system where each ShuttleDock had its own timer.
/// Now all wagons depart together on a shared countdown.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TrainController : NetworkBehaviour
{
    [Header("Train Mode")]
    [SerializeField] private TrainMode _mode = TrainMode.StationMode;

    [Header("Spline Path")]
    [SerializeField] private SplineContainer _splineContainer;
    [SerializeField] private string _stationStartKey = "StationStart";
    [SerializeField] private string _stationBoardingKey = "StationBoarding";
    [SerializeField] private string _stationEndKey = "StationEnd";
    [Tooltip("Normalized spline position (0..1) where the game mode load is triggered during departure. Set to -1 to disable (falls back to StationEnd).")]
    [SerializeField, Range(-1f, 1f)] private float _loadGameModeProgress = -1f;

    [Header("Movement Speeds")]
    [SerializeField] private float _arrivalSpeed = 0.15f;
    [SerializeField] private float _departureSpeed = 0.2f;

    [Header("Movement Curves")]
    [Tooltip("Remaps arrival progress (0→1). Use ease-out (fast start, slow end) for a braking feel.")]
    [SerializeField] private AnimationCurve _arrivalCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Remaps departure progress (0→1). Use ease-in (slow start, fast end) for an acceleration feel.")]
    [SerializeField] private AnimationCurve _departureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Boarding Settings (StationMode)")]
    [SerializeField] private int _countdownSeconds = 45;
    [SerializeField] private float _offScreenResetDelay = 5f;

    [Header("Offboarding Settings (WaitingZoneMode)")]
    [SerializeField] private float _offboardingSeconds = 5f;

    [Header("UI References")]
    [SerializeField] private TextMeshPro _departureCountdownTextA;
    [SerializeField] private TextMeshPro _departureCountdownTextB;

    /// <summary>
    /// Returns wagon info for dispatch: (wagonIndex, matchType, boardedCount).
    /// Used by SpaceStationManager to determine which sessions to create.
    /// </summary>
    private readonly List<(int wagonIndex, MatchType matchType, int boardedCount)> _wagonBoardingInfoBuffer = new();

    private GameLoopService _gameLoopService;
    private float _stationStartProgress;
    private float _stationBoardingProgress;
    private float _stationEndProgress;
    private bool _stationProgressResolved;
    private bool _wasAuthority;             // tracks StateAuthority to detect transfer mid-session
    private float _authorityHealTimer;      // throttles orphaned-authority reclaim checks
    private const float _authorityHealIntervalSeconds = 1f;
    private float _renderSplineProgress;    // smoothed local copy of splineProgress for jitter-free rendering
    private float _phaseElapsed;            // time elapsed in current Arriving/Departing phase
    private float _phaseDuration;           // total duration of current Arriving/Departing phase
    private float _phaseStartProgress;      // splineProgress at the start of the current phase
    private float _phaseEndProgress;        // splineProgress target for the current phase

    [Networked, OnChangedRender(nameof(OnTrainPhaseChanged))]
    public int TrainPhase { get; set; }

    [Networked]
    public float SplineProgress { get; set; }

    [Networked]
    public long DepartureTime { get; set; }

    [Networked, OnChangedRender(nameof(OnDepartureTimerChanged))]
    public float DepartureTimer { get; set; }

    [Networked]
    public int TotalBoarded { get; set; }

    [Networked]
    public NetworkBool LoadPointReached { get; set; }

    /// <summary>
    /// SyncedTime ticks when train entered OffScreen phase. 0 = not in OffScreen.
    /// Networked so the 5 s reset timer survives authority transfers mid-phase.
    /// </summary>
    [Networked]
    public long OffScreenStartTicks { get; set; }

    /// <summary>
    /// SyncedTime ticks when train entered Offboarding phase. 0 = not in Offboarding.
    /// Networked so the offboarding timer survives authority transfers mid-phase.
    /// </summary>
    [Networked]
    public long OffboardingStartTicks { get; set; }

    public TrainPhaseType PhaseType
    {
        get => (TrainPhaseType)TrainPhase;
        set => TrainPhase = (int)value;
    }

    public override void Spawned()
    {
        base.Spawned();

        _gameLoopService = ServiceLocator.Get<GameLoopService>();
        _gameLoopService?.Register(this);

        ResolveStationPositions();

        if (HasStateAuthority)
        {
            SplineProgress = _stationStartProgress;
            DepartureTime = 0;
            DepartureTimer = 0f;
            TotalBoarded = 0;
            LoadPointReached = false;
            OffScreenStartTicks = 0;
            OffboardingStartTicks = 0;
            BeginCurvePhase(_stationStartProgress, _stationBoardingProgress, _arrivalSpeed);
            PhaseType = TrainPhaseType.Arriving;
        }

        _renderSplineProgress = SplineProgress;
        ApplySplinePositionImmediate();
        SetTrainVisible(true);
        SetCountdownVisible(false);

        _wasAuthority = HasStateAuthority;
    }

    private void ResolveStationPositions()
    {
        if (_stationProgressResolved)
            return;

        _splineContainer ??= GetComponentInParent<SplineContainer>();
        if (_splineContainer != null)
        {
            var start = SplineHelper.GetStationSplineProgress(_splineContainer, _stationStartKey);
            var boarding = SplineHelper.GetStationSplineProgress(_splineContainer, _stationBoardingKey);
            var end = SplineHelper.GetStationSplineProgress(_splineContainer, _stationEndKey);

            _stationStartProgress = start >= 0f ? start : 0f;
            _stationBoardingProgress = boarding >= 0f ? boarding : 0.5f;
            _stationEndProgress = end >= 0f ? end : 1f;
            _stationProgressResolved = true;
        }
        else
        {
            _stationStartProgress = 0f;
            _stationBoardingProgress = 0.5f;
            _stationEndProgress = 1f;
            GameLogger.Warning("[TrainController] No SplineContainer found — using fallback spline positions");
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
            return;
        }

        ResolveStationPositions();

        switch (PhaseType)
        {
            case TrainPhaseType.Arriving:
                UpdateArriving();
                break;
            case TrainPhaseType.Docked:
                UpdateDocked();
                break;
            case TrainPhaseType.Countdown:
                UpdateCountdown();
                break;
            case TrainPhaseType.Departing:
                UpdateDeparting();
                break;
            case TrainPhaseType.OffScreen:
                UpdateOffScreen();
                break;
            case TrainPhaseType.Offboarding:
                UpdateOffboarding();
                break;
            case TrainPhaseType.Idle:
                // Do nothing — waiting for external trigger from WaitingZoneManager
                break;
        }
    }

    /// <summary>
    /// Initializes curve-driven phase timing. Duration is computed from the T range
    /// and the base speed so the overall travel time stays the same regardless of
    /// the curve shape.
    /// </summary>
    private void BeginCurvePhase(float fromProgress, float toProgress, float speedPerSecond)
    {
        var range = Mathf.Abs(toProgress - fromProgress);
        _phaseStartProgress = fromProgress;
        _phaseEndProgress = toProgress;
        _phaseElapsed = 0f;
        _phaseDuration = speedPerSecond > 0f ? range / speedPerSecond : 0f;
    }

    private void UpdateArriving()
    {
        _phaseElapsed += Runner.DeltaTime;
        var progress = _phaseDuration > 0f ? Mathf.Clamp01(_phaseElapsed / _phaseDuration) : 1f;
        var eased = _arrivalCurve.Evaluate(progress);
        SplineProgress = Mathf.Lerp(_phaseStartProgress, _phaseEndProgress, eased);

        if (progress >= 1f)
        {
            SplineProgress = _stationBoardingProgress;
            PhaseType = TrainPhaseType.Docked;
            SetAllWagonDoors(true);
        }
    }

    private void UpdateDocked()
    {
        if (_mode == TrainMode.WaitingZoneMode)
        {
            // WaitingZone arrival: go straight to offboarding (force players off)
            OffboardingStartTicks = SyncedTime.Now.Ticks;
            PhaseType = TrainPhaseType.Offboarding;
            return;
        }

        // StationMode: wait for players to board, then start countdown
        var boarded = GetTotalBoarded();
        TotalBoarded = boarded;

        if (boarded > 0 && DepartureTime == 0)
        {
            DepartureTime = SyncedTime.Now.AddSeconds(_countdownSeconds).Ticks;
            PhaseType = TrainPhaseType.Countdown;
            SetCountdownVisible(true);
        }
    }

    private void UpdateOffboarding()
    {
        // Defensive: if the networked start tick wasn't set (e.g. legacy state), initialize now.
        if (OffboardingStartTicks == 0)
            OffboardingStartTicks = SyncedTime.Now.Ticks;

        double elapsedSeconds = (SyncedTime.Now.Ticks - OffboardingStartTicks) / (double)TimeSpan.TicksPerSecond;
        if (elapsedSeconds >= _offboardingSeconds)
        {
            // Time's up — close doors and depart
            SetAllWagonDoors(false);
            BeginCurvePhase(SplineProgress, _stationEndProgress, _departureSpeed);
            PhaseType = TrainPhaseType.Departing;
            OffboardingStartTicks = 0;
        }
    }

    private void UpdateCountdown()
    {
        var boarded = GetTotalBoarded();
        TotalBoarded = boarded;

        // Cancel if everyone left
        if (boarded == 0)
        {
            DepartureTime = 0;
            DepartureTimer = 0f;
            PhaseType = TrainPhaseType.Docked;
            SetCountdownVisible(false);
            return;
        }

        var remainingTicks = DepartureTime - SyncedTime.Now.Ticks;
        DepartureTimer = Mathf.Max(0f, (float)(remainingTicks / (double)TimeSpan.TicksPerSecond));

        if (remainingTicks <= 0)
        {
            TriggerDeparture();
        }
    }

    private void TriggerDeparture()
    {
        DepartureTimer = 0f;
        LoadPointReached = false;
        BeginCurvePhase(SplineProgress, _stationEndProgress, _departureSpeed);
        PhaseType = TrainPhaseType.Departing;
        AssignDepartureSessionCodes();
        SetAllWagonDoors(false);
        SetCountdownVisible(false);
    }

    /// <summary>
    /// State-authority only: stamps each wagon that has at least one boarder with
    /// a fresh 4-char session code. Passengers read this in
    /// <see cref="SpaceStationManager.DispatchToWaitingZone"/> to build a deterministic
    /// WaitingZone session name — every passenger of the same wagon lands in the same
    /// room instead of racing FillRoom with a null SessionName.
    /// </summary>
    private void AssignDepartureSessionCodes()
    {
        if (_gameLoopService == null)
            return;

        var networkManager = ServiceLocator.Get<INetworkManager>() as NetworkManager;
        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
        {
            var wagon = wagons[i];
            if (wagon.BoardedCount <= 0)
            {
                wagon.SetDepartureSessionCode(string.Empty);
                continue;
            }

            // Fallback Guid path mirrors GenerateSessionCode's own fallback so the
            // wagon always gets *some* code even if the NetworkManager service is
            // unavailable (e.g. in isolated tests).
            string code = networkManager != null
                ? networkManager.GenerateSessionCode()
                : Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            wagon.SetDepartureSessionCode(code);
        }
    }

    private void UpdateDeparting()
    {
        _phaseElapsed += Runner.DeltaTime;
        float progress = _phaseDuration > 0f ? Mathf.Clamp01(_phaseElapsed / _phaseDuration) : 1f;
        float eased = _departureCurve.Evaluate(progress);
        SplineProgress = Mathf.Lerp(_phaseStartProgress, _phaseEndProgress, eased);

        // Signal game mode load when the train crosses the LoadGameMode marker
        if (!LoadPointReached && _loadGameModeProgress >= 0f && SplineProgress >= _loadGameModeProgress)
            LoadPointReached = true;

        if (progress >= 1f)
        {
            SplineProgress = _stationEndProgress;
            // If no LoadGameMode knot was placed, signal now as fallback
            if (!LoadPointReached)
                LoadPointReached = true;
            PhaseType = TrainPhaseType.OffScreen;
            OffScreenStartTicks = SyncedTime.Now.Ticks;
        }
    }

    private void UpdateOffScreen()
    {
        // Defensive: if the networked start tick wasn't set (e.g. legacy state or race),
        // initialize it now so the timer starts counting rather than firing immediately.
        if (OffScreenStartTicks == 0)
            OffScreenStartTicks = SyncedTime.Now.Ticks;

        double elapsedSeconds = (SyncedTime.Now.Ticks - OffScreenStartTicks) / (double)TimeSpan.TicksPerSecond;

        if (elapsedSeconds >= _offScreenResetDelay)
        {
            if (_mode == TrainMode.WaitingZoneMode)
            {
                // Go to Idle — WaitingZoneManager decides when/if next cycle starts
                SetTrainVisible(false);
                PhaseType = TrainPhaseType.Idle;
                OffScreenStartTicks = 0;
                return;
            }

            // StationMode: loop immediately
            SplineProgress = _stationStartProgress;
            DepartureTime = 0;
            DepartureTimer = 0f;
            TotalBoarded = 0;
            LoadPointReached = false;
            OffScreenStartTicks = 0;
            ResetAllWagonBoardedCounts();
            BeginCurvePhase(_stationStartProgress, _stationBoardingProgress, _arrivalSpeed);
            PhaseType = TrainPhaseType.Arriving;
        }
    }

    /// <summary>
    /// Re-initializes local-only timing fields when StateAuthority transfers mid-session
    /// (e.g. after the previous authority boarded the train and left for WaitingZone).
    /// OffScreen/Offboarding timers live on networked ticks (OffScreenStartTicks,
    /// OffboardingStartTicks) so they do NOT need to be restored here — they keep
    /// counting across authority transfers.
    /// Mirrors the pattern in GameLoopManager.RecoverAuthorityState.
    /// </summary>
    private void RecoverAuthorityState()
    {
        ResolveStationPositions();

        switch (PhaseType)
        {
            case TrainPhaseType.Arriving:
                BeginCurvePhase(SplineProgress, _stationBoardingProgress, _arrivalSpeed);
                break;
            case TrainPhaseType.Departing:
                BeginCurvePhase(SplineProgress, _stationEndProgress, _departureSpeed);
                break;
            // Docked, Countdown, OffScreen, Offboarding, Idle: driven by networked state only
        }
    }

    /// <summary>
    /// Start a new arrival cycle (passengers arriving, will be offboarded).
    /// Call from WaitingZoneManager when new players need a visual arrival train.
    /// </summary>
    public void TriggerArrivalCycle()
    {
        if (!HasStateAuthority)
            return;

        SplineProgress = _stationStartProgress;
        DepartureTime = 0;
        DepartureTimer = 0f;
        TotalBoarded = 0;
        LoadPointReached = false;
        OffScreenStartTicks = 0;
        OffboardingStartTicks = 0;
        ResetAllWagonBoardedCounts();
        SetTrainVisible(true);
        _renderSplineProgress = SplineProgress;
        ApplySplinePositionImmediate(); // Teleport transform to start BEFORE phase change propagates
        BeginCurvePhase(_stationStartProgress, _stationBoardingProgress, _arrivalSpeed);
        PhaseType = TrainPhaseType.Arriving;
    }

    /// <summary>
    /// Re-applies the last interpolated wagon position so Player.Update()
    /// (which runs at -50, after us at -100) reads a stable transform.
    /// Does NOT advance the interpolation — Render() handles that after tick simulation.
    /// Also self-heals orphaned authority (see TrySelfHealAuthority).
    /// </summary>
    private void Update()
    {
        ApplySplinePosition();
        TrySelfHealAuthority();
    }

    /// <summary>
    /// The Train.prefab NetworkObject is NOT flagged "Is Master Client Object", so when
    /// the original StateAuthority player leaves the session (e.g. boards the train and
    /// JoinGameEx's into WaitingZone), Fusion does not auto-transfer authority. The
    /// object becomes orphaned — no client runs FixedUpdateNetwork on its authority path
    /// and the state machine freezes.
    ///
    /// This self-heal runs on the Shared Mode Master Client once per second. When it
    /// detects that the train has no valid (still-connected) StateAuthority, it claims
    /// authority itself. The subsequent FixedUpdateNetwork tick sees the
    /// (!_wasAuthority → HasStateAuthority) transition and calls RecoverAuthorityState.
    /// Networked timers (OffScreenStartTicks, OffboardingStartTicks) keep counting across
    /// the gap so the train returns within the usual delay.
    /// </summary>
    private void TrySelfHealAuthority()
    {
        if (Runner == null || !Runner.IsRunning || Object == null)
            return;

        _authorityHealTimer += Time.unscaledDeltaTime;
        if (_authorityHealTimer < _authorityHealIntervalSeconds)
            return;
        _authorityHealTimer = 0f;

        if (!Runner.IsSharedModeMasterClient)
            return;
        if (HasStateAuthority)
            return;

        var auth = Object.StateAuthority;
        if (!auth.IsRealPlayer)
        {
            Object.RequestStateAuthority();
            return;
        }

        // If the recorded authority is a player who is no longer connected, reclaim.
        bool authorityIsActive = false;
        foreach (var p in Runner.ActivePlayers)
        {
            if (p == auth)
            {
                authorityIsActive = true;
                break;
            }
        }

        if (!authorityIsActive)
        {
            Object.RequestStateAuthority();
        }
    }

    public override void Render()
    {
        // Smoothly interpolate toward the networked splineProgress to avoid jitter
        // from discrete network ticks. Authority is already smooth (MoveTowards each tick).
        // Only interpolate here (once per frame) — Update() just re-applies the position.
        const float lerpSpeed = 12f;
        _renderSplineProgress = Mathf.Lerp(_renderSplineProgress, SplineProgress, Time.deltaTime * lerpSpeed);

        // Snap when very close to avoid lingering drift
        if (Mathf.Abs(_renderSplineProgress - SplineProgress) < 0.0001f)
            _renderSplineProgress = SplineProgress;

        ApplySplinePosition();
    }

    /// <summary>
    /// Positions each wagon independently along the spline based on _renderSplineProgress (lead wagon)
    /// and each wagon's DistanceOffset. Wagons curve naturally along the track.
    /// Also sets the TrainController's own transform to the lead position for backwards compat.
    /// </summary>
    public void ApplySplinePosition()
    {
        if (!_splineContainer)
            return;

        var spline = _splineContainer.Spline;
        if (spline == null)
            return;

        var totalLength = spline.GetLength();
        if (totalLength <= 0f)
            return;

        // Convert the lead's normalized T to real arc-length distance so that
        // wagon DistanceOffset (in meters) is subtracted correctly. Without this,
        // non-uniform spline parameterization causes wagons to bunch up at curves
        // and at the end of the spline.
        var leadDistance = SplineHelper.SplineProgressToDistance(spline, _renderSplineProgress);

        // Position the TrainController itself at the lead wagon's position
        SplineHelper.Evaluate(_splineContainer, _renderSplineProgress, out Vector3 leadPos, out Quaternion leadRot);
        transform.SetPositionAndRotation(leadPos, leadRot);

        // Position each wagon independently along the spline using real distances.
        // When a wagon's distance falls outside the spline (negative at start, or
        // past the end), extrapolate linearly along the tangent so wagons never
        // pile up at the spline boundaries.
        if (_gameLoopService == null) return;

        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
        {
            var wagonDist = leadDistance - wagons[i].DistanceOffset;
            SplineHelper.EvaluateAtDistance(_splineContainer, spline, wagonDist, totalLength, out Vector3 wagonPos, out Quaternion wagonRot);
            wagonRot *= Quaternion.Euler(0f, wagons[i].AngleOffset, 0f);
            wagons[i].ApplySplinePosition(wagonPos, wagonRot);
        }
    }

    /// <summary>
    /// Snaps the render T to the current networked value and teleports wagons immediately.
    /// Use when the player needs the exact authoritative position (e.g., teleport onto train).
    /// </summary>
    public void SnapSplinePosition()
    {
        _renderSplineProgress = SplineProgress;
        ApplySplinePositionImmediate();
    }

    /// <summary>
    /// Same as ApplySplinePosition but teleports wagons instantly (no physics interpolation).
    /// Used for initial spawn, phase snaps, and teleport-onto-train cases.
    /// </summary>
    public void ApplySplinePositionImmediate()
    {
        if (!_splineContainer)
            return;

        var spline = _splineContainer.Spline;
        if (spline == null)
            return;

        var totalLength = spline.GetLength();
        if (totalLength <= 0f)
            return;

        var leadDistance = SplineHelper.SplineProgressToDistance(spline, _renderSplineProgress);

        SplineHelper.Evaluate(_splineContainer, _renderSplineProgress, out Vector3 leadPos, out Quaternion leadRot);
        transform.SetPositionAndRotation(leadPos, leadRot);

        if (_gameLoopService == null) return;

        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
        {
            var wagonDist = leadDistance - wagons[i].DistanceOffset;
            SplineHelper.EvaluateAtDistance(_splineContainer, spline, wagonDist, totalLength, out Vector3 wagonPos, out Quaternion wagonRot);
            wagonRot *= Quaternion.Euler(0f, wagons[i].AngleOffset, 0f);
            wagons[i].TeleportToSplinePosition(wagonPos, wagonRot);
        }
    }

    private int GetTotalBoarded()
    {
        if (_gameLoopService == null)
            return 0;

        var total = 0;
        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
        {
            total += wagons[i].BoardedCount;
        }
        return total;
    }

    private void SetAllWagonDoors(bool open)
    {
        if (_gameLoopService == null)
            return;

        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
            wagons[i].SetDoorsOpen(open);
    }

    private void ResetAllWagonBoardedCounts()
    {
        if (_gameLoopService == null)
            return;

        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
            wagons[i].BoardedCount = 0;
    }

    private void SetTrainVisible(bool visible)
    {
        if (_gameLoopService == null)
            return;

        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
            wagons[i].SetVisible(visible);
    }

    /// <summary>
    /// Returns wagon info for dispatch. Reuses an internal buffer — do not cache the returned list.
    /// </summary>
    public List<(int wagonIndex, MatchType matchType, int boardedCount)> GetWagonBoardingInfo()
    {
        _wagonBoardingInfoBuffer.Clear();
        if (_gameLoopService == null)
            return _wagonBoardingInfoBuffer;

        var wagons = _gameLoopService.Wagons;
        for (var i = 0; i < wagons.Count; i++)
        {
            _wagonBoardingInfoBuffer.Add((wagons[i].WagonIndex, wagons[i].WagonMatchType, wagons[i].BoardedCount));
        }
        return _wagonBoardingInfoBuffer;
    }

    private void SetCountdownVisible(bool visible)
    {
        if (_departureCountdownTextA != null)
            _departureCountdownTextA.gameObject.SetActive(visible);

        if (_departureCountdownTextB != null)
            _departureCountdownTextB.gameObject.SetActive(visible);
    }

    public void OnTrainPhaseChanged()
    {
        switch (PhaseType)
        {
            case TrainPhaseType.Arriving:
                // Snap transform to exact position so WaitingZoneManager sees the
                // correct position when positioning the player this frame.
                SetTrainVisible(true);
                SnapSplinePosition();
                break;
            case TrainPhaseType.Docked:
            case TrainPhaseType.Offboarding:
                SetCountdownVisible(false);
                break;
            case TrainPhaseType.Countdown:
                SetCountdownVisible(true);
                break;
            case TrainPhaseType.Departing:
                SetCountdownVisible(false);
                break;
            case TrainPhaseType.Idle:
                SetTrainVisible(false);
                break;
        }

    }

    public void OnDepartureTimerChanged()
    {
        var remainingSeconds = Mathf.CeilToInt(DepartureTimer);
        var countdownText = $"{remainingSeconds / 60:D2}:{remainingSeconds % 60:D2}";

        if (_departureCountdownTextA != null)
            _departureCountdownTextA.text = countdownText;

        if (_departureCountdownTextB != null)
            _departureCountdownTextB.text = countdownText;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, 1f);

        if (!_splineContainer)
            return;

        DrawStationGizmo(_stationStartKey, Color.green, 0.6f);
        DrawStationGizmo(_stationBoardingKey, Color.cyan, 0.6f);
        DrawStationGizmo(_stationEndKey, Color.red, 0.6f);

        if (_loadGameModeProgress >= 0f)
            DrawProgressGizmo("LoadGameMode", _loadGameModeProgress, Color.yellow, 0.8f);
    }

    private void DrawStationGizmo(string key, Color color, float radius)
    {
        var progress = SplineHelper.GetStationSplineProgress(_splineContainer, key);
        if (progress < 0f)
            return;

        DrawProgressGizmo(key, progress, color, radius);
    }

    private void DrawProgressGizmo(string label, float progress, Color color, float radius)
    {
        SplineHelper.Evaluate(_splineContainer, progress, out var pos, out _);
        Gizmos.color = color;
        Gizmos.DrawWireSphere(pos, radius);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * (radius + 0.3f), label);
#endif
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        _gameLoopService?.Unregister(this);
    }
}
