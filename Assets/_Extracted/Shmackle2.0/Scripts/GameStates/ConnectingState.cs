using Stratton.Core;
using Stratton.Loading;
using Stratton.Networking;

namespace Shmackle.GameStates
{
    public class ConnectingState : GameStateBase
    {
        #region Fields

        private GameStateSystem _gameStateSystem;
        private NetworkingSystem _networkingSystem;
        private LoadingProgressMonitor _loadingProgressMonitor;

        #endregion

        #region Properties

        public static GameStateType Type => BaseGameStateType.Connecting;

        #endregion

        #region Public Methods

        public override async void Enter()
        {
            base.Enter();

            _gameStateSystem = GameSystemsManager.Instance.Get<GameStateSystem>();
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            _loadingProgressMonitor = AppServicesManager.Instance.Get<LoadingProgressMonitor>();

            _loadingProgressMonitor.ChangeLoadingStep(BaseLoadingStepType.Matchmaking);
            var matchmakingResult = await _networkingSystem.StartMatchmaker();

            _loadingProgressMonitor.ChangeLoadingStep(BaseLoadingStepType.Connecting);
            await _networkingSystem.StartConnection(matchmakingResult.ConnectionArguments);

            _gameStateSystem.GoToState(BaseGameStateType.Gameplay);
        }

        #endregion
    }
}