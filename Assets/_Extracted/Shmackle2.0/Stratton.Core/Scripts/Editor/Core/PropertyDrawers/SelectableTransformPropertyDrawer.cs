using UnityEngine;
using UnityEditor;
namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(SelectableTransformAttribute))]
    public class SelectableTransformPropertyDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float popupWidth = 20f;
            var rect = new Rect(position.x, position.y, position.width - popupWidth, position.height);
            var rect2 = new Rect(rect.x + rect.width, position.y, popupWidth, position.height);

            EditorGUI.PropertyField(rect, property, GUIContent.none);

            Transform[] children = GetPossibleTransforms(property);
            if (children != null)
            {
                string[] names = new string[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    names[i] = string.Format("{0}", children[i].GetStringPath());
                }
                int id = EditorGUI.Popup(rect2, -1, names);
                if (id != -1)
                {
                    property.objectReferenceValue = children[id];
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

        #region Private Methods

        private static Transform[] GetPossibleTransforms(SerializedProperty property)
        {
            Transform[] children = null;
            var parentObject = property.serializedObject.targetObject;
            var parentGameObject = parentObject as GameObject;
            if (parentGameObject)
            {
                children = parentGameObject.GetComponentsInChildren<Transform>();
            }
            else
            {
                var parentTransform = parentObject as Transform;
                if (parentTransform)
                {
                    children = parentTransform.GetComponentsInChildren<Transform>();
                }
                else
                {
                    var parentBehaviour = parentObject as MonoBehaviour;
                    if (parentBehaviour)
                    {
                        children = parentBehaviour.GetComponentsInChildren<Transform>();
                    }
                }
            }
            return children;
        }

        #endregion
    }
}