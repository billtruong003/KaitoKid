using Shmackle.Player.Headlight;
using UnityEngine;

namespace Shmackle.Gameplay.Flashlight
{
    /// <summary>
    /// Detects when a collider enters the trigger and determines whether it
    /// approached from the front or the back of this object.
    /// Based on the entry direction, it forces the player flashlight on or off.
    /// </summary>
    public class FlashlightTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<FlashlightController>(out var flashlightController))
            {
                // Direction from this trigger to the entering collider
                Vector3 directionToOther = (other.transform.position - transform.position).normalized;
                
                float entryDirection = Vector3.Dot(transform.forward, directionToOther);

                if (entryDirection > 0f) 
                    flashlightController.ToggleLight(true); // Entered from the front
                else
                    flashlightController.ToggleLight(false);  // Entered from the back
                
            }
        }
    }
}