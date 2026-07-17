using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using System.Collections.Generic;

namespace BillSamples.Runner
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════════╗
    /// ║  BILL RUNNER — MEGA SETUP SCRIPT                          ║
    /// ║  Drop on an empty GO, press Play → full Endless Runner.   ║
    /// ║  Assign your 3D models/sprites in the Inspector.          ║
    /// ╚══════════════════════════════════════════════════════════════╝
    /// </summary>
    public class RunnerSetup : MonoBehaviour
    {
        [Header("═══ CHARACTER MODELS ═══")]
        [Tooltip("Your character 3D model. Leave null for placeholder capsule.")]
        public GameObject characterModelPrefab;

        [Header("═══ OBSTACLE PREFABS (optional) ═══")]
        public GameObject cratePrefab;
        public GameObject spikePrefab;
        public GameObject lowBeamPrefab;
        public GameObject birdEnemyPrefab;
        public GameObject barrelPrefab;
        public GameObject sawBladePrefab;

        [Header("═══ COLLECTIBLE PREFABS (optional) ═══")]
        public GameObject coinPrefab;
        public GameObject powerUpPrefab;

        [Header("═══ ENVIRONMENT ═══")]
        public Material groundMaterial;
        public Material skyMaterial;
        public Color skyColor = new Color(0.55f, 0.8f, 0.95f);
        public Color groundColor = new Color(0.6f, 0.5f, 0.3f);

        [Header("═══ SETTINGS ═══")]
        public bool use2DPhysics = false; // false = 3D side-scroller

        [Header("═══ BOOTSTRAP ═══")]
        [Tooltip("True = builds scene in Awake (standalone). False = call Build() from Launcher.")]
        public bool autoBootstrap = true;

        // Internal refs
        private RunnerPlayer _player;
        private RunnerChunkSpawner _chunkSpawner;
        private RunnerCamera _runnerCamera;
        private RunnerGameManager _manager;
        private RunnerHUD _hud;
        private RunnerGameOverPanel _goPanel;
        private bool _built;

        void Awake()
        {
            if (autoBootstrap) Build();
        }

        public void Build()
        {
            if (_built) return;
            _built = true;

            Debug.Log("[RunnerSetup] ═══ Building Endless Runner Scene ═══");

            CreateCamera();
            CreatePlayer();
            CreateChunkPrefabs();
            CreateCollectiblePrefabs();
            CreatePowerUpPrefabs();
            CreateVFXPrefabs();
            CreateParallaxBackground();
            CreateUI();
            CreateChunkSpawner();
            CreateGameManager();

            Debug.Log("[RunnerSetup] ═══ Scene Ready! ═══");
        }

        public void Teardown()
        {
            if (_manager != null) _manager.Cleanup();
            if (_chunkSpawner != null) _chunkSpawner.ClearAll();
            Bill.Pool.ReturnAll("coin_bronze");
            Bill.Pool.ReturnAll("vfx_dust");
            string[] chunkKeys = { "chunk_flat_easy","chunk_flat_med","chunk_elevated","chunk_gap","chunk_slide","chunk_mixed_hard" };
            foreach (var k in chunkKeys) Bill.Pool.ReturnAll(k);
            _built = false;
        }

        // ─── CAMERA ─────────────────────────────

        void CreateCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.tag = "MainCamera";
            }

            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(0, 3, -10);
            cam.backgroundColor = skyColor;
            cam.clearFlags = CameraClearFlags.SolidColor;

            _runnerCamera = cam.gameObject.AddComponent<RunnerCamera>();

            Debug.Log("[RunnerSetup] ✓ Camera created");
        }

        // ─── PLAYER ─────────────────────────────

        void CreatePlayer()
        {
            var playerGO = new GameObject("Player");
            playerGO.transform.position = new Vector3(0, 0.5f, 0);

            // Visual
            GameObject visual;
            if (characterModelPrefab != null)
            {
                visual = Instantiate(characterModelPrefab, playerGO.transform);
                visual.name = "CharacterModel";
            }
            else
            {
                // Placeholder: capsule
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "CharacterModel_Placeholder";
                visual.transform.SetParent(playerGO.transform);
                visual.transform.localPosition = new Vector3(0, 0, 0);
                visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                var r = visual.GetComponent<Renderer>();
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = new Color(0.2f, 0.6f, 1f);

                var vc = visual.GetComponent<Collider>();
                if (vc) Destroy(vc);

                // "Eyes" 
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                eye.transform.SetParent(visual.transform);
                eye.transform.localPosition = new Vector3(0.3f, 0.3f, 0);
                eye.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
                eye.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
                Destroy(eye.GetComponent<Collider>());
            }

            // Collider
            if (use2DPhysics)
            {
                var col = playerGO.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.6f, 1f);
                col.offset = new Vector2(0, 0.5f);
                col.isTrigger = true;
                var rb = playerGO.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                var col = playerGO.AddComponent<BoxCollider>();
                col.size = new Vector3(0.6f, 1f, 0.6f);
                col.center = new Vector3(0, 0.5f, 0);
                col.isTrigger = true;
                var rb = playerGO.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            _player = playerGO.AddComponent<RunnerPlayer>();
            _player.modelRoot = visual.transform;
            _player.animator = visual.GetComponentInChildren<Animator>();
            _player.LoadUpgrades();

            _runnerCamera.target = playerGO.transform;

            Debug.Log("[RunnerSetup] ✓ Player created");
        }

        // ─── CHUNK PREFABS ──────────────────────

        void CreateChunkPrefabs()
        {
            // Generate procedural chunk prefabs and register them in the pool.
            // Each chunk is a 20-unit wide section with ground + obstacles.

            CreateChunkVariant("chunk_flat_easy", ChunkDifficulty.Easy);
            CreateChunkVariant("chunk_flat_med", ChunkDifficulty.Medium);
            CreateChunkVariant("chunk_elevated", ChunkDifficulty.Elevated);
            CreateChunkVariant("chunk_gap", ChunkDifficulty.Gap);
            CreateChunkVariant("chunk_slide", ChunkDifficulty.Slide);
            CreateChunkVariant("chunk_mixed_hard", ChunkDifficulty.Hard);

            Debug.Log("[RunnerSetup] ✓ 6 chunk prefab categories registered");
        }

        enum ChunkDifficulty { Easy, Medium, Elevated, Gap, Slide, Hard }

        void CreateChunkVariant(string key, ChunkDifficulty diff)
        {
            var chunk = new GameObject($"Chunk_{key}");
            chunk.SetActive(false);

            float w = 20f;

            // Ground platform
            if (diff != ChunkDifficulty.Gap)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                ground.transform.SetParent(chunk.transform);
                ground.transform.localPosition = new Vector3(w / 2, -0.5f, 0);
                ground.transform.localScale = new Vector3(w, 1, 3);
                var r = ground.GetComponent<Renderer>();
                r.material = groundMaterial ?? new Material(Shader.Find("Standard")) { color = groundColor };
                // Ground collider for reference (player uses raycasts or position checks)
            }

            // Populate obstacles based on difficulty
            switch (diff)
            {
                case ChunkDifficulty.Easy:
                    PlaceObstacle(chunk.transform, "obs_crate", new Vector3(8, 0.5f, 0));
                    PlaceCoins(chunk.transform, new Vector3(4, 1f, 0), 5, 1.2f);
                    break;

                case ChunkDifficulty.Medium:
                    PlaceObstacle(chunk.transform, "obs_crate", new Vector3(5, 0.5f, 0));
                    PlaceObstacle(chunk.transform, "obs_crate2", new Vector3(10, 1f, 0));
                    PlaceObstacle(chunk.transform, "obs_beam_low", new Vector3(15, 1.5f, 0));
                    PlaceCoins(chunk.transform, new Vector3(7, 2f, 0), 3, 1f); // Arc above crates
                    break;

                case ChunkDifficulty.Elevated:
                    // Raised platform
                    var plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plat.name = "Platform";
                    plat.transform.SetParent(chunk.transform);
                    plat.transform.localPosition = new Vector3(10, 1.5f, 0);
                    plat.transform.localScale = new Vector3(6, 0.5f, 3);
                    plat.GetComponent<Renderer>().material = new Material(Shader.Find("Standard")) { color = new Color(0.5f, 0.45f, 0.35f) };
                    PlaceCoins(chunk.transform, new Vector3(8, 3f, 0), 4, 1f);
                    PlaceObstacle(chunk.transform, "obs_crate", new Vector3(4, 0.5f, 0));
                    break;

                case ChunkDifficulty.Gap:
                    // Ground with a gap in the middle
                    var gLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    gLeft.name = "Ground_Left";
                    gLeft.transform.SetParent(chunk.transform);
                    gLeft.transform.localPosition = new Vector3(4, -0.5f, 0);
                    gLeft.transform.localScale = new Vector3(8, 1, 3);
                    gLeft.GetComponent<Renderer>().material = groundMaterial ?? new Material(Shader.Find("Standard")) { color = groundColor };

                    var gRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    gRight.name = "Ground_Right";
                    gRight.transform.SetParent(chunk.transform);
                    gRight.transform.localPosition = new Vector3(16, -0.5f, 0);
                    gRight.transform.localScale = new Vector3(8, 1, 3);
                    gRight.GetComponent<Renderer>().material = groundMaterial ?? new Material(Shader.Find("Standard")) { color = groundColor };

                    // Kill zone in gap
                    var killZone = new GameObject("KillZone");
                    killZone.transform.SetParent(chunk.transform);
                    killZone.transform.localPosition = new Vector3(10, -2f, 0);
                    killZone.tag = "Spike"; // Instant kill
                    if (use2DPhysics) { var kc = killZone.AddComponent<BoxCollider2D>(); kc.size = new Vector2(3, 2); kc.isTrigger = true; }
                    else { var kc = killZone.AddComponent<BoxCollider>(); kc.size = new Vector3(3, 2, 3); kc.isTrigger = true; }

                    PlaceCoins(chunk.transform, new Vector3(10, 2.5f, 0), 3, 0.8f); // Lure over gap
                    break;

                case ChunkDifficulty.Slide:
                    PlaceObstacle(chunk.transform, "obs_beam_low", new Vector3(5, 1.5f, 0));
                    PlaceObstacle(chunk.transform, "obs_beam_low", new Vector3(10, 1.5f, 0));
                    PlaceObstacle(chunk.transform, "obs_crate", new Vector3(15, 0.5f, 0));
                    PlaceCoins(chunk.transform, new Vector3(7, 0.3f, 0), 4, 1.5f); // Low coins
                    break;

                case ChunkDifficulty.Hard:
                    PlaceObstacle(chunk.transform, "obs_crate", new Vector3(3, 0.5f, 0));
                    PlaceObstacle(chunk.transform, "obs_saw", new Vector3(7, 1.5f, 0));
                    PlaceObstacle(chunk.transform, "obs_beam_low", new Vector3(11, 1.5f, 0));
                    PlaceObstacle(chunk.transform, "obs_crate2", new Vector3(15, 1f, 0));
                    PlaceCoins(chunk.transform, new Vector3(5, 2.5f, 0), 3, 0.8f);
                    PlacePowerUp(chunk.transform, new Vector3(18, 1.5f, 0));
                    break;
            }

            // Destroy existing ground if gap (we already made custom ground)
            if (diff == ChunkDifficulty.Gap)
            {
                var defaultGround = chunk.transform.Find("Ground");
                if (defaultGround) Destroy(defaultGround.gameObject);
            }

            Bill.Pool.Register(key, chunk, 3);
        }

        void PlaceObstacle(Transform parent, string obsType, Vector3 localPos)
        {
            GameObject obs;
            Color color;
            Vector3 scale;

            switch (obsType)
            {
                case "obs_crate":
                    obs = cratePrefab ? Instantiate(cratePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                    scale = new Vector3(1, 1, 1); color = new Color(0.6f, 0.4f, 0.2f);
                    break;
                case "obs_crate2":
                    obs = cratePrefab ? Instantiate(cratePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                    scale = new Vector3(1, 2, 1); color = new Color(0.55f, 0.35f, 0.18f);
                    break;
                case "obs_beam_low":
                    obs = lowBeamPrefab ? Instantiate(lowBeamPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                    scale = new Vector3(3, 0.3f, 2); color = new Color(0.5f, 0.5f, 0.5f);
                    break;
                case "obs_saw":
                    obs = sawBladePrefab ? Instantiate(sawBladePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    scale = new Vector3(1.2f, 0.1f, 1.2f); color = new Color(0.7f, 0.7f, 0.7f);
                    var sawObs = obs.AddComponent<RunnerObstacle>();
                    sawObs.type = RunnerObstacle.ObstacleType.SawBlade;
                    sawObs.sinWave = true;
                    sawObs.sinAmplitude = 1f;
                    sawObs.sinFrequency = 2f;
                    break;
                default:
                    obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    scale = Vector3.one; color = Color.red;
                    break;
            }

            obs.name = obsType;
            obs.transform.SetParent(parent);
            obs.transform.localPosition = localPos;
            obs.transform.localScale = scale;
            obs.tag = "Obstacle";

            if (obs.GetComponent<RunnerObstacle>() == null)
            {
                var ro = obs.AddComponent<RunnerObstacle>();
                ro.instantKill = (obsType == "obs_spike");
            }

            // Set material if placeholder
            var renderer = obs.GetComponent<Renderer>();
            if (renderer != null && obsType.StartsWith("obs_") && cratePrefab == null)
            {
                renderer.material = new Material(Shader.Find("Standard")) { color = color };
            }

            // Collider
            if (use2DPhysics)
            {
                var col3d = obs.GetComponent<Collider>();
                if (col3d) Destroy(col3d);
                var col2d = obs.AddComponent<BoxCollider2D>();
                col2d.isTrigger = true;
            }
            else
            {
                var col = obs.GetComponent<Collider>();
                if (col) col.isTrigger = true;
                else { obs.AddComponent<BoxCollider>().isTrigger = true; }
            }
        }

        void PlaceCoins(Transform parent, Vector3 startPos, int count, float spacing)
        {
            for (int i = 0; i < count; i++)
            {
                var coinGO = coinPrefab ? Instantiate(coinPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coinGO.name = $"Coin_{i}";
                coinGO.transform.SetParent(parent);
                coinGO.transform.localPosition = startPos + Vector3.right * (i * spacing);
                coinGO.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
                coinGO.tag = "Coin";

                if (coinPrefab == null)
                {
                    var r = coinGO.GetComponent<Renderer>();
                    r.material = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.85f, 0.2f) };
                }

                if (coinGO.GetComponent<RunnerCollectible>() == null)
                    coinGO.AddComponent<RunnerCollectible>();

                if (use2DPhysics)
                {
                    var c3 = coinGO.GetComponent<Collider>(); if (c3) Destroy(c3);
                    var c2 = coinGO.AddComponent<CircleCollider2D>(); c2.radius = 0.3f; c2.isTrigger = true;
                }
                else
                {
                    var col = coinGO.GetComponent<Collider>();
                    if (col) col.isTrigger = true;
                    else { var sc = coinGO.AddComponent<SphereCollider>(); sc.radius = 0.3f; sc.isTrigger = true; }
                }
            }
        }

        void PlacePowerUp(Transform parent, Vector3 pos)
        {
            string[] items = { "item_magnet", "item_shield", "item_speed", "item_2x", "item_tiny" };
            float[] durations = { 8f, 0f, 5f, 10f, 6f }; // shield = single hit

            int idx = Random.Range(0, items.Length);

            var itemGO = powerUpPrefab ? Instantiate(powerUpPrefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            itemGO.name = items[idx];
            itemGO.transform.SetParent(parent);
            itemGO.transform.localPosition = pos;
            itemGO.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            itemGO.tag = "PowerUp";

            if (powerUpPrefab == null)
            {
                var r = itemGO.GetComponent<Renderer>();
                Color c = idx switch
                {
                    0 => Color.magenta,
                    1 => new Color(0.3f, 0.8f, 1f),
                    2 => Color.yellow,
                    3 => Color.green,
                    4 => new Color(0.8f, 0.5f, 1f),
                    _ => Color.white
                };
                r.material = new Material(Shader.Find("Standard")) { color = c };
            }

            var pu = itemGO.AddComponent<RunnerPowerUp>();
            pu.itemKey = items[idx];
            pu.duration = durations[idx];

            if (use2DPhysics)
            {
                var c3 = itemGO.GetComponent<Collider>(); if (c3) Destroy(c3);
                var c2 = itemGO.AddComponent<CircleCollider2D>(); c2.radius = 0.4f; c2.isTrigger = true;
            }
            else
            {
                var col = itemGO.GetComponent<Collider>();
                if (col) col.isTrigger = true;
                else { var sc = itemGO.AddComponent<SphereCollider>(); sc.radius = 0.4f; sc.isTrigger = true; }
            }
        }

        // ─── COLLECTIBLE / VFX PREFABS ──────────

        void CreateCollectiblePrefabs()
        {
            // These are for coins spawned individually via Pool.Spawn (magnet pull etc.)
            var coinProto = coinPrefab ? Instantiate(coinPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coinProto.name = "Coin_Pooled";
            coinProto.SetActive(false);
            coinProto.transform.localScale = new Vector3(0.4f, 0.05f, 0.4f);
            coinProto.tag = "Coin";
            if (coinProto.GetComponent<RunnerCollectible>() == null) coinProto.AddComponent<RunnerCollectible>();
            Bill.Pool.Register("coin_bronze", coinProto, 20);
        }

        void CreatePowerUpPrefabs()
        {
            // Power-ups are embedded in chunks, no separate pool needed for basic setup
        }

        void CreateVFXPrefabs()
        {
            // Simple dust particle for jump
            var dustGO = new GameObject("VFX_Dust");
            dustGO.SetActive(false);
            var ps = dustGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 1f;
            main.startSize = 0.2f;
            main.maxParticles = 5;
            main.duration = 0.2f;
            main.loop = false;
            main.startColor = new Color(0.7f, 0.6f, 0.4f, 0.6f);
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 5) });

            Bill.Pool.Register("vfx_dust", dustGO, 5);

            Debug.Log("[RunnerSetup] ✓ VFX prefabs registered");
        }

        // ─── PARALLAX BG ────────────────────────

        void CreateParallaxBackground()
        {
            var bgRoot = new GameObject("ParallaxRoot");

            CreateBGLayer(bgRoot.transform, "Sky", 0.05f, skyColor * 0.95f, 0, 8f);
            CreateBGLayer(bgRoot.transform, "Mountains", 0.15f, new Color(0.4f, 0.5f, 0.6f), 0, 4f);
            CreateBGLayer(bgRoot.transform, "TreesFar", 0.3f, new Color(0.15f, 0.35f, 0.15f), -1f, 3f);
            CreateBGLayer(bgRoot.transform, "TreesNear", 0.5f, new Color(0.2f, 0.45f, 0.2f), -1.5f, 2.5f);

            Debug.Log("[RunnerSetup] ✓ Parallax background (4 layers)");
        }

        void CreateBGLayer(Transform parent, string name, float ratio, Color color, float yPos, float height)
        {
            var layer = new GameObject($"BG_{name}");
            layer.transform.SetParent(parent);
            layer.transform.position = new Vector3(0, yPos, 10 - ratio * 10); // Further back for slower layers

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Tile";
            quad.transform.SetParent(layer.transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = new Vector3(40, height, 1);
            var r = quad.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            var col = quad.GetComponent<Collider>(); if (col) Destroy(col);

            var parallax = layer.AddComponent<RunnerParallaxLayer>();
            parallax.speedRatio = ratio;
            parallax.width = 40f;
            parallax.Init(Camera.main.transform);
        }

        // ─── UI ──────────────────────────────────

        void CreateUI()
        {
            var canvasGO = new GameObject("RunnerCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // HUD
            var hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(canvasGO.transform, false);
            _hud = hudGO.AddComponent<RunnerHUD>();

            // Menu group
            var menuGroup = CreateUIGroup("MenuGroup", hudGO.transform);
            _hud.menuGroup = menuGroup;
            _hud.menuBestText = CreateText("Best: 0m", menuGroup.transform, new Vector2(0, 100), 36, Color.white);
            _hud.menuCoinsText = CreateText("Coins: 0", menuGroup.transform, new Vector2(0, 50), 28, Color.yellow);
            CreateText("BILL RUNNER", menuGroup.transform, new Vector2(0, 250), 64, Color.white);
            CreateText("TAP TO RUN", menuGroup.transform, new Vector2(0, -50), 28, new Color(1, 1, 1, 0.7f));

            // Gameplay group
            var gpGroup = CreateUIGroup("GameplayGroup", hudGO.transform);
            _hud.gameplayGroup = gpGroup;
            _hud.distanceText = CreateText("0m", gpGroup.transform, new Vector2(0, 480), 48, Color.white);
            _hud.coinsText = CreateText("0", gpGroup.transform, new Vector2(400, 480), 32, Color.yellow);

            var heartsGO = new GameObject("Hearts");
            heartsGO.transform.SetParent(gpGroup.transform, false);
            var hrt = heartsGO.AddComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(-400, 480);
            hrt.sizeDelta = new Vector2(200, 40);
            var hlg = heartsGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            _hud.heartsContainer = heartsGO.transform;

            // Game Over Panel
            var goPanelGO = new GameObject("GameOverPanel");
            goPanelGO.transform.SetParent(canvasGO.transform, false);
            _goPanel = goPanelGO.AddComponent<RunnerGameOverPanel>();

            var goRT = goPanelGO.AddComponent<RectTransform>();
            goRT.anchorMin = Vector2.zero; goRT.anchorMax = Vector2.one; goRT.sizeDelta = Vector2.zero;
            _goPanel.canvasGroup = goPanelGO.AddComponent<CanvasGroup>();

            // Overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(goPanelGO.transform, false);
            var oRT = overlay.AddComponent<RectTransform>();
            oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one; oRT.sizeDelta = Vector2.zero;
            overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            // Card
            var card = new GameObject("Card");
            card.transform.SetParent(goPanelGO.transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f); cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(500, 500);
            card.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            _goPanel.panelRoot = cardRT;

            CreateText("RUN OVER", card.transform, new Vector2(0, 180), 48, Color.white);
            _goPanel.distText = CreateText("0m", card.transform, new Vector2(0, 100), 64, new Color(0.3f, 0.9f, 1f));
            _goPanel.coinsEarnedText = CreateText("+0", card.transform, new Vector2(0, 40), 32, Color.yellow);
            _goPanel.bestText = CreateText("Best: 0m", card.transform, new Vector2(0, -10), 24, new Color(0.7f, 0.7f, 0.7f));

            var newBest = CreateText("NEW BEST!", card.transform, new Vector2(0, -50), 28, new Color(1f, 0.8f, 0.2f));
            _goPanel.newBestLabel = newBest;

            _goPanel.retryButton = CreateButton("RETRY", card.transform, new Vector2(-100, -160), new Vector2(160, 60), new Color(0.2f, 0.7f, 0.3f));
            _goPanel.menuButton = CreateButton("MENU", card.transform, new Vector2(100, -160), new Vector2(160, 60), new Color(0.7f, 0.4f, 0.2f));

            Debug.Log("[RunnerSetup] ✓ UI created");
        }

        // ─── CHUNK SPAWNER ──────────────────────

        void CreateChunkSpawner()
        {
            var go = new GameObject("ChunkSpawner");
            _chunkSpawner = go.AddComponent<RunnerChunkSpawner>();
            Debug.Log("[RunnerSetup] ✓ ChunkSpawner created");
        }

        // ─── GAME MANAGER ───────────────────────

        void CreateGameManager()
        {
            var go = new GameObject("RunnerGameManager");
            _manager = go.AddComponent<RunnerGameManager>();
            _manager.player = _player;
            _manager.chunkSpawner = _chunkSpawner;
            _manager.runnerCamera = _runnerCamera;
            _manager.hud = _hud;
            _manager.gameOverPanel = _goPanel;
            _goPanel.Init(_manager);

            Debug.Log("[RunnerSetup] ✓ GameManager wired");
        }

        // ─── UI Helpers ─────────────────────────

        GameObject CreateUIGroup(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            return go;
        }

        Text CreateText(string content, Transform parent, Vector2 pos, int size, Color color)
        {
            var go = new GameObject(content.Replace(" ", "").Replace(":", ""));
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(500, size + 20);
            var t = go.AddComponent<Text>();
            t.text = content; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
            t.color = color; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Button CreateButton(string label, Transform parent, Vector2 pos, Vector2 size, Color bgColor)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.color = bgColor;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<Text>();
            t.text = label; t.fontSize = 24; t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return btn;
        }
    }
}
