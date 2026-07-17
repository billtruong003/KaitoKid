using UnityEditor;

namespace Stratton.Core.Editor
{
    public class CalcMD5SumWindow : EditorWindow
    {
        #region Fields

        private string _str;

        #endregion

        #region Public Methods

        [MenuItem("Window/Other/MD5Sum")]
        public static void ShowWindow()
        {
            GetWindow(typeof(CalcMD5SumWindow));
        }

        #endregion

        #region Private Methods

        void OnGUI()
        {
            _str = EditorGUILayout.TextField("Value:", _str);
            if (!string.IsNullOrEmpty(_str))
            {
                EditorGUILayout.TextField("MD5:", MD5Encoder.Md5Sum(_str));
            }
        }

        #endregion
    }
}