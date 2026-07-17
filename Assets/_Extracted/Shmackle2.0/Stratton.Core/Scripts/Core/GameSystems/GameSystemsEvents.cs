using System;

namespace Stratton.Core
{
    public class AllGameSystemsInitializedEvent
    {
    }

    public class GameSystemInitializationFailedEvent
    {
        public Type Type;
        public InitializationErrorCode ErrorCode;
        public string ErrorMessage;
    }

    public class GameSystemInitializationCancelledEvent
    {
        public Type Type;
    }

    public class GameSystemInitializationPausedEvent
    {
        public Type Type;
    }

    public struct AppQuitEvent { }

    public struct AppFocusedEvent
    {
        public bool IsFocused;
    }

    public struct AppPausedEvent
    {
        public bool IsPaused;
    }
}