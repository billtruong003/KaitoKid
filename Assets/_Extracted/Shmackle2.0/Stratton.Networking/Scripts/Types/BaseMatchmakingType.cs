using Stratton.Core;

namespace Stratton.Networking
{
    public interface IMatchmakingTypeList : IBaseTypeList
    {
    }

    public class BaseMatchmakingType : IMatchmakingTypeList
    {
        public static readonly MatchmakingType None = new MatchmakingType(nameof(None));
        public static readonly MatchmakingType PhotonCloud = new MatchmakingType(nameof(PhotonCloud));
        public static readonly MatchmakingType Multiplay = new MatchmakingType(nameof(Multiplay));
    }
}
