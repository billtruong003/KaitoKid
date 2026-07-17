using UnityEngine;
using ModularTopDown.Locomotion;

namespace Mythfall.Player
{
    /// <summary>
    /// Rotates the player transform independent of locomotion (CLAUDE.md Rule 4).
    /// Priority: face TargetSelector.CurrentTarget if set, else face HorizontalVelocity.
    /// Skill executions can call LockRotation(true) to freeze facing during cast/dash.
    /// Requires CharacterLocomotion.ExternalRotationControl = true (set by PlayerBase.Awake).
    /// </summary>
    public class PlayerFacing : MonoBehaviour
    {
        [SerializeField] float rotationSpeed = 15f;
        [SerializeField] float minMoveSqrToFace = 0.04f; // 0.2 m/s threshold

        TargetSelector targetSelector;
        CharacterLocomotion locomotion;
        bool rotationLocked;

        public bool RotationLocked => rotationLocked;

        void Awake()
        {
            targetSelector = GetComponent<TargetSelector>();
            locomotion = GetComponent<CharacterLocomotion>();
        }

        void Update()
        {
            if (rotationLocked) return;

            Vector3 lookDir = ResolveLookDirection();
            if (lookDir.sqrMagnitude < 0.0001f) return;

            var targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        Vector3 ResolveLookDirection()
        {
            // Priority 1 — face the current combat target
            if (targetSelector != null && targetSelector.CurrentTarget != null)
            {
                var to = targetSelector.CurrentTarget.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f) return to.normalized;
            }

            // Priority 2 — face current horizontal velocity (movement direction)
            if (locomotion != null)
            {
                var vel = locomotion.HorizontalVelocity;
                if (vel.sqrMagnitude > minMoveSqrToFace) return vel.normalized;
            }

            return Vector3.zero;
        }

        public void LockRotation(bool locked) => rotationLocked = locked;
    }
}
