using UnityEngine;
using Fusion.XR.Shared.Core;
using Player.Config;

namespace Shmackle.Player.Turning
{
    /// <summary>
    /// Handles smooth and snap turning behavior for the local player based on joystick input.
    /// </summary>
    public class PlayerTurningController : MonoBehaviour
    {
        [SerializeField] private TurningConfig _turningConfig;

        [Header("Turning Settings")]
        private bool _isSmoothTurning;
        private bool _isSnapTurning;
        private bool _snapOnCooldown;
        private int _turningSpeed = 0;
        private int _turningPreset;

        private ILateralizedRigPart _rigPart;
        private PlayerLocomotion _playerLocomotion;
        private LocalJoystickTracker _localJoystickTracker;
        private Vector2 _cachedStick;

        private void Awake()
        {
            if (_rigPart == null)
            {
                _rigPart = GetComponentInParent<ILateralizedRigPart>();
                _localJoystickTracker = new LocalJoystickTracker(_rigPart);
            }

            if (!_playerLocomotion)
            {
                var rig = GetComponentInParent<IRig>();
                if (rig != null)
                    _playerLocomotion = rig.transform.GetComponentInChildren<PlayerLocomotion>();
            }

            OnConfigUpdate();
        }

        private void OnEnable()
        {
            if (_turningConfig)
                _turningConfig.ConfigUpdated += OnConfigUpdate;
        }

        private void OnDisable()
        {
            if (_turningConfig)
                _turningConfig.ConfigUpdated -= OnConfigUpdate;
        }

        private void OnConfigUpdate()
        {
            if (!_turningConfig)
                return;
            
            _turningPreset = _turningConfig.TurningPresets;
            _isSmoothTurning = _turningConfig.IsSmoothTurningEnabled;
            _isSnapTurning = _turningConfig.IsSnapTurningEnabled;

            // NOTE (Russ): This turning speed value is used for both smooth and snap turning.
            // The turning preset is temporary and will be removed later.
            _turningSpeed = _turningPreset switch
            {
                1 => _turningConfig.SlowTurningSpeed,
                2 => _turningConfig.MediumTurningSpeed,
                3 => _turningConfig.FastTurningSpeed,
                _ => _turningSpeed
            };
        }
        
        private void Update()
        {
            if (!_playerLocomotion || _localJoystickTracker == null)
                return;

            var stick = _localJoystickTracker.ReadValue<Vector2>();

            if (stick.HasValue)
                _cachedStick = stick.Value;
            else
                _cachedStick = Vector2.zero;

            if (_isSmoothTurning)
                ApplySmoothTurning(_cachedStick);
            else if (_isSnapTurning)
                ApplySnapTurning(_cachedStick);
        }


        /// <summary>
        /// Applies analog smooth rotation based on joystick input magnitude.
        /// Rotation scales with turning speed and time.
        /// </summary>
        private void ApplySmoothTurning(Vector2 stick)
        {
            float xInput = stick.x;

            // Ensuring tiny stick jitters do not rotate the player
            // (e.g. when the joystick is pushed slightly but not enough to trigger a snap)
            if (Mathf.Abs(xInput) < 0.1f)
                return;

            float baseTurnRate = 45f;
            float rotationAmount = xInput * _turningSpeed * baseTurnRate * Time.deltaTime;

            _playerLocomotion.Turn(rotationAmount);
        }

        /// <summary>
        /// Applies discrete snap turns when joystick passes threshold.
        /// </summary>
        private void ApplySnapTurning(Vector2 stick)
        {
            float xInput = stick.x;
            
            if (!_snapOnCooldown)
            {
                if (xInput > 0.7f)
                {
                    // Normalize turning speed from range 6–15 into 0–1
                    float normalizedTurningSpeed  = (_turningSpeed - 6f) / 9f;
                    
                    // Convert normalized value into a snap angle between 9° and 90°
                    float snapAngle = Mathf.Lerp(9f, 90f, normalizedTurningSpeed );

                    // Apply the snap rotation
                    _playerLocomotion.Turn(snapAngle);
                    _snapOnCooldown = true;
                }
                else if (xInput < -0.7f)
                {
                    float normalizedTurningSpeed  = (_turningSpeed - 6f) / 9f;
                    float snapAngle = Mathf.Lerp(9f, 90f, normalizedTurningSpeed );

                    _playerLocomotion.Turn(-snapAngle);
                    _snapOnCooldown = true;
                }
            }
            if (Mathf.Abs(xInput) < 0.3f)
                _snapOnCooldown = false;
        }
    }

}
