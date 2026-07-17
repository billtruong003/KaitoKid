using Stratton.Core;
using Stratton.Loading.Types;

namespace Stratton.Loading
{
    public interface ILoadingFlowTypeList : IBaseTypeList
    {
    }

    public sealed class BaseLoadingFlowType : ILoadingFlowTypeList
    {
        public static readonly LoadingFlowType None = new LoadingFlowType(nameof(None));
        public static readonly LoadingFlowType InitialLoading = new LoadingFlowType(nameof(InitialLoading));
        public static readonly LoadingFlowType GameplayLoading = new LoadingFlowType(nameof(GameplayLoading));
        public static readonly LoadingFlowType GameplayUnloading = new LoadingFlowType(nameof(GameplayUnloading));
    }
}