using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Stratton.Core.Editor
{
    public class ShaderGenerator : EditorWindow
    {
        public enum ShaderGenerationMode
        {
            VertFrag,
            Surface,
            SurfaceVert
        }

        #region Fields

        public static ShaderGeneratorPropertySet GlobalSet;
        public static ShaderGeneratorPropertySet VertFragSet;
        public static ShaderGeneratorPropertySet SurfaceSet;
        public ShaderGenerationMode Mode;

        private const string _shaderVertFragTemplate = "/Stratton/Core/Editor/Generators/Templates/ShaderVertFragTemplate.txt";
        private const string _shaderSurfaceTemplate = "/Stratton/Core/Editor/Generators/Templates/ShaderSurfaceTemplate.txt";
        private const string _shaderSurfaceVertTemplate = "/Stratton/Core/Editor/Generators/Templates/ShaderSurfaceVertTemplate.txt";

        private string _targetDirectory;
        private string _shaderGUIPath = "Stratton/CustomShader";
        private string _shaderName = "CustomShader";
        private Vector2 _scrollPos;

        #endregion

        #region Public Methods

        [MenuItem("Assets/Create/Shader from window", true, 100)]
        public static bool GenerateInspectorValidate()
        {
            if (AssetDatabase.GetAssetPath(Selection.activeObject).IsNullOrEmpty())
            {
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Create/Shader from window")]
        public static void GenerateInspector()
        {
            if (Selection.activeObject == null)
            {
                return;
            }

            ShowWindow(Selection.activeObject);
        }

        public void Initialize(UnityEngine.Object targetAsset)
        {
            _targetDirectory = AssetDatabase.GetAssetPath(targetAsset);
            if (_targetDirectory.LastIndexOf('/') < _targetDirectory.LastIndexOf('.'))
            {
                _targetDirectory = _targetDirectory.Substring(0, _targetDirectory.LastIndexOf('/'));
            }
            else
            {
                _targetDirectory += '/';
            }

            GlobalSet = new ShaderGeneratorPropertySet("Global settings");
            InitGlobalProps(GlobalSet.ReplacementProps, GlobalSet.ListProps);
        }

        #endregion

        #region Private Methods

        private static void ShowWindow(UnityEngine.Object targetAsset)
        {
            ShaderGenerator generator = (ShaderGenerator) GetWindow(typeof(ShaderGenerator));
            generator.Initialize(targetAsset);
        }

        private void InitGlobalProps(List<ShaderGeneratorReplacementProperty> replProps,
            List<ShaderGeneratorListProperty> listProps)
        {
            replProps.Add(new ShaderGeneratorReplacementProperty("QUEUE",
                new[] { "\"Queue\"=\"Geometry\"", "\"Queue\"=\"Transparent\"", "\"Queue\"=\"Overlay\"" },
                new[] { "Geometry", "Transparent", "Overlay" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("RENDERTYPE",
                new[] { "\"RenderType\"=\"Opaque\"", "\"RenderType\"=\"Transparent\"" },
                new[] { "Geometry", "Transparent" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("BLEND",
                new[] { "", "Blend SrcAlpha One", "Blend SrcAlpha OneMinusSrcAlpha" },
                new[] { "None", "Addtive", "AlphaBlend" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("CULL",
                new[] { "", "Cull Off", "Cull Front", "Cull Back" }, new[] { "Default", "Off", "Front", "Back" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("ZWRITE", new[] { "", "ZWrite Off", "ZWrite On" },
                new[] { "Default", "Off", "On" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("ZTEST", new[] { "", "ZTest Off", "ZTest Always" },
                new[] { "Default", "Off", "Always" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("LIGHTING", new[] { "Lighting Off", "", "Lighting On" },
                new[] { "Off", "Default", "On" }));
            replProps.Add(new ShaderGeneratorReplacementProperty("FOG", new[] { "Fog{Mode Off}", "Fog{Color(0,0,0,0)}" },
                new[] { "Mode Off", "Clear" }));

            var shaderProperties = new ShaderGeneratorListProperty("PROPERTIESLIST", new[]
            {
                "#INSERT#(\"#INSERT#\", 2D) = \"white\" {}", "#INSERT#(\"#INSERT#\", Range(0,1)) = 1",
                "#INSERT#(\"#INSERT#\", Float) = 1", "#INSERT#(\"#INSERT#\", Vector) = (0,0,0,0)",
                "#INSERT#(\"#INSERT#\", Color) = (1,1,1,1)"
            },
                new[] { "Texture2D", "Range", "Float", "Vector4", "Color" });
            shaderProperties.Child = new ShaderGeneratorListProperty("PROPFIELDLIST",
                new[]
                {
                    "sampler2D #INSERT#;\nfloat4 #INSERT#_ST;", "fixed #INSERT#;", "fixed #INSERT#;", "fixed4 #INSERT#;",
                    "fixed4 #INSERT#;"
                });
            shaderProperties.Child.Child = new ShaderGeneratorListProperty("PROPFIELDINIT",
                new[] { "o.uv = TRANSFORM_TEX(v.texcoord, #INSERT#);" });
            shaderProperties.Child.Child.Child = new ShaderGeneratorListProperty("PROPFIELDLOAD",
                new[] { "fixed4 #INSERT#Val = tex2D(#INSERT#, i.uv);" });
            listProps.Add(shaderProperties);
        }

        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            _shaderName = EditorGUILayout.TextField("Shader name", _shaderName);
            _shaderGUIPath = EditorGUILayout.TextField("Shader gui path", _shaderGUIPath);
            Mode = (ShaderGenerationMode) EditorGUILayout.EnumPopup("Shader type", Mode);
            if (GlobalSet == null)
            {
                Close();
            }
            GlobalSet.DrawGUI();
            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            if (Mode == ShaderGenerationMode.VertFrag)
            {
                GenerateVertFrag();
            }
            else if (Mode == ShaderGenerationMode.Surface)
            {
                GenerateSurface();
            }
            else
            {
                GenerateSurfaceVert();
            }
        }

        private void GenerateVertFrag()
        {
            string text = File.ReadAllText(Application.dataPath + "/" + _shaderVertFragTemplate);
            text = ReplaceShaderName(text);
            text = GlobalSet.ReplaceTextWithSelectedValues(text);
            SaveShader(text);
        }

        private void GenerateSurface()
        {
            string text = File.ReadAllText(Application.dataPath + "/" + _shaderSurfaceTemplate);
            text = ReplaceShaderName(text);
            text = GlobalSet.ReplaceTextWithSelectedValues(text);
            SaveShader(text);
        }

        private void GenerateSurfaceVert()
        {
            string text = File.ReadAllText(Application.dataPath + "/" + _shaderSurfaceVertTemplate);
            text = ReplaceShaderName(text);
            text = GlobalSet.ReplaceTextWithSelectedValues(text);
            SaveShader(text);
        }

        private void SaveShader(string text)
        {
            string finalScriptPath = _targetDirectory.Replace("Assets/", Application.dataPath + "/") + _shaderName +
                                     ".shader";
            File.WriteAllText(finalScriptPath, text);
            AssetDatabase.Refresh();
            Log.Message(BaseLogChannel.Core, "Saved " + finalScriptPath);
            InspectorEditor.ShowNotification("Generated shader");

            string assetPath = finalScriptPath.Replace(Application.dataPath, "Assets");
            var asset = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            AssetDatabase.OpenAsset(asset);
        }

        private string ReplaceShaderName(string text)
        {
            return text.Replace("SHADERNAME", _shaderGUIPath);
        }

        #endregion
    }
}