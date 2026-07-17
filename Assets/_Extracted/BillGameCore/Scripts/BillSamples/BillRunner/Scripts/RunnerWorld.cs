using System.Collections.Generic;
using UnityEngine;
using BillGameCore;

namespace BillSamples.Runner
{
    /// <summary>
    /// Manages map chunks. Spawns ahead, despawns behind.
    /// </summary>
    public class RunnerChunkSpawner : MonoBehaviour
    {
        [Header("Chunk Settings")]
        public float chunkWidth = 20f;
        public float spawnAhead = 40f;   // How far ahead to keep loaded
        public float despawnBehind = 15f; // How far behind to keep before despawn

        [Header("Pool Keys per category")]
        public string[] easyChunkKeys = { "chunk_flat_easy" };
        public string[] mediumChunkKeys = { "chunk_flat_med" };
        public string[] elevatedKeys = { "chunk_elevated" };
        public string[] gapKeys = { "chunk_gap" };
        public string[] slideKeys = { "chunk_slide" };
        public string[] hardKeys = { "chunk_mixed_hard" };

        private Transform _playerTransform;
        private float _nextSpawnX;
        private string _lastChunkKey;
        private List<GameObject> _activeChunks = new List<GameObject>(10);
        private float _currentDistance;

        public void Init(Transform player)
        {
            _playerTransform = player;
            _nextSpawnX = 0;
            _lastChunkKey = "";
            ClearAll();
        }

        /// <summary>Generate initial chunks.</summary>
        public void PreGenerate(int count = 3)
        {
            for (int i = 0; i < count; i++)
                SpawnNextChunk();
        }

        public void ClearAll()
        {
            foreach (var c in _activeChunks)
            {
                if (c != null) Bill.Pool.Return(c);
            }
            _activeChunks.Clear();
        }

        void Update()
        {
            if (_playerTransform == null) return;
            _currentDistance = _playerTransform.position.x;

            // Spawn ahead
            while (_nextSpawnX < _currentDistance + spawnAhead)
            {
                SpawnNextChunk();
            }

            // Despawn behind
            for (int i = _activeChunks.Count - 1; i >= 0; i--)
            {
                if (_activeChunks[i] == null)
                {
                    _activeChunks.RemoveAt(i);
                    continue;
                }
                float chunkEnd = _activeChunks[i].transform.position.x + chunkWidth;
                if (chunkEnd < _currentDistance - despawnBehind)
                {
                    Bill.Pool.Return(_activeChunks[i]);
                    _activeChunks.RemoveAt(i);
                }
            }
        }

        void SpawnNextChunk()
        {
            string key = PickChunk();
            var chunkGO = Bill.Pool.Spawn(key);
            if (chunkGO == null)
            {
                // Fallback: easy chunk
                chunkGO = Bill.Pool.Spawn(easyChunkKeys[0]);
                if (chunkGO == null) { _nextSpawnX += chunkWidth; return; }
            }

            chunkGO.transform.position = new Vector3(_nextSpawnX, 0, 0);
            _activeChunks.Add(chunkGO);
            _nextSpawnX += chunkWidth;
        }

        string PickChunk()
        {
            // Build available pool based on distance
            var available = new List<string>();
            available.AddRange(easyChunkKeys);

            if (_currentDistance >= 200) available.AddRange(mediumChunkKeys);
            if (_currentDistance >= 300) available.AddRange(slideKeys);
            if (_currentDistance >= 400) available.AddRange(elevatedKeys);
            if (_currentDistance >= 600) available.AddRange(gapKeys);
            if (_currentDistance >= 800) available.AddRange(hardKeys);

            // Pick random, avoid repeat
            string key = available[Random.Range(0, available.Count)];
            int attempts = 0;
            while (key == _lastChunkKey && available.Count > 1 && attempts < 10)
            {
                key = available[Random.Range(0, available.Count)];
                attempts++;
            }

            _lastChunkKey = key;
            return key;
        }
    }

    /// <summary>
    /// Parallax background layer. Auto-tiles and scrolls relative to camera.
    /// </summary>
    public class RunnerParallaxLayer : MonoBehaviour
    {
        public float speedRatio = 0.5f; // 0 = static, 1 = moves with camera
        public float width = 20f;

        private Transform _cam;
        private float _startX;
        private Transform _tileA, _tileB;

        public void Init(Transform cam)
        {
            _cam = cam;
            _startX = transform.position.x;

            // Duplicate for seamless tiling
            if (transform.childCount > 0)
            {
                _tileA = transform.GetChild(0);
                _tileB = Instantiate(_tileA, transform);
                _tileB.localPosition = new Vector3(width, 0, 0);
            }
        }

        void Update()
        {
            if (_cam == null) return;

            float camDelta = _cam.position.x * speedRatio;
            transform.position = new Vector3(_startX + camDelta, transform.position.y, transform.position.z);

            // Tile wrapping
            if (_tileA != null && _tileB != null)
            {
                float relativeCam = _cam.position.x - transform.position.x;
                if (relativeCam > _tileA.localPosition.x + width)
                    _tileA.localPosition = new Vector3(_tileB.localPosition.x + width, 0, 0);
                if (relativeCam > _tileB.localPosition.x + width)
                    _tileB.localPosition = new Vector3(_tileA.localPosition.x + width, 0, 0);
            }
        }
    }

    /// <summary>
    /// Camera follows the player.
    /// </summary>
    public class RunnerCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(5f, 3f, -10f);
        public float smoothSpeed = 5f;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPos = target.position + offset;
            desiredPos.y = offset.y; // Lock Y
            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        }
    }
}
