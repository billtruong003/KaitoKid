using Stratton.Core;

namespace Stratton.Networking
{
    public sealed class NetworkingLogChannel : ILogChannelList
    {
        public static readonly Log.Channel NetworkRunnerCallbacks = new Log.Channel(nameof(NetworkRunnerCallbacks));
        public static readonly Log.Channel NetworkingSystem = new Log.Channel(nameof(NetworkingSystem));
        public static readonly Log.Channel Matchmaking = new Log.Channel(nameof(Matchmaking));
        public static readonly Log.Channel NetworkingSettings = new Log.Channel(nameof(NetworkingSettings));
        public static readonly Log.Channel Multiplay = new Log.Channel(nameof(Multiplay));
        public static readonly Log.Channel ObjectPool = new Log.Channel(nameof(ObjectPool));
        public static readonly Log.Channel SceneManager = new Log.Channel(nameof(SceneManager));
        public static readonly Log.Channel Connections = new Log.Channel(nameof(Connections));
        public static readonly Log.Channel ClientPrediction = new Log.Channel(nameof(ClientPrediction));
        public static readonly Log.Channel Voice = new Log.Channel(nameof(Voice));
        public static readonly Log.Channel Player = new Log.Channel(nameof(Player));
    }
}