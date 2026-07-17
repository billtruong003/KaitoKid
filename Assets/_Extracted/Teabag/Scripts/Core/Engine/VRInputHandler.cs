using System.Collections.Generic;

using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using UnityEngine.XR;

namespace Teabag.Core
{
public class VRInputHandler
{
    static XRNode leftHandNode = XRNode.LeftHand;
    static XRNode rightHandNode = XRNode.RightHand;

    static List<InputType> pressedLeft = new List<InputType>();
    static List<InputType> pressedRight = new List<InputType>();

#if UNITY_EDITOR
    private const int InputTypeCount = 6;
    public static bool simulatorOverrideActive;
    public static bool isTrackedOverride;
    private static float[] overrideLeft = new float[InputTypeCount];
    private static float[] overrideRight = new float[InputTypeCount];
    private static bool[] hasOverrideLeft = new bool[InputTypeCount];
    private static bool[] hasOverrideRight = new bool[InputTypeCount];

    public static void SetOverride(bool isLeftHand, InputType type, float value)
    {
        int i = (int)type;
        if (isLeftHand) { hasOverrideLeft[i] = true; overrideLeft[i] = value; }
        else { hasOverrideRight[i] = true; overrideRight[i] = value; }
    }

    public static void ClearOverrides()
    {
        simulatorOverrideActive = false;
        isTrackedOverride = false;
        for (int i = 0; i < InputTypeCount; i++)
        {
            hasOverrideLeft[i] = false; overrideLeft[i] = 0;
            hasOverrideRight[i] = false; overrideRight[i] = 0;
        }
    }

    private static bool TryGetOverride(bool isLeftHand, InputType type, out float value)
    {
        if (!simulatorOverrideActive) { value = 0; return false; }
        int i = (int)type;
        if (isLeftHand && hasOverrideLeft[i]) { value = overrideLeft[i]; return true; }
        if (!isLeftHand && hasOverrideRight[i]) { value = overrideRight[i]; return true; }
        value = 0;
        return false;
    }

    private static bool TryGetJoystickOverride(bool isLeftHand, out Vector2 value)
    {
        if (!simulatorOverrideActive) { value = Vector2.zero; return false; }

        bool isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (isLeftHand == isShiftDown)
        {
            float x = 0;
            float y = 0;
            if (Input.GetKey(KeyCode.RightArrow)) x += 1;
            if (Input.GetKey(KeyCode.LeftArrow)) x -= 1;
            if (Input.GetKey(KeyCode.UpArrow)) y += 1;
            if (Input.GetKey(KeyCode.DownArrow)) y -= 1;

            if (x != 0 || y != 0)
            {
                value = new Vector2(x, y).normalized;
                return true;
            }
        }

        value = Vector2.zero;
        return false;
    }
#endif

    public static float hapticMultiplier
    {
        get => Teabag.Core.GameServices.HapticMultiplier;
        set => Teabag.Core.GameServices.HapticMultiplier = value;
    }

    private static IHardwareHand GetHardwareHand(bool isLeftHand)
    {
        if (!ServiceLocator.TryGet<IRigInfoService>(out var rigInfo)) return null;
        var rig = rigInfo.HardwareRig;
        if (rig == null) return null;
        return isLeftHand ? rig.LeftHand : rig.RightHand;
    }

    public static bool IsTracked(bool IsLeftHand)
    {
#if UNITY_EDITOR
        if (simulatorOverrideActive && isTrackedOverride)
            return true;
#endif
        // HardwareRig hands use TrackedPoseDriver which handles tracking internally.
        // If the hand GameObject is active, it's tracked.
        var hand = GetHardwareHand(IsLeftHand);
        if (hand != null)
            return hand.HandTransform != null && hand.HandTransform.gameObject.activeInHierarchy;

        XRNode node = IsLeftHand ? leftHandNode : rightHandNode;
        InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
        return isTracked;
    }

    /// <summary>
    /// Returns true for the frame that the designated input has been pressed
    /// </summary>
    public static bool GetInput(bool IsLeftHand, InputType inputType)
    {
#if UNITY_EDITOR
        if (TryGetOverride(IsLeftHand, inputType, out float ov))
            return ov > 0.5f;
#endif
        bool output = false;

        var hand = GetHardwareHand(IsLeftHand);
        if (hand != null && (inputType == InputType.Trigger || inputType == InputType.Grip))
            output = ReadButtonFromHandCommand(hand.HandCommand, inputType);
        else
            output = ReadButtonFromXRNode(IsLeftHand, inputType);

        // One frame edge-detection (press, not hold)
        output = ApplyEdgeDetection(IsLeftHand, inputType, output);

        return output;
    }

    /// <summary>
    /// Returns true if input is being held down
    /// </summary>
    public static bool GetInputDown(bool IsLeftHand, InputType inputType)
    {
#if UNITY_EDITOR
        if (TryGetOverride(IsLeftHand, inputType, out float ovd))
            return ovd > 0.5f;
#endif
        var hand = GetHardwareHand(IsLeftHand);
        if (hand != null && (inputType == InputType.Trigger || inputType == InputType.Grip))
            return ReadButtonDownFromHandCommand(hand.HandCommand, inputType);

        return ReadButtonDownFromXRNode(IsLeftHand, inputType);
    }

    public static float GetInputDownAmount(bool IsLeftHand, InputType inputType)
    {
#if UNITY_EDITOR
        if (TryGetOverride(IsLeftHand, inputType, out float ova))
            return ova;
#endif
        var hand = GetHardwareHand(IsLeftHand);
        if (hand != null)
        {
            switch (inputType)
            {
                case InputType.Trigger: return hand.HandCommand.triggerAxisCommand;
                case InputType.Grip: return hand.HandCommand.gripAxisCommand;
                default: return 0;
            }
        }

        float output = 0;
        XRNode node = IsLeftHand ? leftHandNode : rightHandNode;
        switch (inputType)
        {
            case InputType.Trigger:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.trigger, out output);
                break;
            case InputType.Grip:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.grip, out output);
                break;
            default:
                Debug.Log("Input called is not supported - this should not happen.");
                break;
        }
        return output;
    }

    /// <summary>
    /// Gets joystick input
    /// </summary>
    public static Vector2 GetJoystick(bool IsLeftHand)
    {
#if UNITY_EDITOR
        if (TryGetJoystickOverride(IsLeftHand, out Vector2 ov))
            return ov;
#endif
        Vector2 r = new Vector2();
        XRNode node = IsLeftHand ? leftHandNode : rightHandNode;
        InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.primary2DAxis, out r);
        return r;
    }

    /// <summary>
    /// Vibrates a controller
    /// </summary>
    public static void VibrateController(bool IsLeftHand, float strenght, float duration)
    {
        var hand = GetHardwareHand(IsLeftHand);
        if (hand != null)
        {
            hand.HandHaptic?.Rumble();
            return;
        }

        XRNode node = IsLeftHand ? leftHandNode : rightHandNode;
        InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0u, strenght * hapticMultiplier, duration);
    }

    // --- Private helpers ---

    private static bool ReadButtonFromHandCommand(HandCommand cmd, InputType inputType)
    {
        switch (inputType)
        {
            case InputType.Trigger: return cmd.triggerAxisCommand > 0.8f;
            case InputType.Grip: return cmd.gripAxisCommand > 0.5f;
            default: return false;
        }
    }

    private static bool ReadButtonDownFromHandCommand(HandCommand cmd, InputType inputType)
    {
        switch (inputType)
        {
            case InputType.Trigger: return cmd.triggerAxisCommand > 0.8f;
            case InputType.Grip: return cmd.gripAxisCommand > 0.5f;
            default: return false;
        }
    }

    private static bool ReadButtonFromXRNode(bool isLeftHand, InputType inputType)
    {
        bool output = false;
        XRNode node = isLeftHand ? leftHandNode : rightHandNode;

        switch (inputType)
        {
            case InputType.Trigger:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.trigger, out float pressAmount);
                if (pressAmount > 0.8f)
                    output = true;
                break;
            case InputType.Grip:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.gripButton, out output);
                break;
            case InputType.Primary:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.primaryButton, out output);
                break;
            case InputType.Secondary:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.secondaryButton, out output);
                break;
            case InputType.JoystickPress:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.primary2DAxisClick, out output);
                break;
            default:
                Debug.Log("Input called is not supported - this should not happen.");
                break;
        }
        return output;
    }

    private static bool ReadButtonDownFromXRNode(bool isLeftHand, InputType inputType)
    {
        bool output = false;
        XRNode node = isLeftHand ? leftHandNode : rightHandNode;

        switch (inputType)
        {
            case InputType.Trigger:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.trigger, out float triggerPressAmount);
                if (triggerPressAmount > 0.8f)
                    output = true;
                break;
            case InputType.Grip:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.grip, out float gripPressAmount);
                if (gripPressAmount > 0.5f)
                    output = true;
                break;
            case InputType.Primary:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.primaryButton, out output);
                break;
            case InputType.Secondary:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.secondaryButton, out output);
                break;
            case InputType.JoystickPress:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.primary2DAxisClick, out output);
                break;
            case InputType.Menu:
                InputDevices.GetDeviceAtXRNode(node).TryGetFeatureValue(CommonUsages.menuButton, out output);
                break;
            default:
                Debug.Log("Input called is not supported - this should not happen.");
                break;
        }
        return output;
    }

    private static bool ApplyEdgeDetection(bool isLeftHand, InputType inputType, bool rawState)
    {
        if (rawState)
        {
            bool contains = false;

            if (isLeftHand)
            {
                if (!pressedLeft.Contains(inputType))
                    pressedLeft.Add(inputType);
                else
                    contains = true;
            }
            else
            {
                if (!pressedRight.Contains(inputType))
                    pressedRight.Add(inputType);
                else
                    contains = true;
            }

            if (contains)
                return false;
        }
        else
        {
            if (isLeftHand)
            {
                if (pressedLeft.Contains(inputType))
                    pressedLeft.Remove(inputType);
            }
            else
            {
                if (pressedRight.Contains(inputType))
                    pressedRight.Remove(inputType);
            }
        }

        return rawState;
    }
}

public enum InputType
{
    Trigger,
    Grip,
    Primary,
    Secondary,
    JoystickPress,
    Menu
}
}
