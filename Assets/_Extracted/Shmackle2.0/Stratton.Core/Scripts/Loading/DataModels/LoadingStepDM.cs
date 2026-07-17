using System;
using Stratton.Loading.Types;
using UnityEngine;

namespace Stratton.Loading.DataModels
{
    [Serializable]
    public class LoadingStepDM
    {
        // only for inspector list drawing purpose
        [HideInInspector] [SerializeField] private string _name;
        [SerializeField] protected LoadingStepType _stepType;
        [SerializeField] protected float _weight = 1;

        public LoadingStepType StepType => _stepType;
        public float Weight => _weight;
    }
}