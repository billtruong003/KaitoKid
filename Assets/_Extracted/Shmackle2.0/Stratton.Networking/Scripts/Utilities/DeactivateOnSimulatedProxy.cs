using Fusion;
using UnityEngine;

namespace Stratton.Networking
{
    public class DeactivateOnSimulatedProxy : NetworkBehaviour, IAfterSpawned
    {
        #region Serialized Fields

        [SerializeField]
        private NetworkObject _sourceNetworkObject = null;

        #endregion

        #region IAfterSpawned

        void IAfterSpawned.AfterSpawned()
        {
            if (_sourceNetworkObject == null)
            {
                _sourceNetworkObject = GetComponentInParent<NetworkObject>();
            }
            if (_sourceNetworkObject != null)
            {
                if (!_sourceNetworkObject.HasInputAuthority && !_sourceNetworkObject.HasStateAuthority)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        #endregion
    }
}
