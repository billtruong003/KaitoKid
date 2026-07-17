using Photon.Voice;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Shmackle.UI
{
    public class UIRecorderMicrophoneDeviceSetting : UIRecorderSetting
    {
        #region Serialized Fields

        [SerializeField]
        private TMP_Dropdown _dropdown;

        #endregion

        #region Private Fields

        private List<DeviceInfo> _micDevices = new List<DeviceInfo>();

        #endregion

        #region Private Methods

        private void Start()
        {
            // Load microphone devices
            List<string> opts = new List<string>();
            foreach (DeviceInfo item in Platform.CreateAudioInEnumerator(this.Logger))
            {
                opts.Add(item.Name);
                _micDevices.Add(item);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(opts);
            _dropdown.SetValueWithoutNotify(GetMicDeviceIndex(_recorder.MicrophoneDevice));
            _dropdown.onValueChanged.AddListener(OnMicDeviceChanged);
        }

        private int GetMicDeviceIndex(DeviceInfo info)
        {
            return _micDevices.IndexOf(info);
        }
        
        private void OnMicDeviceChanged(int micDeviceIndex)
        {
            _recorder.MicrophoneDevice = _micDevices[micDeviceIndex];
        }

        #endregion

    }
}