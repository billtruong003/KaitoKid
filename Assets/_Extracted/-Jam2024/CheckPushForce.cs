using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPushForce : MonoBehaviour
{
    public float initialPushForce = 10f; // Initial force applied when the object enters the trigger
    //public float forceIncreaseRate = 50f; // Rate at which the force increases

    private void OnTriggerStay(Collider other)
    {
        // Check if the other object has a PufferFishController component
        PufferFishController pufferFish = other.GetComponent<PufferFishController>();

        if (pufferFish != null && pufferFish.popping)
        {
            ApplyPushForce(pufferFish, initialPushForce);
        }
    }

    private void ApplyPushForce(PufferFishController controller, float force)
    {
        Debug.Log("Push");
        if (controller.rb != null)
        {
            controller.rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
        }
    }
}
