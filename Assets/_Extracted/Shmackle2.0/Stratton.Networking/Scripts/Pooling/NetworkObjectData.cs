using Fusion;
using UnityEngine;

namespace Stratton.Networking
{
    [System.Serializable]
    public class NetworkObjectData
    {
        public NetworkObjectType NetworkObjectType;
        public NetworkObject NetworkObjectPrefab;
        public string NetworkObjectGuid;
        [Tooltip("Include object in pooler")]
        public bool Poolable;
        [Tooltip("Works only if Poolable is set to true")]
        public int PrewarmedInstances;
        [Tooltip("Works only if Poolable is set to true")]
        public int MaxInstances;
    }
}
