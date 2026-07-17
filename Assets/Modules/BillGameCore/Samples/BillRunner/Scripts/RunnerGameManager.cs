using UnityEngine;
using UnityEngine.UI;
using BillGameCore;

namespace BillSamples.Runner
{
    /// <summary>
    /// Central game manager for Endless Runner.
    /// </summary>
    public class RunnerGameManager : MonoBehaviour
    {
        [Header("References (set by Setup)")]
        public RunnerPlayer player;
        public RunnerChunkSpawner chunkSpawner;
        public RunnerCamera runnerCamera;
        public RunnerHUD hud;
        public RunnerGameOverPanel gameOverPanel;

        // Difficulty
        private static readonly float[] SpeedAtDist = { 6f, 6.8f, 7.5f, 8f, 8.5f, 9f };
        private static readonly float[] DistThresholds = { 0, 200, 400, 600, 800, 1200 };
        private float _lastSpeed;

        void OnEnable()
        {
            Bill.Events.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            Bill.Events.Subscribe<DistanceChangedEvent>(OnDistanceChanged);
            Bill.Events.Subscribe<CoinCollectedEvent>(OnCoinCollected);
        }

        void OnDisable()
        {
            Bill.Events.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            Bill.Events.Unsubscribe<DistanceChangedEvent>(OnDistanceChanged);
            Bill.Events.Unsubscribe<CoinCollectedEvent>(OnCoinCollected);
        }

        void Start()
        {
            // Register states
            Bill.State.AddState(new RunnerMenuState());
            Bill.State.AddState(new RunnerShopState());
            Bill.State.AddState(new RunnerLoadingState());
            Bill.State.AddState(new RunnerPlayState());
            Bill.State.AddState(new RunnerPauseState());
            Bill.State.AddState(new RunnerGameOverState());

            Bill.State.OnEnter<RunnerMenuState>(OnEnterMenu);
            Bill.State.OnEnter<RunnerPlayState>(() => { });
            Bill.State.OnEnter<RunnerGameOverState>(OnEnterGameOver);

            // Cheats
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Bill.Cheat.Register("rcoins", () =>
            {
                int c = Bill.Save.GetInt("runner_coins", 0) + 1000;
                Bill.Save.Set("runner_coins", c);
                Debug.Log($"[Cheat] Coins = {c}");
            }, "Add 1000 coins");
            Bill.Cheat.Register("rgod", () =>
            {
                player.maxHP = 999;
                player.currentHP = 999;
            }, "God mode");
#endif

            Bill.State.GoTo<RunnerMenuState>();
        }

        void Update()
        {
            // Menu tap
            if (Bill.State.IsInState<RunnerMenuState>())
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                    StartRun();
                if (Input.GetKeyDown(KeyCode.Tab))
                    Bill.State.GoTo<RunnerShopState>();
            }

            // Pause
            if (Bill.State.IsInState<RunnerPlayState>() && Input.GetKeyDown(KeyCode.Escape))
                Bill.State.GoTo<RunnerPauseState>();
            else if (Bill.State.IsInState<RunnerPauseState>() && Input.GetKeyDown(KeyCode.Escape))
                Bill.State.GoTo<RunnerPlayState>();

            // Update speed based on distance
            if (Bill.State.IsInState<RunnerPlayState>() && player != null && player.alive)
            {
                float dist = player.distanceTraveled;
                float speed = CalculateSpeed(dist);
                if (!Mathf.Approximately(speed, _lastSpeed))
                {
                    _lastSpeed = speed;
                    player.currentSpeed = speed;
                    Bill.Events.Fire(new SpeedChangedEvent { NewSpeed = speed });
                }
            }
        }

        // ─── State Transitions ───────────────────

        void OnEnterMenu()
        {
            player?.ResetPlayer();
            player?.LoadUpgrades();
            chunkSpawner?.ClearAll();
            gameOverPanel?.Hide();

            int best = Bill.Save.GetInt("runner_best_dist", 0);
            int coins = Bill.Save.GetInt("runner_coins", 0);
            hud?.ShowMenu(best, coins);
        }

        void StartRun()
        {
            Bill.State.GoTo<RunnerLoadingState>();

            // Init world
            chunkSpawner?.Init(player.transform);
            chunkSpawner?.PreGenerate(3);
            player?.ResetPlayer();

            // Short loading delay then start
            Bill.Timer.Delay(0.3f, () =>
            {
                Bill.Events.Fire(new RunnerStartEvent());
                Bill.State.GoTo<RunnerPlayState>();
                hud?.ShowGameplay(player.maxHP);
            });
        }

        void OnEnterGameOver()
        {
            // Already handled by PlayerDiedEvent
        }

        // ─── Events ──────────────────────────────

        void OnPlayerDied(PlayerDiedEvent e)
        {
            // Save
            int totalCoins = Bill.Save.GetInt("runner_coins", 0) + e.Coins;
            Bill.Save.Set("runner_coins", totalCoins);

            int best = Bill.Save.GetInt("runner_best_dist", 0);
            bool isNewBest = e.Distance > best;
            if (isNewBest) Bill.Save.Set("runner_best_dist", e.Distance);

            int runs = Bill.Save.GetInt("runner_total_runs", 0) + 1;
            Bill.Save.Set("runner_total_runs", runs);
            Bill.Save.Flush();

            Bill.State.GoTo<RunnerGameOverState>();
            gameOverPanel?.Show(e.Distance, e.Coins, best, isNewBest);
        }

        void OnDistanceChanged(DistanceChangedEvent e)
        {
            hud?.UpdateDistance(e.Meters);
        }

        void OnCoinCollected(CoinCollectedEvent e)
        {
            if (player != null)
                hud?.UpdateCoins(player.coinsCollected);
        }

        float CalculateSpeed(float distance)
        {
            for (int i = SpeedAtDist.Length - 1; i >= 0; i--)
            {
                if (distance >= DistThresholds[i]) return SpeedAtDist[i];
            }
            return SpeedAtDist[0];
        }

        // ─── Public (UI buttons) ─────────────────

        public void OnRetryButton()
        {
            Bill.Audio.Play("sfx_button");
            StartRun();
        }

        public void OnMenuButton()
        {
            Bill.Audio.Play("sfx_button");
            Bill.State.GoTo<RunnerMenuState>();
        }

        public void OnResumeButton()
        {
            Bill.State.GoTo<RunnerPlayState>();
        }

        public void Cleanup()
        {
            Bill.Events.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            Bill.Events.Unsubscribe<DistanceChangedEvent>(OnDistanceChanged);
            Bill.Events.Unsubscribe<CoinCollectedEvent>(OnCoinCollected);
            Bill.Audio.StopMusic(0f);
            Time.timeScale = 1f;
        }
    }

    // ─── HUD ─────────────────────────────────────

    public class RunnerHUD : MonoBehaviour
    {
        public GameObject menuGroup;
        public Text menuBestText, menuCoinsText;

        public GameObject gameplayGroup;
        public Text distanceText, coinsText;
        public Transform heartsContainer;

        private GameObject[] _heartIcons;

        public void ShowMenu(int best, int coins)
        {
            if (menuGroup) menuGroup.SetActive(true);
            if (gameplayGroup) gameplayGroup.SetActive(false);
            if (menuBestText) menuBestText.text = $"Best: {best}m";
            if (menuCoinsText) menuCoinsText.text = $"Coins: {coins}";
        }

        public void ShowGameplay(int maxHP)
        {
            if (menuGroup) menuGroup.SetActive(false);
            if (gameplayGroup) gameplayGroup.SetActive(true);
            UpdateDistance(0);
            UpdateCoins(0);

            // Create heart icons
            if (heartsContainer != null)
            {
                foreach (Transform child in heartsContainer) Destroy(child.gameObject);
                _heartIcons = new GameObject[maxHP];
                for (int i = 0; i < maxHP; i++)
                {
                    var h = new GameObject($"Heart_{i}");
                    h.transform.SetParent(heartsContainer, false);
                    var img = h.AddComponent<Image>();
                    img.color = Color.red;
                    var rt = h.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(30, 30);
                    _heartIcons[i] = h;
                }
            }
        }

        public void UpdateDistance(int meters)
        {
            if (distanceText) distanceText.text = $"{meters}m";
        }

        public void UpdateCoins(int coins)
        {
            if (coinsText) coinsText.text = coins.ToString();
        }

        public void LoseHeart(int remaining)
        {
            if (_heartIcons == null) return;
            int index = remaining; // lose the next heart
            if (index >= 0 && index < _heartIcons.Length && _heartIcons[index] != null)
            {
                BillTween.Scale(_heartIcons[index].transform, 1.5f, 0.15f)
                    .OnComplete(() => BillTween.Scale(_heartIcons[index].transform, 0f, 0.2f));
            }
        }
    }

    // ─── Game Over Panel ─────────────────────────

    public class RunnerGameOverPanel : MonoBehaviour
    {
        public RectTransform panelRoot;
        public CanvasGroup canvasGroup;
        public Text distText, coinsEarnedText, bestText, newBestLabel;
        public Button retryButton, menuButton;

        private RunnerGameManager _manager;

        public void Init(RunnerGameManager manager)
        {
            _manager = manager;
            if (retryButton) retryButton.onClick.AddListener(() => _manager.OnRetryButton());
            if (menuButton) menuButton.onClick.AddListener(() => _manager.OnMenuButton());
            Hide();
        }

        public void Show(int distance, int coins, int best, bool isNewBest)
        {
            gameObject.SetActive(true);
            if (distText) distText.text = $"{distance}m";
            if (coinsEarnedText) coinsEarnedText.text = $"+{coins}";
            if (bestText) bestText.text = $"Best: {Mathf.Max(distance, best)}m";
            if (newBestLabel) newBestLabel.gameObject.SetActive(isNewBest);

            if (canvasGroup) { canvasGroup.alpha = 0; BillTween.Fade(canvasGroup, 1f, 0.3f); }
            if (panelRoot)
            {
                panelRoot.anchoredPosition = new Vector2(0, -600);
                BillTween.Float(-600f, 0f, 0.4f, v => panelRoot.anchoredPosition = new Vector2(0, v))
                    .SetEase(EaseType.OutBack);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
