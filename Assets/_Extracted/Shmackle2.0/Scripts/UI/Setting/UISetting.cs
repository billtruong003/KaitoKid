using Player.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.UI
{
    /// <summary>
    /// Handles user interface controls for locomotion settings, including smooth turning,
    /// snap turning, and turning speed presets.
    /// </summary>
    public class UISetting : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private TurningConfig _turningConfig;

        [Header("Smooth Turning")]
        [SerializeField] private TMP_Text _smoothTurningValueText;
        [SerializeField] private Button _smoothTurningButton;
      

        [Header("Snap Turning")]
        [SerializeField] private TMP_Text _snapTurningValueText;
        [SerializeField] private Button _snapTurningButton;
        

        [Header("Turning Speed")]
        [SerializeField] private Slider _turningSpeedSlider;
        [SerializeField] private TMP_Text _turningSpeedValueText;
        
        private int _turningSpeed;
        private bool _isSmoothTurningEnabled;
        private bool _isSnapTurningEnabled;
          
        private const string ON_TEXT = "ON";
        private const string OFF_TEXT = "OFF";
        private const string SLOW_TEXT = "SLOW";
        private const string MEDIUM_TEXT = "MEDIUM";
        private const string FAST_TEXT = "FAST";

        private void Awake()
        {
            if (_smoothTurningButton)
                _smoothTurningButton.onClick.AddListener(OnSmoothTurningButtonClicked);

            if (_snapTurningButton)
                _snapTurningButton.onClick.AddListener(OnSnapTurningButtonClicked);

            if (_turningSpeedSlider)
                _turningSpeedSlider.onValueChanged.AddListener(OnTurningSpeedChanged);
            
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
            if (_turningConfig)
            {
                _isSmoothTurningEnabled = _turningConfig.IsSmoothTurningEnabled;
                _isSnapTurningEnabled = _turningConfig.IsSnapTurningEnabled;
                _turningSpeed = _turningConfig.TurningPresets;
            }

            RefreshUI();
        }

        /// <summary>
        /// Toggle the snap turning and disabling smooth if smooth is enabled.
        /// </summary>
        private void OnSnapTurningButtonClicked()
        {
            // Toggle snap turning
            var newValue = !_isSnapTurningEnabled;
            SetSnapTurning(newValue);
        }

        /// <summary>
        /// Toggle the smooth turning and disabling snap if snap is enabled.
        /// </summary>
        private void OnSmoothTurningButtonClicked()
        {
            // Toggle smooth turning
            var newValue = !_isSmoothTurningEnabled;
            SetSmoothTurning(newValue);
        }

        /// <summary>
        /// Update the turning speed slider value and update the locomotion config.
        /// </summary>
        /// <param name="value"></param>
        private void OnTurningSpeedChanged(float value)
        {
            _turningSpeed = (int)value;
            UpdateLocomotionConfig();
        }
        
        private void SetSnapTurning(bool enabled)
        {
            _isSnapTurningEnabled = enabled;

            if (enabled)
                _isSmoothTurningEnabled = false; // Only one function can be active

            RefreshUI();
            UpdateLocomotionConfig();
        }
        
        private void SetSmoothTurning(bool enabled)
        {
            _isSmoothTurningEnabled = enabled;

            if (enabled)
                _isSnapTurningEnabled = false; // Mutually exclusive

            RefreshUI();
            UpdateLocomotionConfig();
        }

        /// <summary>
        /// Updates visual UI elements to match current settings.
        /// </summary>
        private void RefreshUI()
        {
            _snapTurningValueText.text = _isSnapTurningEnabled ? ON_TEXT : OFF_TEXT;
            _smoothTurningValueText.text = _isSmoothTurningEnabled ? ON_TEXT : OFF_TEXT;

            if (_turningSpeedSlider)
                _turningSpeedSlider.value = _turningSpeed;

            // Convert preset number to readable label
            _turningSpeedValueText.text = _turningSpeed switch
            {
                1 => SLOW_TEXT,
                2 => MEDIUM_TEXT,
                3 => FAST_TEXT,
                _ => _turningSpeed.ToString()
            };
        }

        /// <summary>
        /// Pushes the updated values back to the locomotion config
        /// </summary>
        private void UpdateLocomotionConfig()
        {
            if (!_turningConfig)
                return;

            _turningConfig.IsSmoothTurningEnabled = _isSmoothTurningEnabled;
            _turningConfig.IsSnapTurningEnabled = _isSnapTurningEnabled;
            _turningConfig.TurningPresets = _turningSpeed;
            
            _turningConfig.ConfigUpdated.Invoke();
        }
    }
}
