using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.UI
{
    public class UIRecorderVoiceDetectionSetting : UIRecorderSetting
    {
        #region Serialized Fields

        [SerializeField]
        private Toggle _toggle;
        [SerializeField]
        private Slider _slider;
        [SerializeField]
        private TMP_Text _sliderText;

        #endregion

        #region Protected Methods

        protected override void Awake()
        {
            base.Awake();
            _toggle.SetIsOnWithoutNotify(_recorder.VoiceDetection);
            _toggle.onValueChanged.AddListener(OnVoiceDetectionChanged);
            _slider.SetValueWithoutNotify(_recorder.VoiceDetectionThreshold);
            _slider.onValueChanged.AddListener(OnVoiceDetectionThresholdChanged);
            _slider.gameObject.SetActive(_recorder.VoiceDetection);
            SyncSliderText();
        }

        #endregion

        #region Private Methods

        private void OnVoiceDetectionChanged(bool isOn)
        {
            _recorder.VoiceDetection = isOn;
            _slider.gameObject.SetActive(isOn);
        }

        private void OnVoiceDetectionThresholdChanged(float threshold)
        {
            _recorder.VoiceDetectionThreshold = threshold;
            SyncSliderText();
        }

        private void SyncSliderText()
        {
            _sliderText.SetText(_slider.value.ToString("F2"));
        }

        #endregion
    }
}