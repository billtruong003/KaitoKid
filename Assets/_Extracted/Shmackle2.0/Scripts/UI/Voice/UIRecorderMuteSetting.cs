using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.UI
{
    public class UIRecorderMuteSetting : UIRecorderSetting
    {
        #region Serialized Fields
        
        [SerializeField]
        private Toggle _toggle;
        
        #endregion

        #region Protected Methods

        protected override void Awake()
        {
            base.Awake();
            _toggle.SetIsOnWithoutNotify(_recorder.RecordingEnabled);
            _toggle.onValueChanged.AddListener(OnRecordingEnabledChanged);
        }

        #endregion

        #region Private Methods

        private void OnRecordingEnabledChanged(bool isEnabled)
        {
            _recorder.RecordingEnabled = isEnabled;
        }

        #endregion
    }
}