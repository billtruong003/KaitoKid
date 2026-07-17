using UnityEngine;
using UnityEngine.Events;
using Photon.Voice.Unity;
using Fusion;
using Photon.Voice;

namespace Stratton.Networking.Voice.Sample
{
    /// <summary>
    /// An optional object to place in a world to wait for all voice dependencies and expose their functions.
    /// Useful if needed by UIs directly on the hierarchy
    /// </summary>
    public class SampleVoiceManager : NetworkBehaviour
    {
        [SerializeField]
        private UnityEvent _onVoiceInit;
        private VoiceConnection _voiceConnection;
        private Recorder _recorder;

        public override void Spawned()
        {
            base.Spawned();
            Init(Runner.GetComponentInChildren<VoiceConnection>(), Runner.GetComponentInChildren<Recorder>());
        }

        void Init(VoiceConnection voiceConnection, Recorder recorder)
        {
            if (!voiceConnection)
            {
                throw new MissingComponentException("Invalid voice connection component");
            }
            if (!recorder)
            {
                throw new MissingComponentException("Invalid recorder component");
            }
            _voiceConnection = voiceConnection;
            _recorder = recorder;

            if (_onVoiceInit != null)
            {
                _onVoiceInit.Invoke();
            }
        }

        public void ToggleMute()
        {
            SetMuted(!_recorder.TransmitEnabled);
        }

        public void SetMuted(bool isMuted)
        {
            _recorder.TransmitEnabled = isMuted;
        }

        public void SetMicrophoneDevice(DeviceInfo newDeviceInfo)
        {
            if (_recorder != null)
            {
                _recorder.MicrophoneDevice = newDeviceInfo;
            }
        }

        public void JoinListenChannel(byte channel)
        {
            _voiceConnection.Client.OpChangeGroups(null, new byte[] { channel });
        }

        public void ClearListenChannel(byte channel)
        {
            _voiceConnection.Client.OpChangeGroups(new byte[] { channel }, null);
        }

        public void SetTalkChannel(byte channel)
        {
            _recorder.InterestGroup = channel;
        }
    }
}
