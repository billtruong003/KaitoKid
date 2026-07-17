using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Autohand.Demo
{
    public class OpenXRAutoHandFingerBender : MonoBehaviour
    {
        #region ===== Enums =====

        public enum FingerType
        {
            Grip,
            Trigger,
            Primary,
            Undefine
        }

        #endregion

        #region ===== Nested Classes =====

        [Serializable]
        public class Events
        {
            public UnityEvent<float[]> onBendAction = null;
            public UnityEvent<float[]> onUnbendAction = null;
        }

        #endregion

        #region ===== Fields =====

        [SerializeField]
        private FingerType type = FingerType.Undefine;
        [SerializeField]
        private Hand hand = null;
        [SerializeField]
        private InputActionProperty bendAction = default;
        [SerializeField]
        private InputActionProperty unbendAction = default;
        [SerializeField]
        private Events fingerEvents = new();

        [HideInInspector]
        public float[] bendOffsets;
        public bool pressed;

        #endregion

        #region ===== Properties =====

        public Events FingerEvents => fingerEvents;

        public FingerType Type => type;

        public Hand Hand => hand;

        #endregion

        #region ===== Methods =====

        private void OnEnable()
        {
            if (bendAction.action != null) bendAction.action.Enable();
            if (bendAction.action != null) bendAction.action.performed += BendAction;
            if (unbendAction.action != null) unbendAction.action.Enable();
            if (unbendAction.action != null) unbendAction.action.performed += UnbendAction;
        }
        private void OnDisable()
        {
            if (bendAction.action != null) bendAction.action.performed -= BendAction;
            if (unbendAction.action != null) unbendAction.action.performed -= UnbendAction;
        }

        void BendAction(InputAction.CallbackContext a)
        {
            Debug.Log(gameObject.name);
            if (!pressed)
            {
                pressed = true;
                for (int i = 0; i < hand.fingers.Length; i++)
                {
                    hand.fingers[i].bendOffset += bendOffsets[i];
                }
            }
            fingerEvents?.onBendAction?.Invoke(bendOffsets);
        }

        void UnbendAction(InputAction.CallbackContext a)
        {
            Debug.Log(gameObject.name);
            if (pressed)
            {
                pressed = false;
                for (int i = 0; i < hand.fingers.Length; i++)
                {
                    hand.fingers[i].bendOffset -= bendOffsets[i];
                }
            }
            fingerEvents?.onUnbendAction?.Invoke(bendOffsets);
        }

        #endregion
    }
}