using System.Collections.Generic;
using Stratton.Core;
using UnityEditor;
using UnityEngine;

namespace Stratton.CI.Editor
{
    public class DeploymentWindow : EditorWindow
    {
        #region Fields

        protected DeploymentConfigBase _deploymentConfig;

        #endregion

        #region Public Methods

        [MenuItem("Deployment/Deployment Window")]
        public static void Show()
        {
            DeploymentWindow window = (DeploymentWindow)GetWindow(typeof(DeploymentWindow));
            window.minSize = new Vector2(500, 250);
            window.titleContent = new GUIContent("Deployment");
        }

        #endregion

        #region Private Methods

        protected void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // Deployment Config
            GUILayout.BeginVertical();
            GUILayout.Label("Deployment Config", EditorStyles.boldLabel);
            _deploymentConfig = (DeploymentConfigBase)EditorGUILayout.ObjectField(_deploymentConfig, typeof(DeploymentConfigBase), false);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(_deploymentConfig == null);
            if (GUILayout.Button("Deploy"))
            {
                var deployer = new Deployer(DeployerType.Local);
                deployer.Deploy(GetDeploymentParameters());
            }
            EditorGUI.EndDisabledGroup();
        }

        protected Dictionary<string, string> GetDeploymentParameters()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("deploymentConfig", _deploymentConfig.name);
            return parameters;
        }

        #endregion
    }
}