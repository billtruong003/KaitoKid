using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Networking
{
    public enum ApplicationType
    {
        Headless = 0,
        Client = 1,
    }

    [System.Serializable]
    public struct StringSessionProperty
    {
        public string propertyName;
        public string value;
    }

    [CreateAssetMenu(fileName = "NetworkingSettings", menuName = "Settings/Networking Settings")]
    public class NetworkingSettings : ScriptableObject
    {
        #region Serialized Fields

        [Header("Room configuration")]
        [SerializeField] private GameMode _gameMode = GameMode.Shared;
        [SerializeField] ApplicationType _applicationType = ApplicationType.Client;
        [SerializeField] MatchmakingType _matchmakingType = BaseMatchmakingType.PhotonCloud;
        [SerializeField] private string _sessionName = "SampleFusion";
        [Tooltip("Set it to 0 to use the DefaultPlayers value, from the Global NetworkProjectConfig (simulation section)")]
        [SerializeField] private int _playerCount = 0;

        [Header("Room selection criteria")]
        [Tooltip("If connectionCriterias include SessionProperties, additionalSessionProperties (editable in the inspector) will be added to sessionProperties")]
        [SerializeField] private List<StringSessionProperty> _additionalSessionProperties = new List<StringSessionProperty>();
        
        [Header("Fusion settings")]
        [Tooltip("Fusion runner. Automatically created if not set")]
        [SerializeField] private NetworkRunner _runnerPrefab;
        [SerializeField] private NetworkSceneManagerDefault _sceneManagerPrefab;

        [Header("Player settings")]
        [SerializeField] private float _areaOfInterestRadius = 10f;

        #endregion

        #region Properties

        public GameMode GameMode => _gameMode;
        public ApplicationType ApplicationType => _applicationType;
        public MatchmakingType MatchmakingType => _matchmakingType;
        public string SessionName => _sessionName;
        public int PlayerCount => _playerCount;
        public List<StringSessionProperty> AdditionalSessionProperties => _additionalSessionProperties;
        
        public NetworkRunner RunnerPrefab => _runnerPrefab;
        public NetworkSceneManagerDefault SceneManagerPrefab => _sceneManagerPrefab;

        public float AreaOfInterestRadius => _areaOfInterestRadius;

        #endregion
    }
}