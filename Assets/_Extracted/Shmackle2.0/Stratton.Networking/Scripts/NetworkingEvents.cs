using Fusion;

namespace Stratton.Networking
{
    public class PlayerEventBase
    {
        public PlayerRef Player;
    }
    public class PlayerTargetEventBase
    {
        public PlayerRef SourcePlayer;
        public PlayerRef TargetPlayer;
    }

    public class PlayerJoinedSharedModeEvent : PlayerEventBase { }
    public class PlayerJoinedHostModeEvent : PlayerEventBase { }
    public class PlayerLeftHostModeEvent : PlayerEventBase { }
    public class PlayerJoinedEvent : PlayerEventBase { }
    public class PlayerLeftEvent : PlayerEventBase { }
    public class PlayerJoinedAudioGroupEvent : PlayerTargetEventBase { }
    public class PlayerLeftAudioGroupEvent : PlayerTargetEventBase { }
    public class SceneLoadedEvent {}
}