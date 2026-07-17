using Stratton.Core.Editor;
using UnityEditor;

namespace Stratton.Networking.Editor
{
    /// <summary>
    /// Property drawer for NetworkObjectType
    /// </summary>
    [CustomPropertyDrawer(typeof(MatchmakingType))]
    public class MatchmakingTypePropertyDrawer : BaseTypeDrawer<MatchmakingType, IMatchmakingTypeList>
    {
    }
}
