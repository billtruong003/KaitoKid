using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

/// <summary>
/// AABB zone inside a train wagon that detects local player head position entry/exit.
/// Routes boarding events to SpaceStationManager or WaitingZoneManager depending on
/// which is active in the current scene.
///
/// Replaces ShuttleDockZone. Checks TrainController.trainPhase instead of per-shuttle phase.
/// </summary>
public class TrainWagonZone : MonoBehaviour
{
    [Header("Zone Identity")]
    [SerializeField] private int _wagonIndex;
    [SerializeField] private MatchType _matchType;

    [Header("Detection AABB (local space)")]
    [SerializeField] private Vector3 _boxCenter = Vector3.zero;
    [SerializeField] private Vector3 _boxSize = new Vector3(3f, 3f, 3f);

    private bool _wasInside;
    private bool _departureFired;
    private float _debugLogTimer;

    private IGorillaService _gorillaService;
    private GameLoopService _gameLoopService;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private void Update()
    {
        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        var rig = LocalHardwareRig;
        if (rig == null)
            return;

        // Check train phase for departure lockout
        _gameLoopService ??= ServiceLocator.Get<GameLoopService>();
        var train = _gameLoopService?.TrainController;
        if (train)
        {
            if (train.TrainPhase == (int)TrainPhaseType.Departing || train.TrainPhase == (int)TrainPhaseType.OffScreen)
            {
                if (!_departureFired)
                {
                    _departureFired = true;
                    _wasInside = false;
                }
                return;
            }

            if (_departureFired && train.TrainPhase == (int)TrainPhaseType.Docked)
                _departureFired = false;
        }

        var headWorld = rig.Headset.Position;
        var inside = IsInsideAABB(headWorld);

        if (inside && !_wasInside)
        {
            NotifyBoarded(_wagonIndex, (int)_matchType);
            GameLogger.Info($"[TrainWagonZone {_wagonIndex}] Player entered wagon (matchType={_matchType})");
        }
        else if (!inside && _wasInside)
        {
            NotifyUnboarded(_wagonIndex);
            GameLogger.Info($"[TrainWagonZone {_wagonIndex}] Player exited wagon");
        }

        _wasInside = inside;

        // Debug logging
        _debugLogTimer += Time.deltaTime;
        if (_debugLogTimer >= 3f)
        {
            _debugLogTimer = 0f;
            GameLogger.Info($"[TrainWagonZone {_wagonIndex}] inside={inside}, departureFired={_departureFired}");
        }
    }

    private void NotifyBoarded(int wagonIndex, int matchType)
    {
        // Try SpaceStationManager first (SpaceStation scene)
        var station = _gameLoopService?.SpaceStationManager;
        if (station)
        {
            station.OnLocalPlayerBoarded(wagonIndex, matchType);
            return;
        }

        // Try WaitingZoneManager (WaitingZone scene)
        var waitingZone = _gameLoopService?.WaitingZoneManager;
        if (waitingZone)
        {
            waitingZone.OnLocalPlayerBoarded(wagonIndex, matchType);
        }
    }

    private void NotifyUnboarded(int wagonIndex)
    {
        var station = _gameLoopService?.SpaceStationManager;
        if (station)
        {
            station.OnLocalPlayerUnboarded(wagonIndex);
            return;
        }

        var waitingZone = _gameLoopService?.WaitingZoneManager;
        if (waitingZone)
        {
            waitingZone.OnLocalPlayerUnboarded(wagonIndex);
        }
    }

    private bool IsInsideAABB(Vector3 worldPos)
    {
        var local = transform.InverseTransformPoint(worldPos);
        var half = _boxSize * 0.5f;
        var boxCenter = _boxCenter;

        return local.x >= boxCenter.x - half.x && local.x <= boxCenter.x + half.x
            && local.y >= boxCenter.y - half.y && local.y <= boxCenter.y + half.y
            && local.z >= boxCenter.z - half.z && local.z <= boxCenter.z + half.z;
    }

    private void OnDestroy()
    {
        _gorillaService = null;
        _gameLoopService = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _matchType switch
        {
            MatchType.FreeForAll => new Color(1f, 0.9f, 0.2f, 0.3f),
            MatchType.Duo => new Color(0.2f, 0.4f, 1f, 0.3f),
            _ => new Color(0.2f, 1f, 0.4f, 0.3f)
        };

        var old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(_boxCenter, _boxSize);
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.8f);
        Gizmos.DrawWireCube(_boxCenter, _boxSize);
        Gizmos.matrix = old;
    }
}
