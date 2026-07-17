using UnityEngine;

namespace Hyper.AudioSystems
{
    [CreateAssetMenu(fileName = "AnalyzedAudioData", menuName = "Hyper/Analyzed Audio Data")]
    public sealed class AnalyzedAudioData : ScriptableObject
    {
        [field: SerializeField] public AudioClip LinkedClip { get; private set; }
        [field: SerializeField] public float DetectedBPM { get; private set; }
        [field: SerializeField] public int TotalBeats { get; private set; }
        [field: SerializeField] public float Duration { get; private set; }

        [SerializeField] private float[] _bassEnergyCurve;
        [SerializeField] private float _sampleRate = 0.05f;

        public void SetData(AudioClip clip, float bpm, int totalBeats, float duration, float[] energyCurve, float sampleRate)
        {
            LinkedClip = clip;
            DetectedBPM = bpm;
            TotalBeats = totalBeats;
            Duration = duration;
            _bassEnergyCurve = energyCurve;
            _sampleRate = sampleRate;
        }

        public float GetEnergyAtTime(float time)
        {
            if (_bassEnergyCurve == null || _bassEnergyCurve.Length == 0) return 0f;
            int index = Mathf.FloorToInt(time / _sampleRate);
            return _bassEnergyCurve[Mathf.Clamp(index, 0, _bassEnergyCurve.Length - 1)];
        }
    }
}