using Photon.Voice.Unity;
using UnityEngine;
using Photon.Voice;

namespace Shmackle.Audio
{
    /// <summary>
    /// Captures raw audio data from a Photon Voice remote speaker stream.
    /// Computes a normalized amplitude value (0–1) from decoded audio frames.
    /// </summary>
    public class SpeakerAudioTap : MonoBehaviour
    {
        /// <summary>
        /// The most recent normalized amplitude value (0–1)
        /// calculated from the incoming audio frame.
        /// </summary>
        public float CurrentAmplitude { get; private set; }

        private Speaker _speaker; 
        private bool _subscribed;
        
        private void Awake()
        {
            if(!_speaker)
                _speaker = GetComponent<Speaker>();
        }

        /// <summary>
        /// Subscribes to the RemoteVoice audio frame callback once the voice stream becomes available.
        /// </summary>
        private void Update()
        {
            // Subscribe once when RemoteVoice gets assigned
            if (!_subscribed && _speaker && _speaker.RemoteVoice != null)
            {
                _speaker.RemoteVoice.FloatFrameDecoded += OnAudioFrame;
                _subscribed = true;
            }
        }

        /// <summary>
        /// Ensures the audio callback is unregistered when this component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_subscribed && _speaker && _speaker.RemoteVoice != null)
                _speaker.RemoteVoice.FloatFrameDecoded -= OnAudioFrame;
        }

        /// <summary>
        /// Processes a decoded audio frame from Photon Voice, computes the peak amplitude,
        /// normalizes it to 0–1, and updates <see cref="CurrentAmplitude"/>.
        /// </summary>
        /// <param name="frame">Decoded floating-point audio frame data.</param>
        private void OnAudioFrame(FrameOut<float> frame)
        {
            // Each value in 'buffer' is a single audio sample ranging between -1 and +1.
            var buffer = frame.Buf;
            if (buffer == null || buffer.Length == 0)
            {
                CurrentAmplitude = 0f;
                return;
            }

            float peak = 0f;

            // Find the peak absolute amplitude in the frame
            for (int i = 0; i < buffer.Length; i++)
            {
                float absoluteSample = Mathf.Abs(buffer[i]);
                if (absoluteSample > peak)
                    peak = absoluteSample;
            }

            // Normalize to a 0–1 range
            CurrentAmplitude = Mathf.Clamp01(peak);
        }
    }
}
