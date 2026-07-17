using System;
using NaughtyAttributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;


public class PlayerInputListener : MonoBehaviour
{
    public ShmackleNetworkRig shmackleNetworkRig;
    public bool isActive;

    [FoldoutGroup("Trigger Input")][SerializeField] private InputActionReference leftHandTrigger;
    [FoldoutGroup("Trigger Input")][SerializeField] private InputActionReference rightHandTrigger;

    [FoldoutGroup("Trigger Input")][Sirenix.OdinInspector.ReadOnly]public float leftTriggerValue;
    [FoldoutGroup("Trigger Input")][Sirenix.OdinInspector.ReadOnly]public float rightTriggerValue;

    [FoldoutGroup("Grip Input")][SerializeField] private InputActionReference leftHandGrip;
    [FoldoutGroup("Grip Input")][SerializeField] private InputActionReference rightHandGrip;

    [FoldoutGroup("Grip Input")][Sirenix.OdinInspector.ReadOnly]public float leftGripValue;
    [FoldoutGroup("Grip Input")][Sirenix.OdinInspector.ReadOnly]public float rightGripValue;

    [FoldoutGroup("Primary Input")][SerializeField] private InputActionReference leftPrimary;
    [FoldoutGroup("Primary Input")][SerializeField] private InputActionReference rightPrimary;

    [FoldoutGroup("Primary Input")][Sirenix.OdinInspector.ReadOnly]public float leftPrimaryValue;
    [FoldoutGroup("Primary Input")][Sirenix.OdinInspector.ReadOnly]public float rightPrimaryValue;


    [FoldoutGroup("Thumb Click")][SerializeField] private InputActionReference leftThumbClick;
    [FoldoutGroup("Thumb Click")][SerializeField] private InputActionReference rightThumbClick;
    [FoldoutGroup("Thumb Click")][Sirenix.OdinInspector.ReadOnly]public float leftThumbClickValue;
    [FoldoutGroup("Thumb Click")][Sirenix.OdinInspector.ReadOnly]public float rightThumbClickValue;
    
    [FoldoutGroup("Axis Input")]public InputActionProperty moveAxis;
    [FoldoutGroup("Axis Input")]public InputActionProperty turnAxis;
    [FoldoutGroup("Axis Input")]public Vector2 moveInput;
    [FoldoutGroup("Axis Input")]public Vector2 turnInput;
    public enum ButtonState
    {
        Idle,
        Pressed,
        Released,
        Holding
    }

    [Header("States")]
    public ButtonState leftGripState = ButtonState.Idle;
    public ButtonState rightGripState = ButtonState.Idle;
    public ButtonState leftTriggerState = ButtonState.Idle;
    public ButtonState rightTriggerState = ButtonState.Idle;
    public ButtonState leftPrimaryButtonState = ButtonState.Idle;
    public ButtonState rightPrimaryButtonState = ButtonState.Idle;
    public ButtonState leftThumbClickState = ButtonState.Idle;
    public ButtonState rightThumbClickState = ButtonState.Idle;
    

    public void EnableInputActions()
    {
        isActive = true;
        leftHandTrigger.action.Enable();
        rightHandTrigger.action.Enable();
        leftHandGrip.action.Enable();
        rightHandGrip.action.Enable();
        leftPrimary.action.Enable();
        rightPrimary.action.Enable();
        leftThumbClick.action.Enable();
        rightThumbClick.action.Enable();
        moveAxis.action.Enable();
        turnAxis.action.Enable();
        
    }

    private void Update()
    {
        if (isActive)
        {
            // Read float values from InputActions
            leftTriggerValue = leftHandTrigger.action.ReadValue<float>();
            rightTriggerValue = rightHandTrigger.action.ReadValue<float>();

            leftGripValue = leftHandGrip.action.ReadValue<float>();
            rightGripValue = rightHandGrip.action.ReadValue<float>();

            leftPrimaryValue = leftPrimary.action.ReadValue<float>();
            rightPrimaryValue = rightPrimary.action.ReadValue<float>();
            
            leftThumbClickValue = leftThumbClick.action.ReadValue<float>();
            rightThumbClickValue = rightThumbClick.action.ReadValue<float>();
            
            moveInput = moveAxis.action.ReadValue<Vector2>();
            turnInput = turnAxis.action.ReadValue<Vector2>();
            

            // Update button states
            UpdateButtonState(ref leftTriggerState, leftTriggerValue > 0.1f);
            UpdateButtonState(ref rightTriggerState, rightTriggerValue > 0.1f);
            UpdateButtonState(ref leftGripState, leftGripValue > 0.1f);
            UpdateButtonState(ref rightGripState, rightGripValue > 0.1f);
            UpdateButtonState(ref leftPrimaryButtonState, leftPrimaryValue > 0.5f);
            UpdateButtonState(ref rightPrimaryButtonState, rightPrimaryValue > 0.5f);
            UpdateButtonState(ref leftThumbClickState, leftThumbClickValue > 0.5f);
            UpdateButtonState(ref rightThumbClickState, rightThumbClickValue > 0.5f);
        }
        
    }

    private void UpdateButtonState(ref ButtonState state, bool isPressed)
    {
        switch (state)
        {
            case ButtonState.Idle:
                if (isPressed) state = ButtonState.Pressed;
                break;
            case ButtonState.Pressed:
                state = isPressed ? ButtonState.Holding : ButtonState.Released;
                break;
            case ButtonState.Holding:
                if (!isPressed) state = ButtonState.Released;
                break;
            case ButtonState.Released:
                state = isPressed ? ButtonState.Pressed : ButtonState.Idle;
                break;
        }
    }

    public void ReleaseAllInputs()
    {
        UpdateButtonState(ref leftTriggerState, false);
        UpdateButtonState(ref rightTriggerState, false);
        UpdateButtonState(ref leftGripState, false);
        UpdateButtonState(ref rightGripState, false);
        UpdateButtonState(ref leftPrimaryButtonState, false);
        UpdateButtonState(ref rightPrimaryButtonState, false);
        UpdateButtonState(ref leftThumbClickState, false);
        UpdateButtonState(ref rightThumbClickState, false);
    }
}
