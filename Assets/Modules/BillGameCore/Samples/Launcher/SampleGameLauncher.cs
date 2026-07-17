using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using BillSamples.Flappy;
using BillSamples.Runner;
using BillSamples.TowerDefense;

namespace BillSamples.Launcher
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════════════╗
    /// ║  BILL SAMPLE GAME LAUNCHER                                     ║
    /// ║                                                                 ║
    /// ║  Use this when all 3 sample games live in the SAME project.    ║
    /// ║  It prevents bootstrap conflicts by:                            ║
    /// ║   1. Managing which game is active                             ║
    /// ║   2. Cleaning up state machine between switches                ║
    /// ║   3. Destroying previous game objects before loading next      ║
    /// ║                                                                 ║
    /// ║  SETUP:                                                         ║
    /// ║   • Create a scene called "Launcher"                           ║
    /// ║   • Add empty GO → attach this script                          ║
    /// ║   • Drag your 3 Setup prefabs into the slots                   ║
    /// ║   • Set autoBootstrap = false on ALL 3 Setup scripts           ║
    /// ║   • Press Play → pick a game from the menu                     ║
    /// ║                                                                 ║
    /// ║  OR: skip this entirely and use separate scenes per game       ║
    /// ║  with autoBootstrap = true (standalone mode).                   ║
    /// ╚══════════════════════════════════════════════════════════════════╝
    /// </summary>
    public class SampleGameLauncher : MonoBehaviour
    {
        [Header("═══ GAME SETUP PREFABS ═══")]
        [Tooltip("Prefab with FlappySetup (autoBootstrap = false)")]
        public GameObject flappySetupPrefab;

        [Tooltip("Prefab with RunnerSetup (autoBootstrap = false)")]
        public GameObject runnerSetupPrefab;

        [Tooltip("Prefab with TDSetup (autoBootstrap = false)")]
        public GameObject tdSetupPrefab;

        // Active game tracking
        private enum ActiveGame { None, Flappy, Runner, TowerDefense }
        private ActiveGame _active = ActiveGame.None;

        private GameObject _activeGameRoot;
        private FlappySetup _flappySetup;
        private RunnerSetup _runnerSetup;
        private TDSetup _tdSetup;

        // UI
        private GameObject _launcherUI;
        private bool _uiBuilt;

        void Start()
        {
            if (!Bill.IsReady)
            {
                Bill.Events.Subscribe<GameReadyEvent>(e => ShowLauncherMenu());
                return;
            }
            ShowLauncherMenu();
        }

        void Update()
        {
            // Global back-to-launcher hotkey
            if (_active != ActiveGame.None && Input.GetKeyDown(KeyCode.F12))
            {
                ReturnToLauncher();
            }
        }

        // ─── PUBLIC API ──────────────────────────

        public void LaunchFlappy()
        {
            SwitchTo(ActiveGame.Flappy);
        }

        public void LaunchRunner()
        {
            SwitchTo(ActiveGame.Runner);
        }

        public void LaunchTowerDefense()
        {
            SwitchTo(ActiveGame.TowerDefense);
        }

        public void ReturnToLauncher()
        {
            TeardownActiveGame();
            ShowLauncherMenu();
        }

        // ─── CORE SWITCH LOGIC ───────────────────

        void SwitchTo(ActiveGame target)
        {
            if (_active == target) return;

            // 1. Teardown current game
            TeardownActiveGame();

            // 2. Hide launcher UI
            if (_launcherUI != null) _launcherUI.SetActive(false);

            // 3. Clean BillGameCore state machine (remove all states from previous game)
            Bill.State.Cleanup();

            // 4. Cancel all timers from previous game
            Bill.Timer.CancelAll();

            // 5. Return all pools
            Bill.Pool.ReturnAll();

            // 6. Stop any music
            Bill.Audio.StopMusic(0f);

            // 7. Reset time scale
            Time.timeScale = 1f;

            // 8. Build new game
            _active = target;
            BuildActiveGame();
        }

        void TeardownActiveGame()
        {
            switch (_active)
            {
                case ActiveGame.Flappy:
                    if (_flappySetup != null) _flappySetup.Teardown();
                    break;
                case ActiveGame.Runner:
                    if (_runnerSetup != null) _runnerSetup.Teardown();
                    break;
                case ActiveGame.TowerDefense:
                    if (_tdSetup != null) _tdSetup.Teardown();
                    break;
            }

            // Destroy the game root (all game objects are children)
            if (_activeGameRoot != null)
            {
                Destroy(_activeGameRoot);
                _activeGameRoot = null;
            }

            _flappySetup = null;
            _runnerSetup = null;
            _tdSetup = null;
            _active = ActiveGame.None;
        }

        void BuildActiveGame()
        {
            _activeGameRoot = new GameObject($"[{_active}]_Root");

            switch (_active)
            {
                case ActiveGame.Flappy:
                    GameObject flappyGO;
                    if (flappySetupPrefab != null)
                        flappyGO = Instantiate(flappySetupPrefab, _activeGameRoot.transform);
                    else
                    {
                        flappyGO = new GameObject("FlappySetup");
                        flappyGO.transform.SetParent(_activeGameRoot.transform);
                    }
                    _flappySetup = flappyGO.GetComponent<FlappySetup>();
                    if (_flappySetup == null) _flappySetup = flappyGO.AddComponent<FlappySetup>();
                    _flappySetup.autoBootstrap = false;
                    _flappySetup.Build();
                    break;

                case ActiveGame.Runner:
                    GameObject runnerGO;
                    if (runnerSetupPrefab != null)
                        runnerGO = Instantiate(runnerSetupPrefab, _activeGameRoot.transform);
                    else
                    {
                        runnerGO = new GameObject("RunnerSetup");
                        runnerGO.transform.SetParent(_activeGameRoot.transform);
                    }
                    _runnerSetup = runnerGO.GetComponent<RunnerSetup>();
                    if (_runnerSetup == null) _runnerSetup = runnerGO.AddComponent<RunnerSetup>();
                    _runnerSetup.autoBootstrap = false;
                    _runnerSetup.Build();
                    break;

                case ActiveGame.TowerDefense:
                    GameObject tdGO;
                    if (tdSetupPrefab != null)
                        tdGO = Instantiate(tdSetupPrefab, _activeGameRoot.transform);
                    else
                    {
                        tdGO = new GameObject("TDSetup");
                        tdGO.transform.SetParent(_activeGameRoot.transform);
                    }
                    _tdSetup = tdGO.GetComponent<TDSetup>();
                    if (_tdSetup == null) _tdSetup = tdGO.AddComponent<TDSetup>();
                    _tdSetup.autoBootstrap = false;
                    _tdSetup.Build();
                    break;
            }

            Debug.Log($"[Launcher] Switched to {_active}. Press F12 to return to launcher.");
        }

        // ─── LAUNCHER MENU UI ────────────────────

        void ShowLauncherMenu()
        {
            if (!_uiBuilt) BuildLauncherUI();
            if (_launcherUI != null) _launcherUI.SetActive(true);

            // Reset camera for menu
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.transform.rotation = Quaternion.identity;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            }
        }

        void BuildLauncherUI()
        {
            _launcherUI = new GameObject("LauncherCanvas");
            var canvas = _launcherUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = _launcherUI.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _launcherUI.AddComponent<GraphicRaycaster>();

            // Title
            MakeText("BillGameCore v3", _launcherUI.transform, new Vector2(0, 350), 64, Color.white);
            MakeText("SAMPLE GAMES", _launcherUI.transform, new Vector2(0, 280), 36, new Color(0.6f, 0.8f, 1f));

            // Game buttons
            MakeGameButton("BILL FLAPPY",
                "Arcade · One-tap · Portrait\nTap to fly, dodge pipes, beat your score!",
                new Color(0.95f, 0.8f, 0.2f), new Vector2(0, 130), LaunchFlappy);

            MakeGameButton("BILL RUNNER",
                "Endless Runner · Landscape\nRun, jump, slide, collect coins, buy upgrades!",
                new Color(0.2f, 0.7f, 1f), new Vector2(0, -30), LaunchRunner);

            MakeGameButton("BILL DEFENSE",
                "Tower Defense · Strategy\n6 towers, 10 enemies, 15 waves, upgrade & defend!",
                new Color(0.9f, 0.35f, 0.2f), new Vector2(0, -190), LaunchTowerDefense);

            // Footer
            MakeText("Press F12 during any game to return here", _launcherUI.transform,
                new Vector2(0, -380), 20, new Color(0.5f, 0.5f, 0.5f));
            MakeText("Each game uses separate namespace: BillSamples.Flappy / .Runner / .TowerDefense",
                _launcherUI.transform, new Vector2(0, -420), 16, new Color(0.4f, 0.4f, 0.4f));

            _uiBuilt = true;
        }

        void MakeGameButton(string title, string desc, Color accent, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            // Card background
            var card = new GameObject("Card_" + title.Replace(" ", ""));
            card.transform.SetParent(_launcherUI.transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            cardRT.anchoredPosition = pos;
            cardRT.sizeDelta = new Vector2(700, 120);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.18f, 0.18f, 0.24f);

            // Accent bar
            var bar = new GameObject("AccentBar");
            bar.transform.SetParent(card.transform, false);
            var barRT = bar.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 0); barRT.anchorMax = new Vector2(0, 1);
            barRT.sizeDelta = new Vector2(6, 0); barRT.anchoredPosition = new Vector2(3, 0);
            bar.AddComponent<Image>().color = accent;

            // Title
            MakeText(title, card.transform, new Vector2(0, 20), 32, accent);

            // Description
            MakeText(desc, card.transform, new Vector2(0, -25), 18, new Color(0.65f, 0.65f, 0.7f));

            // Button overlay
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.onClick.AddListener(onClick);

            // Hover color
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.32f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.38f);
            btn.colors = colors;
        }

        Text MakeText(string content, Transform parent, Vector2 pos, int size, Color color)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(680, size * 3 + 10);
            var t = go.AddComponent<Text>();
            t.text = content; t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
