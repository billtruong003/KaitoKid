using UnityEngine;
using BillGameCore;

namespace BillSamples.Runner
{
    /// <summary>
    /// Obstacle component. Attach to any obstacle in a chunk.
    /// </summary>
    public class RunnerObstacle : MonoBehaviour
    {
        public enum ObstacleType { Crate, DoubleCrate, Spike, LowBeam, BirdEnemy, RollingBarrel, SawBlade, FallingPlatform }

        public ObstacleType type;
        public bool instantKill; // true for spikes, gaps
        public int damage = 1;

        [Header("Movement (for moving obstacles)")]
        public float moveSpeed;
        public Vector3 moveDirection = Vector3.left;
        public bool sinWave;
        public float sinAmplitude = 0.5f;
        public float sinFrequency = 2f;

        private Vector3 _startPos;
        private float _time;

        void OnEnable()
        {
            _startPos = transform.localPosition;
            _time = 0;
        }

        void Update()
        {
            if (moveSpeed > 0)
            {
                transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
            }

            if (sinWave)
            {
                _time += Time.deltaTime;
                Vector3 pos = transform.localPosition;
                pos.y = _startPos.y + Mathf.Sin(_time * sinFrequency) * sinAmplitude;
                transform.localPosition = pos;
            }
        }
    }

    /// <summary>
    /// Coin collectible. Rotates and bobs.
    /// </summary>
    public class RunnerCollectible : MonoBehaviour
    {
        public enum CoinType { Bronze, Silver, Gold }

        public CoinType coinType = CoinType.Bronze;
        public int value = 1;

        private float _bobTime;

        void OnEnable()
        {
            _bobTime = Random.Range(0f, Mathf.PI * 2);
            value = coinType switch
            {
                CoinType.Silver => 3,
                CoinType.Gold => 10,
                _ => 1
            };
        }

        void Update()
        {
            // Rotate
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);

            // Bob
            _bobTime += Time.deltaTime;
            Vector3 pos = transform.localPosition;
            pos.y += Mathf.Sin(_bobTime * 3f) * 0.003f;
            transform.localPosition = pos;
        }
    }

    /// <summary>
    /// Power-up item. Floats and glows.
    /// </summary>
    public class RunnerPowerUp : MonoBehaviour
    {
        public string itemKey = "item_magnet"; // item_magnet, item_shield, item_speed, item_2x, item_tiny
        public float duration = 8f;

        void OnEnable()
        {
            // Float bob via Tween
            BillTween.LocalMoveY(transform, transform.localPosition.y + 0.3f, 0.8f)
                .SetEase(EaseType.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        void OnDisable()
        {
            BillTween.KillTarget(transform);
        }
    }
}
