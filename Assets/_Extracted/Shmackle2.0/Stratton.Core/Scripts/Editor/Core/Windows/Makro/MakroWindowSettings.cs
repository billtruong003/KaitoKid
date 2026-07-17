using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public class MakroWindowSettings : EditorWindow
    {
        #region Fields

        private MakroSettings _settings;
        private Vector2 _searchScrollPos;

        #endregion

        #region Public Methods

        [MenuItem("Tools/Stratton/Makro/Settings")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MakroWindowSettings));
        }

        #endregion

        #region Private Methods

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
            _searchScrollPos = EditorGUILayout.BeginScrollView(_searchScrollPos, GUILayout.Height(position.height - 50));
            foreach (var d in _settings.Data)
            {
                Edit(d);
            }
            EditorGUILayout.EndScrollView();

            if (InspectorEditor.Button("New"))
            {
                New();
            }

            if (InspectorEditor.Button("Save"))
            {
                Save();
            }
        }

        private void Edit(MakroData data)
        {
            if (
                data.Show =
                InspectorEditor.FoldOut(data.Show,
                    data.ButtonName + ": " + (data.Target == null ? "EMPTY" : data.Target + "")))
            {
                EditorGUILayout.BeginVertical();
                data.ButtonName = InspectorEditor.TextField(data.ButtonName, "  Button name");
                data.Target = InspectorEditor.ObjectView(data.Target, typeof(UnityEngine.Object), "  Target:");
                if (data.Target != null)
                {
                    var assetPath = AssetDatabase.GetAssetPath(data.Target);
                    if (assetPath.Contains(".unity"))
                    {
                        data.TargetType = MakroData.MakroTargetType.Scene;
                        data.Action = (MakroData.MakroActionType) InspectorEditor.EnumField(data.Action, "  Action");
                    }
                    else if (assetPath.Contains(".prefab"))
                    {
                        data.TargetType = MakroData.MakroTargetType.Script;
                        data.MethodName = InspectorEditor.TextField(data.MethodName, "  Method name");
                        data.ComponentName = InspectorEditor.TextField(data.ComponentName, "  Component name");
                    }
                    else if (assetPath.Contains(".cs"))
                    {
                        data.TargetType = MakroData.MakroTargetType.Script;
                        data.MethodName = InspectorEditor.TextField(data.MethodName, "  Method name");
                    }
                    else
                    {
                        InspectorEditor.Label("Unsupported type");
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void New()
        {
            _settings.Data.Add(new MakroData());
        }

        private void Save()
        {
            for (int i = 0; i < _settings.Data.Count; i++)
            {
                if (_settings.Data[i].ButtonName.Length == 0)
                {
                    _settings.Data.RemoveAt(i--);
                }
            }
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }

        #endregion
    }
}