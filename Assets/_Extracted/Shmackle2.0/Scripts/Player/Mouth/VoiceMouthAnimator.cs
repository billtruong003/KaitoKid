using Fusion.XR.Shared.Core;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using Player.Config;
using Shmackle.Audio;
using Stratton.Networking.Voice;
using UnityEngine;

namespace Shmackle.Player.Mouth
{
    /// <summary>
    /// Controls mouth movement by driving a blendshape based on real-time voice amplitude.
    /// Supports both local microphone input and remote player audio through Photon Voice.
    /// </summary>
    public class VoiceMouthAnimator : MonoBehaviour
    {
        [SerializeField] private MouthConfig _mouthConfig;
        
        [Header("Blendshape Target")]
        [SerializeField] private SkinnedMeshRenderer _meshRenderer;
        
        private VoiceNetworkObject _voiceNetworkObject;
        private Recorder _recorder;
        private SpeakerAudioTap _audioTap;
        private SpeakerStatusListener _speakerStatusListener;
        private LocalMouthAnimator _localMouthAnimator;

        private IHardwareRig _localHardwareRig;
        private int _blendShapeIndex = -1;
        private float _currentBlend;
        private float _mouthOpenCloseSpeed;
        private float _mouthSpeechSensitivity;
        private bool _lastSpeakingState;
        private const string BLEND_SHAPE_NAME = "Talking";

        private void Awake()
        {
            if (!_voiceNetworkObject)
                _voiceNetworkObject = GetComponent<VoiceNetworkObject>();

            if (_meshRenderer && _meshRenderer.sharedMesh)
                _blendShapeIndex = _meshRenderer.sharedMesh.GetBlendShapeIndex(BLEND_SHAPE_NAME);

            if (!_speakerStatusListener)
                _speakerStatusListener = GetComponent<SpeakerStatusListener>();

            if (!_audioTap)
                _audioTap = GetComponent<SpeakerAudioTap>();

            ConfigUpdate();
        }

        private void OnEnable()
        {
            _mouthConfig.ConfigUpdated += ConfigUpdate;
        }

        private void OnDisable()
        {
            _mouthConfig.ConfigUpdated -= ConfigUpdate;
        }

        private void ConfigUpdate()
        {
            _mouthSpeechSensitivity = _mouthConfig.MouthSpeechSensitivity;
            _mouthOpenCloseSpeed = _mouthConfig.MouthOpenCloseSpeed;
        }
        
        private void Start()
        {
            if (_voiceNetworkObject.IsLocal)
            {
                _recorder = _voiceNetworkObject.RecorderInUse;
                _localHardwareRig = HardwareRigsRegistry.GetHardwareRig();
                
                if (_localHardwareRig != null && !_localMouthAnimator)
                    _localMouthAnimator = _localHardwareRig.transform.GetComponentInChildren<LocalMouthAnimator>();
            }
        }
        
        /// <summary>
        /// Gets the normalized voice amplitude (0–1) from either the local microphone
        /// or the remote audio stream depending on player ownership.
        /// </summary>
        /// <returns>Current amplitude value from voice input.</returns>
        private float GetVoiceAmplitude()
        {
            // Local mic amplitude
            if (_voiceNetworkObject.IsLocal && _recorder && _recorder.LevelMeter != null)
                return Mathf.Clamp01(_recorder.LevelMeter.CurrentPeakAmp);

            // Remote player amplitude
            if (!_voiceNetworkObject.IsLocal && _audioTap)
                return _audioTap.CurrentAmplitude;

            return 0f;
        }

        private void Update()
        {
            float targetBlend = 0f;
            
            bool isSpeaking = _speakerStatusListener != null && _speakerStatusListener.IsSpeaking;

            if (isSpeaking)
            {
                float rawAmplitude = GetVoiceAmplitude();
                float boostedAmplitude = rawAmplitude * _mouthSpeechSensitivity;
               
                // ensures we never go above 1.0
                // the value 100 represents the max value of the blendshape since our blend shape is between 0 and 100
                targetBlend = Mathf.Clamp01(boostedAmplitude) * 100; 
            }

            // Smoothly interpolate final mouth position
            _currentBlend = Mathf.Lerp(_currentBlend, targetBlend, Time.deltaTime * _mouthOpenCloseSpeed);

            ApplyBlendshape(_currentBlend);

            if (_voiceNetworkObject.IsLocal && _localMouthAnimator)
                _localMouthAnimator.SetMouthValue(_currentBlend);

        }

        /// <summary>
        /// Applies the computed blendshape weight to the mesh renderer's blendshape.
        /// </summary>
        /// <param name="value">Blendshape weight value to apply.</param>
        private void ApplyBlendshape(float value)
        {
            if (!_meshRenderer || _blendShapeIndex < 0)
                return;

            _meshRenderer.SetBlendShapeWeight(_blendShapeIndex, value);
        }
    }
}