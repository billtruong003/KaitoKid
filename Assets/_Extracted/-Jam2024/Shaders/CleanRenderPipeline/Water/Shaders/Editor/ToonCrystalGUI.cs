using UnityEditor;
using UnityEngine;

public class ToonCrystalGUI : ShaderGUI
{
    static bool _foldSurface = true;
    static bool _foldInterior = true;
    static bool _foldMatcap = false;
    static bool _foldFresnel = true;
    static bool _foldSpecular = false;
    static bool _foldCel = true;
    static bool _foldRim = false;
    static bool _foldEmission = false;

    static GUIStyle _headerStyle;
    static GUIStyle _sectionBox;
    static bool _stylesInit;

    static readonly Color AccentCrystal = new Color(0.5f, 0.7f, 0.95f, 1f);
    static readonly Color AccentInterior = new Color(0.4f, 0.5f, 0.85f, 1f);
    static readonly Color AccentGlow = new Color(0.85f, 0.7f, 1f, 1f);

    static void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;
        _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, richText = true };
        _sectionBox = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 6, 6),
            margin = new RectOffset(0, 0, 2, 4)
        };
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        InitStyles();
        Material mat = materialEditor.target as Material;

        EditorGUILayout.Space(4);
        DrawBanner("TOON CRYSTAL", AccentCrystal);
        EditorGUILayout.Space(4);

        // ━━ Surface ━━
        _foldSurface = DrawSection("Surface", _foldSurface, AccentCrystal, () =>
        {
            DrawTextureProp(materialEditor, properties, "_BaseMap", "_BaseColor", "Surface Texture");
        });

        // ━━ Fake Interior ━━
        _foldInterior = DrawSection("Fake Interior", _foldInterior, AccentInterior, () =>
        {
            DrawTextureProp(materialEditor, properties, "_InteriorMap", "_InteriorColor", "Interior Texture");
            EditorGUILayout.Space(2);
            DrawProp(materialEditor, properties, "_InteriorDepth", "Parallax Depth");
            DrawProp(materialEditor, properties, "_InteriorBlend", "Interior Blend");
            DrawHelpBox("Texture fakes depth inside the crystal");
            DrawHelpBox("Depth = how far 'inside' the camera looks");
        });

        // ━━ Matcap ━━
        _foldMatcap = DrawSection("Matcap (Fake Reflection)", _foldMatcap, AccentCrystal, () =>
        {
            MaterialProperty toggle = FindProperty("_UseMatcap", properties, false);
            if (toggle != null)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(toggle, "Enable Matcap");
                if (EditorGUI.EndChangeCheck())
                {
                    if (toggle.floatValue > 0.5f)
                        mat.EnableKeyword("_USE_MATCAP");
                    else
                        mat.DisableKeyword("_USE_MATCAP");
                }
            }

            if (mat.IsKeywordEnabled("_USE_MATCAP"))
            {
                MaterialProperty matcapTex = FindProperty("_Matcap", properties, false);
                if (matcapTex != null)
                    materialEditor.TexturePropertySingleLine(new GUIContent("Matcap Texture"), matcapTex);
                DrawProp(materialEditor, properties, "_MatcapStrength", "Matcap Strength");
                DrawHelpBox("Use a matcap texture for cheap environment reflection");
            }
        });

        // ━━ Fresnel ━━
        _foldFresnel = DrawSection("Fresnel Edge Glow", _foldFresnel, AccentGlow, () =>
        {
            DrawProp(materialEditor, properties, "_FresnelColor", "Fresnel Color");
            DrawProp(materialEditor, properties, "_FresnelPower", "Fresnel Power");
            DrawHelpBox("Edge glow — higher power = thinner edge");
        });

        // ━━ Specular ━━
        _foldSpecular = DrawSection("Specular Facets", _foldSpecular, AccentCrystal, () =>
        {
            DrawProp(materialEditor, properties, "_SpecColor", "Specular Color");
            DrawProp(materialEditor, properties, "_SpecCutoff", "Specular Cutoff");
            DrawProp(materialEditor, properties, "_SpecSmoothness", "Specular Smoothness");
            DrawHelpBox("Hard toon highlight — mimics crystal facet reflections");
        });

        // ━━ Cel Shading ━━
        _foldCel = DrawSection("Cel Shading", _foldCel, AccentCrystal, () =>
        {
            DrawProp(materialEditor, properties, "_ShadowColor", "Shadow Color");
            DrawProp(materialEditor, properties, "_Threshold", "Shadow Threshold");
            DrawProp(materialEditor, properties, "_Smoothness", "Shadow Smoothness");
        });

        // ━━ Rim ━━
        _foldRim = DrawSection("Rim Light", _foldRim, AccentCrystal, () =>
        {
            DrawProp(materialEditor, properties, "_RimColor", "Rim Color (RGB + A = intensity)");
            DrawProp(materialEditor, properties, "_RimPower", "Rim Power");
        });

        // ━━ Emission ━━
        _foldEmission = DrawSection("Emission", _foldEmission, AccentGlow, () =>
        {
            MaterialProperty toggle = FindProperty("_UseEmission", properties, false);
            if (toggle != null)
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(toggle, "Enable Emission");
                if (EditorGUI.EndChangeCheck())
                {
                    if (toggle.floatValue > 0.5f)
                        mat.EnableKeyword("_EMISSION");
                    else
                        mat.DisableKeyword("_EMISSION");
                }
            }

            if (mat.IsKeywordEnabled("_EMISSION"))
            {
                DrawProp(materialEditor, properties, "_EmissionColor", "Emission Color (HDR)");
                DrawHelpBox("Use HDR color for bloom-compatible glow");
            }
        });

        EditorGUILayout.Space(6);
        materialEditor.RenderQueueField();
    }

    // ════════════════════════════════════════════════════════════════
    // Drawing Helpers (same pattern as ToonTerrainGUI)
    // ════════════════════════════════════════════════════════════════

    static bool DrawSection(string title, bool foldout, Color accent, System.Action drawContent)
    {
        EditorGUILayout.Space(2);
        Rect headerRect = GUILayoutUtility.GetRect(1f, 22f, GUILayout.ExpandWidth(true));

        Color bgCol = foldout ? new Color(0.25f, 0.25f, 0.25f, 0.6f) : new Color(0.2f, 0.2f, 0.2f, 0.3f);
        EditorGUI.DrawRect(headerRect, bgCol);
        EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 3f, headerRect.height), accent);

        Event e = Event.current;
        if (e.type == EventType.MouseDown && headerRect.Contains(e.mousePosition))
        {
            foldout = !foldout;
            e.Use();
        }

        Rect labelRect = new Rect(headerRect.x + 16f, headerRect.y + 2f, headerRect.width - 16f, headerRect.height);
        EditorGUI.LabelField(labelRect, (foldout ? "▼ " : "► ") + title, _headerStyle);

        if (foldout)
        {
            EditorGUILayout.BeginVertical(_sectionBox);
            drawContent();
            EditorGUILayout.EndVertical();
        }

        return foldout;
    }

    static void DrawBanner(string text, Color color)
    {
        Rect r = GUILayoutUtility.GetRect(1f, 28f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.8f));
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 2f), color);

        GUIStyle s = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = color }
        };
        EditorGUI.LabelField(r, text, s);
    }

    static void DrawProp(MaterialEditor editor, MaterialProperty[] props, string name, string label)
    {
        MaterialProperty p = FindProperty(name, props, false);
        if (p != null) editor.ShaderProperty(p, label);
    }

    static void DrawTextureProp(MaterialEditor editor, MaterialProperty[] props,
        string texName, string colorName, string label)
    {
        MaterialProperty tex = FindProperty(texName, props, false);
        MaterialProperty col = FindProperty(colorName, props, false);
        if (tex != null && col != null)
            editor.TexturePropertySingleLine(new GUIContent(label), tex, col);
    }

    static void DrawHelpBox(string msg)
    {
        EditorGUILayout.LabelField(msg, EditorStyles.centeredGreyMiniLabel);
    }
}
