using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;

namespace Teabag.GameMode
{
    /// <summary>
    /// Project-side wiring for <see cref="MovingPlatformLocomotion"/>. Lives on the JungleRig (XR)
    /// prefab. At runtime it:
    ///   • Locates the rig's <see cref="LocomotionController"/> and pulls out the
    ///     <see cref="MovingPlatformLocomotion"/> + <see cref="ClimbingLocomotion"/> modules.
    ///   • Supplies the platform-override resolver from <see cref="SpaceStationManager.LocalWagonTransform"/>
    ///     (so a player whose head is inside a wagon's <c>TrainWagonZone</c> still inherits the
    ///     wagon's translation/yaw even with no foot-collider hit).
    ///   • Supplies the freeze predicate by inspecting the rig's Rigidbody kinematic flag
    ///     (so the platform follow goes silent whenever external code — e.g. a mode manager's
    ///     spawn/teleport hold — has taken ownership of the rig).
    ///
    /// The Jungle XRKit avatar package stays free of project types — all coupling lives here.
    /// </summary>
    [DefaultExecutionOrder(-50)] // After LocomotionController.Awake (-5? actually it's -5), before gameplay.
    public class JungleRigPlatformLink : MonoBehaviour
    {
        [Tooltip("Optional explicit reference. If null we GetComponentInChildren at startup.")]
        [SerializeField] private LocomotionController _locomotionController;

        private MovingPlatformLocomotion _platformModule;
        private GameLoopService _gameLoopService;

        private void Awake()
        {
            if (_locomotionController == null)
            {
                _locomotionController = GetComponentInChildren<LocomotionController>(true);
            }

            if (_locomotionController == null)
            {
                Debug.LogWarning("[JungleRigPlatformLink] No LocomotionController found on rig — moving-platform follow will be inactive.");
                return;
            }

            _locomotionController.GetLocomotionModule<MovingPlatformLocomotion>(out _platformModule);
            if (_platformModule == null)
            {
                // Module not registered on this rig — that's fine, link does nothing.
                return;
            }

            _locomotionController.GetLocomotionModule<ClimbingLocomotion>(out var climbing);
            _platformModule.ClimbingModule = climbing;

            _platformModule.OverridePlatformResolver = ResolveBoardedWagon;
            _platformModule.ExternalFreezeProvider = ResolveLocalRigFrozen;
        }

        private void OnDestroy()
        {
            if (_platformModule == null) return;
            _platformModule.OverridePlatformResolver = null;
            _platformModule.ExternalFreezeProvider = null;
            _platformModule.ClimbingModule = null;
        }

        private GameLoopService GameLoop
        {
            get
            {
                if (_gameLoopService != null) return _gameLoopService;
                ServiceLocator.TryGet(out _gameLoopService);
                return _gameLoopService;
            }
        }

        private Transform ResolveBoardedWagon()
        {
            var spaceStation = GameLoop?.SpaceStationManager;
            return spaceStation != null ? spaceStation.LocalWagonTransform : null;
        }

        private bool ResolveLocalRigFrozen()
        {
            // Any external kinematic hold (SpaceStationManager.TryHoldRig during spawn teleport,
            // future mode-manager freezes) shows up as Rigidbody.isKinematic == true. Reading
            // that flag directly keeps this link decoupled from specific mode managers and
            // automatically covers every freeze path without per-manager wiring.
            var rb = _locomotionController != null ? _locomotionController.PlayerRigidbody : null;
            return rb != null && rb.isKinematic;
        }
    }
}
