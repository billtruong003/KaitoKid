#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace SpumOnline.Editor
{
    /// <summary>
    /// Mega setup wizard v2: tab-based editor for setting up all game scenes, prefabs, and wiring.
    /// Menu: BillGameCore > SPUM Online > Scene & Character Setup
    /// </summary>
    public class SceneSetupWizard : EditorWindow
    {
        // -------------------------------------------------------
        // Constants
        // -------------------------------------------------------

        const string ScenesPath = "Assets/Scenes";
        const string PrefabsPath = "Assets/Prefabs";
        const string SkeletonDataPath = "Assets/Layer Lab/2D Art Maker/AMCasual Character/Demo/SpineAnimation/Casual Character_SkeletonData.asset";
        const string LayerLabPath = "Assets/Layer Lab";

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private int _selectedTab;
        private Vector2 _scroll;
        private List<string> _log = new();

        private readonly string[] _tabNames = { "1. Prefabs", "2. Bootstrap", "3. CharacterSelect", "4. GameWorld" };

        // -------------------------------------------------------
        // Menu
        // -------------------------------------------------------

        [MenuItem("BillGameCore/SPUM Online/Scene & Character Setup", false, 10)]
        static void Open()
        {
            var w = GetWindow<SceneSetupWizard>("Scene Setup Wizard v2");
            w.minSize = new Vector2(580, 700);
        }

        // -------------------------------------------------------
        // GUI
        // -------------------------------------------------------

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("SPUM Online - Scene & Character Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Tab bar
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_selectedTab)
            {
                case 0: DrawPrefabsTab(); break;
                case 1: DrawBootstrapTab(); break;
                case 2: DrawCharacterSelectTab(); break;
                case 3: DrawGameWorldTab(); break;
            }

            // Full setup button
            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("RUN ALL TABS (Full Setup)", GUILayout.Height(38)))
            {
                _log.Clear();
                RunPrefabSetup();
                RunBootstrapSetup();
                RunCharacterSelectSetup();
                RunGameWorldSetup();
                SetupBuildSettings();
                Log("=== Full setup complete! ===");
            }
            GUI.backgroundColor = Color.white;

            // Log
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            foreach (var line in _log)
            {
                if (line.StartsWith("[ERR")) EditorGUILayout.HelpBox(line, MessageType.Error);
                else if (line.StartsWith("[WARN")) EditorGUILayout.HelpBox(line, MessageType.Warning);
                else EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }

        // ===============================================================
        // TAB 1: Prefabs
        // ===============================================================

        void DrawPrefabsTab()
        {
            EditorGUILayout.HelpBox(
                "Tao 5 prefab:\n" +
                "- PlayerLocal (SkeletonAnimation + PartsManager + LocalPlayerController + SkillController + CharacterVisualSync)\n" +
                "- PlayerRemote (SkeletonAnimation + PartsManager + RemotePlayerController + CharacterVisualSync)\n" +
                "- MobPrefab (SkeletonAnimation + PartsManager + MobController + HP bar)\n" +
                "- LootDropPrefab (SpriteRenderer + LootDrop + name label + glow)\n" +
                "- DamagePopupPrefab (TMP_Text + DamagePopup)",
                MessageType.Info);

            DrawStatus("PlayerLocal.prefab", File.Exists($"{PrefabsPath}/PlayerLocal.prefab"));
            DrawStatus("PlayerRemote.prefab", File.Exists($"{PrefabsPath}/PlayerRemote.prefab"));
            DrawStatus("MobPrefab.prefab", File.Exists($"{PrefabsPath}/MobPrefab.prefab"));
            DrawStatus("LootDropPrefab.prefab", File.Exists($"{PrefabsPath}/LootDropPrefab.prefab"));
            DrawStatus("DamagePopupPrefab.prefab", File.Exists($"{PrefabsPath}/DamagePopupPrefab.prefab"));
            DrawStatus("InventorySlotPrefab.prefab", File.Exists($"{PrefabsPath}/InventorySlotPrefab.prefab"));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Setup All Prefabs", GUILayout.Height(30)))
            {
                _log.Clear();
                RunPrefabSetup();
            }
        }

        void RunPrefabSetup()
        {
            Log("> Creating prefabs...");
            EnsureDir(PrefabsPath);

            CreateCharacterPrefab("PlayerLocal", true);
            CreateCharacterPrefab("PlayerRemote", false);
            CreateMobPrefab();
            CreateLootDropPrefab();
            CreateDamagePopupPrefab();
            CreateInventorySlotPrefab();

            AssetDatabase.Refresh();
            Log("  All prefabs done.");
        }

        void CreateCharacterPrefab(string name, bool isLocal)
        {
            string path = $"{PrefabsPath}/{name}.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var root = new GameObject(name);

            // Character skeleton child
            var charGo = new GameObject("Character");
            charGo.transform.SetParent(root.transform);
            charGo.transform.localPosition = Vector3.zero;

            AddSpineComponents(charGo, name);

            // Game scripts on root
            AddComponentSafe(root, "SpumOnline.CharacterVisualSync");
            if (isLocal)
            {
                AddComponentSafe(root, "SpumOnline.LocalPlayerController");
                AddComponentSafe(root, "SpumOnline.SkillController");
            }
            else
            {
                AddComponentSafe(root, "SpumOnline.RemotePlayerController");
            }

            // Collider
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 1.2f);
            col.offset = new Vector2(0f, 0.6f);

            // Name label
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(root.transform);
            labelGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 3;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = isLocal ? "You" : "Player";
            tmp.sortingOrder = 50;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log($"  + {name}.prefab");
        }

        void CreateMobPrefab()
        {
            string path = $"{PrefabsPath}/MobPrefab.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var root = new GameObject("MobPrefab");

            var charGo = new GameObject("Character");
            charGo.transform.SetParent(root.transform);
            charGo.transform.localPosition = Vector3.zero;
            AddSpineComponents(charGo, "MobPrefab");

            AddComponentSafe(root, "SpumOnline.NPC.MobController");

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 1.0f);
            col.offset = new Vector2(0f, 0.5f);

            // HP Bar (World Space Canvas)
            var canvasGo = new GameObject("HPBarCanvas");
            canvasGo.transform.SetParent(root.transform);
            canvasGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            var canvasRT = canvasGo.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(1f, 0.1f);
            canvasRT.localScale = Vector3.one * 0.01f;
            var hpSliderGo = CreateWorldSlider(canvasGo.transform, new Color(0.8f, 0.1f, 0.1f));

            // Name label
            var nameGo = new GameObject("NameLabel");
            nameGo.transform.SetParent(root.transform);
            nameGo.transform.localPosition = new Vector3(0, 1.4f, 0);
            var nameTMP = nameGo.AddComponent<TextMeshPro>();
            nameTMP.fontSize = 2.5f;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.text = "Mob";
            nameTMP.sortingOrder = 50;

            // Wire MobController fields
            var mobType = FindType("SpumOnline.NPC.MobController");
            if (mobType != null)
            {
                var mobComp = root.GetComponent(mobType);
                if (mobComp != null)
                {
                    var so = new SerializedObject(mobComp);
                    SetField(so, "hpBarSlider", hpSliderGo.GetComponent<Slider>());
                    SetField(so, "hpBarRoot", canvasGo);
                    so.ApplyModifiedProperties();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log("  + MobPrefab.prefab");
        }

        void CreateLootDropPrefab()
        {
            string path = $"{PrefabsPath}/LootDropPrefab.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var root = new GameObject("LootDropPrefab");

            // Item sprite
            var itemSR = root.AddComponent<SpriteRenderer>();
            itemSR.sortingOrder = 5;

            // Glow sprite (child)
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform);
            glowGo.transform.localPosition = Vector3.zero;
            glowGo.transform.localScale = Vector3.one * 1.5f;
            var glowSR = glowGo.AddComponent<SpriteRenderer>();
            glowSR.sortingOrder = 4;
            glowSR.color = new Color(1f, 1f, 0.5f, 0.4f);

            // Name label
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(root.transform);
            labelGo.transform.localPosition = new Vector3(0, 0.5f, 0);
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 2;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "Item";
            tmp.sortingOrder = 50;

            // LootDrop script
            AddComponentSafe(root, "SpumOnline.NPC.LootDrop");

            // Wire references
            var lootType = FindType("SpumOnline.NPC.LootDrop");
            if (lootType != null)
            {
                var comp = root.GetComponent(lootType);
                var so = new SerializedObject(comp);
                SetField(so, "itemSprite", itemSR);
                SetField(so, "glowSprite", glowSR);
                SetField(so, "nameLabel", tmp);
                so.ApplyModifiedProperties();
            }

            // Collider for click pickup
            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log("  + LootDropPrefab.prefab");
        }

        void CreateDamagePopupPrefab()
        {
            string path = $"{PrefabsPath}/DamagePopupPrefab.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var root = new GameObject("DamagePopupPrefab");
            var tmp = root.AddComponent<TextMeshPro>();
            tmp.fontSize = 5;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "0";
            tmp.sortingOrder = 200;
            tmp.color = Color.red;

            AddComponentSafe(root, "SpumOnline.DamagePopup");

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log("  + DamagePopupPrefab.prefab");
        }

        void CreateInventorySlotPrefab()
        {
            string path = $"{PrefabsPath}/InventorySlotPrefab.prefab";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var root = new GameObject("InventorySlot");
            var rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64, 64);
            var bgImg = root.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.22f);

            // Icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(root.transform, false);
            var iconRT = iconGo.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(4, 4); iconRT.offsetMax = new Vector2(-4, -4);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = new Color(1, 1, 1, 0.1f);
            iconImg.raycastTarget = false;

            // Quantity text
            var qtyGo = new GameObject("Quantity");
            qtyGo.transform.SetParent(root.transform, false);
            var qtyRT = qtyGo.AddComponent<RectTransform>();
            qtyRT.anchorMin = new Vector2(1, 0); qtyRT.anchorMax = new Vector2(1, 0);
            qtyRT.pivot = new Vector2(1, 0);
            qtyRT.anchoredPosition = new Vector2(-2, 2);
            qtyRT.sizeDelta = new Vector2(40, 20);
            var qtyTMP = qtyGo.AddComponent<TextMeshProUGUI>();
            qtyTMP.fontSize = 12;
            qtyTMP.alignment = TextAlignmentOptions.BottomRight;
            qtyTMP.color = Color.white;
            qtyTMP.text = "";
            qtyTMP.raycastTarget = false;

            // InventorySlotUI script
            var slotType = FindType("SpumOnline.UI.InventorySlotUI");
            if (slotType != null)
            {
                var comp = root.AddComponent(slotType);
                var so = new SerializedObject(comp);
                SetField(so, "iconImage", iconImg);
                SetField(so, "quantityText", qtyTMP);
                so.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Log("  + InventorySlotPrefab.prefab");
        }

        // ===============================================================
        // TAB 2: Bootstrap
        // ===============================================================

        void DrawBootstrapTab()
        {
            EditorGUILayout.HelpBox(
                "Setup Bootstrap scene:\n" +
                "- GameManager (DontDestroyOnLoad) + wire all 5 prefabs\n" +
                "- Camera, EventSystem\n" +
                "- Loading UI (title + status)\n" +
                "- Auto-add to Build Settings index 0",
                MessageType.Info);

            DrawStatus("Bootstrap.unity", File.Exists($"{ScenesPath}/Bootstrap.unity"));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Setup Bootstrap Scene", GUILayout.Height(30)))
            {
                _log.Clear();
                RunBootstrapSetup();
            }
        }

        void RunBootstrapSetup()
        {
            Log("> Setting up Bootstrap scene...");

            string path = $"{ScenesPath}/Bootstrap.unity";
            EnsureDir(ScenesPath);

            if (!File.Exists(path))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, path);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ClearScene(scene);

            // Camera
            SetupCamera(new Vector3(0, 0, -10), 5, new Color(0.05f, 0.05f, 0.08f));

            // EventSystem
            EnsureEventSystem();

            // GameManager
            var gmGo = new GameObject("GameManager");
            var gmType = FindType("SpumOnline.GameManager");
            if (gmType != null)
            {
                var gmComp = gmGo.AddComponent(gmType);
                var so = new SerializedObject(gmComp);

                SetField(so, "localPlayerPrefab", LoadPrefab("PlayerLocal"));
                SetField(so, "remotePlayerPrefab", LoadPrefab("PlayerRemote"));
                SetField(so, "mobPrefab", LoadPrefab("MobPrefab"));
                SetField(so, "lootDropPrefab", LoadPrefab("LootDropPrefab"));
                SetField(so, "damagePopupPrefab", LoadPrefab("DamagePopupPrefab"));
                so.ApplyModifiedProperties();
                Log("    + GameManager with all 5 prefabs wired");
            }
            else
            {
                Log("    [WARN] GameManager type not found (need STDB_BINDINGS)");
            }

            // Loading UI Canvas
            var canvas = CreateScreenCanvas("LoadingCanvas", 100);

            var titleGo = CreateTMPLabel(canvas.transform, "TitleText", "SPUM Online", new Vector2(0, 80), 48);
            CenterAnchor(titleGo, new Vector2(600, 80));
            titleGo.GetComponent<TMP_Text>().color = new Color(0.8f, 0.9f, 1f);

            var statusGo = CreateTMPLabel(canvas.transform, "StatusText", "Connecting to server...", new Vector2(0, -20), 24);
            CenterAnchor(statusGo, new Vector2(600, 40));
            statusGo.GetComponent<TMP_Text>().color = new Color(0.6f, 0.6f, 0.7f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("  Bootstrap done.");
        }

        // ===============================================================
        // TAB 3: CharacterSelect
        // ===============================================================

        void DrawCharacterSelectTab()
        {
            EditorGUILayout.HelpBox(
                "Setup CharacterSelect scene:\n" +
                "- PreviewCharacter (SkeletonAnimation + PartsManager)\n" +
                "- 16 part cyclers: Skin, Eyes, Brow, Mouth, Hair, Hair Hat, Beard, Top, Bottom, Boots, Gloves, Helmet, Eyewear, Gear Left, Gear Right, Back\n" +
                "- 4 color pickers: Skin, Hair, Beard, Brow (RGB sliders)\n" +
                "- Random + Create buttons\n" +
                "- Wire all into CharacterCustomizeUI",
                MessageType.Info);

            DrawStatus("CharacterSelect.unity", File.Exists($"{ScenesPath}/CharacterSelect.unity"));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Setup CharacterSelect Scene", GUILayout.Height(30)))
            {
                _log.Clear();
                RunCharacterSelectSetup();
            }
        }

        void RunCharacterSelectSetup()
        {
            Log("> Setting up CharacterSelect scene...");

            string path = $"{ScenesPath}/CharacterSelect.unity";
            EnsureDir(ScenesPath);
            if (!File.Exists(path))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, path);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ClearScene(scene);

            SetupCamera(new Vector3(0, 0.5f, -10), 3, new Color(0.12f, 0.12f, 0.18f));
            EnsureEventSystem();

            // Preview Character
            var previewGo = new GameObject("PreviewCharacter");
            previewGo.transform.position = new Vector3(-2.5f, -0.5f, 0);
            previewGo.transform.localScale = Vector3.one * 1.5f;
            AddSpineComponents(previewGo, "PreviewCharacter");

            // UI Canvas
            var canvasGo = CreateScreenCanvas("UICanvas", 10);

            // Panel frame (right side, full height with scroll)
            var panelFrame = CreateUIElement(canvasGo.transform, "CustomizePanel", Vector2.zero);
            var panelFrameRT = panelFrame.GetComponent<RectTransform>();
            panelFrameRT.anchorMin = new Vector2(1, 0);
            panelFrameRT.anchorMax = new Vector2(1, 1);
            panelFrameRT.pivot = new Vector2(1, 0.5f);
            panelFrameRT.offsetMin = new Vector2(-540, 10);
            panelFrameRT.offsetMax = new Vector2(-10, -10);
            var panelFrameImg = panelFrame.AddComponent<Image>();
            panelFrameImg.color = new Color(0.08f, 0.08f, 0.14f, 0.92f);
            panelFrameImg.raycastTarget = true;
            var panelCG = panelFrame.AddComponent<CanvasGroup>();

            // Viewport (standard Unity ScrollView pattern: Image + RectMask2D)
            var viewportGo = CreateUIElement(panelFrame.transform, "Viewport", Vector2.zero);
            var viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = Color.clear;
            viewportImg.raycastTarget = true;
            viewportGo.AddComponent<RectMask2D>();

            // ScrollRect on the viewport's parent (panelFrame)
            var panelScrollRect = panelFrame.AddComponent<ScrollRect>();
            panelScrollRect.horizontal = false;
            panelScrollRect.vertical = true;
            panelScrollRect.movementType = ScrollRect.MovementType.Clamped;
            panelScrollRect.viewport = viewportRT;
            panelScrollRect.scrollSensitivity = 30f;

            // Content inside viewport (responsive: stretch horizontal, auto-height)
            var panelGo = new GameObject("Content");
            panelGo.transform.SetParent(viewportGo.transform, false);
            var panelGoRT = panelGo.AddComponent<RectTransform>();
            panelGoRT.anchorMin = new Vector2(0, 1);
            panelGoRT.anchorMax = new Vector2(1, 1);
            panelGoRT.pivot = new Vector2(0.5f, 1f);
            panelGoRT.anchoredPosition = Vector2.zero;
            panelGoRT.sizeDelta = new Vector2(0, 0); // width from anchors, height from CSF

            var contentVLG = panelGo.AddComponent<VerticalLayoutGroup>();
            contentVLG.padding = new RectOffset(14, 14, 14, 14);
            contentVLG.spacing = 6;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = true;
            contentVLG.childAlignment = TextAnchor.UpperCenter;

            var contentCSF = panelGo.AddComponent<ContentSizeFitter>();
            contentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panelScrollRect.content = panelGoRT;
            var panelRT = panelFrameRT;

            // --- Username ---
            var userLabelGo = LayoutLabel(panelGo.transform, "UsernameLabel", "TEN NHAN VAT", 20, 28);
            var usernameInput = CreateTMPInputField(panelGo.transform, "UsernameInput", Vector2.zero, new Vector2(0, 50));
            usernameInput.AddComponent<LayoutElement>().preferredHeight = 50;
            var validationText = LayoutLabel(panelGo.transform, "ValidationText", "", 14, 20);
            validationText.GetComponent<TMP_Text>().color = Color.red;

            // --- Part Cyclers (16 parts, responsive rows) ---
            string[] partNames = { "Skin", "Eyes", "Brow", "Mouth", "Hair Short", "Hair Hat", "Beard", "Top (Ao)", "Bottom (Quan)", "Boots (Giay)", "Gloves (Gang)", "Helmet (Mu)", "Eyewear (Kinh)", "Gear Left", "Gear Right", "Back (Lung)" };

            var prevButtons = new Button[partNames.Length];
            var nextButtons = new Button[partNames.Length];
            var indexTexts = new TMP_Text[partNames.Length];

            for (int i = 0; i < partNames.Length; i++)
            {
                // Each row: [ Label (flex) | < | 1/1 | > ]
                var row = LayoutRow(panelGo.transform, $"Row_{partNames[i]}", 34, 6);

                var lbl = new GameObject($"Label_{partNames[i]}");
                lbl.transform.SetParent(row.transform, false);
                lbl.AddComponent<RectTransform>();
                var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
                lblTmp.text = partNames[i]; lblTmp.fontSize = 14;
                lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
                lblTmp.color = Color.white;
                var lblLE = lbl.AddComponent<LayoutElement>();
                lblLE.flexibleWidth = 1;
                lblLE.minWidth = 100;

                var prevGo = CreateButton(row.transform, $"Prev_{partNames[i]}", "<", Vector2.zero, Vector2.zero);
                var prevLE = prevGo.AddComponent<LayoutElement>();
                prevLE.preferredWidth = 36; prevLE.preferredHeight = 30;

                var idxGo = new GameObject($"Index_{partNames[i]}");
                idxGo.transform.SetParent(row.transform, false);
                idxGo.AddComponent<RectTransform>();
                var idxTmp = idxGo.AddComponent<TextMeshProUGUI>();
                idxTmp.text = "1/1"; idxTmp.fontSize = 14;
                idxTmp.alignment = TextAlignmentOptions.Center;
                idxTmp.color = Color.white;
                var idxLE = idxGo.AddComponent<LayoutElement>();
                idxLE.preferredWidth = 55;

                var nextGo = CreateButton(row.transform, $"Next_{partNames[i]}", ">", Vector2.zero, Vector2.zero);
                var nextLE = nextGo.AddComponent<LayoutElement>();
                nextLE.preferredWidth = 36; nextLE.preferredHeight = 30;

                prevButtons[i] = prevGo.GetComponent<Button>();
                nextButtons[i] = nextGo.GetComponent<Button>();
                indexTexts[i] = idxGo.GetComponent<TMP_Text>();
            }

            // --- Color Picker ---
            LayoutSpacer(panelGo.transform, 6);
            LayoutLabel(panelGo.transform, "ColorSectionLabel", "MAU SAC", 18, 26);

            string[] colorNames = { "Da", "Toc", "Rau", "L.May" };
            Color[] defaultBtnColors = {
                new Color(1f, 0.85f, 0.72f),
                new Color(0.5f, 0.3f, 0.2f),
                new Color(0.5f, 0.3f, 0.2f),
                new Color(0.3f, 0.2f, 0.15f)
            };

            var colorBtnRow = LayoutRow(panelGo.transform, "ColorButtons", 34, 8);
            var colorButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var btnGo = CreateButton(colorBtnRow.transform, $"ColorBtn_{colorNames[i]}", colorNames[i], Vector2.zero, Vector2.zero);
                btnGo.GetComponent<Image>().color = defaultBtnColors[i];
                var btnLE = btnGo.AddComponent<LayoutElement>();
                btnLE.flexibleWidth = 1; btnLE.preferredHeight = 34;
                colorButtons[i] = btnGo.GetComponent<Button>();
            }

            var colorTargetLabel = LayoutLabel(panelGo.transform, "ColorTargetLabel", "Toc", 14, 20);

            // RGB sliders
            var colorSliders = new Slider[3];
            string[] rgbLabels = { "R", "G", "B" };
            for (int i = 0; i < 3; i++)
            {
                var sliderRow = LayoutRow(panelGo.transform, $"SliderRow_{rgbLabels[i]}", 26, 8);

                var sliderLbl = new GameObject($"Slider_{rgbLabels[i]}_Label");
                sliderLbl.transform.SetParent(sliderRow.transform, false);
                sliderLbl.AddComponent<RectTransform>();
                var slTmp = sliderLbl.AddComponent<TextMeshProUGUI>();
                slTmp.text = rgbLabels[i]; slTmp.fontSize = 14;
                slTmp.alignment = TextAlignmentOptions.MidlineLeft;
                slTmp.color = Color.white;
                var slLE = sliderLbl.AddComponent<LayoutElement>();
                slLE.preferredWidth = 28;

                var sliderGo = CreateSlider(sliderRow.transform, $"Slider_{rgbLabels[i]}", Vector2.zero, Vector2.zero);
                var sliderLE = sliderGo.AddComponent<LayoutElement>();
                sliderLE.flexibleWidth = 1; sliderLE.preferredHeight = 22;
                colorSliders[i] = sliderGo.GetComponent<Slider>();
                colorSliders[i].value = i == 0 ? 0.5f : (i == 1 ? 0.3f : 0.2f);
            }

            // Color preview (small box at end of sliders section)
            var colorPreviewGo = new GameObject("ColorPreview");
            colorPreviewGo.transform.SetParent(panelGo.transform, false);
            colorPreviewGo.AddComponent<RectTransform>();
            var colorPreviewImg = colorPreviewGo.AddComponent<Image>();
            colorPreviewImg.color = new Color(0.5f, 0.3f, 0.2f);
            var cpLE = colorPreviewGo.AddComponent<LayoutElement>();
            cpLE.preferredHeight = 32;

            // --- Action Buttons ---
            LayoutSpacer(panelGo.transform, 6);
            var actionRow = LayoutRow(panelGo.transform, "ActionButtons", 42, 12);

            var randomBtnGo = CreateButton(actionRow.transform, "RandomButton", "NGAU NHIEN", Vector2.zero, Vector2.zero);
            randomBtnGo.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f);
            var randLE = randomBtnGo.AddComponent<LayoutElement>();
            randLE.flexibleWidth = 1; randLE.preferredHeight = 42;

            var createBtnGo = CreateButton(actionRow.transform, "CreateButton", "TAO NHAN VAT", Vector2.zero, Vector2.zero);
            createBtnGo.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);
            var createLE = createBtnGo.AddComponent<LayoutElement>();
            createLE.flexibleWidth = 1; createLE.preferredHeight = 42;

            var statusText = LayoutLabel(panelGo.transform, "StatusText", "", 14, 22);

            // --- Wire CharacterCustomizeUI ---
            var customizeUIType = FindType("SpumOnline.UI.CharacterCustomizeUI");
            if (customizeUIType != null)
            {
                var uiComp = canvasGo.AddComponent(customizeUIType);
                var so = new SerializedObject(uiComp);

                SetField(so, "usernameInput", usernameInput.GetComponent<TMP_InputField>());
                SetField(so, "usernameValidationText", validationText.GetComponent<TMP_Text>());

                var pmType = FindType("LayerLab.ArtMaker.PartsManager");
                if (pmType != null) SetField(so, "_previewParts", previewGo.GetComponent(pmType));

                // 16 part cyclers
                string[] partFieldPrefixes = { "skin", "eyes", "brow", "mouth", "hair", "hairHat", "beard", "top", "bottom", "boots", "gloves", "helmet", "eyewear", "gearLeft", "gearRight", "back" };
                for (int i = 0; i < partFieldPrefixes.Length; i++)
                {
                    SetField(so, $"{partFieldPrefixes[i]}Prev", prevButtons[i]);
                    SetField(so, $"{partFieldPrefixes[i]}Next", nextButtons[i]);
                    SetField(so, $"{partFieldPrefixes[i]}IndexText", indexTexts[i]);
                }

                // Color
                SetField(so, "skinColorButton", colorButtons[0]);
                SetField(so, "hairColorButton", colorButtons[1]);
                SetField(so, "beardColorButton", colorButtons[2]);
                SetField(so, "browColorButton", colorButtons[3]);
                SetField(so, "colorR", colorSliders[0]);
                SetField(so, "colorG", colorSliders[1]);
                SetField(so, "colorB", colorSliders[2]);
                SetField(so, "colorPreview", colorPreviewImg);
                SetField(so, "colorTargetLabel", colorTargetLabel.GetComponent<TMP_Text>());

                // Actions
                SetField(so, "createButton", createBtnGo.GetComponent<Button>());
                SetField(so, "randomButton", randomBtnGo.GetComponent<Button>());
                SetField(so, "statusText", statusText.GetComponent<TMP_Text>());
                SetField(so, "panelCanvasGroup", panelCG);
                SetField(so, "panelRect", panelRT);

                so.ApplyModifiedProperties();
                Log("    + CharacterCustomizeUI wired (16 parts + 4 colors)");
            }
            else
            {
                Log("    [WARN] CharacterCustomizeUI type not found (need STDB_BINDINGS)");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("  CharacterSelect done.");
        }

        // ===============================================================
        // TAB 4: GameWorld
        // ===============================================================

        void DrawGameWorldTab()
        {
            EditorGUILayout.HelpBox(
                "Setup GameWorld scene:\n" +
                "- Camera, Ground, Boundary walls, SpawnPoint\n" +
                "- PlayerSpawner, MobSpawner, LootSpawner\n" +
                "- HUD: HP/MP/EXP bars, Level text, 4 skill slots, Target panel (all wired)\n" +
                "- ChatUI: ScrollRect, messages, input, send (all wired)\n" +
                "- InventoryUI: Grid + equipment + context menu + tooltip (all wired)\n" +
                "- CombatManager",
                MessageType.Info);

            DrawStatus("GameWorld.unity", File.Exists($"{ScenesPath}/GameWorld.unity"));

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Setup GameWorld Scene", GUILayout.Height(30)))
            {
                _log.Clear();
                RunGameWorldSetup();
            }
        }

        void RunGameWorldSetup()
        {
            Log("> Setting up GameWorld scene...");

            string path = $"{ScenesPath}/GameWorld.unity";
            EnsureDir(ScenesPath);
            if (!File.Exists(path))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, path);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            ClearScene(scene);

            SetupCamera(new Vector3(0, 0, -10), 6, new Color(0.15f, 0.2f, 0.12f));
            EnsureEventSystem();

            // Ground
            var groundGo = new GameObject("Ground");
            var groundSR = groundGo.AddComponent<SpriteRenderer>();
            groundSR.color = new Color(0.25f, 0.35f, 0.2f);
            groundSR.sortingOrder = -10;
            var tex = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            groundSR.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1);
            groundGo.transform.localScale = new Vector3(30, 20, 1);

            // Boundary walls
            CreateBoundaryWall("Wall_Top", new Vector3(0, 10, 0), new Vector2(30, 1));
            CreateBoundaryWall("Wall_Bottom", new Vector3(0, -10, 0), new Vector2(30, 1));
            CreateBoundaryWall("Wall_Left", new Vector3(-15, 0, 0), new Vector2(1, 20));
            CreateBoundaryWall("Wall_Right", new Vector3(15, 0, 0), new Vector2(1, 20));

            // Spawners
            var spawnersGo = new GameObject("[Spawners]");
            var psGo = new GameObject("PlayerSpawner"); psGo.transform.SetParent(spawnersGo.transform);
            AddComponentSafe(psGo, "SpumOnline.PlayerSpawner");
            var msGo = new GameObject("MobSpawner"); msGo.transform.SetParent(spawnersGo.transform);
            AddComponentSafe(msGo, "SpumOnline.NPC.MobSpawner");
            var lsGo = new GameObject("LootSpawner"); lsGo.transform.SetParent(spawnersGo.transform);
            AddComponentSafe(lsGo, "SpumOnline.NPC.LootSpawner");
            Log("    + Spawners (Player, Mob, Loot)");

            // CombatManager
            var combatGo = new GameObject("CombatManager");
            combatGo.transform.SetParent(spawnersGo.transform);
            AddComponentSafe(combatGo, "SpumOnline.CombatManager");
            Log("    + CombatManager");

            // ===== HUD Canvas =====
            var hudCanvasGo = CreateScreenCanvas("HUDCanvas", 50);
            SetupHUD(hudCanvasGo);

            // ===== Chat Canvas =====
            SetupChatUI(hudCanvasGo);

            // ===== Inventory =====
            SetupInventoryUI(hudCanvasGo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("  GameWorld done.");
        }

        // -------------------------------------------------------
        // HUD Setup
        // -------------------------------------------------------

        void SetupHUD(GameObject canvasGo)
        {
            var hudType = FindType("SpumOnline.UI.HUD");
            if (hudType == null) { Log("    [WARN] HUD type not found"); return; }

            var hudComp = canvasGo.AddComponent(hudType);
            var so = new SerializedObject(hudComp);

            // --- HP Bar (top-left) ---
            var hpBarGo = CreateHUDSlider(canvasGo.transform, "HPBar", new Color(0.8f, 0.15f, 0.15f),
                new Vector2(20, -20), new Vector2(300, 28), out Slider hpSlider, out TMP_Text hpText, out Image hpFill);
            SetField(so, "hpBar", hpSlider);
            SetField(so, "hpText", hpText);
            SetField(so, "hpFillImage", hpFill);

            // --- MP Bar ---
            var mpBarGo = CreateHUDSlider(canvasGo.transform, "MPBar", new Color(0.15f, 0.3f, 0.8f),
                new Vector2(20, -55), new Vector2(250, 22), out Slider mpSlider, out TMP_Text mpText, out Image mpFill);
            SetField(so, "mpBar", mpSlider);
            SetField(so, "mpText", mpText);
            SetField(so, "mpFillImage", mpFill);

            // --- Level Text ---
            var levelGo = CreateTMPLabel(canvasGo.transform, "LevelText", "Lv. 1", new Vector2(20, -85), 16);
            var levelRT = levelGo.GetComponent<RectTransform>();
            levelRT.anchorMin = new Vector2(0, 1); levelRT.anchorMax = new Vector2(0, 1);
            levelRT.pivot = new Vector2(0, 1);
            SetField(so, "levelText", levelGo.GetComponent<TMP_Text>());

            // --- EXP Bar ---
            var expBarGo = CreateHUDSlider(canvasGo.transform, "EXPBar", new Color(0.8f, 0.7f, 0.1f),
                new Vector2(20, -108), new Vector2(200, 16), out Slider expSlider, out TMP_Text expText, out _);
            SetField(so, "expBar", expSlider);
            SetField(so, "expText", expText);

            // --- Skill Bar (bottom-center) ---
            var skillBar = CreateUIElement(canvasGo.transform, "SkillBar", new Vector2(400, 74));
            var skillBarRT = skillBar.GetComponent<RectTransform>();
            skillBarRT.anchorMin = new Vector2(0.5f, 0); skillBarRT.anchorMax = new Vector2(0.5f, 0);
            skillBarRT.pivot = new Vector2(0.5f, 0);
            skillBarRT.anchoredPosition = new Vector2(0, 20);
            skillBar.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // Set arrays
            var skillIconsProp = so.FindProperty("skillIcons");
            var skillCDOverlaysProp = so.FindProperty("skillCooldownOverlays");
            var skillCDTextsProp = so.FindProperty("skillCooldownTexts");
            var skillKeyLabelsProp = so.FindProperty("skillKeyLabels");

            if (skillIconsProp != null) skillIconsProp.arraySize = 4;
            if (skillCDOverlaysProp != null) skillCDOverlaysProp.arraySize = 4;
            if (skillCDTextsProp != null) skillCDTextsProp.arraySize = 4;
            if (skillKeyLabelsProp != null) skillKeyLabelsProp.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                float x = -135 + i * 90;
                var slotGo = CreateUIElement(skillBar.transform, $"Skill_{i + 1}", new Vector2(70, 70));
                slotGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0);

                // Icon
                var iconImg = slotGo.AddComponent<Image>();
                iconImg.color = new Color(0.2f, 0.2f, 0.3f);

                // Cooldown overlay
                var cdOverlayGo = CreateUIElement(slotGo.transform, "CDOverlay", new Vector2(70, 70));
                var cdOverlayRT = cdOverlayGo.GetComponent<RectTransform>();
                cdOverlayRT.anchorMin = Vector2.zero; cdOverlayRT.anchorMax = Vector2.one;
                cdOverlayRT.offsetMin = Vector2.zero; cdOverlayRT.offsetMax = Vector2.zero;
                var cdImg = cdOverlayGo.AddComponent<Image>();
                cdImg.color = new Color(0, 0, 0, 0.6f);
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.type = Image.Type.Filled;
                cdOverlayGo.SetActive(false);

                // Cooldown text
                var cdTextGo = CreateTMPLabel(slotGo.transform, "CDText", "", new Vector2(0, 0), 14);
                var cdTextRT = cdTextGo.GetComponent<RectTransform>();
                cdTextRT.anchorMin = Vector2.zero; cdTextRT.anchorMax = Vector2.one;
                cdTextRT.offsetMin = Vector2.zero; cdTextRT.offsetMax = Vector2.zero;
                cdTextGo.SetActive(false);

                // Key label
                var keyGo = CreateTMPLabel(slotGo.transform, "KeyLabel", $"{i + 1}", new Vector2(0, -25), 12);

                if (skillIconsProp != null) skillIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconImg;
                if (skillCDOverlaysProp != null) skillCDOverlaysProp.GetArrayElementAtIndex(i).objectReferenceValue = cdImg;
                if (skillCDTextsProp != null) skillCDTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = cdTextGo.GetComponent<TMP_Text>();
                if (skillKeyLabelsProp != null) skillKeyLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = keyGo.GetComponent<TMP_Text>();
            }

            // --- Target Panel (top-right) ---
            var targetPanel = CreateUIElement(canvasGo.transform, "TargetPanel", new Vector2(260, 80));
            var targetPanelRT = targetPanel.GetComponent<RectTransform>();
            targetPanelRT.anchorMin = new Vector2(1, 1); targetPanelRT.anchorMax = new Vector2(1, 1);
            targetPanelRT.pivot = new Vector2(1, 1);
            targetPanelRT.anchoredPosition = new Vector2(-20, -20);
            targetPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            targetPanel.SetActive(false);

            var targetNameGo = CreateTMPLabel(targetPanel.transform, "TargetName", "Target", new Vector2(0, 18), 16);
            var targetHPGo = CreateHUDSlider(targetPanel.transform, "TargetHP", new Color(0.8f, 0.15f, 0.15f),
                new Vector2(10, -15), new Vector2(240, 20), out Slider targetHpSlider, out TMP_Text targetHpText, out _);

            SetField(so, "targetPanel", targetPanel);
            SetField(so, "targetNameText", targetNameGo.GetComponent<TMP_Text>());
            SetField(so, "targetHpBar", targetHpSlider);
            SetField(so, "targetHpText", targetHpText);

            so.ApplyModifiedProperties();
            Log("    + HUD fully wired (HP, MP, EXP, Level, Skills[4], Target)");
        }

        // -------------------------------------------------------
        // ChatUI Setup
        // -------------------------------------------------------

        void SetupChatUI(GameObject canvasGo)
        {
            var chatType = FindType("SpumOnline.UI.ChatUI");
            if (chatType == null) { Log("    [WARN] ChatUI type not found"); return; }

            // Chat container (bottom-left)
            var chatRoot = CreateUIElement(canvasGo.transform, "ChatUI", new Vector2(420, 220));
            var chatRootRT = chatRoot.GetComponent<RectTransform>();
            chatRootRT.anchorMin = new Vector2(0, 0); chatRootRT.anchorMax = new Vector2(0, 0);
            chatRootRT.pivot = new Vector2(0, 0);
            chatRootRT.anchoredPosition = new Vector2(15, 100);
            chatRoot.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.7f);

            // Scroll area
            var scrollGo = CreateUIElement(chatRoot.transform, "ScrollArea", Vector2.zero);
            var scrollRT = scrollGo.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(5, 40); scrollRT.offsetMax = new Vector2(-5, -5);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollGo.AddComponent<Image>().color = Color.clear;
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGo = CreateUIElement(scrollGo.transform, "Content", Vector2.zero);
            var contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 0); contentRT.anchorMax = new Vector2(1, 0);
            contentRT.pivot = new Vector2(0, 0);
            contentRT.offsetMin = Vector2.zero; contentRT.offsetMax = Vector2.zero;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRT;

            // Message display
            var msgGo = new GameObject("MessageDisplay");
            msgGo.transform.SetParent(contentGo.transform, false);
            var msgRT = msgGo.AddComponent<RectTransform>();
            msgRT.anchorMin = Vector2.zero; msgRT.anchorMax = new Vector2(1, 0);
            msgRT.pivot = new Vector2(0, 0);
            msgRT.offsetMin = Vector2.zero; msgRT.offsetMax = Vector2.zero;
            var msgTMP = msgGo.AddComponent<TextMeshProUGUI>();
            msgTMP.fontSize = 13;
            msgTMP.color = Color.white;
            msgTMP.enableWordWrapping = true;
            msgTMP.overflowMode = TextOverflowModes.Overflow;

            // Input row
            var inputGo = CreateTMPInputField(chatRoot.transform, "ChatInput", new Vector2(0, 0), new Vector2(340, 32), "Nhap tin nhan...", 200);
            var inputRT = inputGo.GetComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0); inputRT.anchorMax = new Vector2(0, 0);
            inputRT.pivot = new Vector2(0, 0);
            inputRT.anchoredPosition = new Vector2(5, 5);

            var sendGo = CreateButton(chatRoot.transform, "SendButton", "Gui", new Vector2(0, 0), new Vector2(60, 32));
            var sendRT = sendGo.GetComponent<RectTransform>();
            sendRT.anchorMin = new Vector2(1, 0); sendRT.anchorMax = new Vector2(1, 0);
            sendRT.pivot = new Vector2(1, 0);
            sendRT.anchoredPosition = new Vector2(-5, 5);
            sendGo.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f);

            // Wire ChatUI
            var chatComp = chatRoot.AddComponent(chatType);
            var so = new SerializedObject(chatComp);
            SetField(so, "scrollRect", scrollRect);
            SetField(so, "messageDisplay", msgTMP);
            SetField(so, "contentRect", contentRT);
            SetField(so, "chatInput", inputGo.GetComponent<TMP_InputField>());
            SetField(so, "sendButton", sendGo.GetComponent<Button>());
            so.ApplyModifiedProperties();

            Log("    + ChatUI fully wired");
        }

        // -------------------------------------------------------
        // InventoryUI Setup
        // -------------------------------------------------------

        void SetupInventoryUI(GameObject canvasGo)
        {
            var invType = FindType("SpumOnline.UI.InventoryUI");
            if (invType == null) { Log("    [WARN] InventoryUI type not found"); return; }

            // Inventory panel (center, hidden by default)
            var invPanel = CreateUIElement(canvasGo.transform, "InventoryPanel", new Vector2(500, 550));
            CenterAnchor(invPanel, new Vector2(500, 550));
            var invPanelImg = invPanel.AddComponent<Image>();
            invPanelImg.color = new Color(0.08f, 0.08f, 0.14f, 0.95f);
            var invPanelCG = invPanel.AddComponent<CanvasGroup>();
            invPanel.SetActive(false);

            // Title
            CreateTMPLabel(invPanel.transform, "InvTitle", "INVENTORY", new Vector2(0, 245), 22);

            // Slot container (grid)
            var slotContainer = CreateUIElement(invPanel.transform, "SlotContainer", new Vector2(420, 350));
            slotContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 10);
            var gridLayout = slotContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(64, 64);
            gridLayout.spacing = new Vector2(4, 4);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 6;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            // Equipment section
            CreateTMPLabel(invPanel.transform, "EquipTitle", "EQUIPMENT", new Vector2(0, -180), 16);

            var equipLabels = new string[] { "Weapon", "Armor", "Helmet" };
            var equipSlots = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                float x = -120 + i * 120;
                CreateTMPLabel(invPanel.transform, $"Equip_{equipLabels[i]}_Label", equipLabels[i], new Vector2(x, -200), 12);
                equipSlots[i] = CreateUIElement(invPanel.transform, $"Equip_{equipLabels[i]}", new Vector2(64, 64));
                equipSlots[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(x, -240);
                equipSlots[i].AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);

                // Add InventorySlotUI
                var slotUIType = FindType("SpumOnline.UI.InventorySlotUI");
                if (slotUIType != null) equipSlots[i].AddComponent(slotUIType);
            }

            // Context menu
            var ctxMenu = CreateUIElement(canvasGo.transform, "ContextMenu", new Vector2(160, 130));
            ctxMenu.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
            ctxMenu.SetActive(false);
            var ctxNameGo = CreateTMPLabel(ctxMenu.transform, "CtxItemName", "Item", new Vector2(0, 42), 14);
            var ctxEquipGo = CreateButton(ctxMenu.transform, "CtxEquip", "Equip", new Vector2(0, 12), new Vector2(140, 28));
            var ctxDropGo = CreateButton(ctxMenu.transform, "CtxDrop", "Drop", new Vector2(0, -18), new Vector2(140, 28));
            var ctxUseGo = CreateButton(ctxMenu.transform, "CtxUse", "Use", new Vector2(0, -48), new Vector2(140, 28));

            // Tooltip
            var tooltipGo = CreateUIElement(canvasGo.transform, "TooltipPanel", new Vector2(220, 160));
            tooltipGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            tooltipGo.SetActive(false);
            var ttNameGo = CreateTMPLabel(tooltipGo.transform, "TTName", "Item Name", new Vector2(0, 55), 16);
            var ttDescGo = CreateTMPLabel(tooltipGo.transform, "TTDesc", "Description", new Vector2(0, 20), 12);
            ttDescGo.GetComponent<TMP_Text>().color = new Color(0.7f, 0.7f, 0.7f);
            var ttStatsGo = CreateTMPLabel(tooltipGo.transform, "TTStats", "ATK +5", new Vector2(0, -20), 13);
            ttStatsGo.GetComponent<TMP_Text>().color = new Color(0.5f, 1f, 0.5f);

            // Wire InventoryUI
            var invComp = canvasGo.AddComponent(invType);
            var so = new SerializedObject(invComp);
            SetField(so, "inventoryPanel", invPanel);
            SetField(so, "panelCanvasGroup", invPanelCG);
            SetField(so, "slotContainer", slotContainer.transform);
            SetField(so, "slotPrefab", LoadPrefab("InventorySlotPrefab"));

            // Equipment slots
            var slotUIType2 = FindType("SpumOnline.UI.InventorySlotUI");
            if (slotUIType2 != null)
            {
                SetField(so, "weaponSlot", equipSlots[0].GetComponent(slotUIType2));
                SetField(so, "armorSlot", equipSlots[1].GetComponent(slotUIType2));
                SetField(so, "helmetSlot", equipSlots[2].GetComponent(slotUIType2));
            }

            SetField(so, "contextMenu", ctxMenu);
            SetField(so, "contextEquipButton", ctxEquipGo.GetComponent<Button>());
            SetField(so, "contextDropButton", ctxDropGo.GetComponent<Button>());
            SetField(so, "contextUseButton", ctxUseGo.GetComponent<Button>());
            SetField(so, "contextItemName", ctxNameGo.GetComponent<TMP_Text>());
            SetField(so, "tooltipPanel", tooltipGo);
            SetField(so, "tooltipNameText", ttNameGo.GetComponent<TMP_Text>());
            SetField(so, "tooltipDescText", ttDescGo.GetComponent<TMP_Text>());
            SetField(so, "tooltipStatsText", ttStatsGo.GetComponent<TMP_Text>());

            so.ApplyModifiedProperties();
            Log("    + InventoryUI fully wired (grid + 3 equip slots + context + tooltip)");
        }

        // ===============================================================
        // Build Settings
        // ===============================================================

        void SetupBuildSettings()
        {
            Log("> Setting up Build Settings...");
            var scenes = new List<EditorBuildSettingsScene>();
            string[] order = { "Bootstrap", "CharacterSelect", "GameWorld" };
            foreach (var s in order)
            {
                string p = $"{ScenesPath}/{s}.unity";
                if (File.Exists(p))
                {
                    scenes.Add(new EditorBuildSettingsScene(p, true));
                    Log($"    [{scenes.Count - 1}] {s}");
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Log("  Build Settings updated.");
        }

        // ===============================================================
        // UI Builders (shared)
        // ===============================================================

        // ===============================================================
        // Layout Helpers (responsive UI)
        // ===============================================================

        /// <summary>Create a horizontal layout row with fixed height.</summary>
        GameObject LayoutRow(Transform parent, string name, float height, float spacing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return go;
        }

        /// <summary>Create a TMP label that participates in vertical layout.</summary>
        GameObject LayoutLabel(Transform parent, string name, string text, float fontSize, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            return go;
        }

        /// <summary>Invisible spacer for vertical layout.</summary>
        void LayoutSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        void AddSpineComponents(GameObject go, string label)
        {
            var skelType = FindType("Spine.Unity.SkeletonAnimation");
            if (skelType != null)
            {
                var skelComp = go.AddComponent(skelType);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sortingOrder = 10;

                // Auto-assign SkeletonDataAsset
                var skelDataAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SkeletonDataPath);
                if (skelDataAsset != null)
                {
                    var so = new SerializedObject(skelComp);
                    var skelDataProp = so.FindProperty("skeletonDataAsset");
                    if (skelDataProp != null)
                    {
                        skelDataProp.objectReferenceValue = skelDataAsset;
                        so.ApplyModifiedProperties();
                        Log($"    + SkeletonDataAsset assigned for {label}");
                    }
                }
                else
                {
                    Log($"    [WARN] SkeletonDataAsset not found at {SkeletonDataPath}");
                }
            }
            else
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 10;
                sr.color = new Color(0.5f, 0.8f, 1f);
                Log($"    [WARN] Spine not found, using placeholder for {label}");
            }

            var pmType = FindType("LayerLab.ArtMaker.PartsManager");
            if (pmType != null)
            {
                var pmComp = go.AddComponent(pmType);

                // Wire PartsManager.skeletonAnimation reference
                if (skelType != null)
                {
                    var skelOnGo = go.GetComponent(skelType);
                    if (skelOnGo != null)
                    {
                        var pmSO = new SerializedObject(pmComp);
                        SetField(pmSO, "skeletonAnimation", skelOnGo);
                        pmSO.ApplyModifiedProperties();
                    }
                }
            }
        }

        GameObject CreateScreenCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        GameObject CreateUIElement(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = size;
            return go;
        }

        GameObject CreateTMPLabel(Transform parent, string name, string text, Vector2 pos, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(200, 30);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        GameObject CreateTMPInputField(Transform parent, string name, Vector2 pos, Vector2 size, string placeholder = "Nhap ten...", int charLimit = 20)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f);

            var textArea = new GameObject("Text Area");
            textArea.transform.SetParent(go.transform, false);
            var taRT = textArea.AddComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(10, 5); taRT.offsetMax = new Vector2(-10, -5);
            textArea.AddComponent<RectMask2D>();

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(textArea.transform, false);
            var phRT = phGo.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
            var phTMP = phGo.AddComponent<TextMeshProUGUI>();
            phTMP.text = placeholder; phTMP.fontSize = 18;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            var itGo = new GameObject("Text");
            itGo.transform.SetParent(textArea.transform, false);
            var itRT = itGo.AddComponent<RectTransform>();
            itRT.anchorMin = Vector2.zero; itRT.anchorMax = Vector2.one;
            itRT.offsetMin = Vector2.zero; itRT.offsetMax = Vector2.zero;
            var itTMP = itGo.AddComponent<TextMeshProUGUI>();
            itTMP.fontSize = 18; itTMP.color = Color.white;

            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textViewport = taRT;
            inputField.textComponent = itTMP;
            inputField.placeholder = phTMP;
            inputField.characterLimit = charLimit;
            return go;
        }

        GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.35f);
            go.AddComponent<Button>().targetGraphic = img;

            var tGo = new GameObject("Text");
            tGo.transform.SetParent(go.transform, false);
            var tRT = tGo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        GameObject CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            var faGo = new GameObject("Fill Area");
            faGo.transform.SetParent(go.transform, false);
            var faRT = faGo.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(faGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            fillGo.AddComponent<Image>().color = Color.white;
            slider.fillRect = fillRT;
            return go;
        }

        GameObject CreateHUDSlider(Transform parent, string name, Color fillColor, Vector2 pos, Vector2 size,
            out Slider slider, out TMP_Text valueText, out Image fillImage)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1;
            slider.interactable = false;

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f);

            var faGo = new GameObject("Fill Area");
            faGo.transform.SetParent(go.transform, false);
            var faRT = faGo.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(faGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            fillImage = fillGo.AddComponent<Image>();
            fillImage.color = fillColor;
            slider.fillRect = fillRT;

            var textGo = new GameObject("Value");
            textGo.transform.SetParent(go.transform, false);
            var textRT = textGo.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 0); textRT.offsetMax = Vector2.zero;
            valueText = textGo.AddComponent<TextMeshProUGUI>();
            valueText.fontSize = 12; valueText.alignment = TextAlignmentOptions.Left;
            valueText.color = Color.white;

            return go;
        }

        GameObject CreateWorldSlider(Transform parent, Color fillColor)
        {
            var go = new GameObject("HPSlider");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.interactable = false;

            var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var fa = new GameObject("Fill Area"); fa.transform.SetParent(go.transform, false);
            var faRT = fa.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill"); fill.transform.SetParent(fa.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = fillColor;
            slider.fillRect = fillRT;
            return go;
        }

        // ===============================================================
        // Utility
        // ===============================================================

        void ClearScene(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.GetComponent<Camera>() != null) { Object.DestroyImmediate(go); continue; }
                if (go.name == "EventSystem") { Object.DestroyImmediate(go); continue; }
                Object.DestroyImmediate(go);
            }
        }

        void SetupCamera(Vector3 pos, float orthoSize, Color bg)
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.position = pos;
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.backgroundColor = bg;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        void CenterAnchor(GameObject go, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
        }

        void CreateBoundaryWall(string name, Vector3 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        void EnsureDir(string path) { if (!Directory.Exists(path)) Directory.CreateDirectory(path); }

        void Log(string msg) { _log.Add(msg); Debug.Log($"[SceneSetup] {msg}"); Repaint(); }

        void DrawStatus(string label, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ok ? "\u2705" : "\u274C", GUILayout.Width(20));
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/{name}.prefab");
        }

        void AddComponentSafe(GameObject go, string fullTypeName)
        {
            var type = FindType(fullTypeName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
                go.AddComponent(type);
            else
                Log($"    [WARN] Type '{fullTypeName}' not found");
        }

        System.Type FindType(string fullName)
        {
            return System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == fullName);
        }

        void SetField(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null && value != null) prop.objectReferenceValue = value;
        }
    }
}
#endif
