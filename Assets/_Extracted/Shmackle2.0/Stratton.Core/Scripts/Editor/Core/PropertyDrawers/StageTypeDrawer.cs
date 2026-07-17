using Stratton.Core.Types;
using UnityEditor;

namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(StageType))]
    public class StageTypeDrawer : BaseTypeDrawer<StageType, IStageTypeList> { }
}