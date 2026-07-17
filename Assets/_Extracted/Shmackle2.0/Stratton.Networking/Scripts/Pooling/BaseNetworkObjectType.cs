using Stratton.Core;

namespace Stratton.Networking
{
    public interface INetworkObjectTypeList : IBaseTypeList
    {
    }

    public class BaseNetworkObjectType : INetworkObjectTypeList
    {
        public static readonly NetworkObjectType None = new NetworkObjectType("None");
        public static readonly NetworkObjectType NetworkSceneSynchronizer = new NetworkObjectType("NetworkSceneSynchronizer");
        public static readonly NetworkObjectType FusionEventsRewinder = new NetworkObjectType("FusionEventsRewinder");
        public static readonly NetworkObjectType Player = new NetworkObjectType("Player");
    }
}


