using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Stratton.Core;

namespace Stratton.Core.Editor
{
    public class ScriptGenerator : EditorWindow
    {
        public class RegionData
        {
            public string Name { get; private set; }
            public bool Enabled { get; set; }

            public RegionData(string name, bool enabled)
            {
                Name = name;
                Enabled = enabled;
            }
        }

        #region Enums
        public enum ScriptGenerateType
        {
            MonoBehaviour
        }
        #endregion

        #region Fields
        public const string InspectorTemplatePath = "/Stratton/Core/Editor/Generators/Templates/InspectorTemplate.txt";
        public const string MonoBehaviourTemplatePath = "/Stratton/Core/Editor/Generators/Templates/MonoBehaviourTemplate.txt";
        
        private readonly List<RegionData> _regions = new List<RegionData>()
        {
            new RegionData("Delegates", false), new RegionData("Enums", false),
            new RegionData("Events", false), new RegionData("Fields", true),
            new RegionData("Properties", true), new RegionData("Constructors", false),
            new RegionData("UnityMethods", true), new RegionData("PublicMethods", true),
            new RegionData("PrivateMethods", true)
        };
        private string _templatePath;
        private string _dirPath;
        private Vector2 _scrollPos;
        private string _scriptName;
        #endregion

        #region Public Methods

        [MenuItem("Assets/Create/Inspector Editor", true, 100)]
        public static bool GenerateInspectorValidate()
        {
            var script = Selection.activeObject as MonoScript;
            if (script == null)
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Create/Inspector Editor")]
        public static void GenerateInspector()
        {
            MonoScript script = Selection.activeObject as MonoScript;
            if (script == null)
            {
                return;
            }

            string templatePath = Application.dataPath + InspectorTemplatePath;
            string text = File.ReadAllText(templatePath);
            text = text.Replace("COMPONENT_NAME", script.name);
            text = text.Replace("COMPONENT_LOWER", Char.ToLower(script.name[0]) + script.name.Substring(1));

            var scriptPath = AssetDatabase.GetAssetPath(script).Replace("Assets/", Application.dataPath + "/");
            string scriptDir = scriptPath.Substring(0, scriptPath.LastIndexOf('/')) + "/Editor/";
            Directory.CreateDirectory(scriptDir);
            string finalScriptPath = scriptDir + script.name + "Editor.cs";
            File.WriteAllText(finalScriptPath, text);

            AssetDatabase.Refresh();
            Log.Message(BaseLogChannel.Core, "Generated " + finalScriptPath);
        }


        [MenuItem("Assets/Create/C# MonoBehaviour", false, 0)]
        public static void GenerateMonoBehaviour()
        {
            string dir = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (dir.IsNullOrEmpty())
                return;
            if (dir.Contains("."))
                dir = dir.Substring(0, dir.LastIndexOf('/')) + "/";
            else
                dir += "/";

            string templatePath = Application.dataPath + MonoBehaviourTemplatePath;

            ShowGenerateScriptWindow("C# MonoBehaviour", templatePath, dir, ScriptGenerateType.MonoBehaviour);
        }

        public static void ShowGenerateScriptWindow(string windowName, string templatePath, string dir, ScriptGenerateType scriptType)
        {
            ScriptGenerator generator = (ScriptGenerator)GetWindow(typeof(ScriptGenerator));
            generator.titleContent = new GUIContent(windowName);
            generator.Initialize(templatePath, dir);
        }
        #endregion

        #region Unity Methods
        protected void OnGUI()
        {
            _scriptName = EditorGUILayout.TextField("Script name", _scriptName);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            //TODO when ScriptGenerateType will be extended for other script types add here some switch by type
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Regions:");
            EditorGUI.indentLevel++;
            foreach (var region in _regions)
            {
                region.Enabled = EditorGUILayout.Toggle(region.Name, region.Enabled);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Generate"))
            {
                GenerateScript();
            }
            EditorGUILayout.EndScrollView();

        }
        #endregion

        #region Private Methods

        private void Initialize(string templatePath, string dirPath)
        {
            _templatePath = templatePath;
            _dirPath = dirPath;
        }

        private void GenerateScript()
        {
            string text = File.ReadAllText(_templatePath);
            text = text.Replace("COMPONENT_NAME", _scriptName);
            text = text.Replace("COMPONENT_LOWER", Char.ToLower(_scriptName[0]) + _scriptName.Substring(1));

            StringBuilder regionsSb = new StringBuilder();
            foreach (var region in _regions)
            {
                if (region.Enabled)
                {
                    regionsSb.AppendLine("    #region {0}\n    #endregion\n".SetArgs(region.Name));
                }
            }
            text = text.Replace("REGIONS", regionsSb.ToString());
            
            string finalScriptPath = _dirPath + _scriptName + ".cs";
            File.WriteAllText(finalScriptPath, text);

            AssetDatabase.Refresh();
            Log.Message(BaseLogChannel.Core, "Generated " + finalScriptPath);
            Close();
        }

        #endregion
    }
}