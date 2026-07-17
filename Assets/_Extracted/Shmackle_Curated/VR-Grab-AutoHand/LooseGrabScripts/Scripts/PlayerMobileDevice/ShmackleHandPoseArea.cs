using System;
using System.Collections;
using System.Collections.Generic;
using Autohand;
using UnityEngine;

public class ShmackleHandPoseArea : MonoBehaviour
{
    public HandPoseScriptable handPose;
    
    private void OnTriggerEnter(Collider other)
    {
        ShmackleFinger finger = other.gameObject.GetComponent<ShmackleFinger>();
        if (finger && !finger.hand.IsHolding())
        {
            if (handPose)
            {
                if (finger.hand.left)
                {
                    finger.handAnimator.SetPose(ref handPose.leftPose, 0.01f);
                }
                else
                {
                    finger.handAnimator.SetPose(ref handPose.rightPose, 0.01f);
                }
            }
        }
    }


    private void OnTriggerExit(Collider other)
    {
        ShmackleFinger finger = other.gameObject.GetComponent<ShmackleFinger>();
        if (finger&& !finger.hand.IsHolding())
        {
            // if (handPose)
            // {
            //     if (finger.hand.left)
            //     {
            //         finger.handAnimator.SetPose(ref finger.handAnimator.openHandPose, 0.15f);
            //     }
            //     else
            //     {
            //         finger.handAnimator.SetPose(ref finger.handAnimator.openHandPose, 0.15f);
            //     }
            // }
        }
    }
}
