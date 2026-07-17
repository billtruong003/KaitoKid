using UnityEngine;

namespace Shmackle.Player
{
    /// <summary>
    /// Remote-layer collider placed on the butt that serves as a detectable touch target for RemoteTouchTrigger.
    /// When the local player’s palm trigger overlaps this sphere, it identifies the butt contact and allows the
    /// remote object’s interaction logic to be invoked.
    /// </summary>
    public class RemoteButtTouchTarget : MonoBehaviour
    {
        public enum Side { Left, Right }
        /// <summary>
        /// Manually set the side of the player's hand that this collider is on in the inspector.
        /// </summary>
        [field: Header("Butt Touch Target")]
        [field: Tooltip("Select the hand side that this collider corresponds to.")]
        [field: SerializeField] public Side RigPartSide { get; private set; }
    }
}