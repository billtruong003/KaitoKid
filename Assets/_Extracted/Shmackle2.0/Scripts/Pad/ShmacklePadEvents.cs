using Fusion.XR.Shared.Core;
using Shmackle.Gameplay;
using Shmackle.Pad;

namespace Shmackle.Pad
{
    public abstract class ShmacklePadEvent
    {
        public RigPartSide Side;
    }
    public class ShmacklePadLoadingStartedEvent : ShmacklePadEvent
    {
    }
    public class ShmacklePadLoadingProgressChangedEvent : ShmacklePadEvent
    {
        public float Progress;
    }
    public class ShmacklePadLoadingFinishedEvent : ShmacklePadEvent
    {
        public bool IsCancelled;
        public ShmacklePad Pad;
    }

    public class ShmacklePadSpawnTriggeredEvent : ShmacklePadEvent
    {
        public bool IsTriggering;
    }
}
