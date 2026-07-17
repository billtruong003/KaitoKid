/*
 * ============================================================================
 *  MATERIAL & SHADER MANAGER - Unity Editor Tool
 * ============================================================================
 *  Đặt file này vào folder: Assets/Editor/MaterialShaderManager.cs
 *  Mở tool: Window > Material & Shader Manager
 * ============================================================================
 *  Chức năng:
 *    1. Quét toàn bộ Material trong Scene hiện tại
 *    2. Duplicate các Material default (shared/built-in) thành material riêng
 *    3. Liệt kê & phân loại Shader theo Rendering Mode (Opaque, Cutout, Fade, Transparent...)
 *    4. Chuyển đổi hàng loạt Shader từ loại này sang loại khác
 * ============================================================================
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class MaterialShaderManager : EditorWindow
{
    // ========================================================================
    //  DATA STRUCTURES
    // ========================================================================

    /// <summary>
    /// Phân loại Rendering Mode của Shader
    /// </summary>
    public enum ShaderRenderCategory
    {
        Opaque,         // Không trong suốt
        Cutout,         // Cắt theo alpha threshold
        Fade,           // Fade trong suốt (không nhận specular)
        Transparent,    // Trong suốt (nhận specular)
        Additive,       // Cộng màu (particle, glow)
        Multiply,       // Nhân màu
        UI,             // Shader dành cho UI
        Unlit,          // Không nhận ánh sáng
        Custom,         // Shader tự viết / không xác định
        Unknown         // Không phân loại được
    }

    /// <summary>
    /// Thông tin của 1 Material trong Scene
    /// </summary>
    public class MaterialInfo
    {
        public Material material;
        public string shaderName;
        public ShaderRenderCategory category;
        public List<Renderer> usedByRenderers = new List<Renderer>();
        public bool isDefault;       // Có phải material mặc định (built-in/shared)
        public bool isSelected;      // Được chọn để thao tác
        public bool isDuplicated;    // Đã được duplicate
    }

    // ========================================================================
    //  FIELDS
    // ========================================================================

    private List<MaterialInfo> allMaterials = new List<MaterialInfo>();
    private Dictionary<ShaderRenderCategory, List<MaterialInfo>> categorizedMaterials
        = new Dictionary<ShaderRenderCategory, List<MaterialInfo>>();
    private Dictionary<string, List<MaterialInfo>> shaderGroups
        = new Dictionary<string, List<MaterialInfo>>();

    // UI State
    private Vector2 scrollPos;
    private int currentTab = 0;
    private readonly string[] tabNames = new string[]
    {
        "📋 Tổng Quan", "📦 Duplicate Materials", "🏷️ Phân Loại Shader", "🔄 Chuyển Đổi Shader"
    };

    // Tab 1: Duplicate
    private string duplicateSavePath = "Assets/DuplicatedMaterials";
    private bool selectAllDefault = false;

    // Tab 2: Classify
    private Dictionary<ShaderRenderCategory, bool> categoryFoldouts
        = new Dictionary<ShaderRenderCategory, bool>();

    // Tab 3: Convert
    private ShaderRenderCategory filterSourceCategory = ShaderRenderCategory.Opaque;
    private string filterSourceShader = "";
    private int selectedTargetShaderIndex = 0;
    private string customTargetShader = "";
    private bool selectAllForConvert = false;

    // Danh sách shader phổ biến để chuyển đổi
    private readonly string[] commonShaders = new string[]
    {
        "Standard",
        "Standard (Specular setup)",
        "Universal Render Pipeline/Lit",
        "Universal Render Pipeline/Simple Lit",
        "Universal Render Pipeline/Unlit",
        "Universal Render Pipeline/Complex Lit",
        "Unlit/Color",
        "Unlit/Texture",
        "Unlit/Transparent",
        "Unlit/Transparent Cutout",
        "Mobile/Diffuse",
        "Mobile/Bumped Diffuse",
        "Particles/Standard Surface",
        "Particles/Standard Unlit",
        "UI/Default",
        "Sprites/Default",
        "-- Nhập Shader Tùy Chỉnh --"
    };

    // Styles (khởi tạo lazy)
    private bool stylesInitialized = false;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle boxStyle;
    private GUIStyle categoryHeaderStyle;
    private GUIStyle badgeOpaqueStyle;
    private GUIStyle badgeTransparentStyle;
    private GUIStyle badgeDefaultStyle;

    // ========================================================================
    //  MENU & WINDOW
    // ========================================================================

    [MenuItem("Window/Material && Shader Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<MaterialShaderManager>("Material & Shader Manager");
        window.minSize = new Vector2(550, 600);
        window.Show();
    }

    // ========================================================================
    //  STYLES INITIALIZATION
    // ========================================================================

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 8, 8)
        };

        subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            padding = new RectOffset(0, 0, 4, 4)
        };

        boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(4, 4, 4, 4)
        };

        categoryHeaderStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        badgeOpaqueStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.2f, 0.7f, 0.3f) },
            fontStyle = FontStyle.Bold
        };

        badgeTransparentStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.3f, 0.6f, 1f) },
            fontStyle = FontStyle.Bold
        };

        badgeDefaultStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 0.6f, 0.2f) },
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }

    // ========================================================================
    //  MAIN GUI
    // ========================================================================

    private void OnGUI()
    {
        InitStyles();

        // Header
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🎨 Material & Shader Manager", headerStyle);
        EditorGUILayout.Space(4);

        // Nút quét Scene
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
        if (GUILayout.Button("🔍 Quét Scene Hiện Tại", GUILayout.Width(200), GUILayout.Height(30)))
        {
            ScanScene();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        // Thống kê nhanh
        if (allMaterials.Count > 0)
        {
            DrawQuickStats();
        }

        EditorGUILayout.Space(4);

        // Tabs
        currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(28));
        EditorGUILayout.Space(6);

        // Content
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (currentTab)
        {
            case 0: DrawOverviewTab(); break;
            case 1: DrawDuplicateTab(); break;
            case 2: DrawClassifyTab(); break;
            case 3: DrawConvertTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    // ========================================================================
    //  QUICK STATS BAR
    // ========================================================================

    private void DrawQuickStats()
    {
        EditorGUILayout.BeginHorizontal(boxStyle);

        int totalMats = allMaterials.Count;
        int defaultMats = allMaterials.Count(m => m.isDefault);
        int uniqueShaders = shaderGroups.Count;

        GUILayout.Label($"📊 Tổng: {totalMats} materials", EditorStyles.miniLabel);
        GUILayout.Label($"⚠️ Default: {defaultMats}", badgeDefaultStyle);
        GUILayout.Label($"🏷️ {uniqueShaders} loại shader", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    // ========================================================================
    //  TAB 0: TỔNG QUAN
    // ========================================================================

    private void DrawOverviewTab()
    {
        if (allMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Chưa có dữ liệu. Nhấn \"Quét Scene Hiện Tại\" để bắt đầu.",
                MessageType.Info);
            return;
        }

        // Danh sách tất cả Material
        EditorGUILayout.LabelField("📋 Tất Cả Material Trong Scene", subHeaderStyle);
        EditorGUILayout.Space(4);

        // Table header
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Material", EditorStyles.toolbarButton, GUILayout.Width(180));
        GUILayout.Label("Shader", EditorStyles.toolbarButton, GUILayout.Width(200));
        GUILayout.Label("Loại", EditorStyles.toolbarButton, GUILayout.Width(90));
        GUILayout.Label("Default?", EditorStyles.toolbarButton, GUILayout.Width(60));
        GUILayout.Label("Dùng bởi", EditorStyles.toolbarButton, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        foreach (var info in allMaterials)
        {
            EditorGUILayout.BeginHorizontal();

            // Material name (click để select)
            if (GUILayout.Button(info.material.name, EditorStyles.linkLabel, GUILayout.Width(180)))
            {
                EditorGUIUtility.PingObject(info.material);
                Selection.activeObject = info.material;
            }

            // Shader name
            GUILayout.Label(info.shaderName, GUILayout.Width(200));

            // Category badge
            GUIStyle badgeStyle = GetCategoryBadgeStyle(info.category);
            GUILayout.Label(GetCategoryIcon(info.category) + " " + info.category.ToString(),
                badgeStyle, GUILayout.Width(90));

            // Is default
            if (info.isDefault)
                GUILayout.Label("⚠️ Có", badgeDefaultStyle, GUILayout.Width(60));
            else
                GUILayout.Label("✅ Không", GUILayout.Width(60));

            // Used by count
            GUILayout.Label(info.usedByRenderers.Count.ToString(), GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();
        }
    }

    // ========================================================================
    //  TAB 1: DUPLICATE MATERIALS
    // ========================================================================

    private void DrawDuplicateTab()
    {
        if (allMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Chưa có dữ liệu. Nhấn \"Quét Scene Hiện Tại\" để bắt đầu.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("📦 Duplicate Material Default", subHeaderStyle);
        EditorGUILayout.HelpBox(
            "Các Material mặc định (built-in) được chia sẻ giữa nhiều object. " +
            "Duplicate để tạo bản riêng có thể chỉnh sửa độc lập.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        // Save path
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lưu vào:", GUILayout.Width(60));
        duplicateSavePath = EditorGUILayout.TextField(duplicateSavePath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Chọn folder lưu material", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    duplicateSavePath = "Assets" + path.Substring(Application.dataPath.Length);
                else
                    EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn folder trong Assets!", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Filter chỉ default materials
        var defaultMats = allMaterials.Where(m => m.isDefault).ToList();

        if (defaultMats.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "✅ Không có Material default nào trong scene. Tất cả đều là material riêng!",
                MessageType.Info);
            return;
        }

        // Select all toggle
        EditorGUILayout.BeginHorizontal();
        bool newSelectAll = EditorGUILayout.ToggleLeft(
            $"Chọn tất cả ({defaultMats.Count} material default)", selectAllDefault);
        if (newSelectAll != selectAllDefault)
        {
            selectAllDefault = newSelectAll;
            foreach (var m in defaultMats) m.isSelected = selectAllDefault;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        // List
        foreach (var info in defaultMats)
        {
            EditorGUILayout.BeginHorizontal(boxStyle);

            info.isSelected = EditorGUILayout.Toggle(info.isSelected, GUILayout.Width(20));

            // Material preview
            Rect previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
            EditorGUI.DrawPreviewTexture(previewRect, AssetPreview.GetMiniThumbnail(info.material));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(info.material.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Shader: {info.shaderName}  |  " +
                $"Dùng bởi: {info.usedByRenderers.Count} renderer(s)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (info.isDuplicated)
            {
                GUILayout.Label("✅ Đã duplicate", badgeOpaqueStyle, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);

        // Duplicate button
        int selectedCount = defaultMats.Count(m => m.isSelected && !m.isDuplicated);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = selectedCount > 0 ? new Color(0.3f, 0.7f, 1f) : Color.gray;
        GUI.enabled = selectedCount > 0;

        if (GUILayout.Button($"📦 Duplicate {selectedCount} Material(s)",
            GUILayout.Width(250), GUILayout.Height(32)))
        {
            DuplicateSelectedMaterials(defaultMats.Where(m => m.isSelected && !m.isDuplicated).ToList());
        }

        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    // ========================================================================
    //  TAB 2: PHÂN LOẠI SHADER
    // ========================================================================

    private void DrawClassifyTab()
    {
        if (allMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Chưa có dữ liệu. Nhấn \"Quét Scene Hiện Tại\" để bắt đầu.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("🏷️ Phân Loại Shader Theo Rendering Mode", subHeaderStyle);
        EditorGUILayout.Space(4);

        // Vẽ từng category
        foreach (var kvp in categorizedMaterials.OrderBy(k => k.Key))
        {
            ShaderRenderCategory cat = kvp.Key;
            List<MaterialInfo> mats = kvp.Value;

            if (mats.Count == 0) continue;

            // Ensure foldout exists
            if (!categoryFoldouts.ContainsKey(cat))
                categoryFoldouts[cat] = true;

            EditorGUILayout.Space(2);

            // Category header with colored box
            Color catColor = GetCategoryColor(cat);
            GUI.backgroundColor = catColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            // Foldout header
            EditorGUILayout.BeginHorizontal();
            categoryFoldouts[cat] = EditorGUILayout.Foldout(categoryFoldouts[cat],
                $"  {GetCategoryIcon(cat)}  {cat}  ({mats.Count} materials)", true, categoryHeaderStyle);
            GUILayout.FlexibleSpace();

            // Mô tả ngắn
            GUILayout.Label(GetCategoryDescription(cat), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (categoryFoldouts[cat])
            {
                EditorGUILayout.Space(4);

                // Group by shader name within category
                var shaderSubGroups = mats.GroupBy(m => m.shaderName).OrderBy(g => g.Key);

                foreach (var group in shaderSubGroups)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);

                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUILayout.LabelField($"🔸 {group.Key}", EditorStyles.boldLabel);

                    foreach (var info in group)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(12);

                        if (GUILayout.Button(info.material.name, EditorStyles.linkLabel,
                            GUILayout.Width(200)))
                        {
                            EditorGUIUtility.PingObject(info.material);
                            Selection.activeObject = info.material;
                        }

                        if (info.isDefault)
                            GUILayout.Label("⚠️ Default", badgeDefaultStyle, GUILayout.Width(70));

                        GUILayout.Label($"({info.usedByRenderers.Count} renderer)",
                            EditorStyles.miniLabel);

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        // Bảng tổng hợp
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("📊 Bảng Tổng Hợp Shader", subHeaderStyle);

        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Shader", EditorStyles.toolbarButton, GUILayout.Width(250));
        GUILayout.Label("Phân loại", EditorStyles.toolbarButton, GUILayout.Width(100));
        GUILayout.Label("Số lượng", EditorStyles.toolbarButton, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        foreach (var kvp in shaderGroups.OrderBy(k => k.Key))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(kvp.Key, GUILayout.Width(250));

            ShaderRenderCategory cat = kvp.Value.First().category;
            GUIStyle style = GetCategoryBadgeStyle(cat);
            GUILayout.Label(GetCategoryIcon(cat) + " " + cat.ToString(), style, GUILayout.Width(100));

            GUILayout.Label(kvp.Value.Count.ToString(), GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // ========================================================================
    //  TAB 3: CHUYỂN ĐỔI SHADER
    // ========================================================================

    private void DrawConvertTab()
    {
        if (allMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Chưa có dữ liệu. Nhấn \"Quét Scene Hiện Tại\" để bắt đầu.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("🔄 Chuyển Đổi Shader Hàng Loạt", subHeaderStyle);
        EditorGUILayout.Space(4);

        // ---- BƯỚC 1: Lọc nguồn ----
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Bước 1: Chọn Material nguồn", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // Lọc theo category
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lọc theo loại:", GUILayout.Width(100));
        filterSourceCategory = (ShaderRenderCategory)EditorGUILayout.EnumPopup(filterSourceCategory);
        EditorGUILayout.EndHorizontal();

        // Lọc theo shader name
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Lọc theo shader:", GUILayout.Width(100));

        // Tạo danh sách shader có trong category đã chọn
        var shadersInCategory = allMaterials
            .Where(m => m.category == filterSourceCategory)
            .Select(m => m.shaderName)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        shadersInCategory.Insert(0, "-- Tất cả --");

        int currentShaderIdx = shadersInCategory.IndexOf(filterSourceShader);
        if (currentShaderIdx < 0) currentShaderIdx = 0;

        currentShaderIdx = EditorGUILayout.Popup(currentShaderIdx, shadersInCategory.ToArray());
        filterSourceShader = shadersInCategory[currentShaderIdx];

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // Danh sách material phù hợp filter
        var filteredMats = allMaterials
            .Where(m => m.category == filterSourceCategory)
            .Where(m => filterSourceShader == "-- Tất cả --" || m.shaderName == filterSourceShader)
            .ToList();

        EditorGUILayout.Space(4);

        if (filteredMats.Count == 0)
        {
            EditorGUILayout.HelpBox(
                $"Không có material nào thuộc loại {filterSourceCategory}" +
                (filterSourceShader != "-- Tất cả --" ? $" với shader {filterSourceShader}" : ""),
                MessageType.Warning);
        }
        else
        {
            // Select all
            EditorGUILayout.BeginHorizontal();
            bool newSelectAllConvert = EditorGUILayout.ToggleLeft(
                $"Chọn tất cả ({filteredMats.Count} materials)", selectAllForConvert);
            if (newSelectAllConvert != selectAllForConvert)
            {
                selectAllForConvert = newSelectAllConvert;
                foreach (var m in filteredMats) m.isSelected = selectAllForConvert;
            }
            EditorGUILayout.EndHorizontal();

            // List
            foreach (var info in filteredMats)
            {
                EditorGUILayout.BeginHorizontal();
                info.isSelected = EditorGUILayout.Toggle(info.isSelected, GUILayout.Width(20));

                Rect previewRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24));
                EditorGUI.DrawPreviewTexture(previewRect, AssetPreview.GetMiniThumbnail(info.material));

                GUILayout.Label(info.material.name, GUILayout.Width(160));
                GUILayout.Label(info.shaderName, EditorStyles.miniLabel, GUILayout.Width(200));

                if (info.isDefault)
                    GUILayout.Label("⚠️", GUILayout.Width(25));

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(8);

        // ---- BƯỚC 2: Chọn Shader đích ----
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Bước 2: Chọn Shader đích", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        selectedTargetShaderIndex = EditorGUILayout.Popup("Shader đích:", selectedTargetShaderIndex,
            commonShaders);

        if (selectedTargetShaderIndex == commonShaders.Length - 1)
        {
            customTargetShader = EditorGUILayout.TextField("Tên shader:", customTargetShader);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ---- BƯỚC 3: Thực hiện ----
        int selectedForConvert = filteredMats.Count(m => m.isSelected);
        string targetShaderName = selectedTargetShaderIndex == commonShaders.Length - 1
            ? customTargetShader
            : commonShaders[selectedTargetShaderIndex];

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = selectedForConvert > 0 ? new Color(1f, 0.6f, 0.2f) : Color.gray;
        GUI.enabled = selectedForConvert > 0 && !string.IsNullOrEmpty(targetShaderName);

        if (GUILayout.Button($"🔄 Chuyển {selectedForConvert} Material(s) → {targetShaderName}",
            GUILayout.Width(400), GUILayout.Height(32)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận chuyển đổi Shader",
                $"Bạn có chắc muốn chuyển {selectedForConvert} material(s) sang shader " +
                $"\"{targetShaderName}\"?\n\nThao tác này có thể Undo (Ctrl+Z).",
                "Chuyển đổi", "Hủy"))
            {
                ConvertSelectedShaders(
                    filteredMats.Where(m => m.isSelected).ToList(),
                    targetShaderName);
            }
        }

        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "💡 Tip: Material default (⚠️) nên được Duplicate trước khi chuyển shader " +
            "để tránh ảnh hưởng đến các scene/project khác.",
            MessageType.Warning);
    }

    // ========================================================================
    //  CORE LOGIC: SCAN SCENE
    // ========================================================================

    private void ScanScene()
    {
        allMaterials.Clear();
        categorizedMaterials.Clear();
        shaderGroups.Clear();
        categoryFoldouts.Clear();

        // Tìm tất cả Renderer trong scene
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        // Dictionary để track material đã thêm (tránh duplicate entry)
        Dictionary<Material, MaterialInfo> materialMap = new Dictionary<Material, MaterialInfo>();

        foreach (Renderer renderer in allRenderers)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;

                if (!materialMap.ContainsKey(mat))
                {
                    MaterialInfo info = new MaterialInfo
                    {
                        material = mat,
                        shaderName = mat.shader != null ? mat.shader.name : "None",
                        category = ClassifyShader(mat),
                        isDefault = IsDefaultMaterial(mat),
                        isSelected = false,
                        isDuplicated = false
                    };

                    materialMap[mat] = info;
                }

                materialMap[mat].usedByRenderers.Add(renderer);
            }
        }

        allMaterials = materialMap.Values.ToList();

        // Phân loại theo category
        foreach (ShaderRenderCategory cat in System.Enum.GetValues(typeof(ShaderRenderCategory)))
        {
            categorizedMaterials[cat] = allMaterials.Where(m => m.category == cat).ToList();
        }

        // Group theo shader name
        shaderGroups = allMaterials.GroupBy(m => m.shaderName)
            .ToDictionary(g => g.Key, g => g.ToList());

        Debug.Log($"[MaterialShaderManager] Đã quét xong: {allMaterials.Count} materials, " +
            $"{shaderGroups.Count} loại shader, " +
            $"{allMaterials.Count(m => m.isDefault)} default materials.");

        Repaint();
    }

    // ========================================================================
    //  CORE LOGIC: CLASSIFY SHADER
    // ========================================================================

    /// <summary>
    /// Phân loại shader của material vào các nhóm rendering mode
    /// </summary>
    private ShaderRenderCategory ClassifyShader(Material mat)
    {
        if (mat == null || mat.shader == null) return ShaderRenderCategory.Unknown;

        string shaderName = mat.shader.name.ToLower();

        // ---- UI Shaders ----
        if (shaderName.Contains("ui/") || shaderName.Contains("sprites/"))
            return ShaderRenderCategory.UI;

        // ---- Unlit Shaders ----
        if (shaderName.Contains("unlit"))
        {
            if (shaderName.Contains("transparent"))
                return ShaderRenderCategory.Transparent;
            if (shaderName.Contains("cutout"))
                return ShaderRenderCategory.Cutout;
            return ShaderRenderCategory.Unlit;
        }

        // ---- Particle Shaders ----
        if (shaderName.Contains("particle"))
        {
            if (shaderName.Contains("additive") || shaderName.Contains("add"))
                return ShaderRenderCategory.Additive;
            if (shaderName.Contains("multiply"))
                return ShaderRenderCategory.Multiply;
            // Particles thường transparent
            return ShaderRenderCategory.Transparent;
        }

        // ---- Standard / URP Lit: kiểm tra _Mode property ----
        if (shaderName.Contains("standard") || shaderName.Contains("/lit") ||
            shaderName.Contains("simple lit") || shaderName.Contains("complex lit"))
        {
            // Standard shader sử dụng _Mode: 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
            if (mat.HasProperty("_Mode"))
            {
                int mode = (int)mat.GetFloat("_Mode");
                switch (mode)
                {
                    case 0: return ShaderRenderCategory.Opaque;
                    case 1: return ShaderRenderCategory.Cutout;
                    case 2: return ShaderRenderCategory.Fade;
                    case 3: return ShaderRenderCategory.Transparent;
                }
            }

            // URP sử dụng _Surface: 0=Opaque, 1=Transparent
            if (mat.HasProperty("_Surface"))
            {
                int surface = (int)mat.GetFloat("_Surface");
                if (surface == 1) return ShaderRenderCategory.Transparent;
                return ShaderRenderCategory.Opaque;
            }

            // Kiểm tra render queue fallback
            return ClassifyByRenderQueue(mat);
        }

        // ---- Transparent keywords trong tên ----
        if (shaderName.Contains("transparent") || shaderName.Contains("alpha"))
            return ShaderRenderCategory.Transparent;

        if (shaderName.Contains("cutout") || shaderName.Contains("alphatest"))
            return ShaderRenderCategory.Cutout;

        if (shaderName.Contains("fade"))
            return ShaderRenderCategory.Fade;

        if (shaderName.Contains("additive") || shaderName.Contains("add"))
            return ShaderRenderCategory.Additive;

        if (shaderName.Contains("multiply"))
            return ShaderRenderCategory.Multiply;

        // ---- Fallback: dùng render queue ----
        return ClassifyByRenderQueue(mat);
    }

    /// <summary>
    /// Phân loại dựa trên Render Queue
    /// </summary>
    private ShaderRenderCategory ClassifyByRenderQueue(Material mat)
    {
        int queue = mat.renderQueue;

        if (queue <= 2000) return ShaderRenderCategory.Opaque;        // Background
        if (queue <= 2450) return ShaderRenderCategory.Opaque;        // Geometry (2000)
        if (queue <= 2500) return ShaderRenderCategory.Cutout;        // AlphaTest (2450)
        if (queue <= 3000) return ShaderRenderCategory.Transparent;   // Transparent (3000)
        if (queue <= 4000) return ShaderRenderCategory.Transparent;   // Overlay

        return ShaderRenderCategory.Unknown;
    }

    // ========================================================================
    //  CORE LOGIC: IS DEFAULT MATERIAL
    // ========================================================================

    /// <summary>
    /// Kiểm tra xem material có phải là default/built-in không
    /// </summary>
    private bool IsDefaultMaterial(Material mat)
    {
        if (mat == null) return false;

        string assetPath = AssetDatabase.GetAssetPath(mat);

        // Material không có asset path -> là instance (built-in hoặc runtime)
        if (string.IsNullOrEmpty(assetPath))
            return true;

        // Material nằm trong Resources/unity_builtin_extra hoặc Library
        if (assetPath.StartsWith("Resources/") ||
            assetPath.StartsWith("Library/") ||
            assetPath.Contains("unity_builtin") ||
            assetPath.Contains("unity default"))
            return true;

        // Material nằm trong Packages (ngoại trừ user packages)
        if (assetPath.StartsWith("Packages/com.unity."))
            return true;

        return false;
    }

    // ========================================================================
    //  CORE LOGIC: DUPLICATE MATERIALS
    // ========================================================================

    private void DuplicateSelectedMaterials(List<MaterialInfo> materialsToClone)
    {
        if (materialsToClone.Count == 0) return;

        // Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder(duplicateSavePath))
        {
            CreateFolderRecursive(duplicateSavePath);
        }

        Undo.SetCurrentGroupName("Duplicate Materials");
        int undoGroup = Undo.GetCurrentGroup();

        int successCount = 0;

        foreach (var info in materialsToClone)
        {
            // Tạo bản copy
            Material newMat = new Material(info.material);
            newMat.name = info.material.name + "_Copy";

            // Tạo tên file unique
            string fileName = SanitizeFileName(newMat.name);
            string fullPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{duplicateSavePath}/{fileName}.mat");

            // Lưu asset
            AssetDatabase.CreateAsset(newMat, fullPath);

            // Thay thế trên tất cả renderer đang dùng material cũ
            foreach (Renderer renderer in info.usedByRenderers)
            {
                Undo.RecordObject(renderer, "Replace Material");

                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == info.material)
                        mats[i] = newMat;
                }
                renderer.sharedMaterials = mats;
            }

            info.isDuplicated = true;
            info.material = newMat;
            info.isDefault = false;
            successCount++;

            Debug.Log($"[MaterialShaderManager] Duplicated: {info.material.name} → {fullPath}");
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Hoàn tất",
            $"Đã duplicate thành công {successCount} material(s)\nvào {duplicateSavePath}",
            "OK");

        // Quét lại
        ScanScene();
    }

    // ========================================================================
    //  CORE LOGIC: CONVERT SHADERS
    // ========================================================================

    private void ConvertSelectedShaders(List<MaterialInfo> materialsToConvert, string targetShaderName)
    {
        Shader targetShader = Shader.Find(targetShaderName);

        if (targetShader == null)
        {
            EditorUtility.DisplayDialog("Lỗi",
                $"Không tìm thấy shader \"{targetShaderName}\".\n" +
                "Hãy đảm bảo tên shader chính xác và shader đã được import vào project.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Convert Shaders");
        int undoGroup = Undo.GetCurrentGroup();

        int successCount = 0;
        List<string> warnings = new List<string>();

        foreach (var info in materialsToConvert)
        {
            Material mat = info.material;

            // Cảnh báo nếu là default material
            if (info.isDefault)
            {
                warnings.Add($"⚠️ {mat.name} là material default - nên Duplicate trước!");
                continue; // Bỏ qua default material
            }

            Undo.RecordObject(mat, "Change Shader");

            // Lưu lại các property cũ nếu có thể
            Color? mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : (Color?)null;
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            float? metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : (float?)null;
            float? smoothness = mat.HasProperty("_Glossiness") ?
                mat.GetFloat("_Glossiness") :
                (mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : (float?)null);

            // Đổi shader
            mat.shader = targetShader;

            // Khôi phục các property nếu shader mới cũng có
            if (mainColor.HasValue && mat.HasProperty("_Color"))
                mat.SetColor("_Color", mainColor.Value);
            if (mainColor.HasValue && mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", mainColor.Value);

            if (mainTex != null)
            {
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
            }

            if (normalMap != null)
            {
                if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normalMap);
                if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normalMap);
            }

            if (metallic.HasValue && mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic.Value);

            if (smoothness.HasValue)
            {
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness.Value);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness.Value);
            }

            // Cập nhật info
            info.shaderName = targetShaderName;
            info.category = ClassifyShader(mat);

            EditorUtility.SetDirty(mat);
            successCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();

        // Hiện kết quả
        string message = $"Đã chuyển đổi thành công {successCount} material(s) sang " +
            $"\"{targetShaderName}\"";

        if (warnings.Count > 0)
        {
            message += $"\n\n⚠️ Đã bỏ qua {warnings.Count} material default:\n" +
                string.Join("\n", warnings.Take(5));
            if (warnings.Count > 5)
                message += $"\n... và {warnings.Count - 5} material khác";
        }

        EditorUtility.DisplayDialog("Kết quả chuyển đổi", message, "OK");

        // Quét lại
        ScanScene();
    }

    // ========================================================================
    //  HELPERS
    // ========================================================================

    private string GetCategoryIcon(ShaderRenderCategory cat)
    {
        switch (cat)
        {
            case ShaderRenderCategory.Opaque:      return "🟫";
            case ShaderRenderCategory.Cutout:       return "✂️";
            case ShaderRenderCategory.Fade:         return "🌫️";
            case ShaderRenderCategory.Transparent:  return "💎";
            case ShaderRenderCategory.Additive:     return "✨";
            case ShaderRenderCategory.Multiply:     return "✖️";
            case ShaderRenderCategory.UI:           return "🖼️";
            case ShaderRenderCategory.Unlit:        return "💡";
            case ShaderRenderCategory.Custom:       return "🔧";
            default:                                return "❓";
        }
    }

    private string GetCategoryDescription(ShaderRenderCategory cat)
    {
        switch (cat)
        {
            case ShaderRenderCategory.Opaque:      return "Không trong suốt, hiệu suất tốt nhất";
            case ShaderRenderCategory.Cutout:       return "Cắt alpha theo threshold, 0 hoặc 1";
            case ShaderRenderCategory.Fade:         return "Mờ dần, không nhận specular highlight";
            case ShaderRenderCategory.Transparent:  return "Trong suốt, nhận specular highlight";
            case ShaderRenderCategory.Additive:     return "Cộng màu, dùng cho hiệu ứng phát sáng";
            case ShaderRenderCategory.Multiply:     return "Nhân màu, dùng cho hiệu ứng tối";
            case ShaderRenderCategory.UI:           return "Shader cho UI Canvas & Sprites";
            case ShaderRenderCategory.Unlit:        return "Không bị ảnh hưởng bởi ánh sáng";
            case ShaderRenderCategory.Custom:       return "Shader tùy chỉnh / bên thứ ba";
            default:                                return "Chưa xác định";
        }
    }

    private Color GetCategoryColor(ShaderRenderCategory cat)
    {
        switch (cat)
        {
            case ShaderRenderCategory.Opaque:      return new Color(0.85f, 0.85f, 0.85f);
            case ShaderRenderCategory.Cutout:       return new Color(1f, 0.95f, 0.8f);
            case ShaderRenderCategory.Fade:         return new Color(0.9f, 0.9f, 1f);
            case ShaderRenderCategory.Transparent:  return new Color(0.8f, 0.9f, 1f);
            case ShaderRenderCategory.Additive:     return new Color(1f, 1f, 0.8f);
            case ShaderRenderCategory.Multiply:     return new Color(0.9f, 0.85f, 0.95f);
            case ShaderRenderCategory.UI:           return new Color(0.85f, 1f, 0.85f);
            case ShaderRenderCategory.Unlit:        return new Color(1f, 0.9f, 0.85f);
            case ShaderRenderCategory.Custom:       return new Color(0.95f, 0.85f, 0.85f);
            default:                                return Color.white;
        }
    }

    private GUIStyle GetCategoryBadgeStyle(ShaderRenderCategory cat)
    {
        switch (cat)
        {
            case ShaderRenderCategory.Opaque:
            case ShaderRenderCategory.Unlit:
                return badgeOpaqueStyle ?? EditorStyles.miniLabel;

            case ShaderRenderCategory.Transparent:
            case ShaderRenderCategory.Fade:
            case ShaderRenderCategory.Cutout:
                return badgeTransparentStyle ?? EditorStyles.miniLabel;

            default:
                return badgeDefaultStyle ?? EditorStyles.miniLabel;
        }
    }

    private string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private void CreateFolderRecursive(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
