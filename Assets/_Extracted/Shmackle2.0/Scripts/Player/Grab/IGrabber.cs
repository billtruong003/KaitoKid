using Fusion.XR.Shared.Core;

namespace Shmackle.Player.Grab
{
    /// <summary>
    /// Interface for components that handle grabbing behavior,
    /// including tracking grab state and performing grab and release actions.
    /// </summary>
    public interface IGrabber : IUnityBehaviour
    {
        IGrabbingProvider RigPart { get; set; }
        bool IsGrabbing { get; }
        
        void TryGrab(ShmackleGrabbable grabbable);
        void TryRelease();
    }
}