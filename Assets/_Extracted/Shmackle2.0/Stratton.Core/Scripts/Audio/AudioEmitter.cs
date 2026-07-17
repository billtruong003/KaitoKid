using Stratton.Core;
using Stratton.Effects;
using UnityEngine;

namespace Stratton.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioEmitter : ObjectEmitter
    {
        #region Fields

        protected AudioSystem _audioSystem;
        protected AudioSource _audioSource;
        protected AudioPool _audioPool;

        #endregion

        #region Properties

        public AudioSource AudioSource => _audioSource;

        #endregion

        #region Private Methods

        protected void Awake()
        {
            _audioSystem = GameSystemsManager.Instance.Get<AudioSystem>();
            _audioSource = GetComponent<AudioSource>();
        }

        #endregion

        #region Public Methods

        public override void Init(EmitterPool emitterPool)
        {
            base.Init(emitterPool);
            _audioPool = emitterPool as AudioPool;
        }

        public override void Play()
        {
            var audioData = ObjectEmitterData as AudioData;
            gameObject.SetActive(true);
            _isPaused = false;
            _audioSource.clip = audioData.AudioClip;
            _audioSource.loop = audioData.Loop;
            _audioSource.rolloffMode = AudioRolloffMode.Custom;
            _audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioData.audioRolloff);
            _audioSource.volume = audioData.Volume;
            _audioSource.maxDistance = audioData.audioRolloff.keys[1].time;
            _audioSource.outputAudioMixerGroup = _audioSystem.GetMixerGroup(audioData.AudioGroupName);
            _audioSource.spatialBlend = audioData.IsGlobal ? 0f : 1f;
            _audioSource.Play();
        }

        public override void Pause()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Pause();
                _isPaused = true;
            }
        }

        public void Resume()
        {
            if (_isPaused)
            {
                _audioSource.UnPause();
            }
        }

        public override void Stop()
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
                Release();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!_audioSource.isPlaying && !_isPaused && !_audioSource.loop)
            {
                Release();
            }
        }

        #endregion
    }
}