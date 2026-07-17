using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Stratton.Assets.Editor
{
    [CustomEditor(typeof(AssetsEditorSettings), true)]
    public class AssetsEditorSettingsEditor : UnityEditor.Editor
    {
        #region Fields

        protected AssetsEditorSettings _target;

        private SerializedProperty _addressablesBuildPathProp;
        private SerializedProperty _s3RegionProp;
        private SerializedProperty _s3BucketNameProp;
        private SerializedProperty _s3FolderPathProp;
        private SerializedProperty _s3AccessKeyIdProp;
        private SerializedProperty _s3SecretAccessKeyProp;

        #endregion

        #region Public Methods

        [MenuItem("Tools/Stratton.Assets/Assets Editor Settings")]
        public static void Show()
        {
            AssetsEditorSettingsInitializer.Initialize();
            Selection.activeObject = AssetsEditorSettings.Instance;
            EditorGUIUtility.PingObject(AssetsEditorSettings.Instance);
        }

        public override void OnInspectorGUI()
        {
            AssetsEditorSettingsInitializer.Initialize();

            _target = target as AssetsEditorSettings;

            EditorGUILayout.PropertyField(_addressablesBuildPathProp);

            if (AssetsSettings.Instance.CurrentAssetsHostType == BaseAssetsHostType.S3)
            {
                EditorGUILayout.PropertyField(_s3RegionProp);
                EditorGUILayout.PropertyField(_s3BucketNameProp);
                EditorGUILayout.PropertyField(_s3FolderPathProp);
                EditorGUILayout.PropertyField(_s3AccessKeyIdProp);
                EditorGUILayout.PropertyField(_s3SecretAccessKeyProp);
            }

            serializedObject.ApplyModifiedProperties();

            GUILayout.BeginVertical();
            if (GUILayout.Button("Full Build"))
            {
                _target.FullBuild();
            }
            if (GUILayout.Button("Update Build"))
            {
                _target.UpdateBuild();
            }
            if (GUILayout.Button("Upload"))
            {
                _target.Upload().Forget();
                return;
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region Private Methods

        private void OnEnable()
        {
            _addressablesBuildPathProp = serializedObject.FindProperty("_addressablesBuildPath");
            _s3RegionProp = serializedObject.FindProperty("_s3Region");
            _s3BucketNameProp = serializedObject.FindProperty("_s3BucketName");
            _s3FolderPathProp = serializedObject.FindProperty("_s3FolderPath");
            _s3AccessKeyIdProp = serializedObject.FindProperty("_s3AccessKeyId");
            _s3SecretAccessKeyProp = serializedObject.FindProperty("_s3SecretAccessKey");
        }

        #endregion
    }
}