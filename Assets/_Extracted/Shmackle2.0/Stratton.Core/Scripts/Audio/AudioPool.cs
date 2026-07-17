using System.Collections.Generic;
using System.Linq;
using Stratton.Audio.Factories;
using Stratton.Effects;
using UnityEngine;

namespace Stratton.Audio
{
    public class AudioPool : EmitterPool
    {
        #region Fields

        protected AudioEmitterFactory _audioEmitterFactory;

        #endregion

        #region Properties

        public List<AudioEmitter> AudioEmitters => _emitters.Cast<AudioEmitter>().ToList();

        #endregion

        #region Public Methods

        public AudioPool(AudioEmitterFactory audioEmitterFactor, int poolSize, Transform poolParent) : base(poolSize,
            poolParent)
        {
            _audioEmitterFactory = audioEmitterFactor;
        }

        public void PrefillPool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var audioEmitter = CreateAudioEmitter();
                EnqueueAudioEmitter(audioEmitter);
            }
        }

        public void ReleaseAudioEmitter(AudioEmitter audioEmitter)
        {
            EnqueueAudioEmitter(audioEmitter);
        }

        public AudioEmitter GetAudioEmitter()
        {
            if (_queue.Count > 0)
            {
                return _queue.Dequeue() as AudioEmitter;
            }
            else
            {
                return CreateAudioEmitter();
            }
        }

        #endregion

        #region Private Methods

        private AudioEmitter CreateAudioEmitter()
        {
            var audioEmitter = _audioEmitterFactory.Create();
            audioEmitter.Init(this);
            audioEmitter.transform.parent = _poolParent;
            _emitters.Add(audioEmitter);
            return audioEmitter;
        }

        private void EnqueueAudioEmitter(AudioEmitter audioEmitter)
        {
            audioEmitter.gameObject.SetActive(false);
            audioEmitter.SetParent(_poolParent);
            _queue.Enqueue(audioEmitter);
        }

        #endregion
    }
}