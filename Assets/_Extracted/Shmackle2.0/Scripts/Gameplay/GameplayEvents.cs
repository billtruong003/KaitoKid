namespace Shmackle.Gameplay
{
    public abstract class PlayerStateEvent
    {
        public PlayerState PlayerState;
    }

    public class PlayerStateRegisteredEvent :  PlayerStateEvent { }

    public class PlayerStateUnregisteredEvent : PlayerStateEvent { }
}
