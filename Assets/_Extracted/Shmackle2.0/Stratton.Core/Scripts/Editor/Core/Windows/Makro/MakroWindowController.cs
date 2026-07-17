using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Stratton.Core.Editor
{
    public class MakroWindowController : EditorWindow
    {
        #region Fields

        private MakroSettings _settings;
        private Vector2 _scrollPos;

        #endregion

        #region Public Methods

        [MenuItem("Tools/Stratton/Makro/Controller")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MakroWindowController)).minSize = new Vector2(100, 50);
        }

        public static void UseMakroData(MakroData d)
        {
            if (d.Target == null)
            {
                EditorUtility.DisplayDialog("Error", "Unable to do this - null target", "Ok");
                return;
            }

            if (d.TargetType == MakroData.MakroTargetType.Scene)
            {
                LoadSceneButton(d);
            }
            else
            {
                LoadScriptButton(d);
            }
        }

        #endregion

        #region Private Methods

        private static void LoadSceneButton(MakroData d)
        {
            EditorApplication.isPlaying = false;
            AssetDatabase.Refresh();
            if (d.Action.ToString().Contains("WithSaving"))
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(d.Target));

            if (d.Action.ToString().Contains("Play"))
            {
                EditorApplication.isPlaying = true;
            }
        }

        private static void LoadScriptButton(MakroData d)
        {
            var assetPath = AssetDatabase.GetAssetPath(d.Target);

            if (assetPath.Contains(".cs"))
            {
                Type type = Type.GetType(d.Target.name);
                type.GetMethod(d.MethodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(null, null);
            }
            else
            {
                var component = ((GameObject) d.Target).GetComponent(d.ComponentName);
                var method = component.GetType().GetMethod(d.MethodName);
                if (method != null)
                {
                    method.Invoke(d.Target, null);
                }

                var editor = UnityEditor.Editor.CreateEditor(component);
                editor.GetType().GetMethod(d.MethodName).Invoke(editor, null);
            }
        }

        void OnEnable()
        {
            MonoScript ms = MonoScript.FromScriptableObject(this);
            var scriptPath = AssetDatabase.GetAssetPath(ms);
            _settings = AssetDatabase.LoadAssetAtPath<MakroSettings>(MakroSettings.MakroSettingsPath);
            if (_settings == null)
            {
                _settings = (MakroSettings) CreateInstance(typeof(MakroSettings));
                _settings.Data = new List<MakroData>();
                AssetDatabase.CreateAsset(_settings, MakroSettings.MakroSettingsPath);
                _settings = AssetDatabase.LoadAssetAtPath<MakroSettings>(MakroSettings.MakroSettingsPath);
            }
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _settings.Data.Count; i++)
            {
                var d = _settings.Data[i];
                if (InspectorEditor.Button(d.ButtonName))
                {
                    UseMakroData(d);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}