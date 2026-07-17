namespace Stratton.Core
{
    public interface IGameStateTypeList : IBaseTypeList
    {
    }

    public sealed class BaseGameStateType : IGameStateTypeList
    {
        public static readonly GameStateType None = new GameStateType(nameof(None));
        public static readonly GameStateType Initializing = new GameStateType(nameof(Initializing));
        public static readonly GameStateType Connecting = new GameStateType(nameof(Connecting));
        public static readonly GameStateType LoadingMenu = new GameStateType(nameof(LoadingMenu));
        public static readonly GameStateType Menu = new GameStateType(nameof(Menu));
        public static readonly GameStateType LoadingGameplay = new GameStateType(nameof(LoadingGameplay));
        public static readonly GameStateType Gameplay = new GameStateType(nameof(Gameplay));
        public static readonly GameStateType Results = new GameStateType(nameof(Results));
        public static readonly GameStateType UnloadingGameplay = new GameStateType(nameof(UnloadingGameplay));
        public static readonly GameStateType DeInitializing = new GameStateType(nameof(DeInitializing));
    }
}