using Stratton.Core;
using Stratton.Loading;

namespace Shmackle.GameStates
{
    public class DeInitializingState : GameStateBase
    {
        #region Fields

        private LoadingProgressMonitor _loadingProgressMonitor;

        #endregion

        public static GameStateType Type => BaseGameStateType.DeInitializing;

        public override void Enter()
        {
            base.Enter();

            _loadingProgressMonitor = AppServicesManager.Instance.Get<LoadingProgressMonitor>();

            _loadingProgressMonitor.FinishLoadingFlow();
        }
    }
}