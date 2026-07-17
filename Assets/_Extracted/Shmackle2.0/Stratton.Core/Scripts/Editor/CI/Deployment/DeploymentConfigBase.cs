using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using Stratton.Core.Types;
using UnityEngine;

namespace Stratton.CI.Editor
{
    public abstract class DeploymentConfigBase : ScriptableObject, IDeploymentConfig
    {
        #region Serialized Fields

        [SerializeField] protected StageType _stageType = BaseStageType.Dev;

        #endregion

        #region Fields

        protected DeployerType _deployerType = DeployerType.Local;

        #endregion

        #region Properties

        public StageType StageType => _stageType;

        #endregion

        #region Public Methods

        public virtual void Init(DeployerType deployerType, Dictionary<string, string> parameters)
        {
            _deployerType = deployerType;
        }

        public virtual void OnPostInit()
        {
        }

        public virtual void OnPreDeploy()
        {
            UpdateProjectFiles();
        }

        public virtual async UniTask Deploy()
        {
            await UniTask.CompletedTask;
        }

        public virtual void OnPostDeploy()
        {
            UpdateEnvironmentVariables();
        }

        #endregion

        #region Private Methods

        protected virtual void UpdateProjectFiles()
        {
        }

        protected virtual void UpdateEnvironmentVariables()
        {
        }

        #endregion
    }
}