using System.Collections;
using UnityEngine;
using Fusion.XR.Shared.Base;
using Player.Config;

namespace Shmackle.Player.DoubleJump
{
    public class DoubleJumpController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Transform _rigTransform; // Parent transform for hands (usually the player rig)

        [SerializeField] private PlayerLocomotion _playerLocomotion;
        [SerializeField] private DoubleJumpConfig _doubleJumpConfig;

        [Header("Controller References")]
        [SerializeField] private HardwareController _leftController;
        [SerializeField] private HardwareController _rightController;
        
        [Header("Debugging References")]
        [SerializeField] private LineDrawer _lineDrawer;

        [Header("Sensitivity Settings")]
        [SerializeField] [Tooltip("Time window in seconds to detect both grips as simultaneous")]
        private float _gripTimeWindow = 1f;
        
        [SerializeField] [Tooltip("Delay before double jump is triggered")] 
        private float _delaySeconds = 0.1f;

        private float _velocityMultiplier;
        private float _handAlignmentThreshold;
        private bool _wasGrippingBothLastFrame = false;
        private bool _hasUsedDoubleJump = false;
        private bool _wasLeftGripping = false;
        private bool _wasRightGripping = false;
        private float _leftGripPressTime = -1f;
        private float _rightGripPressTime = -1f;

        private Vector3 _lastRightPosition;
        private Vector3 _lastLeftPosition;
        private float _handVelocity;
        private float _handAlignment;
        private float _handSpeed;
        private bool _isCurrentlyGrounded;
        private bool _canGenerateLineSegment = false;
        
        public float HandSpeed => _handSpeed;
        public float HandVelocity => _handVelocity;
        public bool IsCurrentlyGrounded => _isCurrentlyGrounded;
        public float HandAlignment => _handAlignment;

        public void OnEnable()
        {
            if (!_doubleJumpConfig)
                return;
            
            _doubleJumpConfig.ConfigUpdated += OnConfigUpdate;
        }

        public void OnDisable()
        {
            if (!_doubleJumpConfig)
                return;
            
            _doubleJumpConfig.ConfigUpdated -= OnConfigUpdate;
        }

        private void Awake()
        {
            OnConfigUpdate();
        }

        private void OnConfigUpdate()
        {
            if (!_doubleJumpConfig)
                return;
            
            _canGenerateLineSegment = _doubleJumpConfig.CanGenerateLineSegment;
            _handAlignmentThreshold = _doubleJumpConfig.HandAlignmentThreshold;
            _velocityMultiplier = _doubleJumpConfig.DoubleJumpVelocityMultiplier;
        }

        private void Update()
        {
            if (!_leftController || !_rightController)
                return;
            
            CheckGroundedStateForReset();
            CheckBothGripsTrigger();
        }
        
        private void CheckGroundedStateForReset()
        {
            if (!_playerLocomotion || !_doubleJumpConfig)
                return;
            
            _isCurrentlyGrounded = _playerLocomotion.IsGrounded;

            // Reset double jump when player lands to a solid ground
            if (_isCurrentlyGrounded)
                _hasUsedDoubleJump = false;
        }

        private void CheckBothGripsTrigger()
        {
            if (!_rigTransform || !_playerLocomotion || !_doubleJumpConfig)
                return;

            bool leftGripping = _leftController.IsGrabbing;
            bool rightGripping = _rightController.IsGrabbing;

            // Detect rising edge for left grip
            if (leftGripping && !_wasLeftGripping)
                _leftGripPressTime = Time.time;

            // Detect rising edge for right grip
            if (rightGripping && !_wasRightGripping)
                _rightGripPressTime = Time.time;

            // Reset grip times when released
            if (!leftGripping)
                _leftGripPressTime = -1f;

            if (!rightGripping)
                _rightGripPressTime = -1f;

            // Check if both grips were pressed within the time window
            bool bothGripping = leftGripping && rightGripping;
            bool bothPressedRecently = _leftGripPressTime > 0 && _rightGripPressTime > 0 && Mathf.Abs(_leftGripPressTime - _rightGripPressTime) <= _gripTimeWindow;

            // Trigger when both are gripping and were pressed within time window, and haven't triggered yet
            if (bothGripping && bothPressedRecently && !_wasGrippingBothLastFrame)
            {
                // Capture local positions to avoid parent movement affecting direction
                _lastLeftPosition = _rigTransform.InverseTransformPoint(_leftController.transform.position);
                _lastRightPosition = _rigTransform.InverseTransformPoint(_rightController.transform.position);
                
                // Check if grounded
                if (!_playerLocomotion.IsGrounded || !_doubleJumpConfig.MidAirRequirement)
                {
                    if (_canGenerateLineSegment && _lineDrawer)
                    {
                        _lineDrawer.PinLeftStartPosition(_lastLeftPosition);
                        _lineDrawer.PinRightStartPosition(_lastRightPosition);
                    }
                    StartCoroutine(ApplyPushForce());
                }
               
            }

            _wasGrippingBothLastFrame = bothGripping;
            _wasLeftGripping = leftGripping;
            _wasRightGripping = rightGripping;
        }
        
        private IEnumerator ApplyPushForce()
        {
            yield return new WaitForSeconds(_delaySeconds);
            PushPlayerAccordingToHandPosition();
        }
        
        private void PushPlayerAccordingToHandPosition()
        {
            if (!_rigTransform || !_leftController || !_rightController)
                return;
            
            // Check if already used double jump in the air
            if (_hasUsedDoubleJump)
                return;
            
            // Get current hand positions in local space
            Vector3 currentLeftLocal = _rigTransform.InverseTransformPoint(_leftController.transform.position);
            Vector3 currentRightLocal = _rigTransform.InverseTransformPoint(_rightController.transform.position);
            
            // Calculate direction in local space (from last to current = hand movement direction)
            Vector3 leftDeltaLocal = _lastLeftPosition - currentLeftLocal;
            Vector3 rightDeltaLocal = _lastRightPosition - currentRightLocal;

            Vector3 leftDirection = leftDeltaLocal.normalized;
            Vector3 rightDirection = rightDeltaLocal.normalized;

            var leftDistance = leftDeltaLocal.magnitude;
            var rightDistance = rightDeltaLocal.magnitude;
         
            // Calculate how similar both hand directions are (-1 = opposite, 1 = same)
            _handAlignment = Vector3.Dot(leftDirection, rightDirection);
            
            // just show a line between the two hands 
            if (_canGenerateLineSegment && _lineDrawer)
            {
                _lineDrawer.PinLeftEndPosition(currentLeftLocal);
                _lineDrawer.PinRightEndPosition(currentRightLocal);
            }
         
            // Only push if hands are roughly pointing the same way
            if (_handAlignment < _handAlignmentThreshold)
                return;
            
            // Combine both hand directions in local space, then convert to world space
            Vector3 pushDirectionLocal = (leftDirection + rightDirection).normalized;
            Vector3 pushDirection = _rigTransform.TransformDirection(pushDirectionLocal);
            
            // Calculate average hand speed and clamp it between min and max thresholds
            var averageHandSpeed = (leftDistance + rightDistance) * 0.5f;
            
            _handSpeed = Mathf.Clamp(averageHandSpeed, 0, 1);

            if (_handSpeed < _doubleJumpConfig.HandSpeedThreshold)
                return;
            
            if (_canGenerateLineSegment && _lineDrawer)
            {
                LeftData leftData = new LeftData
                {
                    Position = currentLeftLocal,
                    LastPosition = _lastLeftPosition
                };

                RightData rightData = new RightData
                {
                    Position = currentRightLocal,
                    LastPosition = _lastRightPosition
                };
                
                _lineDrawer.GenerateCenterSegment(leftData, rightData); // if the alignment is good, draw a line between the two hands
            }
            
            var direction = pushDirection * _handSpeed;
            
            if(!_rigidbody)
                return;
            
            _rigidbody.linearVelocity = Vector3.zero;
          
             var resultForce = direction * _velocityMultiplier;
             
            _handVelocity = resultForce.magnitude;
            _rigidbody.AddForce(resultForce, ForceMode.Impulse);

            // Mark double jump as used
            _hasUsedDoubleJump = true;
        }
    }
}