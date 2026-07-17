using System.Collections.Generic;
using Autohand;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Hand = Autohand.Hand;
using InputDevice = UnityEngine.XR.InputDevice;

public class ShmackleHandControllerLink : NetworkBehaviour
{
    public Hand hand;
    [Networked] public bool IsGrabbing { get; private set; }
    [Networked] public bool IsSqueezing { get; private set; }
    
    public InputActionProperty grabAxis;
    public InputActionProperty squeezeAxis;
    public InputActionProperty grabAction;
    public InputActionProperty releaseAction;
    public InputActionProperty squeezeAction;
    public InputActionProperty stopSqueezeAction;
    public InputActionProperty hapticAction;

    private XRNode role;
    private List<InputDevice> devices = new List<InputDevice>();

    
    // Start is called before the first frame update
    void Start()
    {
        /*if (hand.left)
            handLeft = this;
        else
            handRight = this;*/
    }

    public void OnEnable()
    {
        if (grabAction == squeezeAction)
        {
            Debug.LogError("AUTOHAND: You are using the same button for grab and squeeze on HAND CONTROLLER LINK, this will create conflict or errors", this);
        }

        EnableInputActions();

        role = hand.left ? XRNode.LeftHand : XRNode.RightHand;
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

    private void Update()
    {
        if (HasStateAuthority)
        {
            hand.SetGrip(grabAxis.action.ReadValue<float>(), squeezeAxis.action.ReadValue<float>());
            // if (Input.GetKeyDown(KeyCode.G))
            // {
            //     Debug.Log(Object.InputAuthority.PlayerId + "RPC_Grab");
            //     RPC_Grab();
            // }
        }
        
        
    }

    private void EnableInputActions()
    {
        if(grabAxis.action != null) grabAxis.action.Enable();
        if(squeezeAxis.action != null) squeezeAxis.action.Enable();
        if(hapticAction.action != null) hapticAction.action.Enable();
        if(grabAction.action != null) grabAction.action.performed += Grab;
        if (grabAction.action != null) grabAction.action.Enable();
        if (grabAction.action != null) grabAction.action.performed += Grab;
        if (releaseAction.action != null) releaseAction.action.Enable();
        if (releaseAction.action != null) releaseAction.action.performed += Release;
        if (squeezeAction.action != null) squeezeAction.action.Enable();
        if (squeezeAction.action != null) squeezeAction.action.performed += Squeeze;
        if (stopSqueezeAction.action != null) stopSqueezeAction.action.Enable();
        if (stopSqueezeAction.action != null) stopSqueezeAction.action.performed += StopSqueeze;
    }

    private void DisableInputActions()
    {
        if(grabAction.action != null) grabAction.action.performed -= Grab;
        if(releaseAction.action != null) releaseAction.action.performed -= Release;
        if(squeezeAction.action != null) squeezeAction.action.performed -= Squeeze;
        if(stopSqueezeAction.action != null) stopSqueezeAction.action.performed -= StopSqueeze;
    }

    private void Grab(InputAction.CallbackContext context)
    {
        if (!IsGrabbing && HasInputAuthority)
        {
            RPC_Grab();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_Grab()
    {
        IsGrabbing = true;
        hand.Grab();
    }

    private void Release(InputAction.CallbackContext context)
    {
        if (IsGrabbing && HasInputAuthority)
        {
            RPC_Release();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_Release()
    {
        IsGrabbing = false;
        hand.Release();
    }

    private void Squeeze(InputAction.CallbackContext context)
    {
        if (!IsSqueezing && HasInputAuthority)
        {
            RPC_Squeeze();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_Squeeze()
    {
        IsSqueezing = true;
        hand.Squeeze();
    }

    private void StopSqueeze(InputAction.CallbackContext context)
    {
        if (IsSqueezing && HasInputAuthority)
        {
            RPC_StopSqueeze();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_StopSqueeze()
    {
        IsSqueezing = false;
        hand.Unsqueeze();
    }
}
