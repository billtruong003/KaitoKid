using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Autohand;

public class GrabObjectNoGravityZone : MonoBehaviour
{
    public ShmackleGrabbleObject targetObject;
    Rigidbody objectRigidBody;
    Grabbable grabbable;

    void Start()
    {
        if (targetObject != null)
        {
            objectRigidBody = targetObject.GetComponent<Rigidbody>();
            grabbable = targetObject.GetComponent<Grabbable>();
        }
        else
        {
            Debug.LogWarning("No targetObject in GrabObjectNoGravityZone");
        }
        
    }
    
    private void OnTriggerStay(Collider other)
    {
       var detectObject = other.GetComponent<ShmackleGrabbleObject>();
       if (detectObject == targetObject &&
           objectRigidBody && grabbable.IsHeld() == false)
       {
           objectRigidBody.isKinematic = true;
       }
    }

    private void OnTriggerExit(Collider other)
    {
        var detectObject = other.GetComponent<ShmackleGrabbleObject>();
        if (detectObject == targetObject &&
            objectRigidBody)
        {
            objectRigidBody.isKinematic = false;
        }
    }
}
