using MessagePipe;
using UnityEngine;

namespace Shmackle.VFX
{
    /// <summary>
    /// A sample controller for demonstrating how to trigger VFX events.
    /// </summary>
    public class SampleVFXController : MonoBehaviour
    {
        /// <summary>
        /// Publishes a VFXEvent to the global message pipe.
        /// This event signals that a VFX should be played at this object's position.
        /// </summary>

        public void PlayExplodeEvent()
        {
            var publisher = GlobalMessagePipe.GetPublisher<VFXBaseEvent>();
            var vfxEvent = new VFXBaseEvent
            {
                Target = transform
            };
            publisher.Publish(vfxEvent);
        }
    }
}
