using UnityEngine;
using UnityEditor;

namespace Stratton.Core.Editor
{
    public class DebugLogTracerDetailsWindow : EditorWindow
    {
        #region Fields

        public DebugLogTracerData Trace;
        private Vector2 _scrollPos;

        #endregion

        #region Public Methods

        public static DebugLogTracerDetailsWindow ShowWindow()
        {
            var window = (DebugLogTracerDetailsWindow) GetWindow(typeof(DebugLogTracerDetailsWindow));
            window.name = "Log Trace";
            window.titleContent = new GUIContent(window.name);
            return window;
        }

        public void OpenFileOnTraceLine(int i)
        {
            string assetPath = Trace.Files[i].Replace(Application.dataPath, "Assets");
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
            AssetDatabase.OpenAsset(asset, Trace.Lines[i]);
        }

        #endregion

        #region Private Methods

        void OnGUI()
        {
            if (Trace == null)
            {
                return;
            }
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            var labelStyle = EditorStyles.helpBox;
            labelStyle.wordWrap = true;
            Event e = Event.current;
            if (e.control && e.keyCode == KeyCode.C)
            {
                TextEditor te = GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) as TextEditor;
                if (te != null && te.hasSelection && te.style.name == labelStyle.name)
                {
                    te.Copy();
                }
            }
            GUILayout.TextField(Trace.Message, labelStyle);
            var buttonStyle = EditorStyles.miniButtonLeft;
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < Trace.MethodPaths.Count; i++)
            {
                if (GUILayout.Button(Trace.MethodPaths[i] + " : " + Trace.Lines[i], buttonStyle,
                    GUILayout.MaxWidth(position.width)))
                {
                    OpenFileOnTraceLine(i);
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndScrollView();
        }

        #endregion
    }
}