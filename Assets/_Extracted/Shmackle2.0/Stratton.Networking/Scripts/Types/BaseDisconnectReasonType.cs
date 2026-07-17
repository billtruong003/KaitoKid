using Stratton.Core;

namespace Stratton.Networking
{
    public interface IDisconnectReasonTypeList : IBaseTypeList
    {
    }

    public class BaseDisconnectReasonType : IDisconnectReasonTypeList
    {
        public static readonly DisconnectReasonType DisconnectedByClient = new DisconnectReasonType(nameof(DisconnectedByClient));
        public static readonly DisconnectReasonType DisconnectedByServer = new DisconnectReasonType(nameof(DisconnectedByServer));
    }
}
