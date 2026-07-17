using Cysharp.Threading.Tasks;
using Stratton.Core;
using System.Collections.Generic;

namespace Stratton.Networking
{
    public interface IMatchmakingService
    {
        public UniTask<InitializationResult> Init(NetworkingSystem networkingSystem);
        public UniTask<MatchmakingResult> StartMatchmaker(string queueName = null, Dictionary<string, object> customRules = null, Dictionary<string, object> attributes = null, List<CustomQoSResult> customQosResults = null);
        public UniTask CancelMatchmaking();
    }

    public class CustomQoSResult
    {
        public CustomQoSResult(string regionId, double? packetLoss, double? latency)
        {
            RegionId = regionId;
            PacketLoss = packetLoss;
            Latency = latency;
        }
        public string RegionId { get; }
        public double? PacketLoss { get; }
        public double? Latency { get; }
    }
}