using System;

namespace Teabag.Core
{
    using UnityEngine;
    [Serializable]
    public class PerkSettings : IPerkSettings
    {
        [SerializeField] private BasePerkDataObject[] _perkDataBase;
        public BasePerkDataObject[] PerkDataBase => _perkDataBase;

#if UNITY_EDITOR
        private const string PerkSettingsHeader = "Perk Database";
        private const string PerkSettingsDescription = "Provides configuration settings for items within the application.";

        private const string PerkInstanceLabel = "Perk Data";
        private const string PerkInstanceDescription = "Specifies Perk scriptable objects";

        private static readonly GUIContent perkSettingsHeaderGUIContent = new(PerkSettingsHeader);
        private static readonly GUIContent perkSettingsDescriptionGUIContent = new(PerkSettingsDescription);

        private static readonly GUIContent perkInstanceGUIContent = new(PerkInstanceLabel, PerkInstanceDescription);


        public void Editor_OnGUI(Object target)
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                UnityEditor.EditorGUILayout.LabelField(perkSettingsHeaderGUIContent, UnityEditor.EditorStyles.boldLabel);
                UnityEditor.EditorGUILayout.LabelField(perkSettingsDescriptionGUIContent, UnityEditor.EditorStyles.wordWrappedLabel);
                UnityEditor.EditorGUILayout.Space();
            }

            var serializedObject = new UnityEditor.SerializedObject(target);
            serializedObject.UpdateIfRequiredOrScript();

            var perkManagerInstanceSerializedProperty = serializedObject.FindProperty(nameof(PerkSettingsAsset._settings)).FindPropertyRelative(nameof(_perkDataBase));
            UnityEditor.EditorGUILayout.PropertyField(perkManagerInstanceSerializedProperty, perkInstanceGUIContent);

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
