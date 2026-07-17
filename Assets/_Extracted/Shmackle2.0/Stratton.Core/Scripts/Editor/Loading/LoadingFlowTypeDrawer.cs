using Stratton.Core.Editor;
using Stratton.Loading.Types;
using UnityEditor;

namespace Stratton.Loading.Editor
{
    [CustomPropertyDrawer(typeof(LoadingFlowType))]
    public class LoadingFlowTypeDrawer : BaseTypeDrawer<LoadingFlowType, ILoadingFlowTypeList>
    {

    }
}