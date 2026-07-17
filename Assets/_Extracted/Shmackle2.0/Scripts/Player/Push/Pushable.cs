using Fusion;
using System;
using UnityEngine;

namespace Shmackle.Player
{
    [Serializable]
    public struct NetworkPushIdentifierInfo : INetworkStruct
    {
        public NetworkBool IsValidPush;
        public PlayerRef LastPusher;
        public int LastPushTick;
    }
    
    public delegate void ReceivePushEventDelegate(NetworkObject source, Vector3 force);
    public delegate void PushIdentifierInfoChangedDelegate(Pushable source, NetworkPushIdentifierInfo pushIdentifierInfo);
    
    [RequireComponent(typeof(Rigidbody))]
    public class Pushable : NetworkBehaviour, IPushable
    {
        #region Serialized Fields

        [SerializeField, Tooltip("If the player is stationary for this long after being pushed, the last push is invalidated.")]
        private float _stationaryResetDelay = 2.0f;
        [SerializeField, Tooltip("X amount of seconds to wait before invalidating the last push")]
        private float _resetDelay = 8.0f;
        
        #endregion
        
        #region Private Fields
        
        private float _stationaryTimer = 0;
        
        #endregion
        
        #region Protected Fields

        protected Rigidbody _rigidbody;
        
        #endregion
        
        #region Public Fields
        
        public event ReceivePushEventDelegate ReceivePushEvent;
        public event PushIdentifierInfoChangedDelegate PushIdentifierInfoChanged;
        
        #endregion
        
        #region Properties

        public virtual Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody>();
                }
                return _rigidbody;
            }
        }
        
        [Networked, OnChangedRender(nameof(OnPushIdentifierInfoChanged))]
        public ref NetworkPushIdentifierInfo NetworkPushIdentifierInfo => ref MakeRef<NetworkPushIdentifierInfo>();
        
        #endregion
        
        #region Private Methods

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RpcOnReceivePush(NetworkObject source, Vector3 force)
        {
            InternalReceivePush(source, force);
        }

        private void OnPushIdentifierInfoChanged()
        {
            PushIdentifierInfoChanged?.Invoke(this, NetworkPushIdentifierInfo);
        }
        
        #endregion
        
        #region Protected Methods
        
        protected virtual void InternalReceivePush(NetworkObject source, Vector3 force)
        {
            Rigidbody.AddForce(force, ForceMode.Impulse);
            
            _stationaryTimer = 0;
            NetworkPushIdentifierInfo.IsValidPush = true;
            NetworkPushIdentifierInfo.LastPusher = source.InputAuthority;
            NetworkPushIdentifierInfo.LastPushTick = Runner.Tick.Raw;
            
            ReceivePushEvent?.Invoke(source, force);
        }
        
        #endregion
        
        #region Public Methods

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (HasStateAuthority)
            {
                if (NetworkPushIdentifierInfo.IsValidPush)
                {
                    if (Math.Round(Rigidbody.linearVelocity.y) >= 0)
                    {
                        _stationaryTimer += Runner.DeltaTime;
                        if (_stationaryTimer >= _stationaryResetDelay)
                        {
                            _stationaryTimer = 0;
                            NetworkPushIdentifierInfo.IsValidPush = false;
                        }
                    }
                    else
                    {
                        _stationaryTimer = 0;
                    }

                    // Check general delay
                    if (NetworkPushIdentifierInfo.IsValidPush)
                    {
                        int tickDelta = Runner.Tick - NetworkPushIdentifierInfo.LastPushTick;
                        float secondsDelta = tickDelta * Runner.DeltaTime;
                        if (secondsDelta >= _resetDelay)
                        {
                            NetworkPushIdentifierInfo.IsValidPush = false;
                        }
                    }
                }
            }
        }

        #endregion
        
        #region IPushable

        public void ReceivePush(NetworkObject source, Vector3 force)
        {
            RpcOnReceivePush(source, force);
        }

        #endregion
    }
}