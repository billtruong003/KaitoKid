using UnityEngine;
using UnityEditor;

namespace Stratton.Core.Editor
{
    public class StylesPreviewWindow : EditorWindow
    {
        #region Fields

        private Vector2 _scrollPos;

        #endregion

        #region Public Methods

        [MenuItem("Window/Styles/Styles Preview")]
        public static void ShowWindow()
        {
            GetWindow(typeof(StylesPreviewWindow));
        }

        #endregion

        #region Private Methods

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            ShowStyle(EditorStyles.boldLabel, "EditorStyles.boldLabel");
            ShowStyle(EditorStyles.centeredGreyMiniLabel, "EditorStyles.centeredGreyMiniLabel");
            ShowStyle(EditorStyles.colorField, "EditorStyles.colorField");
            ShowStyle(EditorStyles.foldout, "EditorStyles.foldout");
            ShowStyle(EditorStyles.foldoutPreDrop, "EditorStyles.foldoutPreDrop");
            ShowStyle(EditorStyles.helpBox, "EditorStyles.helpBox");
            ShowStyle(EditorStyles.inspectorDefaultMargins, "EditorStyles.inspectorDefaultMargins");
            ShowStyle(EditorStyles.inspectorFullWidthMargins, "EditorStyles.inspectorFullWidthMargins");
            ShowStyle(EditorStyles.label, "EditorStyles.label");
            ShowStyle(EditorStyles.largeLabel, "EditorStyles.largeLabel");
            ShowStyle(EditorStyles.layerMaskField, "EditorStyles.layerMaskField");
            ShowStyle(EditorStyles.miniBoldLabel, "EditorStyles.miniBoldLabel");
            ShowStyle(EditorStyles.miniButton, "EditorStyles.miniButton");
            ShowStyle(EditorStyles.miniButtonLeft, "EditorStyles.miniButtonLeft");
            ShowStyle(EditorStyles.miniButtonMid, "EditorStyles.miniButtonMid");
            ShowStyle(EditorStyles.miniButtonRight, "EditorStyles.miniButtonRight");
            ShowStyle(EditorStyles.miniLabel, "EditorStyles.miniLabel");
            ShowStyle(EditorStyles.miniTextField, "EditorStyles.miniTextField");
            ShowStyle(EditorStyles.numberField, "EditorStyles.numberField");
            ShowStyle(EditorStyles.objectField, "EditorStyles.objectField");
            ShowStyle(EditorStyles.objectFieldMiniThumb, "EditorStyles.objectFieldMiniThumb");
            ShowStyle(EditorStyles.objectFieldThumb, "EditorStyles.objectFieldThumb");
            ShowStyle(EditorStyles.popup, "EditorStyles.popup");
            ShowStyle(EditorStyles.radioButton, "EditorStyles.radioButton");
            ShowStyle(EditorStyles.textArea, "EditorStyles.textArea");
            ShowStyle(EditorStyles.textField, "EditorStyles.textField");
            ShowStyle(EditorStyles.toggle, "EditorStyles.toggle");
            ShowStyle(EditorStyles.toggleGroup, "EditorStyles.toggleGroup");
            ShowStyle(EditorStyles.toggleGroup, "EditorStyles.toggleGroup");
            ShowStyle(EditorStyles.toolbarButton, "EditorStyles.toolbarButton");
            ShowStyle(EditorStyles.toolbarDropDown, "EditorStyles.toolbarDropDown");
            ShowStyle(EditorStyles.toolbarPopup, "EditorStyles.toolbarPopup");
            ShowStyle(EditorStyles.toolbarTextField, "EditorStyles.toolbarTextField");
            ShowStyle(EditorStyles.whiteBoldLabel, "EditorStyles.whiteBoldLabel");
            ShowStyle(EditorStyles.whiteLabel, "EditorStyles.whiteLabel");
            ShowStyle(EditorStyles.whiteLargeLabel, "EditorStyles.whiteLargeLabel");
            ShowStyle(EditorStyles.whiteMiniLabel, "EditorStyles.whiteMiniLabel");
            ShowStyle(EditorStyles.wordWrappedLabel, "EditorStyles.wordWrappedLabel");
            ShowStyle(EditorStyles.wordWrappedMiniLabel, "EditorStyles.wordWrappedMiniLabel");
            ShowStyle(GUI.skin.box, "GUI.skin.box");
            ShowStyle(GUI.skin.button, "GUI.skin.button");
            ShowStyle(GUI.skin.horizontalScrollbar, "GUI.skin.horizontalScrollbar");
            ShowStyle(GUI.skin.horizontalScrollbarLeftButton, "GUI.skin.horizontalScrollbarLeftButton");
            ShowStyle(GUI.skin.horizontalScrollbarRightButton, "GUI.skin.horizontalScrollbarRightButton");
            ShowStyle(GUI.skin.horizontalScrollbarThumb, "GUI.skin.horizontalScrollbarThumb");
            ShowStyle(GUI.skin.horizontalSlider, "GUI.skin.horizontalSlider");
            ShowStyle(GUI.skin.horizontalSliderThumb, "GUI.skin.horizontalSliderThumb");
            ShowStyle(GUI.skin.label, "GUI.skin.label");
            ShowStyle(GUI.skin.scrollView, "GUI.skin.scrollView");
            ShowStyle(GUI.skin.textArea, "GUI.skin.textArea");
            ShowStyle(GUI.skin.textField, "GUI.skin.textField");
            ShowStyle(GUI.skin.toggle, "GUI.skin.toggle");
            ShowStyle(GUI.skin.window, "GUI.skin.window");
            EditorGUILayout.EndScrollView();
        }

        private void ShowStyle(GUIStyle style, string name)
        {
            EditorGUILayout.TextField(name, style);
        }

        #endregion
    }
}