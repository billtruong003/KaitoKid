using UnityEditor;
using UnityEngine;

namespace Stratton.CI.Editor
{
    [CustomEditor(typeof(DeploymentEditorSettings), true)]
    public class DeploymentEditorSettingsEditor : UnityEditor.Editor
    {
        #region Fields

        protected DeploymentEditorSettings _target;

        private SerializedProperty _projectDeploymentConfigsPathProp;

        #endregion

        #region Public Methods

        [MenuItem("Deployment/Deployment Editor Settings")]
        public static void Show()
        {
            DeploymentEditorSettingsInitializer.Initialize();
            Selection.activeObject = DeploymentEditorSettings.Instance;
            EditorGUIUtility.PingObject(DeploymentEditorSettings.Instance);
        }

        public override void OnInspectorGUI()
        {
            DeploymentEditorSettingsInitializer.Initialize();
            _target = target as DeploymentEditorSettings;

            EditorGUILayout.PropertyField(_projectDeploymentConfigsPathProp);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Deployment Window"))
            {
                DeploymentWindow.Show();
            }
        }

        #endregion

        #region Private Methods

        private void OnEnable()
        {
            DeploymentEditorSettingsInitializer.Initialize();

            _target = target as DeploymentEditorSettings;

            _projectDeploymentConfigsPathProp = serializedObject.FindProperty("_projectDeploymentConfigsPath");
        }

        #endregion
    }
}