#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BillGameCore;
using Mythfall.Core;

namespace Mythfall.EditorTools
{
    /// <summary>
    /// One-shot Sprint 0 setup. Creates the three scenes, the [Bootstrap] GameObject,
    /// the BillBootstrapConfig asset, registers everything in Build Settings, and
    /// configures Android Player Settings for IL2CPP / ARM64.
    ///
    /// Idempotent — safe to re-run; existing assets are reused.
    /// </summary>
    public static class Sprint0Setup
    {
        const string ScenesFolder = "Assets/Mythfall/Scenes";
        const string ResourcesFolder = "Assets/Mythfall/Resources";
        const string BootstrapScenePath = ScenesFolder + "/BootstrapScene.unity";
        const string MenuScenePath = ScenesFolder + "/MenuScene.unity";
        const string GameplayScenePath = ScenesFolder + "/GameplayScene.unity";
        const string ConfigAssetPath = ResourcesFolder + "/BillBootstrapConfig.asset";

        [MenuItem("Tools/Mythfall/Sprint 0 — Run Setup")]
        public static void RunSetup()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Sprint0Setup] Exit Play mode before running setup.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                if (!EditorUtility.DisplayDialog(
                    "Unsaved changes",
                    "Active scene has unsaved changes. Continue (changes will be lost)?",
                    "Continue", "Cancel"))
                    return;
            }

            EnsureFolders();
            CreateBillBootstrapConfig();
            CreateBootstrapScene();
            CreateMenuScene();
            CreateGameplayScene();
            RegisterScenesInBuildSettings();
            ConfigureAndroidPlayerSettings();
            ConfigureActiveInputHandler();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);

            Debug.Log("[Sprint0Setup] DONE. Press Play to verify boot sequence.");
        }

        // -------------------------------------------------------------------
        // Folders
        // -------------------------------------------------------------------

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Mythfall");
            EnsureFolder("Assets/Mythfall/Scenes");
            EnsureFolder("Assets/Mythfall/Resources");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // -------------------------------------------------------------------
        // BillBootstrapConfig
        // -------------------------------------------------------------------

        static void CreateBillBootstrapConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BillBootstrapConfig>(ConfigAssetPath);
            if (existing != null)
            {
                Debug.Log("[Sprint0Setup] BillBootstrapConfig already exists — leaving as-is.");
                return;
            }

            var cfg = ScriptableObject.CreateInstance<BillBootstrapConfig>();
            cfg.enforceBootstrapScene = true;
            cfg.targetFrameRate = 60;
            cfg.vSyncCount = 0;
            cfg.enableTracing = true;
            cfg.includeDebugOverlay = true;
            cfg.includeCheatConsole = true;
            // IMPORTANT: leave defaultGameScene EMPTY. GameBootstrap.Initialize() owns the
            // first scene transition so it happens AFTER our services are registered.
            cfg.defaultGameScene = "";
            cfg.returnToEditSceneInEditor = true;

            AssetDatabase.CreateAsset(cfg, ConfigAssetPath);
            Debug.Log("[Sprint0Setup] Created " + ConfigAssetPath);
        }

        // -------------------------------------------------------------------
        // Scenes
        // -------------------------------------------------------------------

        static void CreateBootstrapScene()
        {
            if (File.Exists(BootstrapScenePath))
            {
                Debug.Log("[Sprint0Setup] BootstrapScene exists — verifying contents.");
                var existing = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
                EnsureBootstrapGameObject(existing);
                EditorSceneManager.MarkSceneDirty(existing);
                EditorSceneManager.SaveScene(existing);
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureBootstrapGameObject(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            Debug.Log("[Sprint0Setup] Created " + BootstrapScenePath);
        }

        static void EnsureBootstrapGameObject(Scene scene)
        {
            GameObject bootstrap = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "[Bootstrap]") { bootstrap = root; break; }
            }
            if (bootstrap == null)
            {
                bootstrap = new GameObject("[Bootstrap]");
                SceneManager.MoveGameObjectToScene(bootstrap, scene);
            }
            // Order matters: BillStartup before GameBootstrap so GameBootstrap.Awake can
            // GetComponent<BillStartup>() reliably (RequireComponent would auto-add but
            // explicit order keeps wiring deterministic).
            if (bootstrap.GetComponent<BillStartup>() == null)
                bootstrap.AddComponent<BillStartup>();
            if (bootstrap.GetComponent<GameBootstrap>() == null)
                bootstrap.AddComponent<GameBootstrap>();
            // AudioListener so Unity doesn't warn "no audio listeners" during the brief
            // window before MenuScene loads. MenuScene/GameplayScene cameras have their own.
            if (bootstrap.GetComponent<AudioListener>() == null)
                bootstrap.AddComponent<AudioListener>();

            EnsureSplashCanvas(scene, bootstrap);
            EnforceSingleAudioListener(scene, bootstrap);
        }

        // -------------------------------------------------------------------
        // Bootstrap is the canonical AudioListener owner. If anything else in the
        // scene has one (e.g. a stray Main Camera that Unity auto-creates, or a
        // duplicate from re-runs), strip them so Unity stops warning about multiples.
        // -------------------------------------------------------------------

        static void EnforceSingleAudioListener(Scene scene, GameObject keeper)
        {
            var keeperListener = keeper.GetComponent<AudioListener>();
            int removed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(includeInactive: true))
                {
                    if (listener == keeperListener) continue;
                    Object.DestroyImmediate(listener, allowDestroyingAssets: false);
                    removed++;
                }
            }
            if (removed > 0)
                Debug.Log($"[Sprint0Setup] Removed {removed} duplicate AudioListener(s); keeper is on [Bootstrap].");
        }

        // -------------------------------------------------------------------
        // Splash Canvas
        //   [StartupCanvas] (Canvas, ScreenSpaceOverlay, CanvasGroup, GraphicRaycaster)
        //     - Background (Image, full-screen dark fill)
        //     - Logo       (Image, centered, default Unity UISprite as placeholder)
        // BillStartup.logo + rootCanvasGroup wired via SerializedObject so refs survive
        // across re-runs of this setup.
        // -------------------------------------------------------------------

        static void EnsureSplashCanvas(Scene scene, GameObject bootstrap)
        {
            GameObject canvasGo = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "[StartupCanvas]") { canvasGo = root; break; }
            }

            if (canvasGo == null)
            {
                canvasGo = new GameObject("[StartupCanvas]",
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster), typeof(CanvasGroup));
                SceneManager.MoveGameObjectToScene(canvasGo, scene);

                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // mobile portrait
                scaler.matchWidthOrHeight = 0.5f;

                var group = canvasGo.GetComponent<CanvasGroup>();
                group.alpha = 1f;
                group.blocksRaycasts = true;

                CreateBackground(canvasGo.transform);
                CreateLogo(canvasGo.transform);
            }
            else
            {
                // Dedupe siblings named "Logo (1)" / "Background (1)" left over from
                // accidental Ctrl+D or earlier broken setup runs. Keep first "Logo"
                // and first "Background"; strip any "Foo (N)" duplicates.
                DedupeNumberedSuffixChildren(canvasGo.transform, "Logo");
                DedupeNumberedSuffixChildren(canvasGo.transform, "Background");
            }

            // Wire BillStartup references (always, idempotent — survives re-runs)
            var startup = bootstrap.GetComponent<BillStartup>();
            var logoImg = canvasGo.transform.Find("Logo")?.GetComponent<Image>();
            var canvasGroup = canvasGo.GetComponent<CanvasGroup>();

            if (startup != null)
            {
                var so = new SerializedObject(startup);
                SetObjectRef(so, "logo", logoImg);
                SetObjectRef(so, "rootCanvasGroup", canvasGroup);
                SetStringField(so, "nextScene", "MenuScene");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire GameBootstrap.startup back-reference
            var gameBoot = bootstrap.GetComponent<GameBootstrap>();
            if (gameBoot != null && startup != null)
            {
                var so = new SerializedObject(gameBoot);
                SetObjectRef(so, "startup", startup);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(canvasGo);
        }

        static void CreateBackground(Transform parent)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.04f, 0.04f, 0.06f, 1f);
            img.raycastTarget = false;
        }

        static void CreateLogo(Transform parent)
        {
            var go = new GameObject("Logo", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(512f, 512f);
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            // Unity's built-in default UI sprite as placeholder — swap for real logo later.
            var defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (defaultSprite != null) img.sprite = defaultSprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        static void DedupeNumberedSuffixChildren(Transform parent, string baseName)
        {
            int removed = 0;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                // Match "Background (1)", "Logo (12)" — Unity's auto-rename pattern.
                if (child.name.StartsWith(baseName + " (") && child.name.EndsWith(")"))
                {
                    Object.DestroyImmediate(child.gameObject, allowDestroyingAssets: false);
                    removed++;
                }
            }
            if (removed > 0)
                Debug.Log($"[Sprint0Setup] Removed {removed} duplicate '{baseName} (N)' children from [StartupCanvas].");
        }

        static void SetObjectRef(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        static void SetStringField(SerializedObject so, string fieldName, string value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null) prop.stringValue = value;
        }

        static void CreateMenuScene()
        {
            if (File.Exists(MenuScenePath)) { Debug.Log("[Sprint0Setup] MenuScene exists — skipping."); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("[MenuRoot]");
            SceneManager.MoveGameObjectToScene(root, scene);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            camGo.AddComponent<AudioListener>();
            SceneManager.MoveGameObjectToScene(camGo, scene);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            Debug.Log("[Sprint0Setup] Created " + MenuScenePath);
        }

        static void CreateGameplayScene()
        {
            if (File.Exists(GameplayScenePath)) { Debug.Log("[Sprint0Setup] GameplayScene exists — skipping."); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
            camGo.AddComponent<AudioListener>();
            SceneManager.MoveGameObjectToScene(camGo, scene);

            EditorSceneManager.SaveScene(scene, GameplayScenePath);
            Debug.Log("[Sprint0Setup] Created " + GameplayScenePath);
        }

        // -------------------------------------------------------------------
        // Build Settings
        // -------------------------------------------------------------------

        static void RegisterScenesInBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[Sprint0Setup] Registered 3 scenes in Build Settings (Bootstrap=0, Menu=1, Gameplay=2)");
        }

        // -------------------------------------------------------------------
        // Android Player Settings
        // -------------------------------------------------------------------

        static void ConfigureAndroidPlayerSettings()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28; // Android 9.0
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Default product name — user can override via Player Settings UI
            if (PlayerSettings.productName == "Mythfall" || string.IsNullOrEmpty(PlayerSettings.productName))
                PlayerSettings.productName = "Mythfall Survivor";

            Debug.Log("[Sprint0Setup] Android Player Settings: IL2CPP, ARM64, min API 28, Linear color space");
        }

        // -------------------------------------------------------------------
        // Active Input Handler — set to "Both" so old UnityEngine.Input still works.
        // BillGameCore's CheatConsole uses the legacy Input API for its hotkey;
        // leaving it on "New only" causes a per-frame Tick error.
        // Project Settings → Player → Active Input Handling. enum: 0=Old, 1=New, 2=Both.
        // Requires Editor restart to take effect.
        // -------------------------------------------------------------------

        static void ConfigureActiveInputHandler()
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Sprint0Setup] Could not load ProjectSettings.asset to set Active Input Handler.");
                return;
            }
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null)
            {
                Debug.LogWarning("[Sprint0Setup] activeInputHandler property not found — set manually: Project Settings → Player → Active Input Handling = Both.");
                return;
            }
            if (prop.intValue == 2)
            {
                Debug.Log("[Sprint0Setup] Active Input Handler already 'Both'.");
                return;
            }
            prop.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.LogWarning("[Sprint0Setup] Active Input Handler set to 'Both'. RESTART UNITY for this to take effect (Unity asks anyway when this changes).");
        }

        // -------------------------------------------------------------------
        // Verify menu — quick sanity check after setup
        // -------------------------------------------------------------------

        [MenuItem("Tools/Mythfall/Sprint 0 — Verify Setup")]
        public static void VerifySetup()
        {
            int score = 0, total = 6;

            score += Check(File.Exists(BootstrapScenePath), "BootstrapScene exists");
            score += Check(File.Exists(MenuScenePath), "MenuScene exists");
            score += Check(File.Exists(GameplayScenePath), "GameplayScene exists");
            score += Check(AssetDatabase.LoadAssetAtPath<BillBootstrapConfig>(ConfigAssetPath) != null,
                "BillBootstrapConfig asset present in Resources/");
            score += Check(EditorBuildSettings.scenes.Length >= 3 &&
                           EditorBuildSettings.scenes[0].path == BootstrapScenePath,
                "BootstrapScene is build index 0");
            score += Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
                "Android scripting backend = IL2CPP");

            Debug.Log($"[Sprint0Setup] Verify: {score}/{total} checks passed.");
        }

        static int Check(bool ok, string label)
        {
            Debug.Log($"  [{(ok ? "OK" : "MISS")}] {label}");
            return ok ? 1 : 0;
        }
    }
}
#endif
