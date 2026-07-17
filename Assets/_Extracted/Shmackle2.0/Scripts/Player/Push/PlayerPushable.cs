using Fusion;
using System.Collections;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Shmackle.Player
{
    public class PlayerPushable : Pushable
    {
        #region Serialized Fields

        [SerializeField]
        private float _locomotionResetDelay = 0.5f;
        
        #endregion
        
        #region Private Fields

        private PlayerLocomotion _localPlayerLocomotion;
        private Coroutine _resetLocomotionCoroutine;
        private YieldInstruction _resetLocomotionYieldInstruction;
        
        #endregion
        
        #region Properties

        protected PlayerLocomotion LocalPlayerLocomotion
        {
            get
            {
                if (_localPlayerLocomotion == null)
                {
                    PlayerNetworkRig playerNetworkRig = GetComponent<PlayerNetworkRig>();
                    if (playerNetworkRig != null)
                    {
                        _localPlayerLocomotion = playerNetworkRig.LocalPlayerLocomotion;
                    }
                }
                return _localPlayerLocomotion;
            }
        }
        
        public override Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    if (HasInputAuthority)
                    {
                        if (LocalPlayerLocomotion != null)
                        {
                            _rigidbody = LocalPlayerLocomotion.Rigidbody;
                        }
                    }
                    else
                    {
                        _rigidbody = GetComponent<Rigidbody>();
                    }
                }
                return _rigidbody;
            }
        }
        
        #endregion
        
        #region Protected Methods

        protected override void InternalReceivePush(NetworkObject source, Vector3 force)
        {
            if (_resetLocomotionCoroutine != null)
            {
                StopCoroutine(_resetLocomotionCoroutine);
            }
            if (LocalPlayerLocomotion != null)
            {
                LocalPlayerLocomotion.SetDisableMovement(true);
            }
            base.InternalReceivePush(source, force);
            _resetLocomotionCoroutine = StartCoroutine(WaitResetPlayerLocomotion());
        }

        #endregion
        
        #region Private Methods

        private void Awake()
        {
            _resetLocomotionYieldInstruction = new WaitForSeconds(_locomotionResetDelay);
        }

        private void OnDisable()
        {
            ResetPlayerLocomotion();
        }

        private void ResetPlayerLocomotion()
        {
            if (LocalPlayerLocomotion != null)
            {
                LocalPlayerLocomotion.SetDisableMovement(false);
            }
        }
        
        private IEnumerator WaitResetPlayerLocomotion()
        {
            yield return _resetLocomotionYieldInstruction;
            ResetPlayerLocomotion();
            _resetLocomotionCoroutine = null;
        }
        
        #endregion
    }
}