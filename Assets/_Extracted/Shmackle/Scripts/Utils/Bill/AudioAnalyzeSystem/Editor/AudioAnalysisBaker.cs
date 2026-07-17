#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Linq;

namespace Hyper.AudioSystems.Editor
{
    public sealed class AudioAnalysisBaker : OdinEditorWindow
    {
        [Title("Audio Source Configuration")]
        [BoxGroup("Input"), SerializeField, Required, AssetsOnly, OnValueChanged("StopPreview")]
        private AudioClip _audioClip;

        [BoxGroup("Input"), SerializeField, Required, AssetsOnly]
        private AnalyzedAudioData _targetDataProfile;

        private AudioSource _previewSource;
        private const float LowPassCutoff = 150f;
        private const float OutputResolution = 0.02f;

        [MenuItem("Tools/BillUtils/Hyper/Audio Analysis Baker")]
        private static void OpenWindow()
        {
            GetWindow<AudioAnalysisBaker>().Show();
        }

        protected override void OnDestroy()
        {
            StopPreview();
            base.OnDestroy();
        }

        [BoxGroup("Preview Controls"), Button(ButtonSizes.Medium), GUIColor(0.4f, 1f, 0.4f)]
        [EnableIf("@_audioClip != null")]
        private void PlayPreview()
        {
            if (_previewSource == null)
            {
                GameObject go = new GameObject("EditorAudioPreview") { hideFlags = HideFlags.HideAndDontSave };
                _previewSource = go.AddComponent<AudioSource>();
            }

            if (_audioClip == null) return;

            _previewSource.clip = _audioClip;
            _previewSource.Play();
        }

        [BoxGroup("Preview Controls"), Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
        [EnableIf("@_previewSource != null && _previewSource.isPlaying")]
        private void StopPreview()
        {
            if (_previewSource != null)
            {
                _previewSource.Stop();
                DestroyImmediate(_previewSource.gameObject);
                _previewSource = null;
            }
        }

        [BoxGroup("Processing"), Button(ButtonSizes.Large), GUIColor(0f, 0.8f, 1f)]
        [EnableIf("@_audioClip != null && _targetDataProfile != null")]
        private void BakeAudioData()
        {
            ProcessAudio();
        }

        private void ProcessAudio()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Analyzing Audio", "Reading Samples...", 0.2f);

                float[] samples = new float[_audioClip.samples * _audioClip.channels];
                _audioClip.GetData(samples, 0);

                EditorUtility.DisplayProgressBar("Analyzing Audio", "Processing Frequencies...", 0.5f);
                float[] monoSamples = ConvertToMono(samples, _audioClip.channels);
                float[] bassSamples = ApplyLowPassFilter(monoSamples, _audioClip.frequency, LowPassCutoff);
                float[] energyEnvelope = CalculateEnergyEnvelope(bassSamples, _audioClip.frequency);

                EditorUtility.DisplayProgressBar("Analyzing Audio", "Detecting Beats...", 0.8f);
                List<float> beatTimes = DetectBeats(energyEnvelope, _audioClip.frequency);
                float detectedBPM = CalculateBPM(beatTimes);
                float[] finalCurve = ResampleCurve(energyEnvelope, _audioClip.frequency, OutputResolution);

                _targetDataProfile.SetData(_audioClip, detectedBPM, beatTimes.Count, _audioClip.length, finalCurve, OutputResolution);

                EditorUtility.SetDirty(_targetDataProfile);
                AssetDatabase.SaveAssets();

                Debug.Log($"<color=cyan><b>[AudioBaker]</b></color> Baked {_audioClip.name} successfully. BPM: {detectedBPM}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private float[] ConvertToMono(float[] samples, int channels)
        {
            if (channels == 1) return samples;
            int length = samples.Length / channels;
            float[] mono = new float[length];
            for (int i = 0; i < length; i++)
            {
                float sum = 0;
                for (int c = 0; c < channels; c++) sum += samples[i * channels + c];
                mono[i] = sum / channels;
            }
            return mono;
        }

        private float[] ApplyLowPassFilter(float[] input, int sampleRate, float cutoff)
        {
            float[] output = new float[input.Length];
            float dt = 1f / sampleRate;
            float rc = 1f / (2 * Mathf.PI * cutoff);
            float alpha = dt / (rc + dt);
            output[0] = input[0];
            for (int i = 1; i < input.Length; i++)
            {
                output[i] = output[i - 1] + alpha * (input[i] - output[i - 1]);
            }
            return output;
        }

        private float[] CalculateEnergyEnvelope(float[] input, int sampleRate)
        {
            int windowSize = sampleRate / 20;
            float[] envelope = new float[input.Length];
            for (int i = 0; i < input.Length; i++) envelope[i] = input[i] * input[i];

            float[] smoothed = new float[envelope.Length];
            float currentSum = 0;
            for (int i = 0; i < envelope.Length; i++)
            {
                currentSum += envelope[i];
                if (i >= windowSize) currentSum -= envelope[i - windowSize];
                smoothed[i] = currentSum / windowSize;
            }

            float maxVal = 0f;
            foreach (float v in smoothed) if (v > maxVal) maxVal = v;
            if (maxVal > 0) for (int i = 0; i < smoothed.Length; i++) smoothed[i] /= maxVal;

            return smoothed;
        }

        private List<float> DetectBeats(float[] envelope, int sampleRate)
        {
            List<float> beats = new List<float>();
            int windowSize = sampleRate;
            float thresholdMultiplier = 1.4f;
            for (int i = windowSize; i < envelope.Length - 1; i++)
            {
                float localAverage = 0;
                int start = i - windowSize;
                int count = 0;
                for (int j = 0; j < windowSize; j += 100)
                {
                    localAverage += envelope[start + j];
                    count++;
                }
                localAverage /= count;

                if (envelope[i] > localAverage * thresholdMultiplier &&
                    envelope[i] > envelope[i - 1] &&
                    envelope[i] > envelope[i + 1])
                {
                    float time = (float)i / sampleRate;
                    if (beats.Count == 0 || time - beats[beats.Count - 1] > 0.25f)
                    {
                        beats.Add(time);
                    }
                }
            }
            return beats;
        }

        private float CalculateBPM(List<float> beatTimes)
        {
            if (beatTimes.Count < 2) return 0;
            List<float> intervals = new List<float>();
            for (int i = 1; i < beatTimes.Count; i++) intervals.Add(beatTimes[i] - beatTimes[i - 1]);
            var groups = intervals.GroupBy(i => Mathf.Round(i * 10) / 10f).OrderByDescending(g => g.Count()).ToList();
            if (groups.Count == 0) return 0;

            float dominantInterval = groups[0].Key;
            if (dominantInterval <= 0) return 0;

            float bpm = 60f / dominantInterval;

            while (bpm > 180f) bpm /= 2f;
            while (bpm < 60f && bpm > 0) bpm *= 2f;

            return Mathf.Round(bpm);
        }

        private float[] ResampleCurve(float[] source, int sampleRate, float targetResolution)
        {
            int targetLength = Mathf.CeilToInt((float)source.Length / sampleRate / targetResolution);
            float[] result = new float[targetLength];
            int samplesPerStep = Mathf.FloorToInt(sampleRate * targetResolution);
            for (int i = 0; i < targetLength; i++)
            {
                int startIdx = i * samplesPerStep;
                if (startIdx >= source.Length) break;
                float max = 0;
                for (int j = 0; j < samplesPerStep && (startIdx + j) < source.Length; j++)
                {
                    float val = source[startIdx + j];
                    if (val > max) max = val;
                }
                result[i] = max;
            }
            return result;
        }
    }
}
#endif