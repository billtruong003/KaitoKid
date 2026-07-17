using System;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using MessagePipe;
using UnityEngine;

namespace Stratton.AppReloading
{
    public class AppReloader : AppServiceBase
    {
        #region Serialized Fields

        [SerializeField] private AppReloadSettings _settings;
        [SerializeField] private AppReloadSource[] _reloadSources;

        #endregion

        #region Fields

        protected IPublisher<AppReloadRequestedEvent> _appReloadRequestedEventPublisher;

        protected ISubscriber<AllGameSystemsInitializedEvent> _allGameSystemsInitializedEventSubscriber;

        protected IDisposable _eventsBagDisposable;

        #endregion

        #region Properties

        public bool IsAppReloadRequested { get; protected set; }

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            builder.AddMessageBroker<AppReloadRequestedEvent>();
        }

        public async UniTask Reload()
        {
            IsAppReloadRequested = true;
            _appReloadRequestedEventPublisher.Publish(new());
            await GameSystemsManager.Instance.ReInit();
        }

        public override void Init()
        {
            foreach (var reloadSource in _reloadSources)
            {
                reloadSource.Init(OnAppReloadRequested, _settings);
            }

            _appReloadRequestedEventPublisher = GlobalMessagePipe.GetPublisher<AppReloadRequestedEvent>();
            _allGameSystemsInitializedEventSubscriber = GlobalMessagePipe.GetSubscriber<AllGameSystemsInitializedEvent>();

            var bag = DisposableBag.CreateBuilder();

            _allGameSystemsInitializedEventSubscriber.Subscribe(e => IsAppReloadRequested = false).AddTo(bag);

            _eventsBagDisposable = bag.Build();

            base.Init();
        }

        public override void DeInit()
        {
            _eventsBagDisposable?.Dispose();
            foreach (var item in _reloadSources)
            {
                item.DeInit();
            }

            base.DeInit();
        }

        public void OnAppReloadRequested()
        {
            Reload().Forget();
        }

        #endregion
    }
}