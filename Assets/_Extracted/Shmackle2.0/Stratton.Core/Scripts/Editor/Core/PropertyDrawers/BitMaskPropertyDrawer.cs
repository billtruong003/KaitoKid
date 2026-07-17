using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(BitMaskAttribute))]
    public class BitMaskPropertyDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeAttr = attribute as BitMaskAttribute;
            // Add the actual int value behind the field name
            label.text = label.text + "(" + property.intValue + ")";
            property.intValue = InspectorEditor.DrawBitMask(position,
                property.intValue,
                typeAttr.PropType,
                label,
                mask =>
                    (typeAttr.ZeroMaskIncluded || (mask != 0)) &&
                    (!typeAttr.ElementaryMaskOnly || (mask & (mask - 1)) == 0)
            );
        }

        #endregion
    }
}