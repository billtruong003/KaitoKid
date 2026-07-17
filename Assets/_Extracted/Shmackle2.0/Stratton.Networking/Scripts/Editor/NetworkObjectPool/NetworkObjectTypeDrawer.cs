using Stratton.Core.Editor;
using UnityEditor;

namespace Stratton.Networking.Editor
{
    /// <summary>
    /// Property drawer for NetworkObjectType
    /// </summary>
    [CustomPropertyDrawer(typeof(NetworkObjectType))]
    public class NetworkObjectTypeDrawer : BaseTypeDrawer<NetworkObjectType, INetworkObjectTypeList>
    {
    }
}


