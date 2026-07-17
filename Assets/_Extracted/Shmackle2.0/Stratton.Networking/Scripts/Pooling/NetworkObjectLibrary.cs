using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

namespace Stratton.Networking
{
    [Serializable]
    [CreateAssetMenu(fileName = "New NetworkObject Library", menuName = "Data/Networking/NetworkObject Library")]
    public class NetworkObjectLibrary : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField] private List<NetworkObjectData> _networkObjectsData = new List<NetworkObjectData>();

        #endregion

        #region Properties

        public List<NetworkObjectData> NetworkObjectsData => _networkObjectsData;

        #endregion

        #region Public Methods

        public bool TryGetNetworkObjectPrefab(NetworkObjectType networkObjectType, out NetworkObjectGuid networkObjectGuid)
        {
            foreach (var data in _networkObjectsData)
            {
                if (data.NetworkObjectType == networkObjectType)
                {
                    networkObjectGuid = new NetworkObjectGuid(data.NetworkObjectGuid);
                    return true;
                }
            }
            Core.Log.Error(NetworkingLogChannel.ObjectPool, $"NetworkObject prefab - {networkObjectType.Name} not present in NetworkObjectLibrary");
            networkObjectGuid = NetworkObjectGuid.Empty;
            return false;
        }

        #endregion
    }
}
