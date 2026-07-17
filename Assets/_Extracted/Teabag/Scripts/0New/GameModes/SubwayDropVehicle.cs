using System;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

/// <summary>
/// Networked subway vehicle that handles both boarding and drop phases.
/// During SubwayBoarding: provides spawn points and AABB interior detection.
/// During SubwayDropping: flies over the Main Island on a spline path — players
/// can jump off within the configurable drop zone (T-based range along the path).
///
/// Uses SplineContainer directly (via SplineHelper) for spline-based movement.
///
/// Movement is pure transform-based (SetPositionAndRotation). Player following is handled
/// by the gorilla locomotion GorillaTouchingTarget surface velocity system.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SubwayDropVehicle : NetworkBehaviour, IDoorProvider
{
    private const string WaitingForPlayersText = "Minimal number of players not attained yet.";

    [Header("Player Spawn Points")]
    [Tooltip("Positions inside the vehicle where players are teleported on entry.")]
    public Transform[] spawnPoints;

    [Header("Spline Path")]
    [Tooltip("Reference to the SplineContainer in the scene. Falls back to FindObjectOfType if null.")]
    [SerializeField] private SplineContainer _splineContainer;

    [Header("Movement")]
    [SerializeField] private float _speed = 0.02f;

    [Header("UI References")]
    [SerializeField] private TextMeshPro _playerCountText;
    [SerializeField] private TextMeshPro _countdownText;

    [Header("Station Keys (used for WaitingZone departure)")]
    [Tooltip("Spline Int Data key whose value is the knot index for station end.")]
    [SerializeField] private string _stationEndKey = "StationEnd";

    [Header("Drop Zone")]
    [Tooltip("Normalized progress along the spline path where doors open (0..1).")]
    [SerializeField, Range(0f, 1f), FormerlySerializedAs("_dropZoneStartT")]
    private float _dropZoneStartProgress = 0.05f;
    [Tooltip("Normalized progress along the spline path where doors close (0..1).")]
    [SerializeField, Range(0f, 1f), FormerlySerializedAs("_dropZoneEndT")]
    private float _dropZoneEndProgress = 0.85f;

    [Header("Door Settings")]
    [Tooltip("Initial door state when the vehicle starts. WagonDoor reads this to set the correct starting pose.")]
    [SerializeField] private bool _startDoorsOpen;

    [Header("Interior AABB (local space)")]
    [SerializeField] private Vector3 _interiorCenter = Vector3.zero;
    [SerializeField] private Vector3 _interiorSize = new Vector3(4f, 3f, 14f);

    [Networked]
    public float networkedSplineProgress { get; set; }

    [Networked, OnChangedRender(nameof(OnDoorsOpenChanged))]
    public NetworkBool doorsOpen { get; set; }

    [Networked, OnChangedRender(nameof(OnRouteStartedChanged))]
    public NetworkBool routeStarted { get; set; }

    [Networked]
    public NetworkBool routeComplete { get; set; }

    private float _localSplineProgress;        // authoritative progress (set in FixedUpdateNetwork)
    private float _renderSplineProgress;       // smoothed copy for jitter-free rendering
    private float _stationEndSplineProgress = -1f;
    private bool _playerHasJumped;
    private bool _wasInsideLastFrame;
    private bool _aabbWarmedUp;
    private bool _spawned;
    private bool _localRouteStarted;
    private bool _wasCleaned;
    private bool _showingPlayerCountText;
    private bool _showingCountdownText;
    private WaitingZonePhase? _lastWaitingZonePhase;
    private int _lastPlayerCount = -1;
    private int _lastRequiredPlayers = -1;
    private int _lastRemainingSeconds = -1;

    /// <summary>
    /// Fired on all clients when the door state changes. Parameter is true = open, false = closed.
    /// WagonDoor components subscribe to this to animate sliding.
    /// </summary>
    public event Action<bool> OnDoorsStateChanged;

    /// <summary>
    /// Fired on all clients (authority and late-arriving RPC recipients) the first time
    /// <see cref="InitLocalRouteState"/> snaps the vehicle to spline-knot-0. Subscribers
    /// should re-teleport local players into the now-snapped interior — the transform
    /// delta typically exceeds MovingPlatformLocomotion's per-step carry cap.
    /// </summary>
    public event Action RouteStarted;

    public bool StartDoorsOpen => _startDoorsOpen;
    public bool IsDoorsOpen => doorsOpen;

    private IGorillaService _gorillaService;
    private GameLoopService _gameLoopService;

    public bool IsRouteStarted => _localRouteStarted;
    public bool IsRouteComplete => _spawned && routeComplete;

    /// <summary>
    /// True when the subway has reached or passed the StationEnd knot on its spline.
    /// Used by WaitingZoneManager to know when to trigger scene transition.
    /// </summary>
    public bool HasReachedStationEnd => _stationEndSplineProgress >= 0f && _renderSplineProgress >= _stationEndSplineProgress;

    public bool PlayerIsInside => !_localRouteStarted || (!_playerHasJumped && _wasInsideLastFrame);

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private void Awake()
    {
        _gameLoopService = ServiceLocator.Get<GameLoopService>();
    }

    public Vector3 GetSpawnPosition(int playerIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform.position;

        return spawnPoints[playerIndex % spawnPoints.Length].position;
    }

    public Quaternion GetSpawnRotation(int playerIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform.rotation;

        return spawnPoints[playerIndex % spawnPoints.Length].rotation;
    }

    public override void Spawned()
    {
        base.Spawned();

        _gameLoopService ??= ServiceLocator.Get<GameLoopService>();
        _gameLoopService?.Register(this);
        _spawned = true;

        // Wire blimp-equivalent GameServices bridges so existing code
        // (Player.cs, Weapons) that checks IsInBlimp works with the subway.
        GameServices.BlimpExists = () => _gameLoopService?.SubwayDropVehicle != null;
        GameServices.IsInBlimp = () => { var s = _gameLoopService?.SubwayDropVehicle; return s != null && s.PlayerIsInside; };
        GameServices.GetBlimpTransform = () => _gameLoopService?.SubwayDropVehicle?.transform;
        GameServices.GetBlimpIsInBlimp = () => { var s = _gameLoopService?.SubwayDropVehicle; return s != null && s.PlayerIsInside; };
        GameServices.OpenBlimpDoors = () => { };
        GameServices.CloseBlimpDoors = () => { };

        _localSplineProgress = 0f;
        _playerHasJumped = false;
        _wasInsideLastFrame = false;
        _aabbWarmedUp = false;
        _localRouteStarted = false;
        _showingPlayerCountText = false;
        _showingCountdownText = false;
        _lastWaitingZonePhase = null;
        _lastPlayerCount = -1;
        _lastRequiredPlayers = -1;
        _lastRemainingSeconds = -1;

        // Resolve StationEnd spline progress from Int Data (used for WaitingZone departure)
        if (_splineContainer != null && !string.IsNullOrEmpty(_stationEndKey))
        {
            _stationEndSplineProgress = SplineHelper.GetStationSplineProgress(_splineContainer, _stationEndKey);
        }

        if (HasStateAuthority)
        {
            networkedSplineProgress = 0f;
            doorsOpen = _startDoorsOpen;
            routeStarted = false;
            routeComplete = false;
        }

        SetWaitingRoomTextVisible(false, false);

        // Fire door state after networked values are populated so WagonDoor
        // subscribers see the correct initial pose. OnChangedRender won't fire
        // if the replicated value matches the baseline set at spawn time.
        OnDoorsStateChanged?.Invoke(doorsOpen);
    }

    public void StartRoute()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning("SubwayDropVehicle.StartRoute called on non-authority — ignored");
            return;
        }

        if (_splineContainer == null)
        {
            Debug.LogError("SubwayDropVehicle: no SplineContainer found — forcing route complete to avoid phase stall");
            routeComplete = true;
            return;
        }

        // Close doors on departure (WaitingZone boards with doors open)
        if (doorsOpen)
            doorsOpen = false;

        InitLocalRouteState();
        routeStarted = true;
        Rpc_StartRoute();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!routeStarted || routeComplete)
            return;

        _localSplineProgress += Runner.DeltaTime * _speed;
        _localSplineProgress = Mathf.Clamp01(_localSplineProgress);

        const float networkSyncThreshold = 0.005f;
        if (Mathf.Abs(_localSplineProgress - networkedSplineProgress) > networkSyncThreshold || _localSplineProgress >= 1f)
            networkedSplineProgress = _localSplineProgress;

        if (!doorsOpen && _localSplineProgress >= _dropZoneStartProgress && _localSplineProgress < _dropZoneEndProgress)
        {
            doorsOpen = true;
        }

        if (_localSplineProgress >= 1f)
        {
            routeComplete = true;
            EjectAllPlayers();
        }
    }

    public void Update()
    {
        if (_localRouteStarted && _splineContainer)
        {
            // Determine the target progress to interpolate toward.
            // Authority: _localSplineProgress (advanced at tick rate in FixedUpdateNetwork).
            // Non-authority: networkedSplineProgress (arrives from network at tick rate).
            // Both need smooth interpolation because ticks are lower Hz than rendering.
            var targetProgress = (Object && Runner && HasStateAuthority)
                ? _localSplineProgress
                : networkedSplineProgress;

            const float lerpSpeed = 12f;
            _renderSplineProgress = Mathf.Lerp(_renderSplineProgress, targetProgress, Time.deltaTime * lerpSpeed);

            if (Mathf.Abs(_renderSplineProgress - targetProgress) < 0.0001f)
                _renderSplineProgress = targetProgress;

            SplineHelper.Evaluate(_splineContainer, _renderSplineProgress, out var pos, out var rot);
            transform.SetPositionAndRotation(pos, rot);
        }

        if (!Object || !Runner)
            return;

        CheckPlayerAABB();
        UpdateWaitingRoomText();
    }

    private void UpdateWaitingRoomText()
    {
        if (_playerCountText == null && _countdownText == null)
            return;

        _gameLoopService ??= ServiceLocator.Get<GameLoopService>();
        var waitingZoneManager = _gameLoopService?.WaitingZoneManager;
        if (waitingZoneManager == null)
        {
            SetWaitingRoomTextVisible(false, false);
            return;
        }

        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        var playerCount = _gorillaService?.GorillaCount ?? 0;
        var requiredPlayers = waitingZoneManager.Runner != null
            && (waitingZoneManager.Runner.IsSinglePlayer || waitingZoneManager.Runner.GameMode == Fusion.GameMode.Single)
            ? 1
            : waitingZoneManager.RequiredPlayers;

        var phase = waitingZoneManager.Phase;
        var hasEnoughPlayers = playerCount >= requiredPlayers;
        var remainingSeconds = -1;
        var showPlayerCount = false;
        var showCountdown = false;
        switch (phase)
        {
            case WaitingZonePhase.PreGameLobby:
                showPlayerCount = true;
                break;

            case WaitingZonePhase.Countdown:
                showPlayerCount = true;
                if (hasEnoughPlayers && waitingZoneManager.CurrentTransitionTime != 0)
                {
                    remainingSeconds = Mathf.Max(0, Mathf.CeilToInt((float)(waitingZoneManager.TransitionTime - SyncedTime.Now).TotalSeconds));
                    showCountdown = true;
                }
                break;
        }

        SetWaitingRoomTextVisible(showPlayerCount, showCountdown);

        if (showPlayerCount && _playerCountText != null
            && (phase != _lastWaitingZonePhase || playerCount != _lastPlayerCount || requiredPlayers != _lastRequiredPlayers))
        {
            _playerCountText.text = phase == WaitingZonePhase.PreGameLobby && playerCount < requiredPlayers
                ? WaitingForPlayersText
                : $"{playerCount}/{requiredPlayers}";
        }

        if (showCountdown && _countdownText != null && remainingSeconds != _lastRemainingSeconds)
            _countdownText.text = $"{remainingSeconds / 60:D2}:{remainingSeconds % 60:D2}";

        _lastWaitingZonePhase = phase;
        _lastPlayerCount = playerCount;
        _lastRequiredPlayers = requiredPlayers;
        _lastRemainingSeconds = remainingSeconds;
    }

    private void SetWaitingRoomTextVisible(bool showPlayerCount, bool showCountdown)
    {
        if (_playerCountText != null && _showingPlayerCountText != showPlayerCount)
        {
            _playerCountText.gameObject.SetActive(showPlayerCount);
            _showingPlayerCountText = showPlayerCount;
        }

        if (_countdownText != null && _showingCountdownText != showCountdown)
        {
            _countdownText.gameObject.SetActive(showCountdown);
            _showingCountdownText = showCountdown;
        }
    }

    private void EjectAllPlayers()
    {
        _playerHasJumped = true;
        _wasInsideLastFrame = false;
        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
    }

    private void CheckPlayerAABB()
    {
        if (_playerHasJumped)
            return;

        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        var rig = LocalHardwareRig;
        if (rig == null )
            return;

        var inside = IsInsideInterior(rig.Headset.HeadsetTransform.position);
        if (!_aabbWarmedUp)
        {
            _wasInsideLastFrame = inside;
            _aabbWarmedUp = true;
            return;
        }

        if (_wasInsideLastFrame && !inside && doorsOpen)
        {
            _playerHasJumped = true;
        }

        _wasInsideLastFrame = inside;
    }

    public bool IsInsideInterior(Vector3 worldPos)
    {
        var local = transform.InverseTransformPoint(worldPos);
        var half = _interiorSize * 0.5f;
        var c = _interiorCenter;

        return local.x >= c.x - half.x && local.x <= c.x + half.x
            && local.y >= c.y - half.y && local.y <= c.y + half.y
            && local.z >= c.z - half.z && local.z <= c.z + half.z;
    }

    void OnRouteStartedChanged()
    {
        if (routeStarted && !_localRouteStarted)
        {
            InitLocalRouteState();
        }
    }

    public void InitLocalRouteState()
    {
        if (_localRouteStarted) return;

        if (_splineContainer == null)
        {
            Debug.LogWarning("SubwayDropVehicle: InitLocalRouteState — no SplineContainer found");
            return;
        }

        SplineHelper.Evaluate(_splineContainer, 0f, out Vector3 startPos, out Quaternion startRot);
        transform.SetPositionAndRotation(startPos, startRot);

        _renderSplineProgress = 0f;
        // Set the state flag BEFORE invoking subscribers so state is consistent across
        // clients even if a subscriber throws. Otherwise a thrown subscriber would unwind
        // into Fusion's RPC receive path and leave _localRouteStarted=false on that client.
        _localRouteStarted = true;

        try
        {
            RouteStarted?.Invoke();
        }
        catch (Exception ex)
        {
            GameLogger.Error($"[SubwayDropVehicle] RouteStarted subscriber threw: {ex}");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_StartRoute()
    {
        InitLocalRouteState();
    }

    // OnTChanged intentionally omitted: non-authority clients smooth-lerp
    // toward networkedSplineProgress in Update(), so no per-tick callback is needed.

    void OnDoorsOpenChanged()
    {
        OnDoorsStateChanged?.Invoke(doorsOpen);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(_interiorCenter, _interiorSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_interiorCenter, _interiorSize);
        Gizmos.matrix = old;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(_interiorCenter, _interiorSize);
        Gizmos.matrix = old;

        // Draw drop zone markers on the spline
        if (_splineContainer != null)
        {
            const float gizmoSphereRadius = 2f;

            // Drop zone START (green)
            SplineHelper.Evaluate(_splineContainer, _dropZoneStartProgress, out Vector3 startPos, out _);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPos, gizmoSphereRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(startPos + Vector3.up * 3f, $"DropZone START (T={_dropZoneStartProgress:F2})");
#endif

            // Drop zone END (red)
            SplineHelper.Evaluate(_splineContainer, _dropZoneEndProgress, out Vector3 endPos, out _);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPos, gizmoSphereRadius);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(endPos + Vector3.up * 3f, $"DropZone END (T={_dropZoneEndProgress:F2})");
#endif

            // Draw line between start and end along the spline (sampled)
            Gizmos.color = Color.yellow;
            const int gizmoSplineSegments = 20;
            var tRange = _dropZoneEndProgress - _dropZoneStartProgress;
            for (var i = 0; i < gizmoSplineSegments; i++)
            {
                var t0 = _dropZoneStartProgress + tRange * (i / (float)gizmoSplineSegments);
                var t1 = _dropZoneStartProgress + tRange * ((i + 1) / (float)gizmoSplineSegments);
                SplineHelper.Evaluate(_splineContainer, t0, out var p0, out _);
                SplineHelper.Evaluate(_splineContainer, t1, out var p1, out _);
                Gizmos.DrawLine(p0, p1);
            }
        }
    }

    private void Cleanup()
    {
        if (_wasCleaned)
            return;

        _wasCleaned = true;

        _gameLoopService?.Unregister(this);
        GameServices.BlimpExists = null;
        GameServices.IsInBlimp = null;
        GameServices.GetBlimpTransform = null;
        GameServices.GetBlimpIsInBlimp = null;
        GameServices.OpenBlimpDoors = null;
        GameServices.CloseBlimpDoors = null;
    }

    private void OnDestroy() => Cleanup();

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        Cleanup();
    }
}
