using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Hyper.AudioSystems
{
    public static class RuntimeAudioAnalyzer
    {
        private const float LowPassCutoff = 150f;
        private const float OutputResolution = 0.02f;

        public static AnalyzedAudioData Analyze(AudioClip clip)
        {
            if (clip == null) return null;

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            float[] monoSamples = ConvertToMono(samples, clip.channels);
            float[] bassSamples = ApplyLowPassFilter(monoSamples, clip.frequency, LowPassCutoff);
            float[] energyEnvelope = CalculateEnergyEnvelope(bassSamples, clip.frequency);
            List<float> beatTimes = DetectBeats(energyEnvelope, clip.frequency);
            float detectedBPM = CalculateBPM(beatTimes);
            float[] finalCurve = ResampleCurve(energyEnvelope, clip.frequency, OutputResolution);

            AnalyzedAudioData data = ScriptableObject.CreateInstance<AnalyzedAudioData>();
            data.SetData(clip, detectedBPM, beatTimes.Count, clip.length, finalCurve, OutputResolution);

            return data;
        }

        private static float[] ConvertToMono(float[] samples, int channels)
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

        private static float[] ApplyLowPassFilter(float[] input, int sampleRate, float cutoff)
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

        private static float[] CalculateEnergyEnvelope(float[] input, int sampleRate)
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

        private static List<float> DetectBeats(float[] envelope, int sampleRate)
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

        private static float CalculateBPM(List<float> beatTimes)
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

        private static float[] ResampleCurve(float[] source, int sampleRate, float targetResolution)
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