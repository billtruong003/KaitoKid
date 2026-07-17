using UnityEditor;
using UnityEngine;

namespace Stratton.CI.Editor
{
    public class DeploymentEditorSettings : ScriptableObject
    {
        public const string SettingsFileName = "DeploymentEditorSettings";
        public const string SettingsDirectory = "Settings/Editor";
        public const string SettingsFullPath = "Assets/" + SettingsDirectory + "/" + SettingsFileName + ".asset";

        private static DeploymentEditorSettings _instance;

        #region Serialized Fields

        [SerializeField] private string _projectDeploymentConfigsPath = "Assets/Editor/DeploymentConfigs/";

        #endregion

        #region Properties

        public static DeploymentEditorSettings Instance
        {
            get
            {
                return _instance != null ? _instance : (_instance = Create());
            }
        }

        public string ProjectDeploymentConfigsPath => _projectDeploymentConfigsPath;

        #endregion

        #region Private Methods

        private static DeploymentEditorSettings Create()
        {
            return AssetDatabase.LoadAssetAtPath<DeploymentEditorSettings>(SettingsFullPath);
        }

        private void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        #endregion
    }
}