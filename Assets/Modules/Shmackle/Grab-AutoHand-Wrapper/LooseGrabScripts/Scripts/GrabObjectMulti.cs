using System.Collections.Generic;
using Autohand;
using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabObjectMulti : MonoBehaviour
{
    [SerializeField]
    private NetworkObject networkObject = null;
    [SerializeField]
    private Grabbable grabbable = null;
    [SerializeField]
    private XRGrabInteractable xrGrabInteractable = null;

    private Rigidbody rig;

    
    private void Awake()
    {
        networkObject       = GetComponent<NetworkObject>();
        grabbable           = GetComponentInChildren<Grabbable>();
        xrGrabInteractable  = GetComponentInChildren<XRGrabInteractable>();
    }

    private void Start()
    {
        rig = GetComponent<Rigidbody>();
        
        if (grabbable != null)
        {
            grabbable.onGrab.AddListener(SelectEntered);
            grabbable.onRelease.AddListener(SelectExited);
        }
        if (xrGrabInteractable != null)
        {
            xrGrabInteractable.selectEntered.AddListener(SelectEntered);
            xrGrabInteractable.selectExited.AddListener(SelectExited);
        }
    }

    private void SelectEntered(Hand arg0, Grabbable arg1)
    {
        if (rig)
        {
            rig.linearVelocity = Vector3.zero;
            rig.angularVelocity = Vector3.zero;
            rig.angularDamping = 0.0f;
        }
        
        if (networkObject == null)
            return;
        networkObject.RequestStateAuthority();
    }

    private void SelectExited(Hand arg0, Grabbable arg1)
    {
    }

    public void SelectEntered(SelectEnterEventArgs args)
    {
        
        if (networkObject == null)
            return;
        networkObject.RequestStateAuthority();
    }

    public void SelectExited(SelectExitEventArgs args)
    {
    }
}