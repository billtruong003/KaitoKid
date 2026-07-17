using Player.Config;
using UnityEngine;

namespace Shmackle.Player.Headlight
{
    /// <summary>
    /// This is for the flashlight on the player's head. It will turn on and off based on the config
    /// and it will be toggled by the FlashlightTrigger script.
    /// This will only work on the local player.
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] private Light _flashlightLight;
        [SerializeField] private FlashlightConfig _flashlightConfig;

        private bool _isFlashlightEnabled;
        private float _flashlightIntensity;
        private float _flashlightRange;

        private void Awake()
        {
            OnConfigUpdate();
        }

        private void OnEnable()
        {
            if (_flashlightConfig)
                _flashlightConfig.ConfigUpdated += OnConfigUpdate;
        }

        private void OnDisable()
        {
            if (_flashlightConfig)
                _flashlightConfig.ConfigUpdated -= OnConfigUpdate;
        }

        private void OnConfigUpdate()
        {
            if (!_flashlightConfig || !_flashlightLight)
                return;

            _flashlightIntensity = _flashlightConfig.FlashlightIntensity;
            _isFlashlightEnabled = _flashlightConfig.IsFlashlightEnabled;
            _flashlightRange = _flashlightConfig.FlashlightRange;

            ApplyLightValue();
        }

        /// <summary>
        /// Apply the current light values
        /// </summary>
        private void ApplyLightValue()
        {
            _flashlightLight.gameObject.SetActive(_isFlashlightEnabled);
            _flashlightLight.intensity = _flashlightIntensity;
            _flashlightLight.range = _flashlightRange;
        }
        
        /// <summary>
        /// Force the flashlight to a specific state.
        /// </summary>
        public void ToggleLight(bool isToggled)
        {
            if (!_flashlightLight)
                return;

            _isFlashlightEnabled = isToggled;
            ApplyLightValue();
        }


    }
}