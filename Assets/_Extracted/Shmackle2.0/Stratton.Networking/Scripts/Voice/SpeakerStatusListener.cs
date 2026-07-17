using Photon.Voice.Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace Stratton.Networking.Voice
{
    /// <summary>
    /// Simple event broadcaster when the speaking status of a voice network object changed.
    /// </summary>
    [RequireComponent(typeof(VoiceNetworkObject))]
    public class SpeakerStatusListener : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent<bool> _onChangeStatus;
        private VoiceNetworkObject _voiceNetworkObject;

        private bool _lastSpeakingState;

        public bool IsSpeaking
        {
            get
            {
                return _voiceNetworkObject.Runner != null && (_voiceNetworkObject.IsSpeaking || (_voiceNetworkObject.IsLocal && _voiceNetworkObject.IsRecording));
            }
        }

        private void Awake()
        {
            _voiceNetworkObject = GetComponent<VoiceNetworkObject>();
        }

        private void Start()
        {
            _lastSpeakingState = IsSpeaking;
            _onChangeStatus.Invoke(_lastSpeakingState);
        }

        private void Update()
        {
            if (IsSpeaking != _lastSpeakingState)
            {
                _lastSpeakingState = IsSpeaking;
                _onChangeStatus.Invoke(_lastSpeakingState);
                // Core.Log.Message(NetworkingLogChannel.Voice, $"Is Recording Voice: {_lastSpeakingState}");
            }
        }
    }
}