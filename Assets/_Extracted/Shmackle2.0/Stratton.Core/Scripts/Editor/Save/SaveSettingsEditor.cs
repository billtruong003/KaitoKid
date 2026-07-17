using UnityEditor;
using UnityEngine;

namespace Stratton.Save.Editor
{
    [CustomEditor(typeof(SaveSettings), true)]
    public class SaveSettingsEditor : UnityEditor.Editor
    {
        #region Fields

        protected SaveSettings _target;

        private int _hostSelectedIndex = -1;

        #endregion

        #region Public Methods

        [MenuItem("Tools/Stratton/Save/Save Settings")]
        public static void Show()
        {
            SaveSettingsInitializer.Initialize();
            Selection.activeObject = SaveSettings.Instance;
            EditorGUIUtility.PingObject(SaveSettings.Instance);
        }

        public override void OnInspectorGUI()
        {
            if (_target.SaveHostTypes.Count == 0)
            {
                _target.Refresh();
            }
            if (_hostSelectedIndex == -1)
            {
                for (int i = 0; i < _target.SaveHostTypes.Count; i++)
                {
                    if (_target.SaveHostTypes[i].Equals(_target.CurrentSaveHostType))
                    {
                        _hostSelectedIndex = i;
                    }
                }
            }
            _hostSelectedIndex = EditorGUILayout.Popup("Current host: ", _hostSelectedIndex, _target.SaveHostTypes.ToArray());
            if (_hostSelectedIndex > -1)
            {
                _target.CurrentSaveHostType = _target.SaveHostTypes[_hostSelectedIndex];
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_saveIntervalInMiliseconds"), new GUIContent("Save Interval In Miliseconds"));
            if (_hostSelectedIndex == 4)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_databaseName"), new GUIContent("Database Name"));
            }
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
            SaveSettingsInitializer.Initialize();

            _target = target as SaveSettings;
        }

        #endregion
    }
}