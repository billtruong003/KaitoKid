using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using MessagePipe;
using Stratton.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Stratton.Networking
{
    public enum PeerType
    {
        Client = 0,
        Host = 1,
        Server = 2,
    }

    public class NetworkingSystem : GameSystemBase
    {
        private enum RunnerInitializationSubstate
        {
            Deinitialized = 0,
            InitializationRequested = 1,
            Initialized = 2
        }

        #region Serialized Fields

        [SerializeField] private NetworkingSettings _networkingSettings;
        [SerializeField] private NetworkObjectLibrary _networkObjectLibrary;
        [SerializeField] private GameObject _authenticatorInstance;

        [Header("Info")]
        [SerializeField] private List<StringSessionProperty> _actualSessionProperties = new List<StringSessionProperty>();

        #endregion

        #region Non-Serialized Fields

        private NetworkRunner _runner;
        private int _runnerState = (int)RunnerInitializationSubstate.Deinitialized;
        private NetworkRunnerCallbacksAdapter _networkRunnerCallbacksAdapter = new();
        private INetworkSceneManager _sceneManager;
        private NetworkObjectPool _pooledNetworkObjectProvider;
        private PlayerConnectionsManager _playerConnectionsManager = new();
        private IMatchmakingService _matchmakingService;
        private PeerType _peerType;

        #endregion

        #region Properties

        public NetworkRunner Runner => _runner;
        public NetworkingSettings NetworkingSettings => _networkingSettings;

        public NetworkObjectPool NetworkObjectPool
        {
            get
            {
                if (_pooledNetworkObjectProvider == null)
                {
                    Core.Log.Error(NetworkingLogChannel.ObjectPool, $"NetworkObjectPool not initialized");
                }
                return _pooledNetworkObjectProvider;
            }
        }

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            _playerConnectionsManager.InstallMessageBrokers(builder);
            builder.AddMessageBroker<PlayerJoinedAudioGroupEvent>();
            builder.AddMessageBroker<PlayerLeftAudioGroupEvent>();
        }

        public override async UniTask<InitializationResult> Init()
        {
            _peerType = PeerType.Client;
            Interlocked.Exchange(ref _runnerState, (int)RunnerInitializationSubstate.Deinitialized);
            if (_matchmakingService != null)
            {
                Core.Log.Message(NetworkingLogChannel.Matchmaking, $"Initializing MatchmakingService for {_networkingSettings.MatchmakingType}");
                await _matchmakingService.Init(this);
            }
            return await base.Init();
        }

        public override async UniTask<DeinitializationResult> DeInit()
        {
            if (_runner != null && _runner.IsRunning)
            {
                var options = new ShutdownOptions(ShutdownReason.Ok, true, true);
                await StopConnection(options);
            }
            return await base.DeInit();
        }

        public PeerType GetPeerType()
        {
            return _peerType;
        }

        public virtual NetworkSceneInfo CurrentSceneInfo()
        {
            var activeScene = SceneManager.GetActiveScene();
            SceneRef sceneRef = default;

            if (activeScene.buildIndex < 0 || activeScene.buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError("Current scene is not part of the build settings");
            }
            else
            {
                sceneRef = SceneRef.FromIndex(activeScene.buildIndex);
            }

            var sceneInfo = new NetworkSceneInfo();
            if (sceneRef.IsValid)
            {
                sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Single);
            }
            return sceneInfo;
        }

        public async UniTask StartConnection(ConnectionArguments connectionArguments)
        {
            if (_sceneManager == null)
            {
                _sceneManager = Instantiate<NetworkSceneManagerDefault>(_networkingSettings.SceneManagerPrefab, new InstantiateParameters() { parent = transform });
            }

            // Start or join (depends on gamemode) a session with a specific name
            var args = new StartGameArgs()
            {
                Scene = CurrentSceneInfo(),
                SceneManager = _sceneManager
            };

            if (_authenticatorInstance)
            {
                IAuthenticationValue authenticationValue = _authenticatorInstance.GetComponent<IAuthenticationValue>();
                if (authenticationValue != null)
                {
                    args.AuthValues = authenticationValue.GetValues();
                }
            }

            if (Enum.TryParse<GameMode>(connectionArguments.GameMode, false, out var result))
            {
                args.GameMode = result;
            }
            else
            {
                throw new NotImplementedException($"Couldn't parse provided GameMode {connectionArguments.GameMode} to Fusion {typeof(GameMode)} enum.");
            }

            if (!string.IsNullOrEmpty(connectionArguments.Ip))
            {
                var port = connectionArguments.Port.HasValue ? connectionArguments.Port.Value : 0;
                args.Address = NetAddress.CreateFromIpPort(connectionArguments.Ip, (ushort)port);
            }
            else if (connectionArguments.Port.HasValue)
            {
                args.Address = NetAddress.Any(connectionArguments.Port.Value);
            }

            if (!string.IsNullOrEmpty(connectionArguments.CustomPublicIp))
            {
                args.CustomPublicAddress = NetAddress.CreateFromIpPort(connectionArguments.CustomPublicIp, connectionArguments.CustomPublicIpPort.Value);
            }
            else if (connectionArguments.CustomPublicIpPort.HasValue)
            {
                args.CustomPublicAddress = NetAddress.Any(connectionArguments.CustomPublicIpPort.Value);
            }

            if (string.IsNullOrEmpty(_networkingSettings.SessionName))
            {
                args.SessionName = connectionArguments.SessionName;
            }
            else
            {
                Core.Log.Message(NetworkingLogChannel.NetworkingSystem, $"Starting session with custom SessionName from NetworkingSettings");
                args.SessionName = _networkingSettings.SessionName;
            }

            // Room details
            if (_networkingSettings.PlayerCount > 0)
            {
                args.PlayerCount = _networkingSettings.PlayerCount;
            }

            args.SessionProperties = new Dictionary<string, SessionProperty>();
            args.ConnectionToken = connectionArguments.ConnectionToken;
            if (connectionArguments.AppSettings != null)
            {
                args.CustomPhotonAppSettings = connectionArguments.AppSettings;
            }
            if (connectionArguments.SessionProperties != null)
            {
                args.SessionProperties = GetAllConnectionSessionProperties(connectionArguments.SessionProperties);
            }

            if (_pooledNetworkObjectProvider == null)
            {
                var networkPoolGO = new GameObject("NetworkObjectPool");
                networkPoolGO.transform.SetParent(transform, false);
                networkPoolGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                _pooledNetworkObjectProvider = networkPoolGO.AddComponent<NetworkObjectPool>();
                _pooledNetworkObjectProvider.Init(_networkObjectLibrary);
            }
            args.ObjectProvider = _pooledNetworkObjectProvider;

            if (_runnerState == (int)RunnerInitializationSubstate.Deinitialized)
            {
                Interlocked.Exchange(ref _runnerState, (int)RunnerInitializationSubstate.InitializationRequested);
                while (_runnerState != (int)RunnerInitializationSubstate.Initialized)
                {
                    Core.Log.Message(NetworkingLogChannel.NetworkingSystem, $"Waiting for runner to be instantiated.");
                    await UniTask.Delay(1);
                }
            }

            await _runner.StartGame(args);
            _peerType = _runner.IsSharedModeMasterClient ? PeerType.Host : PeerType.Client;

            string prop = "";
            if (_runner.SessionInfo.Properties != null && _runner.SessionInfo.Properties.Count > 0)
            {
                prop = "SessionProperties: ";
                foreach (var p in _runner.SessionInfo.Properties) prop += $" ({p.Key}={p.Value.PropertyValue}) ";
            }
            Debug.Log($"Session info: Room name {_runner.SessionInfo.Name}. Region: {_runner.SessionInfo.Region}. {prop}");
        }

        public async UniTask StopConnection(ShutdownOptions options)
        {
            Core.Log.Message(NetworkingLogChannel.NetworkingSystem, "Fusion shutdown requested!");
            await _runner.Shutdown(options.DestroyRunner, (Fusion.ShutdownReason)options.ShutdownReason, options.Force);
            DeinitializeSession();
            Core.Log.Message(NetworkingLogChannel.NetworkingSystem, "Fusion shutdown completed.");
        }

        public UniTask<MatchmakingResult> StartMatchmaker(string queueName = null, Dictionary<string, object> playerRules = null, Dictionary<string, object> attributes = null)
        {
            return _matchmakingService.StartMatchmaker(queueName, playerRules, attributes);
        }

        public UniTask CancelMatchmaking()
        {
            return _matchmakingService.CancelMatchmaking();
        }

        #endregion

        #region Private Methods

        private void Awake()
        {
            if (_networkingSettings.ApplicationType == ApplicationType.Client)
            {
                if (_networkingSettings.MatchmakingType != BaseMatchmakingType.None)
                {
                    if (_networkingSettings.MatchmakingType == BaseMatchmakingType.PhotonCloud)
                    {
                        _matchmakingService = new PhotonCloudMatchmakingService();
                    }
                }
            }
            else if (_networkingSettings.ApplicationType == ApplicationType.Headless)
            {
                // Initialize headless-related services here
            }
        }

        private void Update()
        {
            if (_runnerState == (int)RunnerInitializationSubstate.InitializationRequested)
            {
                InitializeSession();
                Interlocked.Exchange(ref _runnerState, (int)RunnerInitializationSubstate.Initialized);
            }
            else if (_runnerState == (int)RunnerInitializationSubstate.Initialized)
            {
                if (_runner == null)
                {
                    Core.Log.Error(NetworkingLogChannel.NetworkingSystem, "Unexpected shutdown! NetworkRunner is null.");
                    DeinitializeSession();
                }
            }
        }

        private void InitializeSession()
        {
            Core.Log.Message(NetworkingLogChannel.NetworkingSystem, "Initializing Fusion session...");
            
            _runner = Instantiate<NetworkRunner>(_networkingSettings.RunnerPrefab, new InstantiateParameters() { parent = transform });
            _runner.ProvideInput = true;
            _runner.AddCallbacks(_networkRunnerCallbacksAdapter);

            _playerConnectionsManager.Init(_networkRunnerCallbacksAdapter);
        }

        private void DeinitializeSession()
        {
            Core.Log.Message(NetworkingLogChannel.NetworkingSystem, "Deinitializing Fusion session...");
            Interlocked.Exchange(ref _runnerState, (int)RunnerInitializationSubstate.Deinitialized);
            if (_runner != null)
            {
                _runner.RemoveCallbacks(_networkRunnerCallbacksAdapter);
                Destroy(_runner.gameObject);
                _runner = null;
            }
            _playerConnectionsManager?.DeInit();
            if (_pooledNetworkObjectProvider != null)
            {
                Destroy(_pooledNetworkObjectProvider.gameObject);
                _pooledNetworkObjectProvider = null;
            }
        }

        private Dictionary<string, SessionProperty> GetAllConnectionSessionProperties(Dictionary<string, object> sessionProperties)
        {
            var propDict = new Dictionary<string, SessionProperty>();
            _actualSessionProperties = new List<StringSessionProperty>();
            if (sessionProperties != null)
            {
                foreach (var prop in sessionProperties)
                {
                    SessionProperty sessionProperty = SessionProperty.Convert(prop.Value);
                    if (sessionProperty != null)
                    {
                        propDict.Add(prop.Key, sessionProperty);
                        _actualSessionProperties.Add(new StringSessionProperty { propertyName = prop.Key, value = sessionProperty });
                    }
                }
            }
            if (_networkingSettings.AdditionalSessionProperties != null)
            {
                foreach (var additionalProperty in _networkingSettings.AdditionalSessionProperties)
                {
                    propDict[additionalProperty.propertyName] = additionalProperty.value;
                    _actualSessionProperties.Add(additionalProperty);
                }

            }
            return propDict;
        }

        #endregion
    }
}