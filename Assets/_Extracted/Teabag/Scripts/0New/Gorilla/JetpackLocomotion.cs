using System;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Teabag.Player
{
    /// <summary>
    /// Locomotion module for the Jetpack. Inherits from BaseLocomotionModule
    /// to integrate directly into the HardwareRig locomotion system.
    /// </summary>
    [Serializable]
    public class JetpackLocomotion : BaseLocomotionModule
    {
        [Header("Configuration")]
        [SerializeField] private JetpackConfig _config;

        private Vector3 _forceDirection = Vector3.zero;
        private float _remainingFuel;
        private bool _isInputActive;

        public event Action<float> OnFuelChange;
        public float FuelRatio => _config != null && _config.MaxFuel > 0 ? _remainingFuel / _config.MaxFuel : 0;

        // Cached service references. Resolved in Initialize() rather than in a field
        // initializer — field initializers run during Unity deserialization, which can
        // fire before ServiceLocator is populated (returning null and binding it for
        // the lifetime of this module). ServiceLocator.Get in FixedUpdate is also a
        // hot-path allocation we don't need to pay every tick.
        private IGameLoopService _gameLoopService;
        private IGorillaService _gorillaService;

        public override void Initialize(ref LocomotionContextValues contextValues)
        {
            base.Initialize(ref contextValues);
            _gameLoopService = ServiceLocator.Get<IGameLoopService>();
            _gorillaService = ServiceLocator.Get<IGorillaService>();
            Reset();
        }

        public override void Reset()
        {
            _forceDirection = Vector3.zero;
            _isInputActive = false;
            if (_config != null)
            {
                _remainingFuel = _config.MaxFuel;
            }
        }

        public override void RegisterEvents()
        {
            if (_config != null && _config.ActivateInput != null && _config.ActivateInput.action != null)
            {
                _config.ActivateInput.action.started += OnActivateStarted;
                _config.ActivateInput.action.canceled += OnActivateCanceled;
            }
        }

        public override void UnregisterEvents()
        {
            if (_config != null && _config.ActivateInput != null && _config.ActivateInput.action != null)
            {
                _config.ActivateInput.action.started -= OnActivateStarted;
                _config.ActivateInput.action.canceled -= OnActivateCanceled;
            }

            _isInputActive = false;
            SyncJetpackState(false);
        }

        private bool CanActivateJetpack()
        {
            if(_contextValues.IsGrounded
            || _contextValues.WasLeftHandTouching
            || _contextValues.WasRightHandTouching){
                return false; }

            //checks if gameloop has started.
            //makes jetpack unlimited if in lobby or waiting area
            if (_gameLoopService == null || !_gameLoopService.HasManager)
            {
                _remainingFuel = _config.MaxFuel;
                return true;
            }
            return _remainingFuel > 0.01f;
        }

        public override void Update()
        {
            // Input is handled via events, but we could do per-frame input checking here if needed
        }

        public override void FixedUpdate()
        {
            if (!_isInputActive)
                return;

            HandleMovement(Time.fixedDeltaTime);
        }

        public override void LateUpdate() { }

        private void HandleMovement(float dt)
        {
            var gorilla = _gorillaService?.LocalGorilla as Gorilla;
            if (gorilla == null)
            {
                return;
            }
            if (!CanActivateJetpack())
            {
                _isInputActive = false;
                SyncJetpackState(false);
                return;
            }
            Vector3 currentVelocity = _contextValues.LinearVelocity;

            Vector3 forward = Vector3.ProjectOnPlane(_contextValues.BodyForward, Vector3.up).normalized;
            Vector3 targetVelocity = forward * _config.MaxVelocityXZ + Vector3.up * _config.TargetVelocityY;

            // Vertical control
            float yError = targetVelocity.y - currentVelocity.y;
            float yForce = yError * _config.AccelerationGainY;

            // Horizontal control
            Vector3 currentXZ = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            Vector3 targetXZVel = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            Vector3 xzError = targetXZVel - currentXZ;
            Vector3 xzForce = xzError * _config.AccelerationGainXZ;
            Vector3 force = xzForce + Vector3.up * yForce;

            // ======================
            // Gravity Compensation
            // ======================
            force.y += -Physics.gravity.y;

            // Clamp Acceleration (based on MaxForce in config)
            float magnitude = force.magnitude;
            if (magnitude > _config.MaxForce)
            {
                force = force.normalized * _config.MaxForce;
                magnitude = _config.MaxForce;
            }

            // ======================
            // Fuel Update & Logging
            // ======================
            if (!UpdateFuel(dt))
            {
                _isInputActive = false;
                SyncJetpackState(false);
                return;
            }

            // Optional Logging for debugging
            //if (Time.frameCount % 30 == 0) // Log approx twice per second
            //{
            //    GameLogger.Info($"[Jetpack] Fuel: {FuelRatio:P1} | Force: {force.magnitude:F2} | Dir: {force.normalized}");
            //}

            // Smooth Force (anti-jitter)
            _forceDirection = Vector3.Lerp(_forceDirection, force, dt * 5f);

            // Apply via context (using Acceleration mode to ensure precise gain control regardless of mass)
            _contextValues.AddExternalForce(_forceDirection, ForceMode.Acceleration);
            // Debug.Log(_forceDirection);
        }

        private bool UpdateFuel(float dt)
        {
            _remainingFuel -= dt * _config.FuelConsumptionPerSecond;
            if (_remainingFuel <= 0)
            {
                _remainingFuel = 0;
                OnFuelChange?.Invoke(0f);
                return false;
            }

            OnFuelChange?.Invoke(FuelRatio);
            return true;
        }

        private void OnActivateStarted(InputAction.CallbackContext context)
        {
            if(!CanActivateJetpack())
            {
                return;
            }
            _contextValues.AddExternalForce(_config.InitialForce * Vector3.up, ForceMode.VelocityChange);
            _isInputActive = true;
            SyncJetpackState(true);
        }

        private void OnActivateCanceled(InputAction.CallbackContext context)
        {
            _isInputActive = false;
            SyncJetpackState(false);
        }

        private void SyncJetpackState(bool enabled)
        {
            var gorilla = _gorillaService?.LocalGorilla as Gorilla;
            if (gorilla != null && gorilla.jetpack != null)
            {
                gorilla.jetpack.IsJetpackEnabled = enabled;
            }
        }
    }
}
