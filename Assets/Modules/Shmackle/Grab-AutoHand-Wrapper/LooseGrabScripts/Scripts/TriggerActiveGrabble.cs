using System;
using System.Collections;
using System.Collections.Generic;
using Autohand;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public class TriggerActiveGrabble : MonoBehaviour
{
    public string tagToCheck;
    public HandType handType;
    
    public bool isUpdateKinematic;
    public bool isKinematic;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(tagToCheck))
        {
            if (other.TryGetComponent<Grabbable>(out Grabbable grabbable))
            {
                grabbable.handType = handType;
                if (isUpdateKinematic)
                {
                    grabbable.body.isKinematic = isKinematic;
                }
            }
            
            
        }
    }
}

