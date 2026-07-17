using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Shmackle.Input
{
    public class HoldInputListener : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField]
        private InputActionProperty _holdInputAction;
        [SerializeField]
        private UnityEvent _onStartHold;
        [SerializeField]
        private UnityEvent _onStopHold;

        #endregion
        
        #region Private Methods

        private void Awake()
        {
            _holdInputAction.action.AddBinding("<Keyboard>/tab");
            _holdInputAction.action.AddBinding("<XRController>{RightHand}/primaryButton");
            _holdInputAction.action.AddBinding("<XRController>{LeftHand}/primaryButton");
        }
        
        private void OnEnable()
        {
            _holdInputAction.action.Enable();
        }

        private void OnDisable()
        {
            _holdInputAction.action.Disable();
        }

        private void Update()
        {
            if (_holdInputAction.action.WasPressedThisFrame())
            {
                _onStartHold.Invoke();
            }
            else if (_holdInputAction.action.WasReleasedThisFrame())
            {
                _onStopHold.Invoke();
            }
        }

        #endregion
        
        
    }
}