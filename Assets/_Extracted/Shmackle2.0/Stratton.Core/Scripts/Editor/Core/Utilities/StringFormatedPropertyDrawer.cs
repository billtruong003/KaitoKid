using System;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public abstract class StringFormatedPropertyDrawer : PropertyDrawer
    {
        protected abstract string SerializedPropertyName { get; }
        protected abstract string ErrorMessage { get; }
        protected abstract string CorrectMessage { get; }
        protected abstract SerializedPropertyType PropertyType { get; }
        protected abstract object StringToData(string str);
        protected abstract bool IsValid(string displayedString);
        protected abstract string DataToString(object objectValue);

        // These constants describe the height of the help box and the text field.
        const int helpHeight = 30;
        const int textHeight = 16;

        protected string DisplayedString;
        private bool isInitialized = false;

        // Here you must define the height of your property drawer. Called by Unity.
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            return base.GetPropertyHeight(prop, label) + helpHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var dataProperty = prop.FindPropertyRelative(SerializedPropertyName);
            if (dataProperty == null)
            {
                Log.Error(BaseLogChannel.Core, "There is no relative property with name " + SerializedPropertyName);
                return;
            }

            if (!isInitialized)
            {
                var serializedPropertyData = GetSerializedPropertyData(dataProperty);
                DisplayedString = DataToString(serializedPropertyData);
                isInitialized = true;
            }

            // Adjust height of the text field
            Rect textFieldPosition = position;
            textFieldPosition.height = textHeight;
            DrawTextField(textFieldPosition, dataProperty, label);

            // Adjust the help box position to appear indented underneath the text field.
            Rect helpPosition = EditorGUI.IndentedRect(position);
            helpPosition.y += textHeight;
            helpPosition.height = helpHeight;
            DrawHelpBox(helpPosition);
        }

        void DrawTextField(Rect position, SerializedProperty prop, GUIContent label)
        {
            // Draw the text field control GUI.

            DisplayedString = EditorGUI.TextField(position, label, DisplayedString);
            
            if (IsValid(DisplayedString))
            {
                var result = StringToData(DisplayedString);
                SetSerializedPropertyData(prop, result);
            }
        }

        void DrawHelpBox(Rect position)
        {
            if (IsValid(DisplayedString))
            {
                EditorGUI.HelpBox(position, CorrectMessage, MessageType.Info);
            }
            else
            {
                EditorGUI.HelpBox(position, ErrorMessage, MessageType.Error);
            }
        }


        #region Property helper functions

        private object GetSerializedPropertyData(SerializedProperty dataProperty)
        {
            switch (PropertyType)
            {
                case SerializedPropertyType.Integer:
                    return dataProperty.intValue;
                case SerializedPropertyType.Boolean:
                    return dataProperty.boolValue;
                case SerializedPropertyType.Float:
                    return dataProperty.floatValue;
                case SerializedPropertyType.String:
                    return dataProperty.stringValue;
                case SerializedPropertyType.Color:
                    return dataProperty.colorValue;
                case SerializedPropertyType.Enum:
                    return dataProperty.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return dataProperty.vector2Value;
                case SerializedPropertyType.Vector3:
                    return dataProperty.vector3Value;
                case SerializedPropertyType.Vector4:
                    return dataProperty.vector4Value;
                case SerializedPropertyType.Rect:
                    return dataProperty.rectValue;
                default:
                    throw new ArgumentOutOfRangeException("PropertyType should be set to one of values in switch insruction.");
            }
        }

        private void SetSerializedPropertyData(SerializedProperty dataProperty, object value)
        {
            switch (PropertyType)
            {
                case SerializedPropertyType.Integer:
                    dataProperty.intValue = (int)value;
                    break;
                case SerializedPropertyType.Boolean:
                    dataProperty.boolValue = (bool)value;
                    break;
                case SerializedPropertyType.Float:
                    dataProperty.floatValue = (float)value;
                    break;
                case SerializedPropertyType.String:
                    dataProperty.stringValue = (string)value;
                    break;
                case SerializedPropertyType.Color:
                    dataProperty.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.Enum:
                    dataProperty.enumValueIndex = (int)value;
                    break;
                case SerializedPropertyType.Vector2:
                    dataProperty.vector2Value = (Vector2)value;
                    break;
                case SerializedPropertyType.Vector3:
                    dataProperty.vector3Value = (Vector3)value;
                    break;
                case SerializedPropertyType.Vector4:
                    dataProperty.vector4Value = (Vector4)value;
                    break;
                case SerializedPropertyType.Rect:
                    dataProperty.rectValue = (Rect) value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("PropertyType should be set to one of values in switch insruction.");
            }
        }

        #endregion
    }
}
