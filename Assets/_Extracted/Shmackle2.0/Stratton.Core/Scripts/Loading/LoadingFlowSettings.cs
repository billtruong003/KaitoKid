using System.Collections.Generic;
using Stratton.Loading.DataModels;
using UnityEngine;

namespace Stratton.Loading
{
    [CreateAssetMenu(fileName = "LoadingFlowSettings", menuName = "Settings/Loading Flow Settings")]
    public class LoadingFlowSettings : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField] protected List<LoadingFlowDM> _loadingFlows = new();

        #endregion

        #region Properties

        public List<LoadingFlowDM> LoadingFlows => _loadingFlows;

        #endregion
    }
}