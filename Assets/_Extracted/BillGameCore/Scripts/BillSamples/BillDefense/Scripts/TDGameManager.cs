using UnityEngine;
using System.Collections.Generic;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    /// <summary>
    /// Spawns enemies per wave definition, tracks active enemies.
    /// </summary>
    public class TDWaveManager : MonoBehaviour
    {
        [Header("Set by Setup")]
        public Vector3[] waypoints;
        public Transform spawnPoint;

        private WaveDefinition[] _waves;
        private int _currentWaveIndex;
        private int _totalEnemiesThisWave;
        private int _enemiesSpawned;
        private int _enemiesKilled;
        private int _enemiesLeaked;
        private bool _spawning;
        private TimerHandle _spawnTimer;
        private List<WaveEntry> _spawnQueue = new List<WaveEntry>();
        private int _spawnQueueIndex;

        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => _waves?.Length ?? 0;
        public bool IsLastWave => _currentWaveIndex >= TotalWaves - 1;
        public bool AllEnemiesDead => (_enemiesKilled + _enemiesLeaked) >= _totalEnemiesThisWave && !_spawning;
        public string CurrentHint => _waves != null && _currentWaveIndex < _waves.Length ? _waves[_currentWaveIndex].hint : null;

        public void Init(WaveDefinition[] waves, Vector3[] path, Transform spawn)
        {
            _waves = waves;
            waypoints = path;
            spawnPoint = spawn;
            _currentWaveIndex = -1;
        }

        public void StartNextWave()
        {
            _currentWaveIndex++;
            if (_currentWaveIndex >= _waves.Length) return;

            var wave = _waves[_currentWaveIndex];
            _totalEnemiesThisWave = 0;
            _enemiesSpawned = 0;
            _enemiesKilled = 0;
            _enemiesLeaked = 0;
            _spawning = true;

            // Build spawn queue
            _spawnQueue.Clear();
            _spawnQueueIndex = 0;
            foreach (var entry in wave.entries)
            {
                _totalEnemiesThisWave += entry.count;
                for (int i = 0; i < entry.count; i++)
                    _spawnQueue.Add(entry);
            }

            Bill.Events.Fire(new WaveStartEvent { WaveNum = CurrentWave, EnemyCount = _totalEnemiesThisWave });

            _spawnTimer = Bill.Timer.Repeat(wave.spawnInterval, SpawnNext);
        }

        void SpawnNext()
        {
            if (_spawnQueueIndex >= _spawnQueue.Count)
            {
                _spawning = false;
                Bill.Timer.Cancel(_spawnTimer);
                return;
            }

            var entry = _spawnQueue[_spawnQueueIndex];
            _spawnQueueIndex++;

            var def = TDDatabase.GetEnemy(entry.enemyType);
            string poolKey = $"enemy_{def.type.ToString().ToLower()}";

            var go = Bill.Pool.Spawn(poolKey);
            if (go == null)
            {
                Debug.LogWarning($"[TDWave] Could not spawn {poolKey} from pool!");
                return;
            }

            go.transform.position = waypoints[0];

            var enemy = go.GetComponent<TDEnemy>();
            if (enemy == null) enemy = go.AddComponent<TDEnemy>();
            enemy.Setup(def, waypoints, _currentWaveIndex);

            _enemiesSpawned++;
            Bill.Events.Fire(new EnemySpawnedEvent { Type = def.displayName, HP = Mathf.RoundToInt(enemy.maxHP) });
        }

        public void OnEnemyKilled() { _enemiesKilled++; }
        public void OnEnemyLeaked() { _enemiesLeaked++; }

        public void StopSpawning()
        {
            _spawning = false;
            if (_spawnTimer != null && _spawnTimer.IsActive) Bill.Timer.Cancel(_spawnTimer);
        }

        public void ReturnAllEnemies()
        {
            foreach (var def in TDDatabase.Enemies)
            {
                string key = $"enemy_{def.type.ToString().ToLower()}";
                Bill.Pool.ReturnAll(key);
            }
        }
    }

    /// <summary>
    /// Central game manager. Economy, lives, state transitions, build phase timer.
    /// </summary>
    public class TDGameManager : MonoBehaviour
    {
        [Header("References (set by Setup)")]
        public TDWaveManager waveManager;
        public TDGrid grid;
        public TDHUD hud;
        public TDTowerPanel towerPanel;
        public TDGameOverPanel gameOverPanel;

        [Header("Game State")]
        public int gold;
        public int lives;
        public int maxLives;
        public int totalKills;
        public int livesLostThisRun;

        // Difficulty
        private int _startGold = 300;
        private int _startLives = 20;
        private float _buildPhaseTime = 30f;
        private float _betweenWaveTime = 15f;
        private TimerHandle _buildTimer;
        private float _buildTimeLeft;

        // Map
        private int _mapId;
        private int _difficulty; // 0=easy,1=normal,2=hard,3=nightmare

        void OnEnable()
        {
            Bill.Events.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            Bill.Events.Subscribe<EnemyLeakedEvent>(OnEnemyLeaked);
        }

        void OnDisable()
        {
            Bill.Events.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            Bill.Events.Unsubscribe<EnemyLeakedEvent>(OnEnemyLeaked);
        }

        void Start()
        {
            // Register states
            Bill.State.AddState(new TDMenuState());
            Bill.State.AddState(new TDMapSelectState());
            Bill.State.AddState(new TDLoadingState());
            Bill.State.AddState(new TDBuildPhaseState());
            Bill.State.AddState(new TDWaveActiveState());
            Bill.State.AddState(new TDWaveCompleteState());
            Bill.State.AddState(new TDPauseState());
            Bill.State.AddState(new TDGameOverState());
            Bill.State.AddState(new TDVictoryState());

            Bill.State.OnEnter<TDBuildPhaseState>(OnEnterBuild);
            Bill.State.OnEnter<TDWaveActiveState>(OnEnterWave);
            Bill.State.OnEnter<TDWaveCompleteState>(OnWaveComplete);
            Bill.State.OnEnter<TDGameOverState>(OnGameOver);
            Bill.State.OnEnter<TDVictoryState>(OnVictory);

            // Cheats
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Bill.Cheat.Register("tdgold", () => { AddGold(500); Debug.Log("[Cheat] +500 gold"); }, "+500 gold");
            Bill.Cheat.Register("tdskip", () => { waveManager.StopSpawning(); waveManager.ReturnAllEnemies(); ForceWaveComplete(); }, "Skip wave");
            Bill.Cheat.Register("tdkillall", () => { waveManager.ReturnAllEnemies(); ForceWaveComplete(); }, "Kill all enemies");
#endif

            // Start game
            StartGame(0, 0);
        }

        void Update()
        {
            // Pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Bill.State.IsInState<TDPauseState>())
                    Bill.State.GoBack();
                else if (Bill.State.IsInState<TDBuildPhaseState>() || Bill.State.IsInState<TDWaveActiveState>())
                    Bill.State.GoTo<TDPauseState>();
            }

            // Check wave clear
            if (Bill.State.IsInState<TDWaveActiveState>() && waveManager.AllEnemiesDead)
            {
                Bill.State.GoTo<TDWaveCompleteState>();
            }

            // Speed toggle (2x)
            if (Input.GetKeyDown(KeyCode.F))
            {
                Time.timeScale = Time.timeScale >= 1.9f ? 1f : 2f;
            }
        }

        // ─── Game Start ──────────────────────────

        public void StartGame(int mapId, int difficulty)
        {
            _mapId = mapId;
            _difficulty = difficulty;

            // Difficulty settings
            switch (difficulty)
            {
                case 0: _startGold = 300; _startLives = 20; break; // Easy
                case 1: _startGold = 250; _startLives = 15; break; // Normal
                case 2: _startGold = 200; _startLives = 10; break; // Hard
                case 3: _startGold = 150; _startLives = 5;  break; // Nightmare
            }

            gold = _startGold;
            lives = _startLives;
            maxLives = _startLives;
            totalKills = 0;
            livesLostThisRun = 0;

            hud?.UpdateGold(gold);
            hud?.UpdateLives(lives, maxLives);
            hud?.UpdateWave(0, waveManager.TotalWaves);

            Bill.State.GoTo<TDBuildPhaseState>();
        }

        // ─── State Handlers ──────────────────────

        void OnEnterBuild()
        {
            float timer = waveManager.CurrentWave == 0 ? _buildPhaseTime : _betweenWaveTime;
            _buildTimeLeft = timer;

            hud?.ShowBuildPhase(waveManager.CurrentWave + 1, waveManager.TotalWaves);
            hud?.UpdateBuildTimer(timer, timer);

            var hint = waveManager.CurrentHint;
            if (!string.IsNullOrEmpty(hint))
                hud?.ShowHint(hint);

            _buildTimer = Bill.Timer.Repeat(0.1f, () =>
            {
                _buildTimeLeft -= 0.1f;
                hud?.UpdateBuildTimer(_buildTimeLeft, timer);
                Bill.Events.Fire(new BuildPhaseTimerEvent { SecondsLeft = _buildTimeLeft });

                if (_buildTimeLeft <= 0)
                {
                    Bill.Timer.Cancel(_buildTimer);
                    SendWave();
                }
            });
        }

        void OnEnterWave()
        {
            waveManager.StartNextWave();
            hud?.ShowWaveActive(waveManager.CurrentWave, waveManager.TotalWaves);
        }

        void OnWaveComplete()
        {
            int wave = waveManager.CurrentWave;
            bool perfect = livesLostThisRun == 0; // This wave specifically
            int bonus = perfect ? 20 + wave * 2 : 10 + wave;

            // Interest bonus
            int interest = Mathf.Min(20, (gold / 100) * 2);
            bonus += interest;

            AddGold(bonus);
            Bill.Events.Fire(new WaveCompleteEvent { WaveNum = wave, BonusGold = bonus, Perfect = perfect });
            Bill.Audio.Play("sfx_wave_clear");

            hud?.ShowWaveComplete(wave, bonus);

            // Check victory
            if (waveManager.IsLastWave)
            {
                Bill.Timer.Delay(2f, () => Bill.State.GoTo<TDVictoryState>());
            }
            else
            {
                Bill.Timer.Delay(3f, () => Bill.State.GoTo<TDBuildPhaseState>());
            }
        }

        void OnGameOver()
        {
            waveManager.StopSpawning();
            Bill.Audio.PlayMusic("bgm_td_defeat", 0f);

            Bill.Save.Set($"td_map_{_mapId}_best_wave",
                Mathf.Max(Bill.Save.GetInt($"td_map_{_mapId}_best_wave", 0), waveManager.CurrentWave));
            Bill.Save.Flush();

            gameOverPanel?.Show(waveManager.CurrentWave, totalKills);
        }

        void OnVictory()
        {
            int stars = 1;
            if (lives >= maxLives / 2) stars = 2;
            if (livesLostThisRun == 0) stars = 3;

            Bill.Save.Set($"td_map_{_mapId}_stars",
                Mathf.Max(Bill.Save.GetInt($"td_map_{_mapId}_stars", 0), stars));
            Bill.Save.Set("td_total_kills", Bill.Save.GetInt("td_total_kills", 0) + totalKills);
            Bill.Save.Flush();

            Bill.Events.Fire(new TDVictoryEvent { Stars = stars, TotalKills = totalKills });
            hud?.ShowVictory(stars, totalKills);
        }

        // ─── Events ──────────────────────────────

        void OnEnemyKilled(EnemyKilledEvent e)
        {
            AddGold(e.GoldReward);
            totalKills++;
            waveManager.OnEnemyKilled();
            hud?.UpdateKills(totalKills);
        }

        void OnEnemyLeaked(EnemyLeakedEvent e)
        {
            lives--;
            livesLostThisRun++;
            waveManager.OnEnemyLeaked();

            Bill.Audio.Play("sfx_life_lost");
            Bill.Events.Fire(new LivesChangedEvent { LivesLeft = lives });
            hud?.UpdateLives(lives, maxLives);

            if (lives <= 0)
            {
                Bill.State.GoTo<TDGameOverState>();
            }
        }

        // ─── Economy ─────────────────────────────

        public void AddGold(int amount)
        {
            gold += amount;
            Bill.Events.Fire(new GoldChangedEvent { NewGold = gold, Delta = amount });
            hud?.UpdateGold(gold);
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount) return false;
            gold -= amount;
            Bill.Events.Fire(new GoldChangedEvent { NewGold = gold, Delta = -amount });
            hud?.UpdateGold(gold);
            return true;
        }

        // ─── Tower Placement ─────────────────────

        public bool TryPlaceTower(TowerType type, Vector2Int tile)
        {
            if (grid == null) return false;
            if (!grid.IsBuildable(tile)) return false;

            var def = TDDatabase.GetTower(type);
            if (!SpendGold(def.BaseCost)) return false;

            var towerGO = grid.PlaceTower(type, tile);
            if (towerGO == null) { AddGold(def.BaseCost); return false; } // Refund

            Bill.Events.Fire(new TowerPlacedEvent { TowerType = def.displayName, Tile = tile });
            return true;
        }

        public bool TryUpgradeTower(TDTower tower)
        {
            if (tower == null || !tower.CanUpgrade) return false;
            if (!SpendGold(tower.UpgradeCost)) return false;
            tower.Upgrade();
            return true;
        }

        public void SellTower(TDTower tower)
        {
            if (tower == null) return;
            int refund = tower.SellPrice;
            Vector2Int pos = tower.gridPosition;
            tower.Sell();
            grid?.ClearTile(pos);
            AddGold(refund);
        }

        // ─── Public UI Callbacks ─────────────────

        public void SendWave()
        {
            if (Bill.State.IsInState<TDBuildPhaseState>())
            {
                if (_buildTimer != null && _buildTimer.IsActive) Bill.Timer.Cancel(_buildTimer);
                AddGold(5); // Early send bonus
                Bill.State.GoTo<TDWaveActiveState>();
            }
        }

        public void OnRetry()
        {
            waveManager.StopSpawning();
            waveManager.ReturnAllEnemies();
            grid?.ClearAllTowers();
            gameOverPanel?.Hide();
            StartGame(_mapId, _difficulty);
        }

        public void OnMenu()
        {
            waveManager.StopSpawning();
            waveManager.ReturnAllEnemies();
            // In a full game you'd load menu scene
            Bill.State.GoTo<TDMenuState>();
        }

        void ForceWaveComplete()
        {
            if (Bill.State.IsInState<TDWaveActiveState>())
                Bill.State.GoTo<TDWaveCompleteState>();
        }

        public void Cleanup()
        {
            Bill.Events.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            Bill.Events.Unsubscribe<EnemyLeakedEvent>(OnEnemyLeaked);
            if (_buildTimer != null && _buildTimer.IsActive) Bill.Timer.Cancel(_buildTimer);
            Bill.Audio.StopMusic(0f);
            Time.timeScale = 1f;
        }
    }
}
