using Stratton.Loading.DataModels;
using UnityEditor;
using UnityEngine;

namespace Stratton.Loading.Editor
{
    [CustomPropertyDrawer(typeof(LoadingStepDM))]
    public class LoadingStepDMPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty segmentTypeProp = property.FindPropertyRelative("_stepType");
            var nameProp = segmentTypeProp.FindPropertyRelative("_name");
            SerializedProperty name = property.FindPropertyRelative("_name");
            name.stringValue = nameProp.stringValue;

            EditorGUI.PropertyField(position, property, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}