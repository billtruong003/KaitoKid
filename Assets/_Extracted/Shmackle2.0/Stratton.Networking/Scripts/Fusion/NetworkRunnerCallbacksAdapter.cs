using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Networking
{
    public class NetworkRunnerCallbacksAdapter : INetworkRunnerCallbacks
    {
        #region Events

        public event Action<NetworkRunner, PlayerRef> PlayerJoined;
        public event Action<NetworkRunner, PlayerRef> PlayerJoinedHostMode;
        public event Action<NetworkRunner, PlayerRef> PlayerJoinedSharedMode;
        public event Action<NetworkRunner, PlayerRef> PlayerLeft;
        public event Action<NetworkRunner, PlayerRef> PlayerLeftHostMode;
        public event Action<NetworkRunner> SceneLoadDone;

        #endregion

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            PlayerJoined?.Invoke(runner, player);
            if (runner.Topology == Topologies.ClientServer)
            {
                PlayerJoinedHostMode?.Invoke(runner, player);
            }
            else
            {
                PlayerJoinedSharedMode?.Invoke(runner, player);
            }
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            PlayerLeft?.Invoke(runner, player);
            if (runner.Topology == Topologies.ClientServer)
            {
                PlayerLeftHostMode?.Invoke(runner, player);
            }
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("OnConnectedToServer");

        }
        public void OnShutdown(NetworkRunner runner, Fusion.ShutdownReason shutdownReason)
        {
            Debug.Log("Shutdown: " + shutdownReason);
        }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log("OnDisconnectedFromServer: " + reason);
        }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.Log("OnConnectFailed: " + reason);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress) { }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            SceneLoadDone?.Invoke(runner);
        }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        #endregion
    }
}