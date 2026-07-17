using UnityEngine;
using UnityEditor;
namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(DoubleRangeAttribute))]
    public class DoubleRangePropertyDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DoubleRangeAttribute doubleRangeAttribute = attribute as DoubleRangeAttribute;

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

                float x = EditorGUI.Slider(xRect, property.vector3Value.x, doubleRangeAttribute.MinX, doubleRangeAttribute.MaxX);
                float y = EditorGUI.Slider(yRect, property.vector3Value.y, doubleRangeAttribute.MinY, doubleRangeAttribute.MaxY);
                float z = EditorGUI.FloatField(zRect, property.vector3Value.z);
                property.vector3Value = new Vector3(x, y, z);
            }
            else if (property.propertyType == SerializedPropertyType.Vector2)
            {
                float fieldHeight = position.height * 0.46f;
                var xRect = new Rect(position.x, position.y, position.width, fieldHeight);
                var yRect = new Rect(position.x, position.y + fieldHeight, position.width, fieldHeight);

                if (doubleRangeAttribute.ForceNames == null || doubleRangeAttribute.ForceNames.Length == 0)
                {
                    EditorGUI.LabelField(xRect.Offset(-10f), "x");
                    EditorGUI.LabelField(yRect.Offset(-10f), "y");
                }
                else
                {
                    EditorGUI.LabelField(xRect.Offset(-10f - 4f * doubleRangeAttribute.ForceNames[0].Length), doubleRangeAttribute.ForceNames[0]);
                    EditorGUI.LabelField(yRect.Offset(-10f - 4f * doubleRangeAttribute.ForceNames[1].Length), doubleRangeAttribute.ForceNames[1]);
                }

                float x = EditorGUI.Slider(xRect, property.vector2Value.x, doubleRangeAttribute.MinX, doubleRangeAttribute.MaxX);
                float y = EditorGUI.Slider(yRect, property.vector2Value.y, doubleRangeAttribute.MinY, doubleRangeAttribute.MaxY);
                property.vector2Value = new Vector2(x, y);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Supported only Vector2 or Vector3");
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

            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                return baseHeight * 2.5f;
            }

            return baseHeight;
        }

        #endregion
    }
}