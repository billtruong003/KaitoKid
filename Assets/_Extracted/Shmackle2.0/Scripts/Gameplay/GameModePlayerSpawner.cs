using Fusion;
using MessagePipe;
using Stratton.Core;
using Stratton.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shmackle.Gameplay
{
    /// <summary>
    /// Spawns player dependent on the game mode system.
    /// Waits for player states before spawning.
    /// Uses game mode player object!
    /// </summary>
    public class GameModePlayerSpawner : MonoBehaviour
    {
        #region Private Fields
        
        private IDisposable _eventsBagDisposable;
        private NetworkingSystem _networkingSystem;
        private GameplaySystem _gameplaySystem;
        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
        private GameModeSettings _gameModeSettings;
        
        #endregion
        
        #region Properties

        private GameModeSettings GameModeSettings
        {
            get
            {
                if (_gameModeSettings)
                {
                    return _gameModeSettings;
                }
                _gameModeSettings = _gameplaySystem.GetActiveGameMode<GameModeBase>().GetSettings<GameModeSettings>();
                return _gameModeSettings;
            }
        }
        
        #endregion
        
        #region Protected Methods

        protected virtual void Awake()
        {
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            _gameplaySystem = GameSystemsManager.Instance.Get<GameplaySystem>();
            
            var bag = DisposableBag.CreateBuilder();

            GlobalMessagePipe.GetSubscriber<PlayerStateRegisteredEvent>().Subscribe(e => OnPlayerJoined(e.PlayerState)).AddTo(bag);
            GlobalMessagePipe.GetSubscriber<PlayerStateUnregisteredEvent>().Subscribe(e => OnPlayerLeft(e.PlayerState)).AddTo(bag);
            
            _eventsBagDisposable = bag.Build();
        }

        protected virtual void OnDestroy()
        {
            _eventsBagDisposable?.Dispose();
        }

        protected virtual void OnPlayerJoined(PlayerState playerState)
        {
            if (_networkingSystem.Runner.Topology == Topologies.Shared)
            {
                if (playerState.IsLocalPlayer)
                {
                    SpawnPlayer(playerState.Owner, out var networkPlayerObject);
                }
            }
            else
            {
                if (_networkingSystem.Runner.IsServer && SpawnPlayer(playerState.Owner, out var networkPlayerObject))
                {
                    // Keep track of the player avatars so we can remove it when they disconnect
                    _spawnedPlayers.Add(playerState.Owner, networkPlayerObject);
                }
            }
        }

        protected virtual void OnPlayerLeft(PlayerState playerState)
        {
            if (_networkingSystem.Runner.IsServer)
            {
                // Find and remove the players avatar (only the host would have stored the spawned game object)
                if (_spawnedPlayers.TryGetValue(playerState.LastValidOwner, out NetworkObject networkObject))
                {
                    _networkingSystem.Runner.Despawn(networkObject);
                    _spawnedPlayers.Remove(playerState.LastValidOwner);
                }
            }
        }
        
        protected virtual bool SpawnPlayer(PlayerRef player, out NetworkObject networkPlayerObject)
        {
            networkPlayerObject = null;
            if (GameModeSettings == null || GameModeSettings.PlayerObjectPrefab == null)
            {
                Stratton.Core.Log.Error(BaseLogChannel.Gameplay, $"There's no valid player object assigned on the game mode settings.");
                return false;
            }
            Transform spawnPoint = GetSpawnPoint(player);
            networkPlayerObject = _networkingSystem.Runner.Spawn(
                GameModeSettings.PlayerObjectPrefab, spawnPoint.position, spawnPoint.rotation, player);
            _networkingSystem.Runner.SetPlayerObject(player, networkPlayerObject);
            return true;
        }

        protected virtual Transform GetSpawnPoint(PlayerRef playerRef)
        {
            return transform;
        }
        
        #endregion
    }
}
