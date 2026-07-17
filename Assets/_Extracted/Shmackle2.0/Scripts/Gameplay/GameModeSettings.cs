using Fusion;
using UnityEngine;

namespace Shmackle.Gameplay
{
    [CreateAssetMenu(fileName = "DefaultGameModeSettings", menuName = "Shmackle/Gameplay/Settings/Default")]
    public class GameModeSettings : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField]
        protected bool _autoStartMatch = true;
        [Header("Base Default Prefabs")]
        [SerializeField, Tooltip("Player state type to represent player data.")]
        private PlayerState _playerStatePrefab;
        [SerializeField, Tooltip("Main player controlled object.")]
        private NetworkObject _playerObjectPrefab;
        [SerializeField, Tooltip("Optional prefabs to be spawned on all clients when this game mode is spawned. " +
                                 "Best place to spawn the UIs that are dependent on the game mode.")]
        private GameObject[] _defaultClientSpawnedPrefabs;

        #endregion

        #region Public Fields

        public bool AutoStartMatch => _autoStartMatch;
        public PlayerState PlayerStatePrefab => _playerStatePrefab;
        public NetworkObject PlayerObjectPrefab => _playerObjectPrefab;
        public GameObject[] DefaultClientSpawnedPrefabs => _defaultClientSpawnedPrefabs;

        #endregion

    }
}