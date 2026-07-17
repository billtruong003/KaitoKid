using Stratton.Core.Types;
using UnityEditor;

namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(DistributionPlatformType))]
    public class DistributionPlatformTypeDrawer : BaseTypeDrawer<DistributionPlatformType, IDistributionPlatformTypeList> { }
}