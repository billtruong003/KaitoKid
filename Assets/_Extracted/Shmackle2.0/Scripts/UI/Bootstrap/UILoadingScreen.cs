using MessagePipe;
using Stratton.Core;
using Stratton.Loading;
using Stratton.Loading.Types;
using TMPro;
using UnityEngine;

namespace Shmackle.UI
{
    public class UILoadingScreen : UIScreenBase
    {
        #region Serialized Fields

        [SerializeField] TextMeshProUGUI _progressLabel;

        #endregion

        #region Non-Serialized Fields

        private LoadingProgressMonitor _loadingProgressMonitor;

        private ISubscriber<LoadingProgressChangedEvent> _loadingProgressChangedEventSubscriber;
        private ISubscriber<LoadingFlowStartedEvent> _loadingFlowStartedEventSubscriber;
        private ISubscriber<LoadingFlowFinishedEvent> _loadingFlowFinishedEventSubscriber;

        protected LoadingFlowType _currentLoadingFlowType = BaseLoadingFlowType.None;

        #endregion

        #region Private Methods

        protected override void Init()
        {
            base.Init();

            _loadingProgressMonitor = AppServicesManager.Instance.Get<LoadingProgressMonitor>();

            _loadingProgressChangedEventSubscriber = GlobalMessagePipe.GetSubscriber<LoadingProgressChangedEvent>();
            _loadingFlowStartedEventSubscriber = GlobalMessagePipe.GetSubscriber<LoadingFlowStartedEvent>();
            _loadingFlowFinishedEventSubscriber = GlobalMessagePipe.GetSubscriber<LoadingFlowFinishedEvent>();

            if (_loadingProgressMonitor.CurrentLoadingFlow != null)
            {
                var flowType = _loadingProgressMonitor.CurrentLoadingFlow.FlowType;
                OnLoadingFlowStartedEvent(flowType);
                OnLoadingProgressChanged(flowType, _loadingProgressMonitor.LoadingProgress);
            }
        }

        protected override void RegisterFullTimeEvents()
        {
            base.RegisterFullTimeEvents();

            var bag = DisposableBag.CreateBuilder();

            _loadingProgressChangedEventSubscriber.Subscribe(e => OnLoadingProgressChanged(e.FlowType, e.Value)).AddTo(bag);
            _loadingFlowStartedEventSubscriber.Subscribe(e => OnLoadingFlowStartedEvent(e.FlowType)).AddTo(bag);
            _loadingFlowFinishedEventSubscriber.Subscribe(e => OnLoadingFlowFinishedEvent(e.FlowType)).AddTo(bag);

            _eventsFullTimeBagDisposable = bag.Build();
        }

        protected virtual bool IsHandlingFlowType(LoadingFlowType flowType)
        {
            return true;
        }

        protected virtual void OnLoadingProgressChanged(LoadingFlowType flowType, float value)
        {
            if (!IsHandlingFlowType(flowType))
                return;

            if (_currentLoadingFlowType == flowType)
            {
                SetLoadingProgress(value);
            }
        }

        protected virtual void OnLoadingFlowStartedEvent(LoadingFlowType flowType)
        {
            if (!IsHandlingFlowType(flowType))
                return;

            _currentLoadingFlowType = flowType;
            Show();
        }

        protected virtual void OnLoadingFlowFinishedEvent(LoadingFlowType flowType)
        {
            if (!IsHandlingFlowType(flowType))
                return;

            if (_currentLoadingFlowType == flowType)
            {
                Hide();
            }
        }

        private void SetLoadingProgress(float progress)
        {
            _progressLabel.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        #endregion
    }
}