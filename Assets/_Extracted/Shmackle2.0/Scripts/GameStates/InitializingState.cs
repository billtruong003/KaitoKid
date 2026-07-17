using Cysharp.Threading.Tasks;
using MessagePipe;
using Shmackle.Events.UI;
using Shmackle.Utilities;
using Stratton.Core;
using Stratton.Loading;
using System;

namespace Shmackle.GameStates
{
    public class InitializingState : GameStateBase
    {
        #region Fields

        private GameStateSystem _gameStateSystem;
        private LoadingProgressMonitor _loadingProgressMonitor;

        // private bool _isUiBootstrapSceneLoaded = false;

        #endregion

        #region Properties

        public static GameStateType Type => BaseGameStateType.Initializing;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            base.InstallMessageBrokers(builder);
        }

        public override async void Enter()
        {
            base.Enter();

            _gameStateSystem = GameSystemsManager.Instance.Get<GameStateSystem>();
            _loadingProgressMonitor = AppServicesManager.Instance.Get<LoadingProgressMonitor>();

            if (_loadingProgressMonitor.CurrentLoadingFlow != null)
            {
                _loadingProgressMonitor.FinishLoadingFlow();
            }
            _loadingProgressMonitor.StartLoadingFlow(BaseLoadingFlowType.InitialLoading);

            Progress<float> gameplaySceneLoadingProgressCallback = new Progress<float>();
            _loadingProgressMonitor.ChangeLoadingStep(BaseLoadingStepType.GameplaySceneLoading, gameplaySceneLoadingProgressCallback);
            await SceneUtils.LoadInternalScene(SceneNames.Gameplay, gameplaySceneLoadingProgressCallback);

            _gameStateSystem.GoToState(BaseGameStateType.Connecting);
        }

        #endregion
    }
}