using UnityEngine;

namespace Stratton.Audio.Factories
{
    public class AudioEmitterFactory
    {
        public AudioEmitter Create()
        {
            var audioEmitterGameObject = new GameObject("AudioEmitter");
            var audioEmitter = audioEmitterGameObject.AddComponent<AudioEmitter>();
            return audioEmitter;
        }
    }
}