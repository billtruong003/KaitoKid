using Fusion;
using Fusion.XR.Shared.Core;
using MessagePipe;
using Shmackle.Networking;
using Stratton.Core;
using Stratton.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmackle.Pad
{
    public class ShmacklePadSpawner : NetworkBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private GameObject _hudPrefab;
        [SerializeField]
        private InputActionProperty _leftHoldInputAction;
        [SerializeField]
        private InputActionProperty _rightHoldInputAction;
        [SerializeField]
        private float _holdSeconds = 1.0f;

        #endregion

        #region Private Fields

        private IPublisher<ShmacklePadLoadingStartedEvent> _padLoadingStartedPublisher;
        private IPublisher<ShmacklePadLoadingProgressChangedEvent> _padLoadingProgressChangedPublisher;
        private IPublisher<ShmacklePadLoadingFinishedEvent> _padLoadingFinishedPublisher;

        private ShmacklePadLoadingStartedEvent _padLoadingStartedEvent;
        private ShmacklePadLoadingProgressChangedEvent _padLoadingProgressChangedEvent;
        private ShmacklePadLoadingFinishedEvent _padLoadingFinishedEvent;

        private NetworkingSystem _networkingSystem;

        private float _currentHoldTime = 0;
        private bool _isActivating = false;
        private RigPartSide _currentAttachSide = RigPartSide.Undefined;
        private InputAction _holdingInputAction;

        #endregion

        #region Properties

        [Networked, OnChangedRender(nameof(OnCurrentPadChanged))]
        private ShmacklePad CurrentPad { get; set; }

    #endregion

        #region Private Methods

        private void Update()
        {
            if (Object && Object.HasInputAuthority)
            {
                if (!CurrentPad || !CurrentPad.IsGrabbed)
                {
                    if (_holdingInputAction == null)
                    {
                        if (_rightHoldInputAction.action.WasPressedThisFrame())
                        {
                            StartPadLoading(RigPartSide.Right);
                        }
                        else if (_leftHoldInputAction.action.WasPressedThisFrame())
                        {
                            StartPadLoading(RigPartSide.Left);
                        }
                    }
                    else if (_holdingInputAction.WasReleasedThisFrame())
                    {
                        StopPadLoading(true);
                    }
                    if (_isActivating)
                    {
                        _currentHoldTime += Time.deltaTime;
                        _padLoadingProgressChangedEvent.Progress = Mathf.Min(1, _currentHoldTime / _holdSeconds);
                        _padLoadingProgressChangedPublisher.Publish(_padLoadingProgressChangedEvent);
                        if (_padLoadingProgressChangedEvent.Progress >= 1)
                        {
                            StopPadLoading(false);
                        }
                    }
                }
            }
        }

        private void OnCurrentPadChanged()
        {
            _padLoadingFinishedEvent.Pad = CurrentPad;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SpawnPad()
        {
            if (CurrentPad != null)
            {
                return;
            }
            if (_networkingSystem.NetworkObjectPool.TryGetNetworkObjectPrefab(ShmackleNetworkObjectType.ShmacklePad, out var shmacklePadPrefab))
            {
                var shmacklePadNO = Runner.Spawn(shmacklePadPrefab, transform.position, transform.rotation, Object.InputAuthority);
                CurrentPad = shmacklePadNO.GetComponent<ShmacklePad>();
            }
        }

        private void StartPadLoading(RigPartSide side)
        {
            _currentAttachSide = side;
            _isActivating = true;
            _padLoadingStartedEvent.Side = side;
            _padLoadingStartedPublisher.Publish(_padLoadingStartedEvent);
            _holdingInputAction = side == RigPartSide.Left ? _leftHoldInputAction.action : _rightHoldInputAction.action;
        }

        private void StopPadLoading(bool isCancelled)
        {
            _isActivating = false;
            _currentHoldTime = 0;
            _holdingInputAction = null;

            _padLoadingFinishedEvent.Side = _currentAttachSide;
            _padLoadingFinishedEvent.IsCancelled = isCancelled;
            _padLoadingFinishedPublisher.Publish(_padLoadingFinishedEvent);
        }

        #endregion

        #region Public Methods

        public override void Spawned()
        {
            base.Spawned();

            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();

            // Only local player needs networked properties (current input).
            // This saves network traffic by not synchronizing networked properties to other clients except local player.
            ReplicateToAll(false);
            ReplicateTo(Object.InputAuthority, true);
            if (HasInputAuthority)
            {
                if (_rightHoldInputAction.action.bindings.Count == 0)
                {
                    _rightHoldInputAction.action.AddBinding("<Keyboard>/space");
                    _rightHoldInputAction.action.AddBinding("<XRController>{RightHand}/secondaryButton");
                }
                _rightHoldInputAction.action.Enable();

                if (_leftHoldInputAction.action.bindings.Count == 0)
                {
                    _leftHoldInputAction.action.AddBinding("<XRController>{LeftHand}/secondaryButton");
                }
                _leftHoldInputAction.action.Enable();

                _padLoadingStartedEvent = new ShmacklePadLoadingStartedEvent();
                _padLoadingProgressChangedEvent = new ShmacklePadLoadingProgressChangedEvent();
                _padLoadingFinishedEvent = new ShmacklePadLoadingFinishedEvent();

                _padLoadingProgressChangedEvent.Progress = 0;
                _padLoadingStartedPublisher = GlobalMessagePipe.GetPublisher<ShmacklePadLoadingStartedEvent>();
                _padLoadingProgressChangedPublisher = GlobalMessagePipe.GetPublisher<ShmacklePadLoadingProgressChangedEvent>();
                _padLoadingFinishedPublisher = GlobalMessagePipe.GetPublisher<ShmacklePadLoadingFinishedEvent>();
                
                RPC_SpawnPad();

                if (_hudPrefab)
                {
                    // Instantiate notifications HUD as child of camera to follow head movement
                    var headSet = HardwareRigsRegistry.GetHardwareRig().Headset;
                    if (headSet != null)
                        Instantiate(_hudPrefab, headSet.gameObject.transform);
                }
            }
        }

        #endregion
    }
}