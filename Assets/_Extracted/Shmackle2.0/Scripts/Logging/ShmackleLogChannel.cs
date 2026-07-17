using Stratton.Core;

namespace Shmackle
{
    public sealed class ShmackleLogChannel : ILogChannelList
    {
        public static readonly Log.Channel User = new Log.Channel(nameof(User));
    }
}