namespace Stratton.Core
{
    public interface ILogChannelList
    {
    }

    public sealed class BaseLogChannel : ILogChannelList
    {
        public static readonly Log.Channel Core = new(nameof(Core));
        public static readonly Log.Channel Debug = new(nameof(Debug));
        public static readonly Log.Channel Exception = new(nameof(Exception));
        public static readonly Log.Channel GameStates = new(nameof(GameStates));
        public static readonly Log.Channel Build = new(nameof(Build));
        public static readonly Log.Channel Deployment = new(nameof(Deployment));
        public static readonly Log.Channel Save = new(nameof(Save));
        public static readonly Log.Channel Loading = new(nameof(Loading));
        public static readonly Log.Channel UI = new(nameof(UI));
        public static readonly Log.Channel Gameplay = new Log.Channel("Gameplay");
        public static readonly Log.Channel ObjectPool = new Log.Channel("ObjectPool");
        public static readonly Log.Channel Assets = new(nameof(Assets));
        public static readonly Log.Channel Audio = new(nameof(Audio));
    }
}