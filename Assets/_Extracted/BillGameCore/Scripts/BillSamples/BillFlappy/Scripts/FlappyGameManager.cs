using UnityEngine;
using BillGameCore;

namespace BillSamples.Flappy
{
    /// <summary>
    /// Central game manager for Flappy. Handles score, difficulty, state transitions.
    /// </summary>
    public class FlappyGameManager : MonoBehaviour
    {
        [Header("References (auto-set by Setup)")]
        public FlappyBird bird;
        public FlappyPipeSpawner pipeSpawner;
        public FlappyHUD hud;
        public FlappyGameOverPanel gameOverPanel;
        public Transform cameraRoot; // For screen shake

        // Score
        private int _score;
        private int _bestScore;

        // Difficulty thresholds
        private static readonly int[] DiffScoreThresholds = { 0, 11, 21, 31, 51 };
        private static readonly float[] DiffGapSizes = { 3.2f, 3.0f, 2.8f, 2.6f, 2.4f };
        private static readonly float[] DiffIntervals = { 1.8f, 1.6f, 1.5f, 1.4f, 1.3f };
        private static readonly float[] DiffSpeeds = { 3.0f, 3.3f, 3.5f, 3.8f, 4.0f };

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            Bill.Events.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            Bill.Events.Subscribe<BirdDiedEvent>(OnBirdDied);

            Bill.State.OnEnter<FlappyMenuState>(OnEnterMenu);
            Bill.State.OnEnter<FlappyPlayState>(OnEnterPlay);
            Bill.State.OnEnter<FlappyGameOverState>(OnEnterGameOver);
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;
            Bill.Events.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            Bill.Events.Unsubscribe<BirdDiedEvent>(OnBirdDied);
        }

        void Start()
        {
            if (!Application.isPlaying) return;
            _bestScore = Bill.Save.GetInt("flappy_best", 0);

            // Register states
            Bill.State.AddState(new FlappyMenuState());
            Bill.State.AddState(new FlappyPlayState());
            Bill.State.AddState(new FlappyPauseState());
            Bill.State.AddState(new FlappyGameOverState());

            // Register cheats
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Bill.Cheat.Register<int>("fscore", s => { _score = s; hud?.UpdateScore(_score); }, "Set flappy score");
            Bill.Cheat.Register("fwin", () => { _score = 99; hud?.UpdateScore(_score); }, "Max score");
#endif

            // Start at menu
            Bill.State.GoTo<FlappyMenuState>();
        }

        void Update()
        {
            // Menu: tap to start
            if (Bill.State.IsInState<FlappyMenuState>())
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    Bill.Events.Fire(new GameStartEvent());
                    Bill.State.GoTo<FlappyPlayState>();
                }
            }

            // Pause toggle
            if (Bill.State.IsInState<FlappyPlayState>() && Input.GetKeyDown(KeyCode.Escape))
            {
                Bill.State.GoTo<FlappyPauseState>();
            }
            else if (Bill.State.IsInState<FlappyPauseState>() && Input.GetKeyDown(KeyCode.Escape))
            {
                Bill.State.GoTo<FlappyPlayState>();
            }
        }

        // ─── State Callbacks ─────────────────────

        void OnEnterMenu()
        {
            _score = 0;
            pipeSpawner?.StopSpawning();
            pipeSpawner?.ReturnAllPipes();
            bird?.ResetBird();
            bird?.StartIdleBob();

            hud?.ShowMenu(_bestScore);
            gameOverPanel?.Hide();
        }

        void OnEnterPlay()
        {
            _score = 0;
            hud?.ShowGameplay();
            hud?.UpdateScore(0);

            // Start pipe spawning
            UpdateDifficulty(0);
            pipeSpawner?.StartSpawning();
        }

        void OnEnterGameOver()
        {
            pipeSpawner?.StopSpawning();
            Bill.Audio.StopMusic(0.5f);
            Bill.Audio.Play("sfx_die");

            // Save
            int totalGames = Bill.Save.GetInt("flappy_total_games", 0) + 1;
            int totalScore = Bill.Save.GetInt("flappy_total_score", 0) + _score;
            Bill.Save.Set("flappy_total_games", totalGames);
            Bill.Save.Set("flappy_total_score", totalScore);

            bool isNewBest = _score > _bestScore;
            if (isNewBest)
            {
                _bestScore = _score;
                Bill.Save.Set("flappy_best", _bestScore);
                Bill.Save.Flush();
                Bill.Events.Fire(new NewBestEvent { Score = _bestScore });
            }

            // Screen shake
            ScreenShake();

            // Show game over after delay
            Bill.Timer.Delay(1.0f, () =>
            {
                int medal = GetMedal(_score);
                gameOverPanel?.Show(_score, _bestScore, isNewBest, medal);
            });
        }

        // ─── Events ──────────────────────────────

        void OnScoreChanged(ScoreChangedEvent e)
        {
            _score += e.Score;
            hud?.UpdateScore(_score);
            UpdateDifficulty(_score);

            // Score punch tween on HUD
            hud?.PunchScore();
        }

        void OnBirdDied(BirdDiedEvent _)
        {
            Bill.State.GoTo<FlappyGameOverState>();
        }

        // ─── Difficulty ──────────────────────────

        void UpdateDifficulty(int score)
        {
            int tier = 0;
            for (int i = DiffScoreThresholds.Length - 1; i >= 0; i--)
            {
                if (score >= DiffScoreThresholds[i]) { tier = i; break; }
            }

            if (pipeSpawner != null)
            {
                pipeSpawner.currentGapSize = DiffGapSizes[tier];
                pipeSpawner.currentSpeed = DiffSpeeds[tier];

                float newInterval = DiffIntervals[tier];
                if (!Mathf.Approximately(newInterval, pipeSpawner.currentInterval))
                    pipeSpawner.UpdateInterval(newInterval);
            }
        }

        // ─── Helpers ─────────────────────────────

        void ScreenShake()
        {
            if (cameraRoot == null) return;

            Vector3 orig = cameraRoot.localPosition;
            float strength = 0.15f;
            int shakes = 4;
            float dur = 0.05f;

            for (int i = 0; i < shakes; i++)
            {
                float dx = (i % 2 == 0 ? strength : -strength);
                BillTween.LocalMoveX(cameraRoot, orig.x + dx, dur).SetDelay(i * dur);
            }
            BillTween.LocalMoveX(cameraRoot, orig.x, dur).SetDelay(shakes * dur);
        }

        int GetMedal(int score)
        {
            if (score >= 50) return 4; // Platinum
            if (score >= 30) return 3; // Gold
            if (score >= 20) return 2; // Silver
            if (score >= 10) return 1; // Bronze
            return 0; // None
        }

        // ─── Public (called by UI buttons) ───────

        public void OnRetryButton()
        {
            Bill.Audio.Play("sfx_swoosh");
            Bill.State.GoTo<FlappyMenuState>();
            // Small delay then start
            Bill.Timer.Delay(0.3f, () =>
            {
                Bill.Events.Fire(new GameStartEvent());
                Bill.State.GoTo<FlappyPlayState>();
            });
        }

        public void OnMenuButton()
        {
            Bill.Audio.Play("sfx_swoosh");
            Bill.State.GoTo<FlappyMenuState>();
        }

        public void OnResumeButton()
        {
            Bill.State.GoTo<FlappyPlayState>();
        }

        /// <summary>
        /// Cleanup: unsubscribe events, cancel timers. Called by FlappySetup.Teardown().
        /// </summary>
        public void Cleanup()
        {
            Bill.Events.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            Bill.Events.Unsubscribe<BirdDiedEvent>(OnBirdDied);
            Bill.Audio.StopMusic(0f);
            Time.timeScale = 1f;
        }
    }
}
