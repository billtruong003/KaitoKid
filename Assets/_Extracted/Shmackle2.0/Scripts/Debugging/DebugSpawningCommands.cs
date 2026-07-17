using System.Collections.Generic;
using Opencoding.CommandHandlerSystem;
using UnityEngine;

namespace Shmackle.Debugging
{
   //NOTE (Russ): Make sure the method names are the exact same as the command names in the Base Debug Command Collection
    public class DebugSpawningCommands : MonoBehaviour
    {
        [Header("Dummy Protobro References")]
        [SerializeField] private GameObject _dummyProtobroPrefab;
        [SerializeField] private DebugFrameTimeCanvas _debugFrameTimeCanvas;
        [SerializeField] private float _cellSize = 0.5f;
        [SerializeField] private int _columns = 4;
        [SerializeField] private int _rows = 4;
        [SerializeField] private float _layerHeight = 0.5f;

        private readonly List<GameObject> _protobroPrefabList = new List<GameObject>();
        private int _currentIndex;
        private DebugFrameTimeCanvas _frameTimeCanvas;

#if DEVELOPMENT_BUILD || UNITY_EDITOR || DEBUG

        private void Awake()
        {
            CommandHandlers.RegisterCommandHandlers(this);
            _protobroPrefabList.Clear(); // Make sure to clear the list before spawning
            _currentIndex = 0;
        }
        
        /// <summary>
        /// This is just a simple way to spawn a dummy prefab in a grid pattern.
        /// </summary>
        [CommandHandler]
        public void SpawnDummy()
        {
            int maxPerLayer = _columns * _rows;

            int layer = _currentIndex / maxPerLayer;
            int indexInLayer = _currentIndex % maxPerLayer;

            int row = indexInLayer / _columns;
            int column = indexInLayer % _columns;

            Vector3 spawnPos = new Vector3(column * _cellSize, 0.4f + (layer * _layerHeight), -row * _cellSize);

            var dummy = Instantiate(_dummyProtobroPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));
            _protobroPrefabList.Add(dummy);
            _currentIndex++;

            if (!_frameTimeCanvas) 
                _frameTimeCanvas = Instantiate(_debugFrameTimeCanvas, new Vector3(2.7f, 0.9f, -1.6f), Quaternion.Euler(14.20f, -45, 0f));

            if (_frameTimeCanvas)
            {
                _frameTimeCanvas.SetDummyProtobroCount(_protobroPrefabList.Count);
                if (!_frameTimeCanvas.gameObject.activeSelf)
                    _frameTimeCanvas.gameObject.SetActive(true);
            }
        }

        [CommandHandler]
        public void RemoveAllDummies()
        {
            if (_protobroPrefabList.Count == 0)
                return;

            _protobroPrefabList.ForEach(Destroy);
            _protobroPrefabList.Clear();
            _currentIndex = 0;

            if (_frameTimeCanvas)
                _frameTimeCanvas.gameObject.SetActive(false);
        }
#endif
    }
}