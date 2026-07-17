using Photon.Voice;
using POpusCodec.Enums;
using UnityEngine;

namespace Stratton.Networking.Voice
{
    [System.Serializable]
    public struct RecorderConfig
    {
        public int MinPlayerCount;
        [Range(6000, 510000)]
        public int Bitrate;
        public SamplingRate SamplingRate;
        public OpusCodec.FrameDuration FrameDuration;

        public RecorderConfig(int minPlayerCount = 0, int bitrate = 30000, SamplingRate samplingRate = SamplingRate.Sampling24000, OpusCodec.FrameDuration frameDuration = OpusCodec.FrameDuration.Frame20ms)
        {
            MinPlayerCount = minPlayerCount;
            Bitrate = bitrate;
            SamplingRate = samplingRate;
            FrameDuration = frameDuration;
        }
    }
}