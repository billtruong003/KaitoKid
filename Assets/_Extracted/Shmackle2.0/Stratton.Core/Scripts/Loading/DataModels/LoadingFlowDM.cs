using System;
using System.Collections.Generic;
using Stratton.Loading.Types;
using UnityEngine;

namespace Stratton.Loading.DataModels
{
    [Serializable]
    public class LoadingFlowDM
    {
        // only for inspector list drawing purpose
        [HideInInspector] [SerializeField] private string _name;
        [SerializeField] protected LoadingFlowType _flowType;
        [SerializeField] protected List<LoadingStepDM> _flowSteps;

        public LoadingFlowType FlowType => _flowType;
        public List<LoadingStepDM> FlowSteps => _flowSteps;
    }
}