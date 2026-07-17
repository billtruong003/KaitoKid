using Stratton.Core.Editor;
using Stratton.Loading.Types;
using UnityEditor;

namespace Stratton.Loading.Editor
{
    [CustomPropertyDrawer(typeof(LoadingStepType))]
    public class LoadingStepTypeDrawer : BaseTypeDrawer<LoadingStepType, ILoadingStepTypeList>
    {

    }
}