#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SpumOnline.Editor
{
    /// <summary>
    /// One-click setup for SPUM Online MMORPG (Layer Lab character system).
    /// Menu: BillGameCore > SPUM Online > Mega Setup
    ///
    /// What it does:
    ///   1. Creates all required scenes (Bootstrap, CharacterSelect, GameWorld)
    ///   2. Configures Build Settings with scenes in correct order
    ///   3. Creates prefabs (Player, Mob, Loot, DamagePopup)
    ///   4. Creates BillBootstrapConfig if missing
    ///   5. Sets up Bootstrap scene with BillStartup
    ///   6. Validates SpacetimeDB SDK is installed
    ///   7. Prints next steps (spacetime publish, generate bindings)
    /// </summary>
    public class SpumOnlineMegaSetup : EditorWindow
    {
        private Vector2 _scroll;
        private List<string> _log = new();
        private bool _running;

        // Status flags
        private bool _hasSpacetimeSDK;
        private bool _hasLayerLab;
        private bool _hasTMP;
        private bool _hasServerFolder;
        private bool _hasScenes;
        private bool _hasPrefabs;
        private bool _hasBootstrapConfig;

        const string ScenesPath = "Assets/Scenes";
        const string PrefabsPath = "Assets/Prefabs";
        const string ResourcesPath = "Assets/Resources";

        [MenuItem("BillGameCore/SPUM Online/Mega Setup", false, 0)]
        static void Open()
        {
            var w = GetWindow<SpumOnlineMegaSetup>("SPUM Online - Mega Setup");
            w.minSize = new Vector2(500, 600);
            w.Refresh();
        }

        [MenuItem("BillGameCore/SPUM Online/Validate Project", false, 1)]
        static void ValidateMenu()
        {
            var w = GetWindow<SpumOnlineMegaSetup>("SPUM Online - Mega Setup");
            w.Refresh();
        }

        void Refresh()
        {
            _hasSpacetimeSDK = File.Exists("Packages/manifest.json") &&
                File.ReadAllText("Packages/manifest.json").Contains("spacetimedbsdk");
            _hasLayerLab = Directory.Exists("Assets/Layer Lab");
            _hasTMP = true; // Built-in in Unity 6
            _hasServerFolder = Directory.Exists("server") &&
                File.Exists("server/StdbModule.csproj");
            _hasScenes = File.Exists($"{ScenesPath}/Bootstrap.unity") &&
                File.Exists($"{ScenesPath}/CharacterSelect.unity") &&
                File.Exists($"{ScenesPath}/GameWorld.unity");
            _hasPrefabs = File.Exists($"{PrefabsPath}/PlayerLocal.prefab") &&
                File.Exists($"{PrefabsPath}/MobPrefab.prefab");
            _hasBootstrapConfig = Resources.Load("BillBootstrapConfig") != null;
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("SPUM Online - Project Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Status panel
            EditorGUILayout.BeginVertical("box");
            DrawStatus("SpacetimeDB SDK", _hasSpacetimeSDK);
            DrawStatus("Layer Lab 2D Art Maker", _hasLayerLab);
            DrawStatus("TextMeshPro", _hasTMP);
            DrawStatus("Server Module (server/)", _hasServerFolder);
            DrawStatus("Scenes (Bootstrap, CharacterSelect, GameWorld)", _hasScenes);
            DrawStatus("Prefabs (Player, Mob, Loot, DamagePopup)", _hasPrefabs);
            DrawStatus("BillBootstrapConfig", _hasBootstrapConfig);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Action buttons
            GUI.enabled = !_running;

            if (GUILayout.Button("Run Full Setup", GUILayout.Height(36)))
                RunFullSetup();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Scenes Only"))
                RunStep(CreateScenes);
            if (GUILayout.Button("Create Prefabs Only"))
                RunStep(CreatePrefabs);
            if (GUILayout.Button("Fix Build Settings"))
                RunStep(SetupBuildSettings);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Setup Bootstrap Scene"))
                RunStep(SetupBootstrapScene);
            if (GUILayout.Button("Enable STDB_BINDINGS"))
                RunStep(EnableBindingsDefine);
            if (GUILayout.Button("Refresh Status"))
                Refresh();
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            // Log
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var line in _log)
            {
                if (line.StartsWith("[ERR"))
                    EditorGUILayout.HelpBox(line, MessageType.Error);
                else if (line.StartsWith("[WARN"))
                    EditorGUILayout.HelpBox(line, MessageType.Warning);
                else
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndScrollView();

            // Next steps
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Next Steps (run in terminal):", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                "1. spacetime publish --server local spum-online -p server\n" +
                "2. spacetime generate --lang csharp --out-dir Assets/module_bindings -p server\n" +
                "3. Click 'Enable STDB_BINDINGS' button above\n" +
                "4. Press Play in Unity!",
                EditorStyles.wordWrappedMiniLabel, GUILayout.Height(60));
            EditorGUILayout.EndVertical();
        }

        void DrawStatus(string label, bool ok)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ok ? "\u2705" : "\u274C", GUILayout.Width(20));
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════
        // Full Setup
        // ═══════════════════════════════════════════════════════

        void RunFullSetup()
        {
            _log.Clear();
            _running = true;

            Log("=== SPUM Online Mega Setup ===");

            // Validate prerequisites
            if (!_hasSpacetimeSDK)
            {
                Log("[ERROR] SpacetimeDB SDK not found. Install via Package Manager:\n" +
                    "  https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk.git");
                _running = false;
                return;
            }
            if (!_hasLayerLab)
            {
                Log("[WARN] Layer Lab 2D Art Maker not found in Assets/Layer Lab. Character visuals won't work without it.");
            }

            CreateFolders();
            CreateScenes();
            CreatePrefabs();
            SetupBuildSettings();
            SetupBootstrapScene();
            CreateBootstrapConfig();

            Log("=== Setup Complete! ===");
            Log("Now run in terminal:");
            Log("  spacetime publish --server local spum-online -p server");
            Log("  spacetime generate --lang csharp --out-dir Assets/module_bindings -p server");

            AssetDatabase.Refresh();
            Refresh();
            _running = false;
        }

        void RunStep(System.Action action)
        {
            _log.Clear();
            _running = true;
            action();
            AssetDatabase.Refresh();
            Refresh();
            _running = false;
        }

        // ═══════════════════════════════════════════════════════
        // Create Folders
        // ═══════════════════════════════════════════════════════

        void CreateFolders()
        {
            Log("> Creating folders...");
            EnsureDir(ScenesPath);
            EnsureDir(PrefabsPath);
            EnsureDir(ResourcesPath);
            EnsureDir("Assets/Scripts/Core");
            EnsureDir("Assets/Scripts/Player");
            EnsureDir("Assets/Scripts/Combat");
            EnsureDir("Assets/Scripts/NPC");
            EnsureDir("Assets/Scripts/UI");
            EnsureDir("Assets/Scripts/Utility");
            Log("  Folders OK.");
        }

        // ═══════════════════════════════════════════════════════
        // Create Scenes
        // ═══════════════════════════════════════════════════════

        void CreateScenes()
        {
            Log("> Creating scenes...");

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Log("  Cancelled by user.");
                return;
            }

            CreateSceneIfMissing("Bootstrap");
            CreateSceneIfMissing("CharacterSelect");
            CreateSceneIfMissing("GameWorld");

            Log("  Scenes created.");
        }

        void CreateSceneIfMissing(string name)
        {
            string path = $"{ScenesPath}/{name}.unity";
            if (File.Exists(path))
            {
                Log($"  {name}.unity already exists, skipping.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            Log($"  Created {name}.unity");
        }

        // ═══════════════════════════════════════════════════════
        // Create Prefabs
        // ═══════════════════════════════════════════════════════

        void CreatePrefabs()
        {
            Log("> Creating prefabs...");

            CreatePlayerPrefab("PlayerLocal", true);
            CreatePlayerPrefab("PlayerRemote", false);
            CreateMobPrefab();
            CreateLootPrefab();
            CreateDamagePopupPrefab();

            Log("  Prefabs created.");
        }

        void CreatePlayerPrefab(string name, bool isLocal)
        {
            string path = $"{PrefabsPath}/{name}.prefab";
            if (File.Exists(path)) { Log($"  {name}.prefab exists, skipping."); return; }

            var go = new GameObject(name);

            // Note: Layer Lab character (SkeletonAnimation + PartsManager) should be
            // added as a child prefab in the Unity Editor after setup.

            // Add scripts
            if (isLocal)
            {
                AddComponentSafe(go, "SpumOnline.LocalPlayerController");
                AddComponentSafe(go, "SpumOnline.SkillController");
            }
            else
            {
                AddComponentSafe(go, "SpumOnline.RemotePlayerController");
            }
            AddComponentSafe(go, "SpumOnline.CharacterVisualSync");

            // Name label
            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            var tmp = labelGo.AddComponent<TMPro.TextMeshPro>();
            tmp.fontSize = 3;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.text = name;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Log($"  Created {name}.prefab");
        }

        void CreateMobPrefab()
        {
            string path = $"{PrefabsPath}/MobPrefab.prefab";
            if (File.Exists(path)) { Log("  MobPrefab.prefab exists, skipping."); return; }

            var go = new GameObject("MobPrefab");
            go.AddComponent<SpriteRenderer>().sortingOrder = 9;
            AddComponentSafe(go, "SpumOnline.MobController");

            // HP bar
            var hpGo = new GameObject("HPBar");
            hpGo.transform.SetParent(go.transform);
            hpGo.transform.localPosition = new Vector3(0, 1.0f, 0);

            // Name
            var nameGo = new GameObject("NameLabel");
            nameGo.transform.SetParent(go.transform);
            nameGo.transform.localPosition = new Vector3(0, 1.3f, 0);
            var tmp = nameGo.AddComponent<TMPro.TextMeshPro>();
            tmp.fontSize = 2.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.text = "Mob";

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Log("  Created MobPrefab.prefab");
        }

        void CreateLootPrefab()
        {
            string path = $"{PrefabsPath}/LootPrefab.prefab";
            if (File.Exists(path)) { Log("  LootPrefab.prefab exists, skipping."); return; }

            var go = new GameObject("LootPrefab");
            go.AddComponent<SpriteRenderer>().sortingOrder = 5;
            go.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
            AddComponentSafe(go, "SpumOnline.LootDropVisual");

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Log("  Created LootPrefab.prefab");
        }

        void CreateDamagePopupPrefab()
        {
            string path = $"{PrefabsPath}/DamagePopup.prefab";
            if (File.Exists(path)) { Log("  DamagePopup.prefab exists, skipping."); return; }

            var go = new GameObject("DamagePopup");
            var tmp = go.AddComponent<TMPro.TextMeshPro>();
            tmp.fontSize = 5;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.text = "0";
            tmp.sortingOrder = 100;
            AddComponentSafe(go, "SpumOnline.DamagePopup");

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Log("  Created DamagePopup.prefab");
        }

        // ═══════════════════════════════════════════════════════
        // Build Settings
        // ═══════════════════════════════════════════════════════

        void SetupBuildSettings()
        {
            Log("> Configuring Build Settings...");

            var scenes = new List<EditorBuildSettingsScene>();

            AddSceneToBuild(scenes, $"{ScenesPath}/Bootstrap.unity");
            AddSceneToBuild(scenes, $"{ScenesPath}/CharacterSelect.unity");
            AddSceneToBuild(scenes, $"{ScenesPath}/GameWorld.unity");

            // Preserve any existing scenes not in our list
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (!scenes.Any(s => s.path == existing.path))
                    scenes.Add(existing);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Log($"  Build Settings: {scenes.Count} scenes configured.");
            Log("  [0] Bootstrap, [1] CharacterSelect, [2] GameWorld");
        }

        void AddSceneToBuild(List<EditorBuildSettingsScene> list, string path)
        {
            if (!File.Exists(path))
            {
                Log($"  [WARN] Scene not found: {path}");
                return;
            }
            if (!list.Any(s => s.path == path))
                list.Add(new EditorBuildSettingsScene(path, true));
        }

        // ═══════════════════════════════════════════════════════
        // Bootstrap Scene Setup
        // ═══════════════════════════════════════════════════════

        void SetupBootstrapScene()
        {
            Log("> Setting up Bootstrap scene...");

            string path = $"{ScenesPath}/Bootstrap.unity";
            if (!File.Exists(path))
            {
                Log("  [ERROR] Bootstrap.unity not found. Create scenes first.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // Check if BillStartup already exists
            var existing = Object.FindFirstObjectByType<BillGameCore.BillStartup>();
            if (existing != null)
            {
                Log("  BillStartup already in scene, skipping.");
                return;
            }

            // Create Canvas
            var canvasGo = new GameObject("StartupCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var cg = canvasGo.AddComponent<CanvasGroup>();

            // Logo image
            var logoGo = new GameObject("Logo");
            logoGo.transform.SetParent(canvasGo.transform, false);
            var logoRT = logoGo.AddComponent<RectTransform>();
            logoRT.anchoredPosition = new Vector2(0, 60);
            logoRT.sizeDelta = new Vector2(300, 300);
            var logoImg = logoGo.AddComponent<UnityEngine.UI.Image>();
            logoImg.color = new Color(1f, 0.6f, 0.2f, 1f); // Orange placeholder

            // Status text (TMP)
            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(canvasGo.transform, false);
            var statusRT = statusGo.AddComponent<RectTransform>();
            statusRT.anchoredPosition = new Vector2(0, -120);
            statusRT.sizeDelta = new Vector2(400, 40);
            var statusTMP = statusGo.AddComponent<TMPro.TextMeshProUGUI>();
            statusTMP.text = "Loading...";
            statusTMP.fontSize = 24;
            statusTMP.alignment = TMPro.TextAlignmentOptions.Center;
            statusTMP.color = Color.white;

            // Progress slider
            var sliderGo = CreateSlider(canvasGo.transform, new Vector2(0, -160), new Vector2(350, 20));

            // BillStartup component
            var startupGo = new GameObject("[BillStartup]");
            var startup = startupGo.AddComponent<BillGameCore.BillStartup>();
            startup.logo = logoImg;
            startup.progressSlider = sliderGo.GetComponent<UnityEngine.UI.Slider>();
            startup.statusText = statusTMP;
            startup.rootCanvasGroup = cg;
            startup.nextScene = "CharacterSelect";
            startup.logoStartScale = 0.3f;
            startup.logoScaleDuration = 0.8f;
            startup.logoEase = BillGameCore.EaseType.OutBack;
            startup.logoHoldDuration = 1.5f;
            startup.fadeOutDuration = 0.5f;

            // GameManager
            var gmGo = new GameObject("[GameManager]");
            AddComponentSafe(gmGo, "SpumOnline.GameManager");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("  Bootstrap scene configured with BillStartup + GameManager.");
        }

        GameObject CreateSlider(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("ProgressSlider");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var slider = go.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            slider.interactable = false;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Fill area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRT = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = Vector2.zero;
            fillAreaRT.offsetMax = Vector2.zero;

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(1f, 0.55f, 0.15f, 1f); // Orange

            slider.fillRect = fillRT;
            slider.targetGraphic = bgImg;

            return go;
        }

        // ═══════════════════════════════════════════════════════
        // Bootstrap Config
        // ═══════════════════════════════════════════════════════

        void CreateBootstrapConfig()
        {
            Log("> Checking BillBootstrapConfig...");

            var existing = Resources.Load<BillGameCore.BillBootstrapConfig>("BillBootstrapConfig");
            if (existing != null)
            {
                Log("  BillBootstrapConfig already exists.");
                return;
            }

            EnsureDir(ResourcesPath);
            var config = ScriptableObject.CreateInstance<BillGameCore.BillBootstrapConfig>();
            config.enforceBootstrapScene = true;
            config.defaultGameScene = "";
            config.targetFrameRate = 60;
            config.vSyncCount = 0;
            config.enableTracing = true;

            AssetDatabase.CreateAsset(config, $"{ResourcesPath}/BillBootstrapConfig.asset");
            AssetDatabase.SaveAssets();
            Log("  Created BillBootstrapConfig in Resources/.");
        }

        // ═══════════════════════════════════════════════════════
        // Enable STDB_BINDINGS define symbol
        // ═══════════════════════════════════════════════════════

        void EnableBindingsDefine()
        {
            Log("> Checking module_bindings...");

            if (!Directory.Exists("Assets/module_bindings"))
            {
                Log("[ERROR] Assets/module_bindings/ not found!");
                Log("  Run first: spacetime publish --server local spum-online -p server");
                Log("  Then:      spacetime generate --lang csharp --out-dir Assets/module_bindings -p server");
                return;
            }

            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (target == BuildTargetGroup.Unknown)
                target = BuildTargetGroup.Standalone;

            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(target);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

            if (defines.Contains("STDB_BINDINGS"))
            {
                Log("  STDB_BINDINGS already defined.");
                return;
            }

            defines = string.IsNullOrEmpty(defines) ? "STDB_BINDINGS" : defines + ";STDB_BINDINGS";
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);

            Log("  Added STDB_BINDINGS to Scripting Define Symbols.");
            Log("  Unity will recompile. Game scripts are now active.");
        }

        [MenuItem("BillGameCore/SPUM Online/Enable Bindings (STDB_BINDINGS)", false, 2)]
        static void EnableBindingsMenu()
        {
            var w = GetWindow<SpumOnlineMegaSetup>("SPUM Online - Mega Setup");
            w.EnableBindingsDefine();
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        void Log(string msg)
        {
            _log.Add(msg);
            Debug.Log($"[MegaSetup] {msg}");
            Repaint();
        }

        void AddComponentSafe(GameObject go, string fullTypeName)
        {
            var type = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Type.EmptyTypes; } })
                .FirstOrDefault(t => t.FullName == fullTypeName);

            if (type != null && typeof(Component).IsAssignableFrom(type))
                go.AddComponent(type);
        }
    }
}
#endif
