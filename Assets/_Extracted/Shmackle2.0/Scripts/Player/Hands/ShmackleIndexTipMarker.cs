using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Core.Interaction;
using UnityEngine;

namespace Shmackle.Player.Hands
{
    /// <summary>
    /// Represents the user's index fingertip for UI interaction in XR.
    /// Handles when interaction can begin, how far the system scans for UI targets,
    /// how deep the fingertip can maintain a press, and whether the fingertip's rig
    /// position visually shifts while maintaining interaction depth.
    /// </summary>
    public class ShmackleIndexTipMarker : MonoBehaviour, IInteractionTip, IRigPartPositionModifier
    {
        [Header("UI Interaction option (with XSCInputModule)")]
        [SerializeField]
        [Tooltip("Maximum distance from the fingertip to start interacting")]
        private float _maxStartInteractionDistance = 0.02f; // Lower value: user must be very close or precise to trigger interaction.

        [SerializeField]
        [Tooltip("Maximum distance from the fingertip to scan for interaction targets")]
        private float _maxInteractionScanDistance = 0.12f; // Lower value: detection happens only close to the fingertip, more precise.

        [SerializeField]
        [Tooltip("Maximum depth to maintain interaction target while interacting")]
        private float _maxMaintainInteractionDepth = 0.12f; // Lower value: interaction breaks sooner if the user overshoots the UI surface.

        [SerializeField]
        [Tooltip("Modify rig part position on interaction depth change")]
        private bool _modifyRigPartPositionOnMaintainedInteraction = true;

        private IRigPart _rigPart;

        // Cached transforms for performance (prevents repeated native Unity calls per frame)
        private Transform _indexTransform;
        private Transform _targetTransform;

        // Cached values updated once per frame
        private Vector3 _indexOrigin;
        private Quaternion _indexRotation;

        private float _maxStartSqr;

        public float MaxStartInteractionDistance => _maxStartInteractionDistance;
        public float MaxInteractionScanDistance => _maxInteractionScanDistance;
        public float MaxMaintainInteractionDepth => _maxMaintainInteractionDepth;
        public IRigPart RigPart => _rigPart;

        public bool CanInteract =>
            (RigPart is not IHardwareRigPart hrp) || hrp.TrackingStatus == RigPartTrackingstatus.Tracked;

        public bool IsSelecting
        {
            get
            {
                // Cannot select if the fingertip is not being tracked
                if (!CanInteract) 
                    return false;

                if (LastInteractionDetailProvider == null)
                    return false;

                // If we are already interacting and maintaining depth, stay in selecting state
                if (LastInteractionDetailProvider.IsMaintainedInteraction)
                    return true;

                // Calculate the allowed selection radius squared
                float maxSqr = _maxStartSqr;

                // Select only if fingertip is close enough to the last hit position
                return (LastInteractionDetailProvider.LastInteractionWorldPosition - _indexOrigin).sqrMagnitude <= maxSqr;
            }
        }

        public Vector3 Origin => _indexOrigin;
        public Quaternion Rotation => _indexRotation;
        public virtual Vector2 ScrollDelta => Vector2.zero;

        public IInteractionDetailsProvider LastInteractionDetailProvider { get; set; } = null;

        public Vector3 PositionOffset
        {
            get
            {
                if (_modifyRigPartPositionOnMaintainedInteraction && LastInteractionDetailProvider != null && LastInteractionDetailProvider.IsMaintainedInteraction && _targetTransform != null)
                {
                    return -LastInteractionDetailProvider.MaintainDepth * _targetTransform.forward;
                }

                return Vector3.zero;
            }
        }

        private void Awake()
        {
            _rigPart = GetComponentInParent<IRigPart>();
            _indexTransform = transform;
            // Precompute squared interaction start distance
            _maxStartSqr = _maxStartInteractionDistance * _maxStartInteractionDistance;
        }

        private void Update()
        {
            // Cache position & rotation
            _indexOrigin = _indexTransform.position;
            _indexRotation = _indexTransform.rotation;

            // Cache target transform reference once instead of per-frame property lookups
            _targetTransform = LastInteractionDetailProvider?.Target?.transform;
        }
    }
}
