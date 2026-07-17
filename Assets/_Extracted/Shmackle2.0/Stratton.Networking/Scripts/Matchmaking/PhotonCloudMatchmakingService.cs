using Cysharp.Threading.Tasks;
using Fusion;
using Stratton.Core;
using System.Collections.Generic;

namespace Stratton.Networking
{
    public class PhotonCloudMatchmakingService : IMatchmakingService
    {
        private NetworkingSystem _networkingSystem;

        public async UniTask CancelMatchmaking()
        {
            await UniTask.CompletedTask;
        }

        public async UniTask<InitializationResult> Init(NetworkingSystem networkingSystem)
        {
            _networkingSystem = networkingSystem;
            await UniTask.CompletedTask;
            return InitializationResult.Success;
        }

        public async UniTask<MatchmakingResult> StartMatchmaker(string queueName = null, Dictionary<string, object> customRules = null, Dictionary<string, object> attributes = null, List<CustomQoSResult> customQosResults = null)
        {
            await UniTask.CompletedTask;

            var properties = new Dictionary<string, object>();

            var connectionArgs = new ConnectionArguments
            {
                GameMode = _networkingSystem.NetworkingSettings.GameMode.ToString(),
                SessionProperties = properties
            };

            return new MatchmakingResult(MatchmakingResultCode.OK, string.Empty, 0, connectionArgs);
        }
    }
}