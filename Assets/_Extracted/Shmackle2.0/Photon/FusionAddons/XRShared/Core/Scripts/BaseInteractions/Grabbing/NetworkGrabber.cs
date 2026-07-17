using UnityEngine;

namespace Fusion.XR.Shared.Core
{
    // Should be placed next to a INetworkRigPart
    [DefaultExecutionOrder(NetworkGrabber.EXECUTION_ORDER)]
    public class NetworkGrabber : NetworkBehaviour, INetworkGrabber
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
