using UnityEngine;
using Stratton.Effects;

namespace Stratton.Audio
{
    [System.Serializable]
    public class AudioData: ObjectEmitterData
    {
        public string AudioKey;
        public AudioClip AudioClip;
        public float Volume = 1f;
        public bool Loop;
        public string AudioGroupName;
        public AnimationCurve audioRolloff = new AnimationCurve(new Keyframe[] { new Keyframe(0f, 1f), new Keyframe(1f, 1f) });
    }
}