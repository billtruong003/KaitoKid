using UnityEngine;
using BillGameCore;

namespace BillSamples.Flappy
{
    /// <summary>
    /// Attach to each pipe pair. Moves left, returns to pool when off-screen.
    /// Created by FlappySetup as a prefab structure.
    /// </summary>
    public class FlappyPipe : MonoBehaviour
    {
        [HideInInspector] public string poolKey = "Pipe";
        [HideInInspector] public float speed = 3f;
        [HideInInspector] public bool scored;

        private float _despawnX = -4f;
        private float _birdX = 0f;

        public Transform topPipe;    // Assign in setup — the upper pipe visual
        public Transform bottomPipe; // Assign in setup — the lower pipe visual
        public Transform scoreZone;  // Trigger between pipes

        /// <summary>
        /// Configure pipe positions for this spawn.
        /// </summary>
        public void Setup(float gapCenterY, float gapSize, float moveSpeed, float birdX)
        {
            speed = moveSpeed;
            scored = false;
            _birdX = birdX;

            float halfGap = gapSize / 2f;

            // Top pipe: bottom edge at gapCenter + halfGap
            if (topPipe != null)
                topPipe.localPosition = new Vector3(0, gapCenterY + halfGap + 5f, 0); // 5 = half pipe height

            // Bottom pipe: top edge at gapCenter - halfGap
            if (bottomPipe != null)
                bottomPipe.localPosition = new Vector3(0, gapCenterY - halfGap - 5f, 0);

            // Score zone in the gap
            if (scoreZone != null)
                scoreZone.localPosition = new Vector3(0, gapCenterY, 0);
        }

        void Update()
        {
            if (!Bill.State.IsInState<FlappyPlayState>()) return;

            // Move left
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // Despawn
            if (transform.position.x < _despawnX)
            {
                Bill.Pool.Return(gameObject);
            }
        }
    }

    /// <summary>
    /// Spawns pipes at intervals. Controlled by FlappyGameManager.
    /// </summary>
    public class FlappyPipeSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public float spawnX = 6f;
        public float gapYMin = -1.5f;
        public float gapYMax = 2.5f;

        // Current difficulty params (updated by difficulty manager)
        [HideInInspector] public float currentGapSize = 3.2f;
        [HideInInspector] public float currentSpeed = 3.0f;
        [HideInInspector] public float currentInterval = 1.8f;

        private TimerHandle _spawnTimer;
        private bool _active;

        public void StartSpawning()
        {
            _active = true;
            _spawnTimer = Bill.Timer.Repeat(currentInterval, SpawnPipe);
        }

        public void StopSpawning()
        {
            _active = false;
            if (_spawnTimer != null && _spawnTimer.IsActive)
                Bill.Timer.Cancel(_spawnTimer);
        }

        public void ReturnAllPipes()
        {
            Bill.Pool.ReturnAll("Pipe");
        }

        /// <summary>
        /// Restart timer with new interval (called when difficulty changes).
        /// </summary>
        public void UpdateInterval(float newInterval)
        {
            if (!_active) return;
            currentInterval = newInterval;
            if (_spawnTimer != null && _spawnTimer.IsActive)
                Bill.Timer.Cancel(_spawnTimer);
            _spawnTimer = Bill.Timer.Repeat(currentInterval, SpawnPipe);
        }

        void SpawnPipe()
        {
            if (!_active) return;

            var pipeGO = Bill.Pool.Spawn("Pipe");
            if (pipeGO == null) return;

            pipeGO.transform.position = new Vector3(spawnX, 0, 0);

            float gapY = Random.Range(gapYMin, gapYMax);

            var pipe = pipeGO.GetComponent<FlappyPipe>();
            if (pipe != null)
            {
                pipe.Setup(gapY, currentGapSize, currentSpeed, 0f);
            }
        }
    }
}
