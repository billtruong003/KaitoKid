using System;
using Stratton.Loading.Types;

namespace Stratton.Loading
{
    public class LoadingStepChangedEvent
    {
        public LoadingFlowType FlowType;
        public LoadingStepType StepType;
        public Progress<float> ProgressCallback;
    }

    public class LoadingFlowStartedEvent
    {
        public LoadingFlowType FlowType;
    }

    public class LoadingFlowFinishedEvent
    {
        public LoadingFlowType FlowType;
    }

    public class LoadingProgressChangedEvent
    {
        public LoadingFlowType FlowType;
        public float Value;
    }
}