using Squido.JungleXRKit.Core;

namespace Teabag.Core
{
    public interface IGameLoopService : IService
    {
        int CurrentPhase { get; }
        bool HasManager { get; }
        bool HasWaitingZoneManager { get; }
        bool SuppressSceneCleanup { get; }
        bool SuppressRespawnOnSceneLoad { get; }
        void NotifyMatchComplete();
        void ReturnToStation();
    }
}
