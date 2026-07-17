using UnityEngine;
using UnityEditor;
namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(ShaderGlobalAttribute))]
    public class ShaderGlobalPropertyDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShaderGlobalAttribute shaderGlobalAttribute = attribute as ShaderGlobalAttribute;

            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(position, property);
            Rect buttonRect = position;
            buttonRect.width = buttonRect.height;
            buttonRect.x -= buttonRect.width - 1;
            if (GUI.Button(buttonRect,
                new GUIContent("#", "Set this global shader property = " + shaderGlobalAttribute.propertyName)))
            {
                if (property.propertyType == SerializedPropertyType.Float)
                {
                    Shader.SetGlobalFloat(shaderGlobalAttribute.propertyName, property.floatValue);
                }
                else if (property.propertyType == SerializedPropertyType.Vector2)
                {
                    Shader.SetGlobalVector(shaderGlobalAttribute.propertyName, property.vector2Value.ToVec3XY().ToVec4());
                }
                else if (property.propertyType == SerializedPropertyType.Vector3)
                {
                    Shader.SetGlobalVector(shaderGlobalAttribute.propertyName, property.vector3Value.ToVec4());
                }
                else if (property.propertyType == SerializedPropertyType.Vector4)
                {
                    Shader.SetGlobalVector(shaderGlobalAttribute.propertyName, property.vector4Value);
                }
                else if (property.propertyType == SerializedPropertyType.Color)
                {
                    Shader.SetGlobalColor(shaderGlobalAttribute.propertyName, property.colorValue);
                }
                else
                {
                    EditorGUI.LabelField(position, label.text, "Not supported type " + property.propertyType);
                }
            }

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = base.GetPropertyHeight(property, label);
            return baseHeight;
        }

        #endregion
    }
}