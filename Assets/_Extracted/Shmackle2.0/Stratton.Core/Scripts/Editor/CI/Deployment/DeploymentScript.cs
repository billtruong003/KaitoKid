using System;
using System.Collections.Generic;
using Stratton.Core;
using UnityEditor;

namespace Stratton.CI.Editor
{
    public static class DeploymentScript
    {
        #region Public Methods

        public static async void Deploy()
        {
            var deployer = new Deployer(DeployerType.Jenkins);
            try
            {
                await deployer.Deploy(GetDeploymentParameters());
            }
            catch (Exception ex)
            {
                Log.Error(BaseLogChannel.Deployment, $"Deployment failed: {ex.Message}");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        public static Dictionary<string, string> GetDeploymentParameters()
        {
            var parameters = new Dictionary<string, string>();
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].Contains("="))
                {
                    var pairString = arguments[i];
                    var pairArray = pairString.Split('=');
                    var pairValue = pairString.Substring(pairArray[0].Length + 1);
                    parameters.Add(pairArray[0], pairValue);
                }
            }
            return parameters;
        }

        #endregion
    }
}