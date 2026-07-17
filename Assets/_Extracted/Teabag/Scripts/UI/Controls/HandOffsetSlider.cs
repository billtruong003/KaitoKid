using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

namespace Teabag.UI.Controls
{
    /// <summary>
    /// Drives the Z component of <see cref="OffsetTrackedPoseDriver.PositionOffset"/> on both
    /// the left and right hand drivers via a <see cref="VRSlider"/>.
    /// The current value is persisted via <see cref="ISettingsManager"/>.
    ///
    /// Slider display range : 0 – 1  (default 0.7)
    /// Mapped offset range  : -0.15 (z) – 0 (z)
    /// </summary>
    public sealed class HandOffsetSlider : MonoBehaviour
    {
        private const string SETTINGS_KEY = "HandPositionOffsetZ";
        private const float DEFAULT_VALUE = 0.7f;

        [Header("Slider")]
        [SerializeField] private VRSlider _slider;

        [Header("Configs")]
        [SerializeField] private float _zMin = -0.15f;
        [SerializeField] private float _zMax = 0f;

        private IDataPersistenceService _dataPersistenceService;
        private OffsetTrackedPoseDriver _leftHandDriver;
        private OffsetTrackedPoseDriver _rightHandDriver;

        private void Start()
        {
            if (_slider == null)
            {
                GameLogger.Warning($"[HandOffsetSlider] No VRSlider assigned on {gameObject.name}.");
                return;
            }

            InitializeAsync().Forget();
        }

        private void OnDestroy()
        {
            if (_slider != null)
                _slider.OnValueChangedEvent.RemoveListener(OnSliderValueChanged);
        }

        private async UniTask InitializeAsync()
        {
            _dataPersistenceService = await ServiceLocator.WaitForServiceAsync<IDataPersistenceService>();

            var rigInfo = await ServiceLocator.WaitForServiceAsync<IRigInfoService>();
            var rig = rigInfo?.HardwareRig;

            if (rig == null)
            {
                GameLogger.Warning("[HandOffsetSlider] IHardwareRig not found via IRigInfoService.");
                return;
            }

            _leftHandDriver = rig.LeftHand?.HandTransform?.GetComponent<OffsetTrackedPoseDriver>();
            _rightHandDriver = rig.RightHand?.HandTransform?.GetComponent<OffsetTrackedPoseDriver>();

            if (_leftHandDriver == null)
                GameLogger.Warning("[HandOffsetSlider] OffsetTrackedPoseDriver not found on LeftHand.");

            if (_rightHandDriver == null)
                GameLogger.Warning("[HandOffsetSlider] OffsetTrackedPoseDriver not found on RightHand.");

            float savedValue = _dataPersistenceService.LoadData(SETTINGS_KEY, DEFAULT_VALUE);
            _slider.SetValue(savedValue);
            ApplyNormalizedValue(savedValue);

            _slider.OnValueChangedEvent.AddListener(OnSliderValueChanged);
        }

        private void OnSliderValueChanged(float normalizedValue)
        {
            _dataPersistenceService?.TrySaveData(SETTINGS_KEY, normalizedValue);
            ApplyNormalizedValue(normalizedValue);
        }

        private void ApplyNormalizedValue(float normalizedValue)
        {
            float zOffset = Mathf.Lerp(_zMin, _zMax, normalizedValue);
            ApplyZOffset(_leftHandDriver, zOffset);
            ApplyZOffset(_rightHandDriver, zOffset);
        }

        private static void ApplyZOffset(OffsetTrackedPoseDriver driver, float z)
        {
            if (driver == null) return;
            Vector3 offset = driver.PositionOffset;
            offset.z = z;
            driver.PositionOffset = offset;
        }
    }
}
