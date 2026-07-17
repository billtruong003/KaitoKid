using UnityEngine;
using UnityEditor;

namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(TrippleRangeAttribute))]
    public class TrippletrippleRangeAttributePropertyDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            TrippleRangeAttribute trippleRangeAttribute = attribute as TrippleRangeAttribute;

            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (property.propertyType == SerializedPropertyType.Vector3)
            {
                float fieldHeight = position.height * 0.3f;
                var xRect = new Rect(position.x, position.y, position.width, fieldHeight);
                var yRect = new Rect(position.x, position.y + fieldHeight, position.width, fieldHeight);
                var zRect = new Rect(position.x, position.y + 2f * fieldHeight, position.width, fieldHeight);

                EditorGUI.LabelField(xRect.Offset(-10f), "x");
                EditorGUI.LabelField(yRect.Offset(-10f), "y");
                EditorGUI.LabelField(zRect.Offset(-10f), "z");

                float x = EditorGUI.Slider(xRect, property.vector3Value.x, trippleRangeAttribute.MinX, trippleRangeAttribute.MaxX);
                float y = EditorGUI.Slider(yRect, property.vector3Value.y, trippleRangeAttribute.MinY, trippleRangeAttribute.MaxY);
                float z = EditorGUI.Slider(zRect, property.vector3Value.z, trippleRangeAttribute.MinZ, trippleRangeAttribute.MaxZ);
                property.vector3Value = new Vector3(x, y, z);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Supported only Vector3");
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = base.GetPropertyHeight(property, label);
            if (property.propertyType == SerializedPropertyType.Vector3)
            {
                return baseHeight * 3.5f;
            }

            return baseHeight;
        }

        #endregion
    }
}