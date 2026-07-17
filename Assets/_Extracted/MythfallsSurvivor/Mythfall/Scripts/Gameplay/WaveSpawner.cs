using UnityEngine;
using BillGameCore;
using Mythfall.Enemy;
using Mythfall.Events;

namespace Mythfall.Gameplay
{
    /// <summary>
    /// Weighted-mix wave spawner. Each wave picks <see cref="enemiesPerWave"/>
    /// archetypes from <see cref="enemyEntries"/> weighted distribution and spawns
    /// them at a random spawn point. Boss spawn is timer-triggered and fires
    /// <see cref="BossSpawnTriggeredEvent"/> for boss controller to handle.
    ///
    /// Day 1 scope: enemy variety + boss trigger event. Elite promotion + boss
    /// instance spawn are wired in Day 2.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class WaveEntry
        {
            public string poolKey = "Enemy_Swarmer";
            [Min(0)] public float weight = 1f;
        }

        [Header("Spawn points")]
        [SerializeField] Transform[] spawnPoints;

        [Header("Wave config")]
        [SerializeField] WaveEntry[] enemyEntries = new[]
        {
            new WaveEntry { poolKey = "Enemy_Swarmer", weight = 3f },
            new WaveEntry { poolKey = "Enemy_Brute",   weight = 1f },
            new WaveEntry { poolKey = "Enemy_Shooter", weight = 1f },
        };

        [SerializeField, Min(1)] int enemiesPerWave = 5;
        [SerializeField, Min(0.1f)] float waveInterval = 5f;
        [SerializeField] bool spawnOnStart = true;

        [Header("Boss")]
        [Tooltip("Seconds from spawner start until boss trigger fires. 0 = disabled.")]
        [SerializeField] float bossSpawnTime = 60f;
        [SerializeField] string bossPoolKey = "Enemy_Rotwood";
        [SerializeField] Transform bossSpawnPoint;
        [Tooltip("If true, stop spawning waves once boss triggers.")]
        [SerializeField] bool stopWavesOnBoss = true;

        TimerHandle waveTimer;
        TimerHandle bossTimer;
        int waveCount;
        bool bossTriggered;
        float totalWeight;

        void Start()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[{name}] No spawn points assigned — disabling.");
                enabled = false;
                return;
            }

            RecalculateWeight();

            if (totalWeight <= 0f)
            {
                Debug.LogWarning($"[{name}] All enemy entries have weight 0 — disabling.");
                enabled = false;
                return;
            }

            if (Bill.Pool == null)
            {
                Debug.LogWarning($"[{name}] Bill.Pool not ready — waiting for GameReady.");
                Bill.Events.SubscribeOnce<GameReadyEvent>(_ => StartSpawning());
                return;
            }

            StartSpawning();
        }

        void RecalculateWeight()
        {
            totalWeight = 0f;
            if (enemyEntries == null) return;
            foreach (var e in enemyEntries) if (e != null) totalWeight += e.weight;
        }

        void StartSpawning()
        {
            if (spawnOnStart) SpawnWave();
            waveTimer = Bill.Timer.Repeat(waveInterval, SpawnWave);

            if (bossSpawnTime > 0f)
                bossTimer = Bill.Timer.Delay(bossSpawnTime, TriggerBoss);
        }

        void OnDestroy()
        {
            if (!Bill.IsReady) return;
            if (waveTimer != null) Bill.Timer.Cancel(waveTimer);
            if (bossTimer != null) Bill.Timer.Cancel(bossTimer);
        }

        void SpawnWave()
        {
            if (bossTriggered && stopWavesOnBoss) return;

            waveCount++;
            for (int i = 0; i < enemiesPerWave; i++)
            {
                var entry = PickEntry();
                if (entry == null) continue;

                var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                var go = Bill.Pool.Spawn(entry.poolKey, point.position, Quaternion.identity);
                if (go == null) continue;

                var enemy = go.GetComponent<EnemyBase>();
                if (enemy != null) enemy.OnSpawn();
            }
        }

        WaveEntry PickEntry()
        {
            if (enemyEntries == null || enemyEntries.Length == 0 || totalWeight <= 0f) return null;
            float roll = Random.value * totalWeight;
            float accum = 0f;
            foreach (var e in enemyEntries)
            {
                if (e == null) continue;
                accum += e.weight;
                if (roll <= accum) return e;
            }
            return enemyEntries[enemyEntries.Length - 1];
        }

        void TriggerBoss()
        {
            if (bossTriggered) return;
            bossTriggered = true;

            Vector3 pos = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
            Bill.Events.Fire(new BossSpawnTriggeredEvent
            {
                poolKey = bossPoolKey,
                position = pos,
            });

            Debug.Log($"[{name}] Boss trigger fired @ {bossSpawnTime}s — listener should spawn '{bossPoolKey}'.");
        }
    }
}
