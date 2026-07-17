using UnityEngine;
using UnityEngine.UI;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════════╗
    /// ║  BILL DEFENSE — MEGA SETUP SCRIPT                         ║
    /// ║  Drop on an empty GO, press Play → full Tower Defense.    ║
    /// ║  Creates: grid map, path, tower slots, enemy prefabs,     ║
    /// ║  projectile pools, VFX, full UI, camera.                  ║
    /// ║                                                            ║
    /// ║  Assign your 3D models in Inspector to replace            ║
    /// ║  placeholder cubes/cylinders.                              ║
    /// ╚══════════════════════════════════════════════════════════════╝
    /// </summary>
    public class TDSetup : MonoBehaviour
    {
        [Header("═══ TOWER MODELS (optional) ═══")]
        [Tooltip("Base prefab for towers. Leave null for placeholder.")]
        public GameObject towerPrefab;

        [Header("═══ ENEMY MODELS (optional) ═══")]
        public GameObject goblinPrefab;
        public GameObject orcPrefab;
        public GameObject wolfPrefab;
        public GameObject skeletonPrefab;
        public GameObject shieldOrcPrefab;
        public GameObject batPrefab;
        public GameObject magePrefab;
        public GameObject golemPrefab;
        public GameObject ghostPrefab;
        public GameObject dragonPrefab;

        [Header("═══ PROJECTILE MODELS (optional) ═══")]
        public GameObject arrowPrefab;
        public GameObject cannonballPrefab;

        [Header("═══ MATERIALS (optional) ═══")]
        public Material pathMaterial;
        public Material buildableMaterial;
        public Material blockedMaterial;

        [Header("═══ MAP SETTINGS ═══")]
        public int mapWidth = 20;
        public int mapHeight = 12;

        [Header("═══ BOOTSTRAP ═══")]
        [Tooltip("True = builds scene in Awake (standalone). False = call Build() from Launcher.")]
        public bool autoBootstrap = true;

        // Created refs
        private TDGrid _grid;
        private TDWaveManager _waveManager;
        private TDGameManager _manager;
        private TDHUD _hud;
        private TDTowerPanel _towerPanel;
        private TDGameOverPanel _goPanel;
        private bool _built;

        void Awake()
        {
            if (autoBootstrap) Build();
        }

        public void Build()
        {
            if (_built) return;
            _built = true;

            Debug.Log("[TDSetup] ═══ Building Tower Defense Scene ═══");

            CreateCamera();
            CreateGrid();
            BuildMap1();
            CreateEnemyPrefabs();
            CreateProjectilePrefabs();
            CreateVFXPrefabs();
            CreateUI();
            CreateWaveManager();
            CreateGameManager();

            Debug.Log("[TDSetup] ═══ Scene Ready! Place towers and defend! ═══");
        }

        public void Teardown()
        {
            if (_manager != null) _manager.Cleanup();
            if (_waveManager != null) { _waveManager.StopSpawning(); _waveManager.ReturnAllEnemies(); }
            if (_grid != null) _grid.ClearAllTowers();
            // Return all TD pools
            foreach (var def in TDDatabase.Enemies) Bill.Pool.ReturnAll($"enemy_{def.type.ToString().ToLower()}");
            string[] projKeys = {"proj_arrow","proj_cannonball","proj_ice","proj_lightning","proj_sniper","proj_poison"};
            foreach (var k in projKeys) Bill.Pool.ReturnAll(k);
            Bill.Pool.ReturnAll("vfx_explosion");
            Bill.Pool.ReturnAll("vfx_line");
            Bill.Pool.ReturnAll("ui_float_text");
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

            // Top-down view
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(mapWidth / 2f, 15, mapHeight / 2f - 2f);
            cam.transform.rotation = Quaternion.Euler(70, 0, 0);
            cam.backgroundColor = new Color(0.25f, 0.3f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            Debug.Log("[TDSetup] ✓ Camera (top-down, ortho size=8)");
        }

        // ─── GRID ────────────────────────────────

        void CreateGrid()
        {
            var gridGO = new GameObject("Grid");
            _grid = gridGO.AddComponent<TDGrid>();
            _grid.Init(mapWidth, mapHeight);
            _grid.pathMaterial = pathMaterial;
            _grid.buildableMaterial = buildableMaterial;
            _grid.blockedMaterial = blockedMaterial;

            Debug.Log("[TDSetup] ✓ Grid created");
        }

        // ─── MAP 1: GREEN VALLEY ─────────────────

        void BuildMap1()
        {
            // Define path tiles
            int[,] pathCoords = {
                // Row: y, x coords where path goes
                // Snake path from left to right
                {1, 2}, {2, 2}, {3, 2}, {4, 2}, {5, 2}, {6, 2}, {7, 2}, // Top horizontal
                {7, 3}, {7, 4}, {7, 5}, {7, 6}, // Down
                {6, 6}, {5, 6}, {4, 6}, {3, 6}, // Left
                {3, 7}, {3, 8},                   // Down
                {4, 8}, {5, 8}, {6, 8}, {7, 8}, {8, 8}, {9, 8}, {10, 8}, {11, 8}, // Right
                {11, 7}, {11, 6}, {11, 5},         // Up
                {12, 5}, {13, 5}, {14, 5}, {15, 5}, {16, 5}, // Right
                {16, 6}, {16, 7}, {16, 8}, {16, 9}, // Down
                {15, 9}, {14, 9}, {13, 9}, {12, 9}, {11, 9}, {10, 9}, {9, 9}, {8, 9}, // Left
                {8, 10},                             // Down
                {9, 10}, {10, 10}, {11, 10}, {12, 10}, {13, 10}, {14, 10}, // Right to exit
            };

            // Set path tiles
            for (int i = 0; i < pathCoords.GetLength(0); i++)
            {
                int x = pathCoords[i, 0];
                int y = pathCoords[i, 1];
                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                    _grid.SetTile(x, y, TileType.Path);
            }

            // Set buildable tiles around the path
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (_grid != null)
                    {
                        // Check if adjacent to path
                        bool nearPath = false;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
                                {
                                    // We need to check the tile type but we set path above
                                    // Simple heuristic: make tiles near path buildable
                                }
                            }
                        }
                    }
                }
            }

            // Manually set buildable zones (near path bends)
            int[][] buildableZones = {
                new[]{3,1}, new[]{4,1}, new[]{5,1},  // Above first horizontal
                new[]{3,3}, new[]{4,3}, new[]{5,3},  // Below first horizontal
                new[]{8,3}, new[]{8,4}, new[]{8,5},  // Inside first turn
                new[]{5,5}, new[]{6,5}, new[]{4,5},  // Inner path
                new[]{9,7}, new[]{10,7}, new[]{9,6}, new[]{10,6}, // Inside big loop
                new[]{13,4}, new[]{14,4}, new[]{15,4}, // Above second horizontal
                new[]{13,6}, new[]{14,6}, new[]{15,6}, // Below second horizontal
                new[]{17,6}, new[]{17,7}, new[]{17,8}, // Right of down section
                new[]{12,8}, new[]{13,8},               // Inside bottom path
                new[]{9,11}, new[]{10,11}, new[]{11,11}, new[]{12,11}, // Below exit path
            };

            foreach (var b in buildableZones)
            {
                if (b[0] >= 0 && b[0] < mapWidth && b[1] >= 0 && b[1] < mapHeight)
                    _grid.SetTile(b[0], b[1], TileType.Buildable);
            }

            // Spawn and exit
            _grid.SetSpawn(1, 2);
            _grid.SetExit(14, 10);

            // Build waypoints (world positions along the path, in order)
            // Simplified: key turning points
            Vector3[] waypoints = {
                _grid.GridToWorld(1, 2),
                _grid.GridToWorld(7, 2),
                _grid.GridToWorld(7, 6),
                _grid.GridToWorld(3, 6),
                _grid.GridToWorld(3, 8),
                _grid.GridToWorld(11, 8),
                _grid.GridToWorld(11, 5),
                _grid.GridToWorld(16, 5),
                _grid.GridToWorld(16, 9),
                _grid.GridToWorld(8, 9),
                _grid.GridToWorld(8, 10),
                _grid.GridToWorld(14, 10),
            };

            foreach (var wp in waypoints)
                _grid.AddWaypoint(wp + Vector3.up * 0.2f); // Slightly above ground

            _grid.BuildVisuals();

            // Path visual: place small markers at waypoints for debugging
            for (int i = 0; i < waypoints.Length; i++)
            {
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Waypoint_{i}";
                marker.transform.position = waypoints[i] + Vector3.up * 0.3f;
                marker.transform.localScale = Vector3.one * 0.2f;
                marker.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
                Destroy(marker.GetComponent<Collider>());
            }

            // Spawn point marker
            var spawnMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnMarker.name = "SpawnPoint";
            spawnMarker.transform.position = waypoints[0] + Vector3.up * 0.5f;
            spawnMarker.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            spawnMarker.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.3f, 0.3f) };
            Destroy(spawnMarker.GetComponent<Collider>());

            // Exit marker
            var exitMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitMarker.name = "ExitPoint";
            exitMarker.transform.position = waypoints[waypoints.Length - 1] + Vector3.up * 0.5f;
            exitMarker.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            exitMarker.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.3f, 0.3f, 1f) };
            Destroy(exitMarker.GetComponent<Collider>());

            Debug.Log("[TDSetup] ✓ Map 1 'Green Valley' built");
        }

        // ─── ENEMY PREFABS ──────────────────────

        void CreateEnemyPrefabs()
        {
            foreach (var def in TDDatabase.Enemies)
            {
                string key = $"enemy_{def.type.ToString().ToLower()}";
                GameObject prefab = GetEnemyPrefab(def.type);

                if (prefab == null)
                {
                    // Placeholder
                    prefab = CreatePlaceholderEnemy(def);
                }

                prefab.name = key;
                prefab.SetActive(false);

                // Ensure TDEnemy component
                if (prefab.GetComponent<TDEnemy>() == null)
                    prefab.AddComponent<TDEnemy>();

                // Collider for tower detection
                if (prefab.GetComponent<Collider>() == null)
                {
                    var sc = prefab.AddComponent<SphereCollider>();
                    sc.radius = 0.4f;
                    sc.isTrigger = true;
                }

                int warmCount = def.type switch
                {
                    EnemyType.Dragon => 1,
                    EnemyType.Golem => 3,
                    EnemyType.DarkMage or EnemyType.ShieldOrc => 6,
                    EnemyType.Wolf or EnemyType.Bat => 12,
                    _ => 10
                };

                Bill.Pool.Register(key, prefab, warmCount);
            }

            Debug.Log($"[TDSetup] ✓ {TDDatabase.Enemies.Length} enemy prefabs registered");
        }

        GameObject GetEnemyPrefab(EnemyType type)
        {
            return type switch
            {
                EnemyType.Goblin => goblinPrefab,
                EnemyType.Orc => orcPrefab,
                EnemyType.Wolf => wolfPrefab,
                EnemyType.Skeleton => skeletonPrefab,
                EnemyType.ShieldOrc => shieldOrcPrefab,
                EnemyType.Bat => batPrefab,
                EnemyType.DarkMage => magePrefab,
                EnemyType.Golem => golemPrefab,
                EnemyType.Ghost => ghostPrefab,
                EnemyType.Dragon => dragonPrefab,
                _ => null
            };
        }

        GameObject CreatePlaceholderEnemy(EnemyDefinition def)
        {
            var go = new GameObject(def.displayName);

            // Body
            PrimitiveType bodyType = def.type switch
            {
                EnemyType.Bat => PrimitiveType.Sphere,
                EnemyType.Golem => PrimitiveType.Cube,
                EnemyType.Dragon => PrimitiveType.Capsule,
                _ => PrimitiveType.Capsule
            };

            var body = GameObject.CreatePrimitive(bodyType);
            body.name = "Body";
            body.transform.SetParent(go.transform);
            body.transform.localPosition = Vector3.up * 0.3f;

            float scale = def.type switch
            {
                EnemyType.Wolf => 0.3f,
                EnemyType.Bat => 0.25f,
                EnemyType.Golem => 0.6f,
                EnemyType.Dragon => 0.8f,
                EnemyType.Goblin => 0.3f,
                _ => 0.4f
            };
            body.transform.localScale = Vector3.one * scale;

            var r = body.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard")) { color = def.color };
            Destroy(body.GetComponent<Collider>());

            // HP bar background
            var hpBG = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hpBG.name = "HPBarBG";
            hpBG.transform.SetParent(go.transform);
            hpBG.transform.localPosition = new Vector3(0, scale + 0.5f, 0);
            hpBG.transform.localScale = new Vector3(0.6f, 0.08f, 0.08f);
            hpBG.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = Color.black };
            Destroy(hpBG.GetComponent<Collider>());

            // HP bar fill
            var hpFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hpFill.name = "HPBarFill";
            hpFill.transform.SetParent(hpBG.transform);
            hpFill.transform.localPosition = Vector3.zero;
            hpFill.transform.localScale = Vector3.one * 0.9f;
            hpFill.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = Color.green };
            Destroy(hpFill.GetComponent<Collider>());

            // Wire TDEnemy refs
            var enemy = go.AddComponent<TDEnemy>();
            enemy.modelRoot = body.transform;
            enemy.hpBarFill = hpFill.transform;

            return go;
        }

        // ─── PROJECTILE PREFABS ──────────────────

        void CreateProjectilePrefabs()
        {
            CreateProjPrefab("proj_arrow", arrowPrefab, new Color(0.6f, 0.4f, 0.15f), new Vector3(0.3f, 0.05f, 0.05f), 20);
            CreateProjPrefab("proj_cannonball", cannonballPrefab, new Color(0.3f, 0.3f, 0.3f), Vector3.one * 0.2f, 10);
            CreateProjPrefab("proj_ice", null, new Color(0.5f, 0.8f, 1f), Vector3.one * 0.12f, 10);
            CreateProjPrefab("proj_lightning", null, Color.yellow, Vector3.one * 0.1f, 8);
            CreateProjPrefab("proj_sniper", null, Color.red, new Vector3(0.4f, 0.03f, 0.03f), 5);
            CreateProjPrefab("proj_poison", null, new Color(0.2f, 0.8f, 0.1f), Vector3.one * 0.15f, 10);

            Debug.Log("[TDSetup] ✓ 6 projectile prefabs registered");
        }

        void CreateProjPrefab(string key, GameObject customPrefab, Color color, Vector3 scale, int warm)
        {
            GameObject prefab;
            if (customPrefab != null)
            {
                prefab = Instantiate(customPrefab);
            }
            else
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                prefab.transform.localScale = scale;
                prefab.GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color")) { color = color };
                Destroy(prefab.GetComponent<Collider>());
            }

            prefab.name = key;
            prefab.SetActive(false);
            if (prefab.GetComponent<TDProjectile>() == null)
                prefab.AddComponent<TDProjectile>();

            Bill.Pool.Register(key, prefab, warm);
        }

        // ─── VFX PREFABS ────────────────────────

        void CreateVFXPrefabs()
        {
            // Explosion
            var explGO = new GameObject("VFX_Explosion");
            explGO.SetActive(false);
            var ps = explGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.4f; main.startSpeed = 3f; main.startSize = 0.15f;
            main.maxParticles = 15; main.duration = 0.2f; main.loop = false;
            main.startColor = new Color(1f, 0.6f, 0.1f);
            var em = ps.emission; em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, 15) });
            Bill.Pool.Register("vfx_explosion", explGO, 8);

            // Line (for lightning / sniper)
            var lineGO = new GameObject("VFX_Line");
            lineGO.SetActive(false);
            var lr = lineGO.AddComponent<LineRenderer>();
            lr.startWidth = 0.05f; lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Unlit/Color")) { color = Color.yellow };
            lr.positionCount = 2;
            Bill.Pool.Register("vfx_line", lineGO, 10);

            // Floating text
            var textGO = new GameObject("UI_FloatText");
            textGO.SetActive(false);
            var tm = textGO.AddComponent<TextMesh>();
            tm.fontSize = 32; tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            Bill.Pool.Register("ui_float_text", textGO, 20);

            Debug.Log("[TDSetup] ✓ VFX prefabs (explosion, line, float text)");
        }

        // ─── UI ──────────────────────────────────

        void CreateUI()
        {
            var canvasGO = new GameObject("TDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── HUD ──
            var hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(canvasGO.transform, false);
            _hud = hudGO.AddComponent<TDHUD>();

            // Top bar
            var topBar = CreatePanel("TopBar", canvasGO.transform,
                new Vector2(0, 500), new Vector2(1900, 50), new Color(0, 0, 0, 0.5f));

            _hud.goldText = CreateText("Gold: 300", topBar.transform, new Vector2(-700, 0), 24, Color.yellow);
            _hud.livesText = CreateText("Lives: 20/20", topBar.transform, new Vector2(-450, 0), 24, Color.white);
            _hud.waveText = CreateText("Wave: 0/15", topBar.transform, new Vector2(-200, 0), 24, Color.white);
            _hud.killsText = CreateText("Kills: 0", topBar.transform, new Vector2(0, 0), 24, Color.white);

            // Build phase group
            var buildGroup = new GameObject("BuildPhaseGroup");
            buildGroup.transform.SetParent(canvasGO.transform, false);
            var bgrt = buildGroup.AddComponent<RectTransform>();
            bgrt.anchoredPosition = new Vector2(0, -450);
            _hud.buildPhaseGroup = buildGroup;

            _hud.buildTimerText = CreateText("30s", buildGroup.transform, new Vector2(-200, 0), 32, Color.white);

            // Timer fill bar
            var timerBG = CreatePanel("TimerBG", buildGroup.transform, new Vector2(0, 0), new Vector2(300, 20), new Color(0.3f, 0.3f, 0.3f));
            var timerFill = CreatePanel("TimerFill", timerBG.transform, Vector2.zero, new Vector2(300, 20), new Color(0.2f, 0.8f, 0.3f));
            var fillImg = timerFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            _hud.buildTimerFill = fillImg;

            _hud.sendWaveButton = CreateButton("SEND WAVE (+5g)", buildGroup.transform, new Vector2(300, 0), new Vector2(220, 40), new Color(0.8f, 0.4f, 0.1f));

            // Hint text
            var hint = CreateText("", canvasGO.transform, new Vector2(0, -350), 28, new Color(1, 0.9f, 0.4f));
            hint.gameObject.SetActive(false);
            _hud.hintText = hint;

            // Wave active group
            var waveGroup = new GameObject("WaveActiveGroup");
            waveGroup.transform.SetParent(canvasGO.transform, false);
            waveGroup.AddComponent<RectTransform>();
            _hud.waveActiveGroup = waveGroup;
            _hud.waveBannerText = CreateText("WAVE 1", waveGroup.transform, new Vector2(0, 300), 56, Color.white);
            waveGroup.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.7f);

            // Wave complete group
            var wcGroup = new GameObject("WaveCompleteGroup");
            wcGroup.transform.SetParent(canvasGO.transform, false);
            wcGroup.AddComponent<RectTransform>();
            _hud.waveCompleteGroup = wcGroup;
            _hud.waveCompleteBonusText = CreateText("Wave Clear! +20g", wcGroup.transform, new Vector2(0, 200), 40, Color.green);

            // Victory group
            var vicGroup = new GameObject("VictoryGroup");
            vicGroup.transform.SetParent(canvasGO.transform, false);
            vicGroup.AddComponent<RectTransform>();
            _hud.victoryGroup = vicGroup;
            CreateText("VICTORY!", vicGroup.transform, new Vector2(0, 200), 64, Color.yellow);
            _hud.victoryStarsText = CreateText("★★★", vicGroup.transform, new Vector2(0, 120), 48, new Color(1, 0.85f, 0.2f));
            _hud.victoryKillsText = CreateText("Enemies defeated: 0", vicGroup.transform, new Vector2(0, 60), 28, Color.white);

            // ── TOWER PANEL (bottom bar) ──
            var tpGO = new GameObject("TowerPanel");
            tpGO.transform.SetParent(canvasGO.transform, false);
            _towerPanel = tpGO.AddComponent<TDTowerPanel>();

            var bottomBar = CreatePanel("BottomBar", canvasGO.transform,
                new Vector2(0, -500), new Vector2(1900, 80), new Color(0, 0, 0, 0.6f));

            _towerPanel.towerButtons = new Button[6];
            _towerPanel.towerCostLabels = new Text[6];

            string[] towerNames = { "Arrow", "Cannon", "Ice", "Lightning", "Sniper", "Poison" };
            Color[] towerColors = {
                new Color(0.3f, 0.6f, 0.2f), new Color(0.6f, 0.35f, 0.15f),
                new Color(0.4f, 0.7f, 0.95f), new Color(0.9f, 0.85f, 0.2f),
                new Color(0.5f, 0.2f, 0.2f), new Color(0.3f, 0.7f, 0.25f)
            };

            for (int i = 0; i < 6; i++)
            {
                float x = -450 + i * 150;
                var btn = CreateButton(towerNames[i], bottomBar.transform, new Vector2(x, 10), new Vector2(130, 40), towerColors[i]);
                _towerPanel.towerButtons[i] = btn;
                _towerPanel.towerCostLabels[i] = CreateText($"{TDDatabase.Towers[i].BaseCost}g", bottomBar.transform, new Vector2(x, -20), 16, Color.yellow);
            }

            // Tower info panel (hidden by default)
            var infoPanel = CreatePanel("TowerInfoPanel", canvasGO.transform,
                new Vector2(400, 200), new Vector2(280, 200), new Color(0.1f, 0.1f, 0.15f, 0.9f));
            _towerPanel.infoPanel = infoPanel;
            _towerPanel.infoName = CreateText("Tower Name", infoPanel.transform, new Vector2(0, 70), 22, Color.white);
            _towerPanel.infoLevel = CreateText("Lv1", infoPanel.transform, new Vector2(0, 45), 18, Color.gray);
            _towerPanel.infoStats = CreateText("DMG: 0  SPD: 0s", infoPanel.transform, new Vector2(0, 20), 16, Color.white);
            _towerPanel.infoBonusText = CreateText("", infoPanel.transform, new Vector2(0, -5), 14, Color.yellow);
            _towerPanel.upgradeButton = CreateButton("Upgrade", infoPanel.transform, new Vector2(-60, -45), new Vector2(110, 30), new Color(0.2f, 0.6f, 0.3f));
            _towerPanel.upgradeCostText = CreateText("100g", infoPanel.transform, new Vector2(-60, -70), 14, Color.yellow);
            _towerPanel.sellButton = CreateButton("Sell", infoPanel.transform, new Vector2(60, -45), new Vector2(110, 30), new Color(0.7f, 0.3f, 0.2f));
            _towerPanel.sellPriceText = CreateText("56g", infoPanel.transform, new Vector2(60, -70), 14, Color.yellow);
            _towerPanel.targetButton = CreateButton("Target: First", infoPanel.transform, new Vector2(0, -90), new Vector2(230, 24), new Color(0.3f, 0.3f, 0.4f));
            _towerPanel.targetModeText = _towerPanel.targetButton.GetComponentInChildren<Text>();
            infoPanel.SetActive(false);

            // ── GAME OVER PANEL ──
            var goPanelGO = new GameObject("GameOverPanel");
            goPanelGO.transform.SetParent(canvasGO.transform, false);
            _goPanel = goPanelGO.AddComponent<TDGameOverPanel>();
            var goRT = goPanelGO.AddComponent<RectTransform>();
            goRT.anchorMin = Vector2.zero; goRT.anchorMax = Vector2.one; goRT.sizeDelta = Vector2.zero;
            _goPanel.canvasGroup = goPanelGO.AddComponent<CanvasGroup>();

            var goOverlay = CreatePanel("Overlay", goPanelGO.transform, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.7f));
            var goORT = goOverlay.GetComponent<RectTransform>();
            goORT.anchorMin = Vector2.zero; goORT.anchorMax = Vector2.one; goORT.sizeDelta = Vector2.zero;

            var goCard = CreatePanel("Card", goPanelGO.transform, Vector2.zero, new Vector2(400, 300), new Color(0.15f, 0.1f, 0.1f, 0.95f));
            CreateText("DEFEATED", goCard.transform, new Vector2(0, 100), 48, Color.red);
            _goPanel.waveReachedText = CreateText("Reached Wave 5", goCard.transform, new Vector2(0, 30), 28, Color.white);
            _goPanel.killCountText = CreateText("Enemies Defeated: 0", goCard.transform, new Vector2(0, -10), 22, Color.gray);
            _goPanel.retryButton = CreateButton("RETRY", goCard.transform, new Vector2(-80, -80), new Vector2(130, 45), new Color(0.2f, 0.6f, 0.3f));
            _goPanel.menuButton = CreateButton("MENU", goCard.transform, new Vector2(80, -80), new Vector2(130, 45), new Color(0.5f, 0.3f, 0.2f));

            Debug.Log("[TDSetup] ✓ Full UI created (HUD, TowerPanel, InfoPanel, GameOverPanel)");
        }

        // ─── WAVE MANAGER ────────────────────────

        void CreateWaveManager()
        {
            var go = new GameObject("WaveManager");
            _waveManager = go.AddComponent<TDWaveManager>();

            var spawnGO = new GameObject("SpawnPoint");
            spawnGO.transform.position = _grid.GetSpawnWorldPos();

            _waveManager.Init(TDDatabase.Map1Waves, _grid.GetWaypoints(), spawnGO.transform);

            Debug.Log("[TDSetup] ✓ WaveManager (15 waves loaded)");
        }

        // ─── GAME MANAGER ────────────────────────

        void CreateGameManager()
        {
            var go = new GameObject("TDGameManager");
            _manager = go.AddComponent<TDGameManager>();
            _manager.waveManager = _waveManager;
            _manager.grid = _grid;
            _manager.hud = _hud;
            _manager.towerPanel = _towerPanel;
            _manager.gameOverPanel = _goPanel;

            _hud.Init(_manager);
            _towerPanel.Init(_manager, _grid);
            _goPanel.Init(_manager);

            _grid.towerBasePrefab = towerPrefab;

            Debug.Log("[TDSetup] ✓ GameManager wired to all systems");
        }

        // ─── UI Helpers ──────────────────────────

        GameObject CreatePanel(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.color = color;
            return go;
        }

        Text CreateText(string content, Transform parent, Vector2 pos, int size, Color color)
        {
            var go = new GameObject(content.Length > 0 ? content.Substring(0, Mathf.Min(content.Length, 12)) : "Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(500, size + 16);
            var t = go.AddComponent<Text>();
            t.text = content; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter;
            t.color = color; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        Button CreateButton(string label, Transform parent, Vector2 pos, Vector2 size, Color bg)
        {
            var go = new GameObject("Btn_" + label.Replace(" ", ""));
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.color = bg;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            var t = lbl.AddComponent<Text>();
            t.text = label; t.fontSize = Mathf.Min(20, (int)size.y - 10);
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return btn;
        }
    }
}
