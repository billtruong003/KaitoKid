using Fusion;
using MessagePipe;
using UnityEngine;

namespace Shmackle.Player
{

    public class ButtSmackNetworkEventRelay
    {
        public Vector3 ContactPosition;
    }

    public class KissNetworkEventRelay
    {
        public Vector3 ContactPosition;
        public Quaternion ContactRotation;
        public NetworkId TargetId;
    }

    /// <summary>
    /// Central RPC-based event broadcaster for a networked player. Attach to the player root to relay
    /// locally-triggered gameplay events (e.g., SFX, VFX, state changes) to all clients. Provides RPC
    /// entry points to invoke synchronized events and local callbacks for systems that need to react.
    /// Use as the go-to component for cross-client event propagation.
    /// </summary>
    public class PlayerNetworkEventRelay : NetworkBehaviour
    {
        private IPublisher<ButtSmackNetworkEventRelay> _playerButtSmackEventPublisher;
        private IPublisher<KissNetworkEventRelay> _playerKissEventPublisher;

        public override void Spawned()
        {
            _playerButtSmackEventPublisher = GlobalMessagePipe.GetPublisher<ButtSmackNetworkEventRelay>();
            _playerKissEventPublisher = GlobalMessagePipe.GetPublisher<KissNetworkEventRelay>();
        }

        public void ButtSmackEvent(Vector3 contactPosition)
        {
            if (!HasStateAuthority) return;
            RPC_ButtSmackEvent(contactPosition);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ButtSmackEvent(Vector3 contactPosition)
        {
            _playerButtSmackEventPublisher.Publish(new ButtSmackNetworkEventRelay { ContactPosition = contactPosition});
        }
        
        public void KissEvent(Vector3 contactPosition, Quaternion contactRotation, NetworkId targetId)
        {
            if (!HasStateAuthority) return;
            RPC_KissEvent(contactPosition, contactRotation, targetId);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_KissEvent(Vector3 contactPosition, Quaternion contactRotation, NetworkId targetId)
        {
            _playerKissEventPublisher.Publish(new KissNetworkEventRelay
            {
                ContactPosition = contactPosition,
                ContactRotation = contactRotation,
                TargetId = targetId
            });
        }
    }
}