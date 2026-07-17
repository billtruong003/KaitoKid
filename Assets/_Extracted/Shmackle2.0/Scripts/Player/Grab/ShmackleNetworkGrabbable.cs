using Fusion;
using Fusion.XR.Shared.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.Player.Grab
{
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class ShmackleNetworkGrabbable : NetworkBehaviour, INetworkGrabbable
    {
        public const int EXECUTION_ORDER = INetworkGrabbable.EXECUTION_ORDER;
        
#if USE_PHYSICSADDON
        private NetworkRigidbody3D _networkRigidbody;
#endif
        
        public virtual bool IsGrabbed => Object != null && CurrentGrabber != null; // We make sure that we are online before accessing [Networked] var

        public UnityEvent OnGrab => _onDidGrabWithoutDetails;
        public UnityEvent OnUngrab => _onUngrab;
        public UnityEvent<GameObject> OnLocalUserGrab => _grabbable?.OnLocalUserGrab;
        
        [Networked]
        public NetworkBool InitialIsKinematicState { get; set; }
        
        [Networked]
        public ShmackleNetworkGrabber CurrentGrabber { get; set; }
        
        [Networked]
        public Vector3 LocalPositionOffset { get; set; }
        
        [Networked]
        public Quaternion LocalRotationOffset { get; set; }
        
        public bool IsReceivingAuthority => _isTakingAuthority;

        [Header("Events")]
        [SerializeField] private UnityEvent _onGrab = new UnityEvent();
        [SerializeField] private UnityEvent _onUngrab = new UnityEvent();
        // For IGrabbable interface compatibility
        private UnityEvent _onDidGrabWithoutDetails = new UnityEvent();
        
        [Header("Advanced options")]
        [SerializeField] private bool _extrapolateWhileTakingAuthority = true;
        [SerializeField]private bool _isTakingAuthority = false;
        [SerializeField] [Tooltip("If true, no check on the state authority options will be done")]
        private bool _allowNonTransferableObject = false;
        [SerializeField] private bool _onlyAllowInputAuthority = false;
        INetworkGrabber INetworkGrabbable.CurrentGrabber => CurrentGrabber;

        private ShmackleGrabbable _grabbable;
        private ChangeDetector _funChangeDetector;
        private ChangeDetector _renderChangeDetector;
        protected virtual void Awake()
        {
#if USE_PHYSICSADDON
            networkRigidbody = GetComponent<NetworkRigidbody3D>();
#endif
            _grabbable = GetComponent<ShmackleGrabbable>();
            if (_grabbable == null)
            {
                // We do not use requireComponent as this classes can be subclassed
                _grabbable = gameObject.AddComponent<ShmackleGrabbable>();
            }
        }
        
        private bool TryDetectGrabberChange(ChangeDetector changeDetector, out ShmackleNetworkGrabber previousGrabber, out ShmackleNetworkGrabber currentGrabber)
        {
            previousGrabber = null;
            currentGrabber = null;
            foreach (var changedNetworkedVarName in changeDetector.DetectChanges(this, out var previous, out var current))
            {
                if (changedNetworkedVarName == nameof(CurrentGrabber))
                {
                    var grabberReader = GetBehaviourReader<ShmackleNetworkGrabber>(changedNetworkedVarName);
                    previousGrabber = grabberReader.Read(previous);
                    currentGrabber = grabberReader.Read(current);
                    return true;
                }
            }
            return false;
        }

        public virtual void LocalUngrab()
        {
            if (Object)
            {
                CurrentGrabber = null;
            }
        }

        public async virtual void LocalGrab()
        {
            if (!Object || !Object.IsValid) return;

            if (!TryGrab())
                return;

            // Ask and wait to receive the stateAuthority to move the object
            _isTakingAuthority = true;
            await Object.WaitForStateAuthority();
            _isTakingAuthority = false;

            // We waited to have the state authority before setting Networked vars
            LocalPositionOffset = _grabbable.LocalPositionOffset;
            LocalRotationOffset = _grabbable.LocalRotationOffset;

            if(!_grabbable.CurrentGrabber)
                return;
            // Update the CurrentGrabber in order to start following position in the FixedUpdateNetwork
            CurrentGrabber = _grabbable.CurrentGrabber.NetworkGrabber;
        }
        
        public override void Spawned()
        {
            base.Spawned();

            var rigidBody = _grabbable.GetRigidbody();
            
            // Save initial kinematic state for later join player
            if (Object.HasStateAuthority && rigidBody)
            {
                InitialIsKinematicState = rigidBody.isKinematic;
            }


            // We store the default kinematic state, while it is not affected by NetworkRigidbody logic
            _grabbable.ExpectedIsKinematic = InitialIsKinematicState;

            _funChangeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            _renderChangeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

            Application.onBeforeRender += OnBeforeRender;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);  
            Application.onBeforeRender -= OnBeforeRender;
        }

        public override void FixedUpdateNetwork()
        {
            if (!_isTakingAuthority && CurrentGrabber && CurrentGrabber.Object.StateAuthority != Object.StateAuthority)
            {
                CurrentGrabber = null;
            }
            // Check if the grabber changed
            if (TryDetectGrabberChange(_funChangeDetector, out var previousGrabber, out var currentGrabber))
            {
                if (previousGrabber)
                {
                    _grabbable.UnlockObjectPhysics();
                }
                if (currentGrabber)
                {
                    _grabbable.LockObjectPhysics();
                }
            }

            if (!IsGrabbed) return;
            // Follow grabber, adding position/rotation offsets
            _grabbable.Follow(followedTransform: CurrentGrabber.transform, LocalPositionOffset, LocalRotationOffset);
        }
        
        public override void Render()
        {
            // Check if the grabber changed, to trigger callbacks only (actual grabbing logic in handled in FUN for the state authority)
            // Those callbacks can't be called in FUN, as FUN is not called on proxies, while render is called for everybody
            if (TryDetectGrabberChange(_renderChangeDetector, out var previousGrabber, out var currentGrabber))
            {
                if (previousGrabber)
                {
                    _onUngrab?.Invoke();
                }
                if (currentGrabber)
                {
                    _onGrab?.Invoke();
                    _onDidGrabWithoutDetails?.Invoke();
                }
            }

            ExtrapolationHandling(ExtrapolationTiming.DuringFusionRender);            
        }

        /// <summary>
        /// Determine if we should override the grabbable position, to better match the hand position
        /// </summary>
        /// <param name="currentTiming"></param>
        protected virtual void ExtrapolationHandling(ExtrapolationTiming currentTiming)
        {
            if (_isTakingAuthority && _extrapolateWhileTakingAuthority && _grabbable.CurrentGrabber)
            {
                ShmackleNetworkGrabber incomingNetworkGrabber = _grabbable.CurrentGrabber.NetworkGrabber;
                if (incomingNetworkGrabber != null && incomingNetworkGrabber.RigPart.RequiredExtrapolationTiming() == currentTiming)
                {
                    // If we are currently taking the authority on the object due to a grab, the network info are still not set
                    //  but we will extrapolate anyway (if the option extrapolateWhileTakingAuthority is true) to avoid having the grabbed object staying still until we receive the authority
                    ExtrapolateWhileTakingAuthority();
                    return;
                }
            }

            if (CurrentGrabber != null && CurrentGrabber.RigPart.RequiredExtrapolationTiming() == currentTiming)
            {
                Extrapolate();
            }
        }

        [BeforeRenderOrder(EXECUTION_ORDER)]
        protected virtual void OnBeforeRender()
        {
            ExtrapolationHandling(ExtrapolationTiming.DuringUnityOnBeforeRender);
        }

        // Extrapolation: Make visual representation follow grabber, adding position/rotation offsets
        protected virtual void Extrapolate()
        {
            // No need to extrapolate if the object is not grabbed.
            // We do not extrapolate for proxies (might be relevant in some cases, but then the grabbing itself should be properly extrapolated, to avoid grabbing visually before the hand interpolation has reached the grabbing position)
            if (!IsGrabbed || Object.HasStateAuthority == false) return;
            var follwedGrabberRoot = CurrentGrabber != null ? CurrentGrabber.gameObject : null;
            _grabbable.Follow(followedTransform: follwedGrabberRoot.transform, LocalPositionOffset, LocalRotationOffset);
        }

        protected virtual void ExtrapolateWhileTakingAuthority()
        {
            // No need to extrapolate if the object is not really grabbed
            if (_grabbable.CurrentGrabber == null) return;
            ShmackleNetworkGrabber networkGrabber = _grabbable.CurrentGrabber.NetworkGrabber;

            // Extrapolation: Make visual representation follow grabber, adding position/rotation offsets
            // We use grabberWhileTakingAuthority instead of CurrentGrabber as we are currently waiting for the authority transfer: the network vars are not already set, so we use the temporary versions
            var follwedGrabberRoot = networkGrabber != null ? networkGrabber : null;
            _grabbable.Follow(followedTransform: follwedGrabberRoot.transform, _grabbable.LocalPositionOffset, _grabbable.LocalRotationOffset);
        }

        void CheckTransferableAuthority(NetworkObject no = null)
        {
            if (_allowNonTransferableObject) return;
            if (no == null) no = Object;
            if (no != null && no.IsObjectWithTransferableAuthority() == false)
            {
                Debug.LogError($"[NetworkGrabbable] {name}'s NetworkObject does not have a proper configuration to allow users to change authority on this:" +
                    " check AllowStateAuthorityOverride, uncheck DestroyOnStateAuthorityLeaves, uncheck IsMasterClientObject." +
                    " If you want other settings, check allowNonTransferableObject on the NetworkGrabbable");
            }
        }

        private void OnValidate()
        {
            if (_allowNonTransferableObject) return;
            ValidationUtils.SceneEditionValidate(gameObject, () => {
                CheckTransferableAuthority(GetComponentInParent<NetworkObject>());
            });
        }

        public virtual bool TryGrab()
        {
            if (_onlyAllowInputAuthority)
            {
                // If no object, it means it is locally spawned only
                return !Object || Object.HasInputAuthority || Object.HasStateAuthority;
            }
            return true;
        }
    }
}