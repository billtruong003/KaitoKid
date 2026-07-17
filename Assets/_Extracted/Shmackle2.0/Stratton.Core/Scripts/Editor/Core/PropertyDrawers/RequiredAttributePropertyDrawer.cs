using System;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class RequiredAttributePropertyDrawer : PropertyDrawer
	{
		private readonly Color _errorColor = new Color(1f, .2f, .2f, .1f);
		
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (IsFieldEmpty(property))
			{
				float height = EditorGUIUtility.singleLineHeight * 2f;
				height += base.GetPropertyHeight(property, label);

				return height;
			}
			else
			{
				return base.GetPropertyHeight(property, label);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!IsFieldSupported(property))
			{
				Debug.LogError($"Required Attribute placed on incompatible field type.");
			}
			
			if (IsFieldEmpty(property))
			{
				position.height = EditorGUIUtility.singleLineHeight * 2f;
				position.height += base.GetPropertyHeight(property, label);
				
				EditorGUI.HelpBox(position, "This field cannot be empty.", MessageType.Error);
				EditorGUI.DrawRect(position, _errorColor);

				position.height = base.GetPropertyHeight(property, label);
				position.y += EditorGUIUtility.singleLineHeight * 2f;
			}

			EditorGUI.PropertyField(position, property, label);
		}

		private bool IsFieldEmpty(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
			{
				return true;
			}
			
			if (property.propertyType == SerializedPropertyType.String && string.IsNullOrEmpty(property.stringValue))
			{
				return true;
			}

			return false;
		}

		private bool IsFieldSupported(SerializedProperty property)
		{
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				return true;
			}
			
			if (property.propertyType == SerializedPropertyType.String)
			{
				return true;
			}

			return false;
		}
	}
}
