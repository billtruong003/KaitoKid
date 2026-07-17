using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    [CustomEditor(typeof(LogSettings), true)]
    public class LogSettingsEditor : UnityEditor.Editor
    {
        #region Fields

        protected LogSettings _target;

        #endregion

        #region Public Methods

        [MenuItem("Tools/Stratton/Logging/Log Settings")]
        public static void Show()
        {
            LogSettingsInitializer.Initialize();
            Selection.activeObject = LogSettings.Instance;
            EditorGUIUtility.PingObject(LogSettings.Instance);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LogSettingsInitializer.Initialize();

            _target = target as LogSettings;

            if (GUILayout.Button("Refresh"))
            {
                _target.Refresh();
            }
            if (GUILayout.Button("Save"))
            {
                _target.Save();
            }
        }

        #endregion
    }
}