using Fusion.XR.Shared.Base;
using Shmackle.Interaction;
using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Handles the climbing mechanics by detecting grippable surfaces and anchoring the player's hands to them.
    /// It interfaces with <see cref="PlayerLocomotion"/> to override hand positions and manage physics state during climbing.
    /// </summary>
    public class ClimbController : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion _playerLocomotion;
        
        [Header("Climbing Settings")]
        [SerializeField] private float _gripRadius = 0.08f;
        [SerializeField] private LayerMask _gripLayers = ~0;

        [Header("Controller References")]
        [SerializeField] private HardwareController _leftController;
        [SerializeField] private HardwareController _rightController;

        private bool _previousLeftGrabbing;
        private bool _previousRightGrabbing;

        private bool _isLeftGripping;
        private bool _isRightGripping;
        private Transform _leftGripSurface;
        private Transform _rightGripSurface;

        // Local anchor offsets
        private Vector3 _leftAnchorOffset;
        private Vector3 _rightAnchorOffset;
        private bool _wasKinematicBeforeGrip;

        /// <summary>
        /// Gets a value indicating whether the player is currently gripping a surface with at least one hand.
        /// </summary>
        private bool IsClimbing => _isLeftGripping || _isRightGripping;

        private void Reset()
        {
            if (_playerLocomotion == null) _playerLocomotion = GetComponent<PlayerLocomotion>();
        }

        private void Update()
        {
            if (_playerLocomotion == null) return;

            // Sync Anchor Positions
            if (_isLeftGripping && _leftGripSurface != null)
                _playerLocomotion.LeftHandAnchorPosition = _leftGripSurface.TransformPoint(_leftAnchorOffset);
            else
                _playerLocomotion.LeftHandAnchorPosition = null;

            if (_isRightGripping && _rightGripSurface != null)
                _playerLocomotion.RightHandAnchorPosition = _rightGripSurface.TransformPoint(_rightAnchorOffset);
            else
                _playerLocomotion.RightHandAnchorPosition = null;

            HandleHand(true, _leftController, ref _previousLeftGrabbing, ref _isLeftGripping, ref _leftGripSurface);
            HandleHand(false, _rightController, ref _previousRightGrabbing, ref _isRightGripping, ref _rightGripSurface);
        }

        /// <summary>
        /// Processes hand input and updates gripping state for a specific hand.
        /// </summary>
        private void HandleHand(bool isLeft, HardwareController controller, ref bool previousGrabbing, ref bool isGripping, ref Transform gripSurface)
        {
            bool isGrabbing = controller.IsGrabbing;

            // State change detection
            if (isGrabbing != previousGrabbing)
            {
                if (isGrabbing)
                {
                    // Try to grab
                    TryGrip(isLeft, ref isGripping, ref gripSurface);
                }
                else
                {
                    // Release
                    if (isGripping)
                    {
                        ReleaseGrip(isLeft, ref isGripping, ref gripSurface);
                    }
                }
                previousGrabbing = isGrabbing;
            }

            // Safety check: if we are gripping but the surface is gone (destroyed)
            if (isGripping && gripSurface == null)
            {
                 ReleaseGrip(isLeft, ref isGripping, ref gripSurface, false);
            }
        }

        /// <summary>
        /// Attempts to find and grip a surface near the hand's current position.
        /// </summary>
        private void TryGrip(bool isLeft, ref bool isGripping, ref Transform gripSurface)
        {
            if (isGripping) return;

            Vector3 handPos = isLeft ? _playerLocomotion.CurrentLeftHandPosition : _playerLocomotion.CurrentRightHandPosition;
            
            if (TryFindSurface(handPos, out var surface))
            {
                // Enforce single-hand grip: Release the other hand if it's gripping
                if (isLeft && _isRightGripping)
                {
                    ReleaseGrip(false, ref _isRightGripping, ref _rightGripSurface, false);
                }
                else if (!isLeft && _isLeftGripping)
                {
                    ReleaseGrip(true, ref _isLeftGripping, ref _leftGripSurface, false);
                }

                // If we were not climbing before, start climbing logic
                if (!IsClimbing) 
                {
                     if (_playerLocomotion.Rigidbody != null)
                     {
                        _wasKinematicBeforeGrip = _playerLocomotion.Rigidbody.isKinematic;
                        _playerLocomotion.Rigidbody.isKinematic = true;
                        _playerLocomotion.Rigidbody.linearVelocity = Vector3.zero;
                        _playerLocomotion.Rigidbody.angularVelocity = Vector3.zero;
                     }
                }

                isGripping = true;
                gripSurface = surface;

                // Calculate offset
                Vector3 localOffset = surface.InverseTransformPoint(handPos);
                if (isLeft) _leftAnchorOffset = localOffset;
                else _rightAnchorOffset = localOffset;

                // Set Anchor (Immediate update for this frame)
                if (isLeft) _playerLocomotion.LeftHandAnchorPosition = surface.TransformPoint(localOffset);
                else _playerLocomotion.RightHandAnchorPosition = surface.TransformPoint(localOffset);
            }
        }

        /// <summary>
        /// Releases the current grip and restores locomotion state if no hands are left gripping.
        /// </summary>
        private void ReleaseGrip(bool isLeft, ref bool isGripping, ref Transform gripSurface, bool applyThrow = true)
        {
            isGripping = false;
            gripSurface = null;
            
            if (isLeft) _playerLocomotion.LeftHandAnchorPosition = null;
            else _playerLocomotion.RightHandAnchorPosition = null;

            if (!IsClimbing)
            {
                // We just stopped climbing
                Vector3 releaseVelocity = Vector3.zero;
                if (_playerLocomotion.Rigidbody != null && applyThrow)
                {
                    releaseVelocity = ComputeReleaseVelocity();
                }

                if (_playerLocomotion.Rigidbody != null)
                {
                    _playerLocomotion.Rigidbody.isKinematic = _wasKinematicBeforeGrip;
                    if (applyThrow && !_playerLocomotion.Rigidbody.isKinematic && releaseVelocity != Vector3.zero)
                    {
                        _playerLocomotion.Rigidbody.linearVelocity = releaseVelocity;
                    }
                }
            }
        }

        /// <summary>
        /// Computes the velocity impulse to apply when releasing a grip, based on recent hand movement.
        /// </summary>
        private Vector3 ComputeReleaseVelocity()
        {
             Vector3 avg = _playerLocomotion.AverageVelocity;
             if (avg.magnitude <= _playerLocomotion.VelocityLimit)
                 return Vector3.zero;
     
             Vector3 desired = _playerLocomotion.JumpMultiplier * avg;
             if (desired.magnitude > _playerLocomotion.MaxJumpSpeed)
                 desired = desired.normalized * _playerLocomotion.MaxJumpSpeed;
             return desired;
        }

        /// <summary>
        /// Performs a sphere overlap check to find a <see cref="GrippableSurface"/> near the specified position.
        /// </summary>
        private bool TryFindSurface(Vector3 position, out Transform surface)
        {
            surface = null;
            var overlaps = Physics.OverlapSphere(position, _gripRadius, _gripLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                var s = overlaps[i].GetComponentInParent<GrippableSurface>();
                if (s != null)
                {
                    surface = s.transform;
                    return true;
                }
            }
            return false;
        }
    }
}
