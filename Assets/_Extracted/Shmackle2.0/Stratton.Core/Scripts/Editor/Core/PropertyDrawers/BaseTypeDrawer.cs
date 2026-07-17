using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public abstract class BaseTypeDrawer<T, T2> : PropertyDrawer where T : IBaseType where T2 : IBaseTypeList
    {
        int _choiceIndex;
        string[] _choices;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            SerializedProperty nameProperty = property.FindPropertyRelative("_name");

            EditorGUI.BeginChangeCheck();
            if (_choices == null)
            {
                _choices = GetTypeNames();
            }
            int selectedIndex = 0;
            if (nameProperty.stringValue.IsNullOrEmpty())
            {
                for (int i = 0; i < _choices.Length; i++)
                {
                    if (_choices[i] == "None")
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _choices.Length; i++)
                {
                    if (_choices[i] == nameProperty.stringValue)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            _choiceIndex = EditorGUI.Popup(position, selectedIndex, _choices);
            if (EditorGUI.EndChangeCheck() || nameProperty.stringValue.IsNullOrEmpty())
            {
                nameProperty.stringValue = _choices[_choiceIndex];
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private string[] GetTypeNames()
        {
            List<string> typeNames = new List<string>();
            List<Type> types = new List<Type>();
            List<FieldInfo> fields = new List<FieldInfo>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(asm.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces().Contains(typeof(T2))));
            }
            foreach (var type in types)
            {
                fields.AddRange(type.GetFields().Where(f =>
                    f.FieldType == typeof(T) ||
                    f.FieldType.IsSubclassOf(typeof(T))));
            }
            foreach (var field in fields)
            {
                var type = (T)field.GetValue(null);
                typeNames.Add(type.Name);
            }
            return typeNames.ToArray();
        }
    }
}