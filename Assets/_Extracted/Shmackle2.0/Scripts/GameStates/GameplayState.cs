using MessagePipe;
using Shmackle.Minimap;
using Shmackle.Pad;
using Stratton.Core;
using Stratton.Loading;

namespace Shmackle.GameStates
{
    public class GameplayState : GameStateBase
    {
        #region Fields

        private LoadingProgressMonitor _loadingProgressMonitor;

        #endregion

        #region Properties

        public static GameStateType Type => BaseGameStateType.Gameplay;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            base.InstallMessageBrokers(builder);
            builder.AddMessageBroker<ShmacklePadLoadingStartedEvent>();
            builder.AddMessageBroker<ShmacklePadLoadingProgressChangedEvent>();
            builder.AddMessageBroker<ShmacklePadLoadingFinishedEvent>();
            builder.AddMessageBroker<ShmacklePadSpawnTriggeredEvent>();
            builder.AddMessageBroker<MinimapSetEnabledEvent>();
        }

        public override void Enter()
        {
            base.Enter();

            _loadingProgressMonitor = AppServicesManager.Instance.Get<LoadingProgressMonitor>();

            _loadingProgressMonitor.FinishLoadingFlow();
        }

        #endregion
    }
}