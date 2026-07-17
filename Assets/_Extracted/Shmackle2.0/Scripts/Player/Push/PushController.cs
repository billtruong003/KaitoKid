using Fusion;
using System;
using System.Collections.Generic;
using Shmackle.Gameplay;
using Stratton.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shmackle.Player
{
    public class PushController : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Input Actions")]
        [SerializeField]
        private InputActionProperty _leftHoldInputAction;
        [SerializeField]
        private InputActionProperty _rightHoldInputAction;
        
        [Header("Prefabs")]
        [SerializeField]
        private GameObject _pushUIPrefab; 
        
        [Header("Scene References")]
        [SerializeField]
        private Transform _sourceTransform;

        [Header("Values")] 
        [SerializeField]
        private bool _overrideableByGameMode = true;
        [SerializeField]
        private PushConfig _pushConfig;
        
        #endregion
        
        #region Private Fields
        
        private Collider[] _colliders;
        
        // For caching only
        private Dictionary<Collider, IPushable> _colliderPushables = new();
        private HashSet<IPushable> _pushablesInContact = new();
        
        private IPushable _ownPushable;
        private PlayerLocomotion _localPlayerLocomotion;
        
        private float _chargeStartTime;
        
        private TickTimer _cooldownTimer;
        
        #endregion
        
        #region Public Fields
        
        public event Action<int> ChargeChanged;
        
        #endregion
        
        #region Properties

        [Header("Runtime Values")]
        [Networked, OnChangedRender(nameof(OnChargeChanged))]
        public int Charge { get; private set; } = -1;
        public float ChargePercent => Mathf.Clamp01((Time.time - _chargeStartTime) / _pushConfig.MaxChargeTime);
        public float CooldownPercent => (1 - (_cooldownTimer.RemainingTime(Runner) ?? 0) / _pushConfig.Cooldown);
        
        protected PlayerLocomotion LocalPlayerLocomotion
        {
            get
            {
                if (!_localPlayerLocomotion)
                {
                    PlayerNetworkRig playerNetworkRig = GetComponentInParent<PlayerNetworkRig>();
                    if (playerNetworkRig)
                    {
                        _localPlayerLocomotion = playerNetworkRig.LocalPlayerLocomotion;
                    }
                }
                return _localPlayerLocomotion;
            }
        }
        
        #endregion
        
        #region Public Methods

        public override void Spawned()
        {
            base.Spawned();
            
            ReplicateToAll(false);
            ReplicateTo(Object.InputAuthority, true);

            if (_overrideableByGameMode)
            {
                GameplaySystem gameplaySystem = GameSystemsManager.Instance.Get<GameplaySystem>();
                GameModeBase gameMode = gameplaySystem.GetActiveGameMode<GameModeBase>();
                if (gameMode)
                {
                    GameModeSettings gameModeSettings = gameMode.GetSettings<GameModeSettings>();
                    if (gameModeSettings)
                    {
                        if (gameModeSettings is IPushConfig externalPushConfig)
                        {
                            _pushConfig = externalPushConfig.PushConfig;
                            Debug.Log($"Using external push config: {_pushConfig.Cooldown}");
                        }
                    }
                }
            }

            if (HasInputAuthority)
            {    
                // Right now, left and right just have the same behavior
                if (_leftHoldInputAction.action.bindings.Count == 0)
                {
                    _leftHoldInputAction.action.AddBinding("<Keyboard>/leftShift");
                    _leftHoldInputAction.action.AddBinding("<XRController>{LeftHand}/trigger");
                }
                _leftHoldInputAction.action.Enable();
                
                if (_rightHoldInputAction.action.bindings.Count == 0)
                {
                    _rightHoldInputAction.action.AddBinding("<Keyboard>/rightShift");
                    _rightHoldInputAction.action.AddBinding("<XRController>{RightHand}/trigger");
                }
                _rightHoldInputAction.action.Enable();

                Instantiate(_pushUIPrefab, transform);
            }
        }

        #endregion
        
        #region Private Methods

        private void Awake()
        {
            _ownPushable = GetComponentInParent<IPushable>();
            _colliders = GetComponentsInChildren<Collider>();
            if (_sourceTransform == null)
            {
                _sourceTransform = transform.root;
            }
            SetColllisionEnabled(false);
        }
        
        private void Update()
        {
            if (Object && Object.HasInputAuthority)
            {
                if (_cooldownTimer.ExpiredOrNotRunning(Runner))
                {
                    if (_leftHoldInputAction.action.WasPressedThisFrame() ||
                        _rightHoldInputAction.action.WasPressedThisFrame())
                    {
                        Charge = 0;
                        _chargeStartTime = Time.time;
                        SetColllisionEnabled(true);
                    }
                    else if (_leftHoldInputAction.action.IsPressed() || _rightHoldInputAction.action.IsPressed())
                    {
                        if (Charge >= 0) // Make sure players pressed it first when there's no cooldown 
                        {
                            Charge = Mathf.FloorToInt((_pushConfig.PushForcePerCharge.Length - 1) * ChargePercent);
                            LocalPlayerLocomotion.SetRuntimeJumpMultiplier(_pushConfig.ChargeJumpMultiplier);
                        }
                    }
                    else if (_leftHoldInputAction.action.WasReleasedThisFrame() ||
                             _rightHoldInputAction.action.WasReleasedThisFrame())
                    {
                        if (Charge >= 0)
                        {
                            SendPush();
                            SetColllisionEnabled(false);
                            Charge = -1;
                            ResetCooldown();
                            LocalPlayerLocomotion.SetRuntimeJumpMultiplier(1);
                        }
                    }
                }
            }
        }

        private void SetColllisionEnabled(bool isEnabled)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                _colliders[i].enabled = isEnabled;
            }
            if (!isEnabled)
            {
                _pushablesInContact.Clear();
            }
        }

        private void SendPush()
        {
            float force = _pushConfig.PushForcePerCharge[Charge];
            foreach (IPushable pushable in _pushablesInContact)
            {
                Vector3 direction = transform.forward;
                direction.y = 0;
                direction.Normalize();
                Debug.DrawRay(_sourceTransform.position, direction * 100, Color.blue, 10);
                pushable.ReceivePush(Object, direction * force);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IPushable pushable = null;
            if (_colliderPushables.ContainsKey(other))
            {
                pushable = _colliderPushables[other];
            }
            else
            {
                pushable = other.GetComponentInParent<IPushable>();
                if (pushable != null)
                {
                    _colliderPushables.Add(other, pushable);
                }
            }
            if (pushable != null)
            {
                if (pushable != _ownPushable)
                {
                    _pushablesInContact.Add(pushable);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_colliderPushables.TryGetValue(other, out IPushable pushable))
            {
                _pushablesInContact.Remove(pushable);
            }
        }

        private void OnChargeChanged()
        {
            ChargeChanged?.Invoke(Charge);
        }

        private void ResetCooldown()
        {
            if (_pushConfig.Cooldown > 0)
            {
                _cooldownTimer = TickTimer.CreateFromSeconds(Runner, _pushConfig.Cooldown);
            }
        }
        
        #endregion
    }
}