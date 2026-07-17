using UnityEngine;

namespace Stratton.Audio.Factories
{
    public class AudioPoolFactory
    {
        public AudioPool Create(AudioEmitterFactory audioEmitterFactor, int poolSize, Transform poolParent)
        {
            var audioPool = new AudioPool(audioEmitterFactor, poolSize, poolParent);
            return audioPool;
        }
    }
}