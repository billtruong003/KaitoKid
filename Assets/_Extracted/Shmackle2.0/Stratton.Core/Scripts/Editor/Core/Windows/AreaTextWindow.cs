using UnityEngine;
using UnityEditor;
using System.Text;

namespace Stratton.Core.Editor
{
    public class AreaTextWindow : EditorWindow
    {
        #region Fields

        private string _text;
		private string _baseText;
        private Vector2 _scrollPos;
		private string[] _splittedText;
		private bool _searchOption;
		private string _searchText;
		private bool _refresh;
		
        public string[] SplittedText
        {
            get
            {
                if (_splittedText == null || _splittedText.Length == 0 || _refresh)
                {
                    if (_text == null)
                    {
                        return new string[] { };
                    }
                    int splitsCount = Mathf.RoundToInt(_text.Length / 10000f) + 1;
                    _splittedText = new string[splitsCount];
                    int step = 10000;
                    for (int i = 0; i < splitsCount; i++)
                    {
                        _splittedText[i] = _text.TrySubstring(i * step, step);
                    }
					_refresh = false;
                }

                return _splittedText;
            }
        }

        #endregion

        #region Public Methods

        public static void ShowWindow(string text, string title, bool searchOption = true)
        {
            AreaTextWindow window = GetWindow(typeof(AreaTextWindow)) as AreaTextWindow;
            window.titleContent = new GUIContent(title);
            window._baseText = window._text = text;
			window._searchOption = searchOption;
        }

        #endregion

        #region Private Methods

        void OnGUI()
        {
			if(_searchOption)
			{
				string newSearchText = EditorGUILayout.TextField("Search: ", _searchText); 
				if(_searchText != newSearchText)
				{
					UpdateTextWithSearchPhrase(_searchText);
					_searchText = newSearchText;
				}
			}
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            var labelStyle = EditorStyles.helpBox;
            labelStyle.wordWrap = true;
            for (int i = 0; i < SplittedText.Length; i++)
            {
                EditorGUILayout.TextArea(SplittedText[i], labelStyle);
            }
            EditorGUILayout.EndScrollView();
        }

		private void UpdateTextWithSearchPhrase(string searchText)
		{
			_refresh = true;
			if(searchText.IsNullOrEmpty())
			{
				_text = _baseText;
				return;
			}
			string[] lines = _baseText.Split('\n');
			StringBuilder sb = new StringBuilder();
			for(int i = 0; i < lines.Length; i++)
			{
				if(lines[i].Contains(searchText))
				{
					sb.AppendLine();
					sb.AppendLine(lines[i]);
				}
			}
			_text = sb.ToString();
		}

        #endregion
    }
}