using Fusion;
using System.Linq;
using UnityEngine;

namespace Stratton.Networking.Voice
{
    /// <summary>
    /// Update recorder config based on global player config
    /// </summary>
    [RequireComponent(typeof(DynamicRecorderConfig))]
    public class GlobalDynamicRecorderConfigHandler : SimulationBehaviour, IPlayerJoined, IPlayerLeft
    {
        private DynamicRecorderConfig _config;

        void Awake()
        {
            _config = GetComponent<DynamicRecorderConfig>();
        }

        void Start()
        {
            _config.ApplyBestConfig(Runner.ActivePlayers.Count());
        }

        public void PlayerJoined(PlayerRef player)
        {
            _config.ApplyBestConfig(Runner.ActivePlayers.Count());
        }

        public void PlayerLeft(PlayerRef player)
        {
            _config.ApplyBestConfig(Runner.ActivePlayers.Count());
        }
    }
}