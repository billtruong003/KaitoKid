using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

[Serializable]
public class XRButtonModel
{
    public XRButton   type    = XRButton.Undefined;
    public bool       pressed = false;
    public UnityEvent onEnter = null;
    public UnityEvent onStay  = null;
    public UnityEvent onExit  = null;
}

public enum XRButton
{
    LeftGrip,
    LeftTrigger,
    LeftPrimary,

    RightGrip,
    RightTrigger,
    RightPrimary,

    Undefined
}

public class PlayerInputListenerEvent : MonoBehaviour
{
    [SerializeField]
    private List<XRButtonModel> xrButtonModels = null;
    private Dictionary<XRButton, XRButtonModel> xrButtonModelMap = null;
    private InputDevice                         leftDevice       = default;
    private InputDevice                         rightDevice      = default;

    private void Awake()
    {
        leftDevice       =   InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightDevice      =   InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        xrButtonModels   ??= new List<XRButtonModel>();
        xrButtonModelMap ??= new Dictionary<XRButton, XRButtonModel>();

        foreach (var xrButtonModel in xrButtonModels)
        {
            if (xrButtonModelMap.TryAdd(xrButtonModel.type, xrButtonModel))
                continue;
            Debug.LogWarning($"XRControllerButtonModel button is duplicated: {xrButtonModel.type}");
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (leftDevice == default)
            Debug.LogError("Invalid left hand device. Check if the devices are connected.");
        if (rightDevice == default)
            Debug.LogError("Invalid right hand device. Check if the devices are connected.");
    }

    private void UpdateDeveice(InputDevice device, InputFeatureUsage<bool> usage, XRButton type)
    {
        if (device.TryGetFeatureValue(usage, out var pressed))
        {
            if (xrButtonModelMap.TryGetValue(type, out var model))
            {
                if (model.pressed && pressed)
                    model.onStay?.Invoke();
                else if (model.pressed == false && pressed == true)
                    model.onEnter?.Invoke();
                else if (model.pressed == true && pressed == false)
                    model.onExit?.Invoke();
                model.pressed = pressed;
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        // Check left hand device
        if (leftDevice.isValid)
        {
            UpdateDeveice(leftDevice, CommonUsages.gripButton, XRButton.LeftGrip);
            UpdateDeveice(leftDevice, CommonUsages.triggerButton, XRButton.LeftTrigger);
            UpdateDeveice(leftDevice, CommonUsages.primaryButton, XRButton.LeftPrimary);
        }

        // Check right hand device
        if (rightDevice.isValid)
        {
            UpdateDeveice(rightDevice, CommonUsages.gripButton, XRButton.RightGrip);
            UpdateDeveice(rightDevice, CommonUsages.triggerButton, XRButton.RightTrigger);
            UpdateDeveice(rightDevice, CommonUsages.primaryButton, XRButton.RightPrimary);
        }
    }
}