using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BillGameCore;

namespace BillSamples.Flappy
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════════╗
    /// ║  BILL FLAPPY — MEGA SETUP SCRIPT                          ║
    /// ║  Drop this on an empty GameObject and press Play.          ║
    /// ║  It creates the ENTIRE scene: camera, bird, pipes,        ║
    /// ║  background, UI canvas, pool prefabs, everything.         ║
    /// ║                                                            ║
    /// ║  After running once, you can swap placeholder cubes/      ║
    /// ║  sprites with your own 3D models or 2D art.               ║
    /// ╚══════════════════════════════════════════════════════════════╝
    /// </summary>
    public class FlappySetup : MonoBehaviour
    {
        [Header("═══ ASSIGN YOUR ASSETS HERE ═══")]
        [Tooltip("Your bird 3D model or 2D sprite prefab. Leave null for placeholder.")]
        public GameObject birdModelPrefab;

        [Tooltip("Your top pipe model/sprite. Leave null for placeholder.")]
        public GameObject topPipePrefab;

        [Tooltip("Your bottom pipe model/sprite. Leave null for placeholder.")]
        public GameObject bottomPipePrefab;

        [Tooltip("Background sprite/material. Leave null for placeholder color.")]
        public Material backgroundMaterial;

        [Tooltip("Ground sprite/material. Leave null for placeholder.")]
        public Material groundMaterial;

        [Header("═══ GAME SETTINGS ═══")]
        public Color skyColor = new Color(0.45f, 0.78f, 0.95f);
        public Color groundColor = new Color(0.85f, 0.75f, 0.55f);
        public bool use2DPhysics = true;

        [Header("═══ BOOTSTRAP ═══")]
        [Tooltip("True = builds scene in Awake (standalone mode). False = call Build() from a Launcher.")]
        public bool autoBootstrap = true;

        // Created references (serialized so they survive scene save/reload)
        [SerializeField, HideInInspector] private FlappyGameManager _manager;
        [SerializeField, HideInInspector] private FlappyBird _bird;
        [SerializeField, HideInInspector] private FlappyPipeSpawner _spawner;
        [SerializeField, HideInInspector] private FlappyHUD _hud;
        [SerializeField, HideInInspector] private FlappyGameOverPanel _gameOverPanel;
        [SerializeField, HideInInspector] private GameObject _pipePrefab;
        [SerializeField, HideInInspector] private bool _built;

        void Awake()
        {
            if (!autoBootstrap) return;
            if (!_built) Build();
            RuntimeInit();
        }

        /// <summary>
        /// Build the entire Flappy scene visuals. Works in both Editor and Play mode.
        /// Idempotent — calling twice does nothing.
        /// </summary>
        public void Build()
        {
            if (_built) return;
            _built = true;

            Debug.Log("[FlappySetup] ═══ Building Flappy Bird Scene ═══");

            CreateTags();
            CreateCamera();
            CreateBackground();
            CreateGround();
            CreateBird();
            CreatePipePrefabAndPool();
            CreateUI();
            CreateGameManager();

            Debug.Log("[FlappySetup] ═══ Scene Built! ═══");
            Debug.Log("[FlappySetup] Tip: Replace placeholder objects with your art assets.");
        }

        /// <summary>
        /// Register runtime services (pool, button callbacks).
        /// Called automatically in Awake when entering Play mode.
        /// </summary>
        void RuntimeInit()
        {
            if (_pipePrefab != null)
            {
                _pipePrefab.SetActive(false);
                Bill.Pool.Register("Pipe", _pipePrefab, 8);
            }
            if (_gameOverPanel != null && _manager != null)
                _gameOverPanel.Init(_manager);
        }

        /// <summary>
        /// Teardown: stops game, returns pools, cleans state machine.
        /// Call before switching to another sample game.
        /// </summary>
        public void Teardown()
        {
            if (_manager != null) _manager.Cleanup();
            if (_spawner != null) { _spawner.StopSpawning(); _spawner.ReturnAllPipes(); }
            Bill.Pool.ReturnAll("Pipe");
            _built = false;
        }

        // ─── HELPERS (edit/play safe) ────────────

        void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        void SetColor(Renderer r, Color color)
        {
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color;
            r.sharedMaterial = mat;
        }

        // ─── TAGS ────────────────────────────────

        void CreateTags()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EnsureTagExists("Obstacle");
                EnsureTagExists("ScoreZone");
                Debug.Log("[FlappySetup] ✓ Tags created (Obstacle, ScoreZone)");
            }
#endif
        }

#if UNITY_EDITOR
        static void EnsureTagExists(string tag)
        {
            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
            if (asset == null) return;
            var so = new UnityEditor.SerializedObject(asset);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
#endif

        // ─── CAMERA ─────────────────────────────

        void CreateCamera()
        {
            // Find or create main camera
            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                camGO.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = skyColor;
            cam.clearFlags = CameraClearFlags.SolidColor;

            Debug.Log("[FlappySetup] ✓ Camera created (Orthographic, size=5)");
        }

        // ─── BACKGROUND ─────────────────────────

        void CreateBackground()
        {
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            bg.transform.position = new Vector3(0, 0, 5);
            bg.transform.localScale = new Vector3(20, 12, 1);

            var renderer = bg.GetComponent<Renderer>();
            if (backgroundMaterial != null)
                renderer.sharedMaterial = backgroundMaterial;
            else
                SetColor(renderer, skyColor);

            // Remove collider from background
            SafeDestroy(bg.GetComponent<Collider>());

            Debug.Log("[FlappySetup] ✓ Background created");
        }

        // ─── GROUND ─────────────────────────────

        void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -5f, 0);
            ground.transform.localScale = new Vector3(20, 1, 1);
            ground.tag = "Obstacle";

            var renderer = ground.GetComponent<Renderer>();
            if (groundMaterial != null)
                renderer.sharedMaterial = groundMaterial;
            else
                SetColor(renderer, groundColor);

            // Grass strip on top of ground
            MakePart("Grass", PrimitiveType.Cube, ground.transform,
                new Vector3(0f, 0.52f, 0f), new Vector3(1f, 0.08f, 1f), new Color(0.35f, 0.8f, 0.25f));
            MakePart("GrassDark", PrimitiveType.Cube, ground.transform,
                new Vector3(0f, 0.48f, 0f), new Vector3(1f, 0.04f, 1f), new Color(0.25f, 0.6f, 0.18f));

            // Add collider for death
            if (use2DPhysics)
            {
                SafeDestroy(ground.GetComponent<Collider>());
                var box2d = ground.AddComponent<BoxCollider2D>();
                box2d.isTrigger = true;
            }
            else
            {
                var col = ground.GetComponent<BoxCollider>();
                col.isTrigger = true;
            }

            // Ceiling (invisible)
            var ceiling = new GameObject("Ceiling");
            ceiling.transform.position = new Vector3(0, 6f, 0);
            ceiling.tag = "Obstacle";
            if (use2DPhysics)
            {
                var c2d = ceiling.AddComponent<BoxCollider2D>();
                c2d.size = new Vector2(20, 1);
                c2d.isTrigger = true;
            }
            else
            {
                var bc = ceiling.AddComponent<BoxCollider>();
                bc.size = new Vector3(20, 1, 1);
                bc.isTrigger = true;
            }

            Debug.Log("[FlappySetup] ✓ Ground + Ceiling created");
        }

        // ─── BIRD ────────────────────────────────

        void CreateBird()
        {
            var birdGO = new GameObject("Bird");
            birdGO.transform.position = new Vector3(0, 1f, 0);

            // Visual
            GameObject visual;
            if (birdModelPrefab != null)
            {
                visual = Instantiate(birdModelPrefab, birdGO.transform);
                visual.name = "BirdModel";
            }
            else
            {
                // Procedural bird: body, belly, beak, eye, wing, tail
                visual = new GameObject("BirdModel_Placeholder");
                visual.transform.SetParent(birdGO.transform);
                visual.transform.localPosition = Vector3.zero;

                var yellow = new Color(1f, 0.85f, 0.1f);
                var darkYellow = new Color(0.9f, 0.7f, 0.05f);
                var orange = new Color(1f, 0.5f, 0.15f);
                var darkOrange = new Color(0.85f, 0.4f, 0.1f);
                var cream = new Color(1f, 0.95f, 0.7f);

                // Body (slightly oval)
                MakePart("Body", PrimitiveType.Sphere, visual.transform,
                    Vector3.zero, new Vector3(0.6f, 0.52f, 0.5f), yellow);

                // Belly (lighter, front-bottom)
                MakePart("Belly", PrimitiveType.Sphere, visual.transform,
                    new Vector3(0.06f, -0.06f, 0f), new Vector3(0.42f, 0.34f, 0.38f), cream);

                // Beak upper
                MakePart("BeakUp", PrimitiveType.Cube, visual.transform,
                    new Vector3(0.32f, 0.02f, 0f), new Vector3(0.2f, 0.08f, 0.16f), orange);

                // Beak lower
                MakePart("BeakDown", PrimitiveType.Cube, visual.transform,
                    new Vector3(0.29f, -0.05f, 0f), new Vector3(0.15f, 0.05f, 0.13f), darkOrange);

                // Eye white
                MakePart("EyeWhite", PrimitiveType.Sphere, visual.transform,
                    new Vector3(0.16f, 0.1f, -0.18f), new Vector3(0.18f, 0.18f, 0.06f), Color.white);

                // Pupil
                MakePart("Pupil", PrimitiveType.Sphere, visual.transform,
                    new Vector3(0.21f, 0.1f, -0.19f), new Vector3(0.09f, 0.1f, 0.04f), Color.black);

                // Wing
                MakePart("Wing", PrimitiveType.Cube, visual.transform,
                    new Vector3(-0.06f, -0.02f, -0.22f), new Vector3(0.25f, 0.07f, 0.18f), darkYellow);

                // Tail feathers
                MakePart("Tail", PrimitiveType.Cube, visual.transform,
                    new Vector3(-0.3f, 0.06f, 0f), new Vector3(0.1f, 0.2f, 0.06f), orange);
            }

            // Collider on bird root
            if (use2DPhysics)
            {
                var circle = birdGO.AddComponent<CircleCollider2D>();
                circle.radius = 0.35f;
                circle.isTrigger = true;

                // Need Rigidbody2D for trigger detection (set to kinematic, we handle physics ourselves)
                var rb = birdGO.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                var sphere = birdGO.AddComponent<SphereCollider>();
                sphere.radius = 0.35f;
                sphere.isTrigger = true;

                var rb = birdGO.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            // Bird script
            _bird = birdGO.AddComponent<FlappyBird>();
            _bird.modelRoot = visual.transform;
            _bird.animator = visual.GetComponentInChildren<Animator>();

            Debug.Log("[FlappySetup] ✓ Bird created" + (birdModelPrefab != null ? " (custom model)" : " (placeholder)"));
        }

        // ─── PIPE PREFAB & POOL ──────────────────

        void CreatePipePrefabAndPool()
        {
            // Build pipe pair prefab (visible in editor, deactivated at runtime by RuntimeInit)
            var pipePairGO = new GameObject("PipePair_Prefab");

            var pipe = pipePairGO.AddComponent<FlappyPipe>();

            // Top pipe
            GameObject topVisual;
            if (topPipePrefab != null)
            {
                topVisual = Instantiate(topPipePrefab, pipePairGO.transform);
            }
            else
            {
                var pipeGreen = new Color(0.3f, 0.75f, 0.3f);
                var capGreen = new Color(0.22f, 0.6f, 0.22f);
                var rimGreen = new Color(0.4f, 0.85f, 0.4f);

                topVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topVisual.transform.SetParent(pipePairGO.transform);
                topVisual.transform.localScale = new Vector3(1.2f, 10f, 1f);
                var r = topVisual.GetComponent<Renderer>();
                SetColor(r, pipeGreen);

                // Cap lip at bottom opening (local coords compensate for parent scale 1.2/10/1)
                MakePart("Cap", PrimitiveType.Cube, topVisual.transform,
                    new Vector3(0f, -0.5f, 0f), new Vector3(1.25f, 0.06f, 1.15f), capGreen);

                // Rim highlight
                MakePart("Rim", PrimitiveType.Cube, topVisual.transform,
                    new Vector3(0f, -0.47f, 0f), new Vector3(1.08f, 0.008f, 1.05f), rimGreen);
            }
            topVisual.name = "TopPipe";
            topVisual.tag = "Obstacle";

            // Top pipe collider
            if (use2DPhysics)
            {
                var col3d = topVisual.GetComponent<Collider>();
                if (col3d) SafeDestroy(col3d);
                var box2d = topVisual.AddComponent<BoxCollider2D>();
                box2d.isTrigger = true;
                // Size auto-matches sprite/mesh in 2D
            }
            else
            {
                var col = topVisual.GetComponent<BoxCollider>();
                if (col) col.isTrigger = true;
                else { col = topVisual.AddComponent<BoxCollider>(); col.isTrigger = true; }
            }

            // Bottom pipe
            GameObject bottomVisual;
            if (bottomPipePrefab != null)
            {
                bottomVisual = Instantiate(bottomPipePrefab, pipePairGO.transform);
            }
            else
            {
                var pipeGreen = new Color(0.3f, 0.75f, 0.3f);
                var capGreen = new Color(0.22f, 0.6f, 0.22f);
                var rimGreen = new Color(0.4f, 0.85f, 0.4f);

                bottomVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottomVisual.transform.SetParent(pipePairGO.transform);
                bottomVisual.transform.localScale = new Vector3(1.2f, 10f, 1f);
                var r = bottomVisual.GetComponent<Renderer>();
                SetColor(r, pipeGreen);

                // Cap lip at top opening
                MakePart("Cap", PrimitiveType.Cube, bottomVisual.transform,
                    new Vector3(0f, 0.5f, 0f), new Vector3(1.25f, 0.06f, 1.15f), capGreen);

                // Rim highlight
                MakePart("Rim", PrimitiveType.Cube, bottomVisual.transform,
                    new Vector3(0f, 0.47f, 0f), new Vector3(1.08f, 0.008f, 1.05f), rimGreen);
            }
            bottomVisual.name = "BottomPipe";
            bottomVisual.tag = "Obstacle";

            if (use2DPhysics)
            {
                var col3d = bottomVisual.GetComponent<Collider>();
                if (col3d) SafeDestroy(col3d);
                var box2d = bottomVisual.AddComponent<BoxCollider2D>();
                box2d.isTrigger = true;
            }
            else
            {
                var col = bottomVisual.GetComponent<BoxCollider>();
                if (col) col.isTrigger = true;
                else { col = bottomVisual.AddComponent<BoxCollider>(); col.isTrigger = true; }
            }

            // Score zone (invisible trigger between pipes)
            var scoreZone = new GameObject("ScoreZone");
            scoreZone.transform.SetParent(pipePairGO.transform);
            scoreZone.tag = "ScoreZone";
            if (use2DPhysics)
            {
                var sz2d = scoreZone.AddComponent<BoxCollider2D>();
                sz2d.size = new Vector2(0.3f, 4f);
                sz2d.isTrigger = true;
            }
            else
            {
                var szCol = scoreZone.AddComponent<BoxCollider>();
                szCol.size = new Vector3(0.3f, 4f, 4f);
                szCol.isTrigger = true;
            }

            // Wire references
            pipe.topPipe = topVisual.transform;
            pipe.bottomPipe = bottomVisual.transform;
            pipe.scoreZone = scoreZone.transform;

            // Save prefab for RuntimeInit pool registration
            _pipePrefab = pipePairGO;

            // Create spawner
            var spawnerGO = new GameObject("PipeSpawner");
            _spawner = spawnerGO.AddComponent<FlappyPipeSpawner>();

            Debug.Log("[FlappySetup] ✓ Pipe prefab created (pool registered on Play)");
        }

        // ─── UI CANVAS ──────────────────────────

        void CreateUI()
        {
            // ── Canvas ──
            var canvasGO = new GameObject("FlappyCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── EventSystem (required for button clicks) ──
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }

            // ── HUD Component ──
            var hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(canvasGO.transform, false);
            _hud = hudGO.AddComponent<FlappyHUD>();

            // Menu group
            var menuGroup = CreateUIGroup("MenuGroup", hudGO.transform);
            _hud.menuGroup = menuGroup;

            _hud.titleText = CreateText("BILL FLAPPY", menuGroup.transform, new Vector2(0, 300),
                72, TextAnchor.MiddleCenter, Color.white);

            _hud.bestScoreText = CreateText("Best: 0", menuGroup.transform, new Vector2(0, 200),
                36, TextAnchor.MiddleCenter, Color.white);

            var tapText = CreateText("TAP TO START", menuGroup.transform, new Vector2(0, -100),
                32, TextAnchor.MiddleCenter, Color.white);
            tapText.gameObject.AddComponent<CanvasGroup>();
            _hud.tapToStartText = tapText;

            // Gameplay group
            var gameplayGroup = CreateUIGroup("GameplayGroup", hudGO.transform);
            _hud.gameplayGroup = gameplayGroup;

            _hud.scoreText = CreateText("0", gameplayGroup.transform, new Vector2(0, 400),
                80, TextAnchor.MiddleCenter, Color.white);

            // Add shadow to score
            var scoreShadow = _hud.scoreText.gameObject.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0, 0, 0, 0.5f);
            scoreShadow.effectDistance = new Vector2(3, -3);

            // ── Game Over Panel ──
            var goPanelGO = new GameObject("GameOverPanel");
            goPanelGO.transform.SetParent(canvasGO.transform, false);
            _gameOverPanel = goPanelGO.AddComponent<FlappyGameOverPanel>();

            var goRT = goPanelGO.AddComponent<RectTransform>();
            goRT.anchorMin = Vector2.zero;
            goRT.anchorMax = Vector2.one;
            goRT.sizeDelta = Vector2.zero;

            var goCG = goPanelGO.AddComponent<CanvasGroup>();
            _gameOverPanel.canvasGroup = goCG;

            // Dark overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(goPanelGO.transform, false);
            var overlayRT = overlay.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.5f);

            // Panel card
            var panelCard = new GameObject("PanelCard");
            panelCard.transform.SetParent(goPanelGO.transform, false);
            var cardRT = panelCard.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(600, 700);
            _gameOverPanel.panelRoot = cardRT;
            var cardImg = panelCard.AddComponent<Image>();
            cardImg.color = new Color(0.95f, 0.9f, 0.8f);

            // Game Over title
            CreateText("GAME OVER", panelCard.transform, new Vector2(0, 260),
                56, TextAnchor.MiddleCenter, new Color(0.8f, 0.2f, 0.1f));

            // Score
            CreateText("Score", panelCard.transform, new Vector2(-120, 150),
                28, TextAnchor.MiddleCenter, new Color(0.4f, 0.35f, 0.3f));
            _gameOverPanel.finalScoreText = CreateText("0", panelCard.transform, new Vector2(-120, 100),
                48, TextAnchor.MiddleCenter, new Color(0.2f, 0.15f, 0.1f));

            // Best
            CreateText("Best", panelCard.transform, new Vector2(120, 150),
                28, TextAnchor.MiddleCenter, new Color(0.4f, 0.35f, 0.3f));
            _gameOverPanel.bestScoreText = CreateText("0", panelCard.transform, new Vector2(120, 100),
                48, TextAnchor.MiddleCenter, new Color(0.2f, 0.15f, 0.1f));

            // New Best label
            var newBestGO = CreateText("NEW!", panelCard.transform, new Vector2(200, 100),
                24, TextAnchor.MiddleCenter, Color.red);
            newBestGO.gameObject.AddComponent<CanvasGroup>();
            _gameOverPanel.newBestLabel = newBestGO;

            // Medal placeholder
            var medalHolder = new GameObject("MedalHolder");
            medalHolder.transform.SetParent(panelCard.transform, false);
            var mhRT = medalHolder.AddComponent<RectTransform>();
            mhRT.anchoredPosition = new Vector2(0, -20);
            mhRT.sizeDelta = new Vector2(80, 80);
            _gameOverPanel.medalHolder = medalHolder;

            var medalImg = medalHolder.AddComponent<Image>();
            medalImg.color = new Color(1, 0.84f, 0);
            _gameOverPanel.medalImage = medalImg;

            // Retry button
            _gameOverPanel.retryButton = CreateButton("RETRY", panelCard.transform,
                new Vector2(-120, -200), new Vector2(200, 70),
                new Color(0.3f, 0.75f, 0.3f));

            // Menu button
            _gameOverPanel.menuButton = CreateButton("MENU", panelCard.transform,
                new Vector2(120, -200), new Vector2(200, 70),
                new Color(0.85f, 0.5f, 0.2f));

            // Hide GameOverPanel by default (shown at runtime when player dies)
            goPanelGO.SetActive(false);

            Debug.Log("[FlappySetup] ✓ UI Canvas created (HUD + GameOverPanel)");
        }

        // ─── GAME MANAGER ────────────────────────

        void CreateGameManager()
        {
            var gmGO = new GameObject("FlappyGameManager");
            _manager = gmGO.AddComponent<FlappyGameManager>();

            _manager.bird = _bird;
            _manager.pipeSpawner = _spawner;
            _manager.hud = _hud;
            _manager.gameOverPanel = _gameOverPanel;
            _manager.cameraRoot = Camera.main?.transform;

            // _gameOverPanel.Init() called in RuntimeInit (needs button callbacks)

            Debug.Log("[FlappySetup] ✓ GameManager created and wired");
        }

        // ─── UI HELPERS ──────────────────────────

        GameObject CreateUIGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        Text CreateText(string content, Transform parent, Vector2 position, int fontSize,
            TextAnchor alignment, Color color)
        {
            var go = new GameObject(content.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(600, fontSize + 20);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        // ─── PREFAB HELPER ───────────────────────

        GameObject MakePart(string name, PrimitiveType type, Transform parent,
            Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.identity;
            var r = go.GetComponent<Renderer>();
            SetColor(r, color);
            var col = go.GetComponent<Collider>();
            if (col) SafeDestroy(col);
            return go;
        }

        Button CreateButton(string label, Transform parent, Vector2 position, Vector2 size, Color bgColor)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;

            var text = labelGO.AddComponent<Text>();
            text.text = label;
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return btn;
        }
    }
}

// ═══════════════════════════════════════════════════════════════
// EDITOR: Menu items + Inspector button
// ═══════════════════════════════════════════════════════════════
#if UNITY_EDITOR
namespace BillSamples.Flappy
{
    using UnityEditor;
    using UnityEditor.SceneManagement;

    public static class FlappySetupMenu
    {
        [MenuItem("BillGameCore/Samples/Create Flappy Bird Scene")]
        static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "FlappyBird";

            var go = new GameObject("[FlappySetup]");
            var setup = go.AddComponent<FlappySetup>();
            setup.Build();

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log("[FlappySetup] Scene built! Save it, then Press Play.");
        }

        [MenuItem("BillGameCore/Samples/Add FlappySetup to Current Scene")]
        static void AddToScene()
        {
            if (Object.FindAnyObjectByType<FlappySetup>() != null)
            {
                EditorUtility.DisplayDialog("FlappySetup", "FlappySetup already exists in this scene.", "OK");
                return;
            }

            var go = new GameObject("[FlappySetup]");
            Undo.RegisterCreatedObjectUndo(go, "Add FlappySetup");
            var setup = go.AddComponent<FlappySetup>();
            setup.Build();
            EditorUtility.SetDirty(setup);
            Selection.activeGameObject = go;
        }
    }

    [CustomEditor(typeof(FlappySetup))]
    public class FlappySetupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var setup = (FlappySetup)target;
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

            // Build button — works in both editor and play mode
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.3f);
            if (GUILayout.Button("Build Flappy Scene", GUILayout.Height(36)))
            {
                setup.Build();
                EditorUtility.SetDirty(setup);
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            GUI.backgroundColor = Color.white;

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(0.85f, 0.3f, 0.3f);
                if (GUILayout.Button("Teardown"))
                    setup.Teardown();
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Assign your art prefabs above, or leave null for procedural placeholders.\n" +
                    "Set 'use2DPhysics = false' for 3D colliders.\n\n" +
                    "Save the scene, then Press Play to start the game.",
                    MessageType.Info);

                GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
                if (GUILayout.Button("Save Scene As...", GUILayout.Height(28)))
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        "Save Flappy Scene", "FlappyBird", "unity", "Save the Flappy Bird scene");
                    if (!string.IsNullOrEmpty(path))
                        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
                }
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
#endif
