using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Text;

namespace Stratton.Core.Editor
{
    [System.Serializable]
    public class ShaderGeneratorListProperty
    {
        #region Fields

        public const string InsertKey = "#INSERT#";

        public string Key;
        public string[] Values;
        public string[] ValueNames;
        public List<int> SelectedValues = new List<int>();
        public List<string> Insertions = new List<string>();
        public bool Show;
        public ShaderGeneratorListProperty Child;

        #endregion

        #region Constructors

        public ShaderGeneratorListProperty(string key, string[] values, string[] valueNames = null)
        {
            Key = key;
            Values = values;
            ValueNames = valueNames;
        }

        #endregion

        #region Public Methods

        public string ReplaceTextWithSelectedValue(string text)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < SelectedValues.Count; i++)
            {
                if (SelectedValues[i] >= Values.Length)
                {
                    continue;
                }
                sb.AppendLine(Values[SelectedValues[i]].Replace(InsertKey, Insertions[i]));
            }

            if (Child != null)
            {
                Child.SelectedValues = SelectedValues;
                Child.Insertions = Insertions;
                return Child.ReplaceTextWithSelectedValue(text.Replace(Key, sb.ToString()));
            }
            return text.Replace(Key, sb.ToString());
        }

        public void DrawGUI()
        {
            if (Show = InspectorEditor.FoldOut(Show, Key))
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                for (int i = 0; i < SelectedValues.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    Insertions[i] = InspectorEditor.TextField(Insertions[i]);
                    if (ValueNames != null)
                    {
                        SelectedValues[i] = InspectorEditor.Popup(SelectedValues[i], "", ValueNames);
                    }
                    else
                    {
                        SelectedValues[i] = InspectorEditor.Popup(SelectedValues[i], "", Values);
                    }
                    if (GUILayout.Button("R"))
                    {
                        SelectedValues.RemoveAt(i);
                        Insertions.RemoveAt(i--);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Add"))
                {
                    SelectedValues.Add(0);
                    Insertions.Add("text");
                }
                EditorGUILayout.EndVertical();
            }
        }

        #endregion
    }
}