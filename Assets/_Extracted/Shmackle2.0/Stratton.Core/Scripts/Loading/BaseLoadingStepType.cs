using Stratton.Core;
using Stratton.Loading.Types;

namespace Stratton.Loading
{
    public interface ILoadingStepTypeList : IBaseTypeList
    {
    }

    public sealed class BaseLoadingStepType : ILoadingStepTypeList
    {
        public static readonly LoadingStepType None = new LoadingStepType(nameof(None));

        // Initial loading
        public static readonly LoadingStepType UIBootstrapSceneLoading = new LoadingStepType(nameof(UIBootstrapSceneLoading));
        public static readonly LoadingStepType UICommonSceneLoading = new LoadingStepType(nameof(UICommonSceneLoading));
        public static readonly LoadingStepType UIMenuSceneLoading = new LoadingStepType(nameof(UIMenuSceneLoading));

        // Connecting
        public static readonly LoadingStepType Matchmaking = new LoadingStepType(nameof(Matchmaking));
        public static readonly LoadingStepType Connecting = new LoadingStepType(nameof(Connecting));

        // Gameplay loading
        public static readonly LoadingStepType GameplaySceneLoading = new LoadingStepType(nameof(GameplaySceneLoading));
        public static readonly LoadingStepType UIGameplaySceneLoading = new LoadingStepType(nameof(UIGameplaySceneLoading));
        public static readonly LoadingStepType GameplayAssetsLoading = new LoadingStepType(nameof(GameplayAssetsLoading));
    }
}