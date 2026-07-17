using UnityEngine;
using UnityEditor;

public class InteractiveSnowShaderGUI : ShaderGUI
{
    private static bool showMainProperties = true;
    private static bool showSnowShape = true;
    private static bool showInteractivePath = true;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        materialEditor.SetDefaultGUIWidths();

        DrawMainProperties(materialEditor, properties);
        DrawSnowShape(materialEditor, properties);
        DrawInteractivePath(materialEditor, properties);
    }

    private void DrawMainProperties(MaterialEditor editor, MaterialProperty[] props)
    {
        showMainProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showMainProperties, "Main Properties");
        if (showMainProperties)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_SnowColor", "Snow Color", editor, props);
            DrawProperty("_MainTex", "Snow Texture", editor, props);
            DrawProperty("_SnowTextureOpacity", "Snow Texture Opacity", editor, props);
            DrawProperty("_SnowTextureScale", "Snow Texture Scale", editor, props);
            DrawProperty("_ShadowColor", "Shadow Color", editor, props);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawSnowShape(MaterialEditor editor, MaterialProperty[] props)
    {
        showSnowShape = EditorGUILayout.BeginFoldoutHeaderGroup(showSnowShape, "Snow Shape");
        if (showSnowShape)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_NoiseTexture", "Snow Noise", editor, props);
            DrawProperty("_NoiseScale", "Noise Scale", editor, props);
            DrawProperty("_NoiseWeight", "Noise Weight", editor, props);
            DrawProperty("_SnowHeight", "Snow Height", editor, props);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawInteractivePath(MaterialEditor editor, MaterialProperty[] props)
    {
        showInteractivePath = EditorGUILayout.BeginFoldoutHeaderGroup(showInteractivePath, "Interactive Path");
        if (showInteractivePath)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProperty("_PathColorIn", "Path Inner Color", editor, props);
            DrawProperty("_PathColorOut", "Path Outer Color", editor, props);
            DrawProperty("_PathBlending", "Path Blending", editor, props);
            DrawProperty("_SnowPathStrength", "Path Strength", editor, props);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }

    private void DrawProperty(string propertyName, string label, MaterialEditor editor, MaterialProperty[] properties)
    {
        MaterialProperty property = FindProperty(propertyName, properties);
        if (property != null)
        {
            editor.ShaderProperty(property, new GUIContent(label));
        }
    }
}