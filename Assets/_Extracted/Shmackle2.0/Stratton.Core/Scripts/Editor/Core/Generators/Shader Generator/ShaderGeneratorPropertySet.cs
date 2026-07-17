using System.Collections.Generic;
using UnityEditor;

namespace Stratton.Core.Editor
{
    [System.Serializable]
    public class ShaderGeneratorPropertySet
    {
        #region Fields

        public bool Show;
        public string Name;

        public List<ShaderGeneratorReplacementProperty> ReplacementProps =
            new List<ShaderGeneratorReplacementProperty>();

        public List<ShaderGeneratorListProperty> ListProps = new List<ShaderGeneratorListProperty>();

        #endregion

        #region Constructors

        public ShaderGeneratorPropertySet(string name)
        {
            Name = name;
        }

        #endregion

        #region Public Methods

        public string ReplaceTextWithSelectedValues(string text)
        {
            foreach (var p in ReplacementProps)
            {
                text = p.ReplaceTextWithSelectedValue(text);
            }
            foreach (var p in ListProps)
            {
                text = p.ReplaceTextWithSelectedValue(text);
            }
            return text;
        }

        public void DrawGUI()
        {
            if (Show = InspectorEditor.FoldOut(Show, Name))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var p in ReplacementProps)
                {
                    p.DrawGUI();
                }
                foreach (var p in ListProps)
                {
                    p.DrawGUI();
                }
                EditorGUILayout.EndVertical();
            }
        }

        #endregion
    }
}