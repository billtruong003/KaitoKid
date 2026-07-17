using UnityEngine;

namespace Shmackle.VFX
{
    /// <summary>
    /// Represents an event that triggers a visual effect (VFX) at a specific target location.
    /// </summary>
    public class VFXBaseEvent
    {
        /// <summary>
        /// The transform of the target where the VFX should be played.
        /// </summary>
        public Transform Target;
    }
}
