using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using UnityEditor;

namespace Stratton.CI.Editor
{
    public class Deployer
    {
        #region Fields

        protected DeployerType _deployerType;

        #endregion

        #region Public Methods

        public Deployer(DeployerType deployerType)
        {
            _deployerType = deployerType;
        }

        public async UniTask Deploy(Dictionary<string, string> parameters)
        {
            Log.Message(BaseLogChannel.Deployment, $"Starting deploying with parameters:\n{GetParametersString(parameters)}");

            var deploymentConfigName = string.Empty;
            if (parameters.ContainsKey("deploymentConfig"))
            {
                deploymentConfigName = parameters["deploymentConfig"];
            }
            else
            {
                throw new Exception($"There's no deployment config given in the parameters!");
            }
            if (deploymentConfigName.IsNullOrEmpty())
            {
                throw new Exception($"There's no deployment config given in the parameters!");
            }

            IDeploymentConfig deploymentConfig = GetDeploymentConfig(deploymentConfigName);

            // Init step
            Log.Message(BaseLogChannel.Deployment, $"Entering deployment phase: Init");
            deploymentConfig.Init(_deployerType, parameters);
            // PostInit step
            Log.Message(BaseLogChannel.Deployment, $"Entering deployment phase: PostInit");
            deploymentConfig.OnPostInit();
            // PreDeploy step
            Log.Message(BaseLogChannel.Deployment, $"Entering deployment phase: PreDeploy");
            deploymentConfig.OnPreDeploy();
            // Deploy step
            Log.Message(BaseLogChannel.Deployment, $"Entering deployment phase: Deploy");
            await deploymentConfig.Deploy();
            // PostDeploy step
            Log.Message(BaseLogChannel.Deployment, $"Entering deployment phase: PostDeploy");
            deploymentConfig.OnPostDeploy();
            Log.Message(BaseLogChannel.Deployment, $"Finished deployment!");
        }

        #endregion

        #region Private Methods

        protected IDeploymentConfig GetDeploymentConfig(string configName)
        {
            var deploymentConfig = AssetDatabase.LoadAssetAtPath<DeploymentConfigBase>($"{DeploymentEditorSettings.Instance.ProjectDeploymentConfigsPath}/{configName}.asset");
            if (deploymentConfig == null)
            {
                throw new Exception($"Can't find deployment config with the given name: {configName}");
            }
            return deploymentConfig;
        }

        protected string GetParametersString(Dictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (var p in parameters)
            {
                sb.AppendLine($"{p.Key}: {p.Value}");
            }
            return sb.ToString();
        }

        #endregion
    }
}