using Fusion;
using MessagePipe;
using Stratton.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Networking
{
    public class PlayerSpawner : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Transform _spawningPoint;

        #endregion

        #region Non-Serialized Fields

        private ISubscriber<PlayerJoinedSharedModeEvent> _playerJoinedSharedModeEventSubscriber;
        private ISubscriber<PlayerJoinedHostModeEvent> _playerJoinedHostModeEventSubscriber;
        private ISubscriber<PlayerLeftHostModeEvent> _playerLeftHostModeEventSubscriber;

        private NetworkingSystem _networkingSystem;

        protected IDisposable _eventsBagDisposable;

        // Dictionary of spawned user prefabs, to store them on the server for host topology, and destroy them on disconnection (for shared topology, use Network Objects's "Destroy When State Authority Leaves" option)
        private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

        #endregion

        private void Start()
        {
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();

            _playerJoinedSharedModeEventSubscriber = GlobalMessagePipe.GetSubscriber<PlayerJoinedSharedModeEvent>();
            _playerJoinedHostModeEventSubscriber = GlobalMessagePipe.GetSubscriber<PlayerJoinedHostModeEvent>();
            _playerLeftHostModeEventSubscriber = GlobalMessagePipe.GetSubscriber<PlayerLeftHostModeEvent>();

            var bag = DisposableBag.CreateBuilder();

            _playerJoinedSharedModeEventSubscriber.Subscribe(e => OnPlayerJoinedSharedMode(e.Player)).AddTo(bag);
            _playerJoinedHostModeEventSubscriber.Subscribe(e => OnPlayerJoinedHostMode(e.Player)).AddTo(bag);
            _playerLeftHostModeEventSubscriber.Subscribe(e => OnPlayerLeftHostMode(e.Player)).AddTo(bag);

            _eventsBagDisposable = bag.Build();
        }

        private void OnDestroy()
        {
            _eventsBagDisposable?.Dispose();
        }

        private void OnPlayerJoinedSharedMode(PlayerRef player)
        {
            if (_networkingSystem.Runner.LocalPlayer == player)
            {
                SpawnPlayer(player, out var networkPlayerObject);
            }
        }

        private void OnPlayerJoinedHostMode(PlayerRef player)
        {
            // The user's prefab has to be spawned by the host
            if (_networkingSystem.Runner.IsServer && SpawnPlayer(player, out var networkPlayerObject))
            {
                // Keep track of the player avatars so we can remove it when they disconnect
                _spawnedPlayers.Add(player, networkPlayerObject);
            }
        }

        private void OnPlayerLeftHostMode(PlayerRef player)
        {
            // The user's prefab has to be spawned by the host
            if (_networkingSystem.Runner.IsServer)
            {
                // Find and remove the players avatar (only the host would have stored the spawned game object)
                if (_spawnedPlayers.TryGetValue(player, out NetworkObject networkObject))
                {
                    _networkingSystem.Runner.Despawn(networkObject);
                    _spawnedPlayers.Remove(player);
                }
            }
        }

        private bool SpawnPlayer(PlayerRef player, out NetworkObject networkPlayerObject)
        {
            networkPlayerObject = null;
            if (_spawningPoint == null)
            {
                Core.Log.Error(NetworkingLogChannel.Player, $"There's no player spawning point assigned!");
                return false;
            }
            if (_networkingSystem.NetworkObjectPool.TryGetNetworkObjectPrefab(BaseNetworkObjectType.Player, out var playerPrefab))
            {
                networkPlayerObject = _networkingSystem.Runner.Spawn(
                    playerPrefab, position: _spawningPoint.position, rotation: _spawningPoint.rotation, player, (runner, obj) =>
                    {
                    });
                _networkingSystem.Runner.SetPlayerObject(player, networkPlayerObject);
                return true;
            }
            return false;
        }
    }
}