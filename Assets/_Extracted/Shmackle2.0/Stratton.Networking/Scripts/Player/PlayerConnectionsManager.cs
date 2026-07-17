using Fusion;
using MessagePipe;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Networking
{
    public class PlayerConnectionsManager
    {
        private struct DisconnectedPlayer
        {
            public PlayerRef PlayerRef;
            public DisconnectReasonType DisconnectReasonType;
        }

        #region Fields

        private IPublisher<PlayerJoinedSharedModeEvent> _playerJoinedSharedModeEventPublisher;
        private IPublisher<PlayerJoinedHostModeEvent> _playerJoinedHostModeEventPublisher;
        private IPublisher<PlayerLeftHostModeEvent> _playerLeftHostModeEventPublisher;
        private IPublisher<PlayerJoinedEvent> _playerJoinedEventPublisher;
        private IPublisher<PlayerLeftEvent> _playerLeftEventPublisher;
        private IPublisher<SceneLoadedEvent> _sceneLoadedEventPublisher;

        private NetworkRunnerCallbacksAdapter _networkRunnerCallbacksAdapter;

        private readonly Dictionary<PlayerRef, string> _connectedPlayersByPlayerRef = new();
        private readonly Dictionary<string, PlayerRef> _connectedPlayersByUserId = new();
        private readonly Dictionary<string, DisconnectedPlayer> _diconnectedUsers = new();

        #endregion

        #region Public Methods

        public void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            builder.AddMessageBroker<PlayerJoinedSharedModeEvent>();
            builder.AddMessageBroker<PlayerJoinedHostModeEvent>();
            builder.AddMessageBroker<PlayerLeftHostModeEvent>();
            builder.AddMessageBroker<PlayerJoinedEvent>();
            builder.AddMessageBroker<PlayerLeftEvent>();
            builder.AddMessageBroker<SceneLoadedEvent>();
        }

        public void Init(NetworkRunnerCallbacksAdapter networkRunnerCallbacksAdapter)
        {
            _networkRunnerCallbacksAdapter = networkRunnerCallbacksAdapter;

            _networkRunnerCallbacksAdapter.PlayerJoined += OnPlayerJoined;
            _networkRunnerCallbacksAdapter.PlayerJoinedSharedMode += OnPlayerJoinedSharedMode;
            _networkRunnerCallbacksAdapter.PlayerJoinedHostMode += OnPlayerJoinedHostMode;
            _networkRunnerCallbacksAdapter.PlayerLeft += OnPlayerLeft;
            _networkRunnerCallbacksAdapter.PlayerLeftHostMode += OnPlayerLeftHostMode;
            _networkRunnerCallbacksAdapter.SceneLoadDone += OnSceneLoadDone;

            _playerJoinedSharedModeEventPublisher = GlobalMessagePipe.GetPublisher<PlayerJoinedSharedModeEvent>();
            _playerJoinedHostModeEventPublisher = GlobalMessagePipe.GetPublisher<PlayerJoinedHostModeEvent>();
            _playerLeftHostModeEventPublisher = GlobalMessagePipe.GetPublisher<PlayerLeftHostModeEvent>();
            _playerJoinedEventPublisher = GlobalMessagePipe.GetPublisher<PlayerJoinedEvent>();
            _playerLeftEventPublisher = GlobalMessagePipe.GetPublisher<PlayerLeftEvent>();
            _sceneLoadedEventPublisher = GlobalMessagePipe.GetPublisher<SceneLoadedEvent>();
        }

        public void DeInit()
        {
            _connectedPlayersByPlayerRef.Clear();
            _connectedPlayersByUserId.Clear();
            _diconnectedUsers.Clear();

            _networkRunnerCallbacksAdapter.PlayerJoined -= OnPlayerJoined;
            _networkRunnerCallbacksAdapter.PlayerJoinedSharedMode -= OnPlayerJoinedSharedMode;
            _networkRunnerCallbacksAdapter.PlayerJoinedHostMode -= OnPlayerJoinedHostMode;
            _networkRunnerCallbacksAdapter.PlayerLeft -= OnPlayerLeft;
            _networkRunnerCallbacksAdapter.PlayerLeftHostMode -= OnPlayerLeftHostMode;
            _networkRunnerCallbacksAdapter.SceneLoadDone -= OnSceneLoadDone;
        }

        #endregion

        #region Private Methods

        public void OnPlayerJoinedSharedMode(NetworkRunner runner, PlayerRef player)
        {
            Core.Log.Message(NetworkingLogChannel.Connections, $"OnPlayerJoinedSharedMode. PlayerId: {player.PlayerId}");
            _playerJoinedSharedModeEventPublisher.Publish(new() { Player = player });
        }

        public void OnPlayerJoinedHostMode(NetworkRunner runner, PlayerRef player)
        {
            Core.Log.Message(NetworkingLogChannel.Connections, $"OnPlayerJoinedHostMode. PlayerId: {player.PlayerId}");

            _playerJoinedHostModeEventPublisher.Publish(new() { Player = player });
        }

        public void OnPlayerLeftHostMode(NetworkRunner runner, PlayerRef player)
        {
            Core.Log.Message(NetworkingLogChannel.Connections, $"OnPlayerLeftHostMode. PlayerId: {player.PlayerId}");

            _playerLeftHostModeEventPublisher.Publish(new() { Player = player });
        }

        private void OnPlayerJoined(NetworkRunner runner, PlayerRef playerRef)
        {
            if (runner.IsServer)
            {
                var userId = runner.GetPlayerUserId(playerRef);
                if (string.IsNullOrEmpty(userId))
                {
                    Core.Log.Error(NetworkingLogChannel.Connections, $"Unable to add user - UserID {userId} can't be connected to {playerRef}");
                    return;
                }
                ConnectUser(runner, playerRef, userId);
            }
            _playerJoinedEventPublisher.Publish(new() { Player = playerRef });
        }

        private void OnPlayerLeft(NetworkRunner runner, PlayerRef playerRef)
        {
            _playerLeftEventPublisher.Publish(new() { Player = playerRef });
            if (runner.IsServer)
            {
                if (_connectedPlayersByPlayerRef.TryGetValue(playerRef, out var userID))
                {
                    DisconnectUser(runner, playerRef, userID, BaseDisconnectReasonType.DisconnectedByClient);
                }
            }
        }

        private void ConnectUser(NetworkRunner runner, PlayerRef playerRef, string userId)
        {
            _connectedPlayersByPlayerRef.Add(playerRef, userId);
            _connectedPlayersByUserId.Add(userId, playerRef);
            if (_diconnectedUsers.TryGetValue(userId, out var oldConnection))
            {
                _diconnectedUsers.Remove(userId);
                Core.Log.Message(NetworkingLogChannel.Connections, $"User {userId} - {playerRef} reconnected");
            }
            else
            {
                Core.Log.Message(NetworkingLogChannel.Connections, $"User {userId} - {playerRef} connected");
            }
        }

        private void DisconnectUser(NetworkRunner runner, PlayerRef playerRef, string userID, DisconnectReasonType disconnectReasonType)
        {
            _connectedPlayersByPlayerRef.Remove(playerRef);
            _connectedPlayersByUserId.Remove(userID);
            var disconnectedPlayer = new DisconnectedPlayer
            {
                PlayerRef = playerRef,
                DisconnectReasonType = disconnectReasonType
            };
            _diconnectedUsers.Add(userID, disconnectedPlayer);
            Core.Log.Message(NetworkingLogChannel.Connections, $"User {userID} - {playerRef} disconnected - {disconnectReasonType.Name}");
        }

        private void OnSceneLoadDone(NetworkRunner runner)
        {
            _sceneLoadedEventPublisher.Publish(new() { });
        }

        #endregion
    }
}