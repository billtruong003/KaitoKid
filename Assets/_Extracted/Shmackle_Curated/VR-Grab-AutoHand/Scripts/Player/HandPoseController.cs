using Shmackle.Runtime;
using UnityEngine;
using UnityEngine.XR;

public class HandPoseController : MonoBehaviour
{

    //LeftHand
    private InputDevice leftController;
    public GameObject LeftHand;
    private Animator LHandAnimator;
    //RightHand
    private InputDevice rightController;
    public GameObject RightHand;
    private Animator RHandAnimator;

    public AnimationClip RestHandPose;

    private float LThumbValue = 0f;
    private float RThumbValue = 0f;
    private float DefaultHL = 0f;
    private float DefaultHR = 0f;

    private PlayerMod gorillaMod = null;

    void Start()
    {
        // Initialize Animator
        LHandAnimator = LeftHand.GetComponent<Animator>();
        RHandAnimator = RightHand.GetComponent<Animator>();

        // Get the XR input devices for left and right controllers
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    public void SetGorillaNetwork(PlayerMod gorillaMod)
    {
        this.gorillaMod = gorillaMod;
    }

    public void SetHands(GameObject leftHand, GameObject rightHand)
    {
        this.LeftHand = leftHand;
        this.RightHand = rightHand;
        this.LHandAnimator = LeftHand.GetComponent<Animator>();
        this.RHandAnimator = RightHand.GetComponent<Animator>();
    }

    //void Update()
    //{
    //    // Check if the right controller is valid
    //    if (rightController.isValid)
    //    {
    //        // Get the trigger and grip values from the right controller
    //        rightController.TryGetFeatureValue(CommonUsages.trigger, out float RTriggerValue);
    //        rightController.TryGetFeatureValue(CommonUsages.grip, out float RGripValue);
    //        //check if thumb touched the buttons
    //        rightController.TryGetFeatureValue(CommonUsages.primaryTouch, out bool RThumbTouch);
    //        rightController.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool RThumbTouch1);

    //        // Example of using the values (e.g., for debugging purposes)
    //        //Debug.Log($"Trigger Value: {RTriggerValue}, Grip Value: {RGripValue}");

    //        if (gorillaMod == null)
    //        {
    //            RHandAnimator.SetFloat("RTriggerValue", RTriggerValue);
    //            RHandAnimator.SetFloat("RGripValue", RGripValue);
    //        }
    //        else
    //        {
    //            gorillaMod?.RPC_RightHandSetFloat("RTriggerValue", RTriggerValue);
    //            gorillaMod?.RPC_RightHandSetFloat("RGripValue", RGripValue);
    //        }
    //        //RHandAnimator.SetBool("RThumbTouch", RThumbTouch);
    //        if (RThumbTouch || RThumbTouch1)
    //        {
    //            RThumbValue = Mathf.Lerp(RThumbValue, 1f, 0.3f);
    //            DefaultHR = Mathf.Lerp(DefaultHR, 0f, 0.3f);
    //        }
    //        else
    //        {
    //            RThumbValue = Mathf.Lerp(RThumbValue, 0f, 0.3f);
    //            DefaultHR = Mathf.Lerp(DefaultHR, 1f, 0.3f);
    //        }
    //        if (gorillaMod == null)
    //        {
    //            RHandAnimator.SetFloat("RThumbValue", RThumbValue);
    //            RHandAnimator.SetFloat("DefaultHR", RThumbValue);
    //        }
    //        else
    //        {
    //            gorillaMod?.RPC_RightHandSetFloat("RThumbValue", RThumbValue);
    //            gorillaMod?.RPC_RightHandSetFloat("DefaultHR", RThumbValue);
    //        }
    //    }
    //    // Check if the left controller is valid
    //    if (leftController.isValid)
    //    {
    //        // Get the trigger and grip values from the right controller
    //        leftController.TryGetFeatureValue(CommonUsages.trigger, out float LTriggerValue);
    //        leftController.TryGetFeatureValue(CommonUsages.grip, out float LGripValue);
    //        //check if thumb touched the buttons
    //        leftController.TryGetFeatureValue(CommonUsages.primaryTouch, out bool LThumbTouch);
    //        leftController.TryGetFeatureValue(CommonUsages.secondaryTouch, out bool LThumbTouch1);

    //        // Example of using the values (e.g., for debugging purposes)
    //        //Debug.Log($"Trigger Value: {RTriggerValue}, Grip Value: {RGripValue}");

    //        if (gorillaMod == null)
    //        {
    //            LHandAnimator.SetFloat("LTriggerValue", LTriggerValue);
    //            LHandAnimator.SetFloat("LGripValue", LGripValue);
    //        }
    //        else
    //        {
    //            gorillaMod.RPC_LeftHandSetFloat("LTriggerValue", LTriggerValue);
    //            gorillaMod.RPC_LeftHandSetFloat("LGripValue", LGripValue);
    //        }
    //        //LHandAnimator.SetBool("LThumbTouch", LThumbTouch);
    //        if (LThumbTouch || LThumbTouch1)
    //        {
    //            LThumbValue = Mathf.Lerp(LThumbValue, 1f, 0.3f);
    //            DefaultHL = Mathf.Lerp(DefaultHL, 0f, 0.3f);
    //        }
    //        else
    //        {
    //            LThumbValue = Mathf.Lerp(LThumbValue, 0f, 0.3f);
    //            DefaultHL = Mathf.Lerp(DefaultHL, 1f, 0.3f);
    //        }
    //        if (gorillaMod == null)
    //        {
    //            LHandAnimator.SetFloat("LThumbValue", LThumbValue);
    //            LHandAnimator.SetFloat("DefaultHL", DefaultHL);
    //        }
    //        else
    //        {
    //            gorillaMod.RPC_LeftHandSetFloat("LThumbValue", LThumbValue);
    //            gorillaMod.RPC_LeftHandSetFloat("DefaultHL", DefaultHL);
    //        }
    //    }
    //}
}
