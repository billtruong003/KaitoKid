using Fusion;
using Fusion.XR.Shared.Core;
using Shmackle.Player.Grab;
using UnityEngine;

namespace Shmackle.Pad
{
    [System.Serializable]
    public struct ShmacklePadActivationInfo : INetworkStruct
    {
        public NetworkBool IsActive;
        public RigPartSide Side;
    }
    public class ShmacklePad : NetworkBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private GameObject _mainSwitch;
        [SerializeField]
        private Collider[] _stateColliders;
        [SerializeField]
        private GameObject[] _stateGameObjects;
        [SerializeField]
        private Animator _animator;

        #endregion

        #region Properties

        public ShmackleGrabbable Grabbable { get; private set; }

        [Networked, OnChangedRender(nameof(OnShmacklePadActivationInfoChanged))]
        public ref ShmacklePadActivationInfo ShmacklePadActivationInfo => ref MakeRef<ShmacklePadActivationInfo>();
        public bool IsGrabbed => Grabbable != null && Grabbable.IsGrabbed;

        #endregion

        #region Private Fields

        private static readonly int _isActiveAnimHash = Animator.StringToHash("IsActive");
        private static readonly int _isLeftSideAnimHash = Animator.StringToHash("IsLeftSide");

        #endregion

        #region Private Methods

        private void Awake()
        {
            Grabbable = GetComponent<ShmackleGrabbable>();
            if (_stateColliders.Length == 0)
            {
                _stateColliders = GetComponentsInChildren<Collider>();
            }
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            // Disable always on start, update on Spawned
            _mainSwitch.SetActive(false);
            SetVisibility(false);
        }

        private void OnShmacklePadActivationInfoChanged()
        {
            if (_animator)
            {
                _animator.SetBool(_isLeftSideAnimHash, ShmacklePadActivationInfo.Side == RigPartSide.Left);
            }
            SetVisibility(ShmacklePadActivationInfo.IsActive);
        }


        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetActiveStatus(bool isActive)
        {
            ShmacklePadActivationInfo.IsActive = isActive;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_Activate(RigPartSide side)
        {
            ShmacklePadActivationInfo.Side = side;
            ShmacklePadActivationInfo.IsActive = true;
        }

        private void SetVisibility(bool isVisibile)
        {
            for(int i = 0; i < _stateColliders.Length; i++)
            {
                _stateColliders[i].enabled = isVisibile;
            }

            for (int i = 0; i < _stateGameObjects.Length; i++)
            {
                _stateGameObjects[i].SetActive(isVisibile);
            }

            if (_animator != null)
            {
                _animator.SetBool(_isActiveAnimHash, isVisibile);
            }

            if (isVisibile)
            {
                if (_mainSwitch != null)
                {
                    _mainSwitch.SetActive(true);
                }
            }
        }

        #endregion

        #region Public Methods

        public override void Spawned()
        {
            base.Spawned();
            SetVisibility(ShmacklePadActivationInfo.IsActive);

            // Initial state
            if (_mainSwitch != null)
            {
                _mainSwitch.SetActive(ShmacklePadActivationInfo.IsActive);
            }
        }

        public void Activate(RigPartSide side)
        {
            RPC_Activate(side);
        }

        public void Deactivate()
        {
            if (Grabbable && Grabbable.CurrentGrabber != null)
            {
                ShmackleGrabber grabber = Grabbable.CurrentGrabber;
                grabber.TryRelease();

                if (grabber.RigPart is IOverridableGrabbingProvider grabProvider)
                {
                    // If force ungrabbed
                    grabProvider.OverrideGrabbing(false);
                }
            }
            RPC_SetActiveStatus(false);
        }

        #endregion
    }
}
