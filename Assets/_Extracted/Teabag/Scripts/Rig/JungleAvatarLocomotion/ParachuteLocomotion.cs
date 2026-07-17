using System;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GorillaLocomotion
{
    /// <summary>
    /// Locomotion module for parachute behavior, integrated into Jungle XRKit.
    /// Needs to be added to LocomotionController's modules list in the inspector.
    /// </summary>
    [Serializable]
    public class ParachuteLocomotion : BaseLocomotionModule
    {
        [Header("Parameters")]
        public float forwardForce = 250f;
        public float downwardForce = 20f;
        public float maxParachuteVelocity = 10f;
        public float deploymentMaxDownwardVelocity = -6f;

        [Header("Runtime State")]
        [SerializeField] private InputActionReference _inputActionReference;
        public bool forceJump;

        private IGorillaService _gorillaService;
        private bool _isParachuting;
        private bool _isInputActive;

        public override void Initialize(ref LocomotionContextValues contextValues)
        {
            base.Initialize(ref contextValues);

            // Require Update for input reading and FixedUpdate for physics
            _updateToggles.update = true;
            _updateToggles.fixedUpdate = true;
            _updateToggles.lateUpdate = false;
        }

        public override void Reset()
        {
            forceJump = false;
            _isParachuting = false;
            _isInputActive = false;

            SetGorillaVisuals(false);
        }

        public override void RegisterEvents()
        {
            if (_inputActionReference != null && _inputActionReference.action != null)
            {
                _inputActionReference.action.performed += OnInputPerformed;
                _inputActionReference.action.canceled += OnInputCanceled;
            }
        }

        public override void UnregisterEvents()
        {
            if (_inputActionReference != null && _inputActionReference.action != null)
            {
                _inputActionReference.action.performed -= OnInputPerformed;
                _inputActionReference.action.canceled -= OnInputCanceled;
            }
        }

        private void OnInputPerformed(InputAction.CallbackContext context)
        {
            _isInputActive = context.ReadValueAsButton();
        }

        private void OnInputCanceled(InputAction.CallbackContext context)
        {
            _isInputActive = false;
        }

        public override void Update()
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            if (_gorillaService == null || !_gorillaService.HasLocalGorilla) return;

            var localGorilla = _gorillaService.LocalGorilla as Gorilla;
            if (localGorilla == null || localGorilla.parachute == null) return;

            // Check if rockets mod is enabled
            if (GameServices.IsModEnabled?.Invoke("Rockets") ?? false)
            {
                if (_isParachuting)
                {
                    _isParachuting = false;
                    SetGorillaVisuals(false);
                }
                return;
            }

            // Use input state from InputActionReference
            if (Input.GetKeyDown(KeyCode.P))
            {
                _isInputActive = true;
            }

            bool isPrimaryHeld = _isInputActive;

            if (!_isParachuting)
            {
                if ((isPrimaryHeld || forceJump) && _contextValues.LinearVelocity.y < deploymentMaxDownwardVelocity)
                {
                    // Deploy parachute
                    _contextValues.LinearVelocity = new Vector3(_contextValues.LinearVelocity.x, 0, _contextValues.LinearVelocity.z);
                    _isParachuting = true;
                    SetGorillaVisuals(true);
                }
            }
            else
            {
                if (isPrimaryHeld)
                {
                    forceJump = false;
                }

                // Close parachute if we let go of the button (and forceJump is disabled) or if we start moving upwards
                if (!(isPrimaryHeld || forceJump) || _contextValues.LinearVelocity.y > 0)
                {
                    _isParachuting = false;
                    SetGorillaVisuals(false);
                }
            }
        }

        public override void FixedUpdate()
        {
            if (_gorillaService == null || !_gorillaService.HasLocalGorilla) return;

            if (GameServices.IsModEnabled?.Invoke("Rockets") ?? false)
            {
                _contextValues.UseGravity = !_contextValues.IsSwimming;
                return;
            }

            if (_isParachuting)
            {
                Vector3 headDir = _contextValues.HeadsetForward;
                headDir.y = 0;

                // Apply continuous forces
                _contextValues.AddExternalForce(headDir * forwardForce, ForceMode.Acceleration);
                _contextValues.AddExternalForce(Vector3.down * downwardForce, ForceMode.Acceleration);

                // Limit velocity (requires manually clamping via direct assignment)
                if (_contextValues.LinearVelocity.magnitude > maxParachuteVelocity)
                {
                    _contextValues.LinearVelocity = Vector3.ClampMagnitude(_contextValues.LinearVelocity, maxParachuteVelocity);
                }

                _contextValues.UseGravity = false;
            }
            else
            {
                _contextValues.UseGravity = !_contextValues.IsSwimming;
            }
        }

        public override void LateUpdate() { }

        private void SetGorillaVisuals(bool parachuting)
        {
            var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
            if (localGorilla != null && localGorilla.parachute != null)
            {
                localGorilla.parachute.isParachuting = parachuting;
            }
        }
    }
}
