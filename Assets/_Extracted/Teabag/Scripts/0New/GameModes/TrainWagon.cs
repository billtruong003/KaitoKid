using System;
using System.Collections.Generic;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

/// <summary>
/// Networked wagon controller. Placed on wagon child GameObjects under the train root.
/// Each wagon has a fixed match type (FFA/Duo/Squads).
///
/// Unlike the old ShuttleDock, wagons do NOT drive their own animation or countdown.
/// TrainController drives the entire train along a spline; wagons move via parenting.
/// Wagons only track boarding count and visual state (doors, model, UI).
///
/// Door visuals are handled by WagonDoor components that subscribe to OnDoorsStateChanged.
///
/// Movement is pure transform-based (SetPositionAndRotation). Player following is handled
/// by the gorilla locomotion GorillaTouchingTarget surface velocity system.
/// </summary>
public class TrainWagon : NetworkBehaviour, IDoorProvider
{
    [Header("Wagon Identity")]
    [SerializeField] private int _wagonIndex;
    [SerializeField] private MatchType _matchType;

    public int WagonIndex => _wagonIndex;
    public MatchType WagonMatchType => _matchType;

    [Networked]
    public int BoardedCount { get; set; }

    [Networked, OnChangedRender(nameof(OnDoorsOpenChanged))]
    public NetworkBool DoorsOpen { get; set; }

    /// <summary>
    /// Short (4-char) code written by the state authority when this wagon begins
    /// departure with at least one boarder. All passengers use this to build the
    /// same WaitingZone session name so they end up in the same room instead of
    /// racing FillRoom with a null SessionName. Empty while the wagon is idle.
    /// </summary>
    [Networked]
    public NetworkString<_8> DepartureSessionCode { get; set; }

    [Header("Visual References")]
    [SerializeField] private GameObject _model;
    [SerializeField] private List<TextMeshPro> _gameModeText;

    [Header("Spline Offset")]
    [Tooltip("Distance in meters behind the lead wagon (index 0) along the spline. Wagon 0 should be 0.")]
    [SerializeField] private float _distanceOffset;
    [Tooltip("Additional Y-axis rotation offset in degrees applied on top of the spline orientation.")]
    [SerializeField] private float _angleOffset;

    public float DistanceOffset => _distanceOffset;
    public float AngleOffset => _angleOffset;

    [Header("Door Settings")]
    [Tooltip("Initial door state when the vehicle starts. WagonDoor reads this to set the correct starting pose.")]
    [SerializeField] private bool _startDoorsOpen;

    [Header("Player Spawn Points")]
    [Tooltip("Positions inside the wagon where players are teleported on boarding arrival.")]
    [SerializeField] private Transform[] _spawnPoints;

    public bool StartDoorsOpen => _startDoorsOpen;
    public bool IsDoorsOpen => DoorsOpen;

    /// <summary>
    /// Fired on all clients when the door state changes. Parameter is true = open, false = closed.
    /// WagonDoor components subscribe to this to animate sliding.
    /// </summary>
    public event Action<bool> OnDoorsStateChanged;

    private GameLoopService _gameLoopService;

    public override void Spawned()
    {
        base.Spawned();

        _gameLoopService = ServiceLocator.Get<GameLoopService>();
        _gameLoopService?.RegisterWagon(this);

        if (_model != null)
            _model.SetActive(true);

        if (HasStateAuthority)
        {
            BoardedCount = 0;
            DoorsOpen = false;
            DepartureSessionCode = default;
        }

        UpdateGameModeText();

        // Fire door state after networked values are populated so WagonDoor
        // subscribers see the correct initial pose. OnChangedRender won't fire
        // if the replicated value matches the baseline set at spawn time.
        OnDoorsStateChanged?.Invoke(DoorsOpen);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerBoarded(PlayerRef player)
    {
        BoardedCount++;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerUnboarded(PlayerRef player)
    {
        BoardedCount = Mathf.Max(0, BoardedCount - 1);
    }

    public void SetDoorsOpen(bool open)
    {
        if (HasStateAuthority)
            DoorsOpen = open;
    }

    /// <summary>
    /// Authority-only: writes the departure session code. Callers that aren't the
    /// wagon's state authority (e.g. TrainController on a different host in odd
    /// ownership transfers) no-op here so we never silently drop the replicated
    /// write.
    /// </summary>
    public void SetDepartureSessionCode(string code)
    {
        if (HasStateAuthority)
            DepartureSessionCode = code ?? string.Empty;
    }

    public void SetVisible(bool show)
    {
        if (_model != null)
            _model.SetActive(show);
    }

    public Vector3 GetSpawnPosition(int playerIndex)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return transform.position;
        return _spawnPoints[playerIndex % _spawnPoints.Length].position;
    }

    public Quaternion GetSpawnRotation(int playerIndex)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
            return transform.rotation;
        return _spawnPoints[playerIndex % _spawnPoints.Length].rotation;
    }

    /// <summary>
    /// Set this wagon's world position and rotation from a spline evaluation.
    /// Called by TrainController each frame so each wagon follows the spline independently.
    /// </summary>
    public void ApplySplinePosition(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Instantly set position/rotation. Same as ApplySplinePosition but kept as a
    /// separate method for API clarity in TrainController snap/teleport cases.
    /// </summary>
    public void TeleportToSplinePosition(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    private void OnDoorsOpenChanged()
    {
        OnDoorsStateChanged?.Invoke(DoorsOpen);
    }

    private void UpdateGameModeText()
    {
        foreach (var gameModeText in _gameModeText)
        {
            if (!gameModeText)
                continue;

            gameModeText.text = Enum.GetName(typeof(MatchType), _matchType);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _wagonIndex switch
        {
            0 => new Color(1f, 0.9f, 0.2f, 0.5f),
            1 => new Color(0.2f, 0.4f, 1f, 0.5f),
            _ => new Color(0.2f, 1f, 0.4f, 0.5f)
        };
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        _gameLoopService?.UnregisterWagon(this);
    }
}
