using UnityEditor;
using UnityEngine;

namespace Stratton.Assets.Editor
{
    [CustomEditor(typeof(AssetsSettings), true)]
    public class AssetsSettingsEditor : UnityEditor.Editor
    {
        #region Fields

        protected AssetsSettings _target;

        private SerializedProperty _addressablesSceneListProp;

        private int _hostSelectedIndex = -1;
        private int _hostPreviouslySelectedIndex = -1;

        #endregion

        #region Public Methods

        [MenuItem("Tools/Stratton.Assets/Assets Settings")]
        public static void Show()
        {
            AssetsSettingsInitializer.Initialize();
            Selection.activeObject = AssetsSettings.Instance;
            EditorGUIUtility.PingObject(AssetsSettings.Instance);
        }

        public override void OnInspectorGUI()
        {
            AssetsSettingsInitializer.Initialize();

            _target = target as AssetsSettings;

            if (_target.AssetsHostTypes.Count == 0)
            {
                _target.Refresh();
            }

            if (_hostSelectedIndex == -1)
            {
                for (int i = 0; i < _target.AssetsHostTypes.Count; i++)
                {
                    if (_target.AssetsHostTypes[i].Equals(_target.CurrentAssetsHostType))
                    {
                        _hostSelectedIndex = i;
                    }
                }
            }

            _hostPreviouslySelectedIndex = _hostSelectedIndex;
            _hostSelectedIndex = EditorGUILayout.Popup("Current host: ", _hostSelectedIndex, _target.AssetsHostTypes.ToArray());
            if (_hostSelectedIndex > -1)
            {
                _target.CurrentAssetsHostType = _target.AssetsHostTypes[_hostSelectedIndex];
            }

            EditorGUILayout.PropertyField(_addressablesSceneListProp);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Refresh"))
            {
                _target.Refresh();
            }
        }

        #endregion

        #region Private Methods

        private void OnEnable()
        {
            _addressablesSceneListProp = serializedObject.FindProperty("_addressablesSceneList");
        }

        #endregion
    }
}