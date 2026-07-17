using UnityEngine;

namespace Mythfall.Polish
{
    /// <summary>
    /// Receives knockback impulses and decays them over time. Drop on the player
    /// (alongside CharacterController) so heavy enemy attacks can shove the player.
    ///
    /// Movement is applied via <see cref="CharacterController.Move"/> so collision
    /// with walls / ground is respected — knockback won't push player out of the map.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [Tooltip("Higher = velocity dies faster. 5 = ~0.2s feel.")]
        [SerializeField] float decayRate = 5f;

        CharacterController controller;
        Vector3 velocity;

        public bool IsKnockedBack => velocity.sqrMagnitude > 0.01f;

        void Awake() => controller = GetComponent<CharacterController>();

        /// <summary>Apply an impulse (world-space). Direction + magnitude both matter.</summary>
        public void ApplyKnockback(Vector3 impulse)
        {
            // Stack with existing residual velocity so rapid hits compound, but cap
            // so a chain of slams can't fling the player across the map.
            velocity += impulse;
            if (velocity.magnitude > 20f) velocity = velocity.normalized * 20f;
        }

        void Update()
        {
            if (velocity.sqrMagnitude < 0.01f)
            {
                velocity = Vector3.zero;
                return;
            }

            controller.Move(velocity * Time.deltaTime);
            velocity = Vector3.Lerp(velocity, Vector3.zero, decayRate * Time.deltaTime);
        }
    }
}
