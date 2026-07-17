using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HandPoseProvider : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private InputDevice leftController;
    private InputDevice rightController;
    public enum interactingHand{
        left,
        right,
        both
    }

    private void OnEnable()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(HandleSelectEntered);
        }

        // Initialize the XR input devices for left and right controllers
        InitializeControllers();
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
        }
    }

    private void InitializeControllers()
    {
        // Get the XR input devices for left and right controllers
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // Debug information to check if devices are correctly initialized
        Debug.Log($"Left Controller Initialized: {leftController.isValid}");
        Debug.Log($"Right Controller Initialized: {rightController.isValid}");
    }

    private void HandleSelectEntered(SelectEnterEventArgs args)
    {
        // Get the interactor object
        //var interactor = args.interactorObject;
        rightController.TryGetFeatureValue(CommonUsages.trigger, out float RTriggerValue);
        rightController.TryGetFeatureValue(CommonUsages.grip, out float RGripValue);
        if (RTriggerValue > 0f || RGripValue>0f)
        {

        }


    }
}
