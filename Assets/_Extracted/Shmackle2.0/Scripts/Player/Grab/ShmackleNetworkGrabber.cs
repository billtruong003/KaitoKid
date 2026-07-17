using Fusion;
using Fusion.XR.Shared.Core;
using UnityEngine;

namespace Shmackle.Player.Grab
{
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class ShmackleNetworkGrabber : NetworkBehaviour, INetworkGrabber
    {
        public const int EXECUTION_ORDER = INetworkRig.EXECUTION_ORDER + 10;
        #region INetworkGrabber
        public INetworkRigPart RigPart => networkRigPart;
        #endregion

        INetworkRigPart networkRigPart;

        protected virtual void Awake()
        {
            networkRigPart = GetComponent<INetworkRigPart>();
            if (networkRigPart == null)
            {
                Debug.LogError("[NetworkGrabber] Missing INetworkRigPart (NetworkHand, NetworkController, ...)");
            }
        }
    }
}