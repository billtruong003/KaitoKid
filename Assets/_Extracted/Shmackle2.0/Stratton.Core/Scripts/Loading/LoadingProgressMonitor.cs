using MessagePipe;
using Stratton.Core;
using Stratton.Loading.DataModels;
using Stratton.Loading.Types;
using System;
using System.Linq;
using UnityEngine;

namespace Stratton.Loading
{
    public class LoadingProgressMonitor : AppServiceBase
    {
        #region Serialized Fields

        [SerializeField] private LoadingFlowSettings _settings;

        #endregion

        #region Fields

        protected IPublisher<LoadingProgressChangedEvent> _loadingProgressChangedEventPublisher;
        protected IPublisher<LoadingStepChangedEvent> _loadingStepChangedEventPublisher;
        protected IPublisher<LoadingFlowStartedEvent> _loadingFlowStartedEventPublisher;
        protected IPublisher<LoadingFlowFinishedEvent> _loadingFlowFinishedEventPublisher;

        protected LoadingFlowDM _currentLoadingFlow;
        protected float _currentLoadingFlowWeights = 0f;
        protected LoadingStepType _currentLoadingStepType = BaseLoadingStepType.None;
        protected int _currentLoadingStepIndex;
        protected Progress<float> _currentLoadingStepProgressCallback;
        protected float _loadingProgress;

        #endregion

        #region Properties

        public float LoadingProgress
        {
            get => _loadingProgress;
            set
            {
                _loadingProgress = value;
                _loadingProgressChangedEventPublisher.Publish(new() { FlowType = _currentLoadingFlow.FlowType, Value = value });
                Log.Message(BaseLogChannel.Debug, $"Aggregated loading progress: {value}");
            }
        }

        public LoadingFlowDM CurrentLoadingFlow => _currentLoadingFlow;

        public LoadingStepType CurrentLoadingStepType => _currentLoadingStepType;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            builder.AddMessageBroker<LoadingStepChangedEvent>();
            builder.AddMessageBroker<LoadingFlowStartedEvent>();
            builder.AddMessageBroker<LoadingFlowFinishedEvent>();
            builder.AddMessageBroker<LoadingProgressChangedEvent>();
        }

        public override void Init()
        {
            _loadingProgressChangedEventPublisher = GlobalMessagePipe.GetPublisher<LoadingProgressChangedEvent>();
            _loadingStepChangedEventPublisher = GlobalMessagePipe.GetPublisher<LoadingStepChangedEvent>();
            _loadingFlowStartedEventPublisher = GlobalMessagePipe.GetPublisher<LoadingFlowStartedEvent>();
            _loadingFlowFinishedEventPublisher = GlobalMessagePipe.GetPublisher<LoadingFlowFinishedEvent>();

            base.Init();
        }

        public override void DeInit()
        {
            if (_currentLoadingStepProgressCallback != null)
            {
                _currentLoadingStepProgressCallback.ProgressChanged -= CurrentLoadingStepProgressChanged;
            }
            base.DeInit();
        }

        public virtual void StartLoadingFlow(LoadingFlowType flowType)
        {
            if (_currentLoadingFlow != null)
            {
                Log.Error(BaseLogChannel.Loading, $"Can't start a new loading flow of type {flowType.Name} because the current flow of type {_currentLoadingFlow.FlowType.Name} hasn't finished yet!");
                return;
            }
            _currentLoadingFlow = _settings.LoadingFlows.Find(f => f.FlowType == flowType);
            if (_currentLoadingFlow == null)
            {
                Log.Error(BaseLogChannel.Loading, $"Can't find loading flow of type {flowType.Name}");
                return;
            }
            ResetLoadingProgress();
            _currentLoadingFlowWeights = 0;
            for (var i = 0; i < _currentLoadingFlow.FlowSteps.Count; i++)
            {
                _currentLoadingFlowWeights += _currentLoadingFlow.FlowSteps[i].Weight;
            }
            _loadingFlowStartedEventPublisher.Publish(new() { FlowType = flowType });
        }

        public virtual void ChangeLoadingStep(LoadingStepType stepType, Progress<float> progressCallback = null)
        {
            if (_currentLoadingFlow == null)
            {
                Log.Error(BaseLogChannel.Loading, $"Can't change the loading step because there's no current loading flow in progress!");
                return;
            }
            if (!_currentLoadingFlow.FlowSteps.Any(s => s.StepType == stepType))
            {
                Log.Error(BaseLogChannel.Loading, $"Can't change the loading step because it's not a part of the current loading flow!");
                return;
            }
            UnregisterLoadingProgressCallback();
            _currentLoadingStepType = stepType;
            var stepNumberIndex = -1;
            for (var i = 0; i < _currentLoadingFlow.FlowSteps.Count; i++)
            {
                if (_currentLoadingFlow.FlowSteps[i].StepType == stepType)
                {
                    stepNumberIndex = i;
                    break;
                }
            }
            if (stepNumberIndex == -1)
            {
                Log.Error(BaseLogChannel.Loading, $"Can't find loading step of type {stepType.Name} in the loading flow of type {_currentLoadingFlow.FlowType.Name}");
                return;
            }
            _currentLoadingStepIndex = stepNumberIndex;
            LoadingProgress = CalculateLoadingProgressForCurrentFlow(0);
            RegisterLoadingProgressCallback(progressCallback);
            _loadingStepChangedEventPublisher.Publish(new() { FlowType = _currentLoadingFlow.FlowType, StepType = stepType, ProgressCallback = progressCallback });
        }

        public virtual void FinishLoadingFlow()
        {
            if (_currentLoadingFlow == null)
            {
                return;
            }
            UnregisterLoadingProgressCallback();
            _loadingFlowFinishedEventPublisher.Publish(new() { FlowType = _currentLoadingFlow.FlowType });
            _currentLoadingFlow = null;
        }

        #endregion

        #region Private Methods

        private void CurrentLoadingStepProgressChanged(object sender, float e)
        {
            LoadingProgress = CalculateLoadingProgressForCurrentFlow(e);
        }

        protected virtual float CalculateLoadingProgressForCurrentFlow(float currentLoadingStepProgress)
        {
            var finishedSteps = 0f;
            for (var i = 0; i < _currentLoadingStepIndex; i++)
            {
                finishedSteps += _currentLoadingFlow.FlowSteps[i].Weight;
            }
            return finishedSteps / _currentLoadingFlowWeights + (_currentLoadingFlow.FlowSteps[_currentLoadingStepIndex].Weight / _currentLoadingFlowWeights) * currentLoadingStepProgress;
        }

        protected virtual void ResetLoadingProgress()
        {
            LoadingProgress = 0;
            UnregisterLoadingProgressCallback();
        }

        protected virtual void RegisterLoadingProgressCallback(Progress<float> progressCallback)
        {
            _currentLoadingStepProgressCallback = progressCallback;
            if (_currentLoadingStepProgressCallback != null)
            {
                _currentLoadingStepProgressCallback.ProgressChanged += CurrentLoadingStepProgressChanged;
            }
        }

        protected virtual void UnregisterLoadingProgressCallback()
        {
            if (_currentLoadingStepProgressCallback != null)
            {
                _currentLoadingStepProgressCallback.ProgressChanged -= CurrentLoadingStepProgressChanged;
            }
        }

        protected virtual void OnDestroy()
        {
            DeInit();
        }

        #endregion
    }
}