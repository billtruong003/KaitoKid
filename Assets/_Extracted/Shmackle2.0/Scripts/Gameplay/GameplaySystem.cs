using Cysharp.Threading.Tasks;
using Fusion;
using MessagePipe;
using Stratton.Core;
using Stratton.Networking;
using System;
using Shmackle.Player;
using UnityEngine;

namespace Shmackle.Gameplay
{
    public class GameplaySystem : GameSystemBase
    {
        #region Serialize Field

        [SerializeField, Tooltip("Fallback game mode of scenes if no game mode is available on the scene.")]
        private GameModeBase _defaultGameModePrefab;

        #endregion

        #region Private Fields

        [Header("Runtime Values")]
        [SerializeField]
        private GameModeBase _activeGameMode;

        private NetworkingSystem _networkingSystem;
        private IDisposable _eventsBagDisposable;

        #endregion
        
        #region Properties
        
        private GameModeBase ActiveGameMode
        {
            get
            {
                if (_activeGameMode == null)
                {
                    _activeGameMode = FindAnyObjectByType<GameModeBase>();
                }
                return _activeGameMode;
            }
        }

        #endregion

        #region Public Methods

        public override UniTask<InitializationResult> Init()
        {
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            
            var bag = DisposableBag.CreateBuilder();

            GlobalMessagePipe.GetSubscriber<PlayerJoinedEvent>().Subscribe(e => OnPlayerJoined(e.Player)).AddTo(bag);
            GlobalMessagePipe.GetSubscriber<PlayerLeftEvent>().Subscribe(e => OnPlayerLeft(e.Player)).AddTo(bag);
            GlobalMessagePipe.GetSubscriber<SceneLoadedEvent>().Subscribe(e => OnSceneLoadDone()).AddTo(bag);
            
            _eventsBagDisposable = bag.Build();
            
            return base.Init();
        }

        public override UniTask<DeinitializationResult> DeInit()
        {   
            _eventsBagDisposable?.Dispose();
            return base.DeInit();
        }

        public override void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder)
        {
            builtinContainerBuilder.AddMessageBroker<PlayerStateRegisteredEvent>();
            builtinContainerBuilder.AddMessageBroker<PlayerStateUnregisteredEvent>();
            builtinContainerBuilder.AddMessageBroker<ButtSmackNetworkEventRelay>();
            builtinContainerBuilder.AddMessageBroker<KissNetworkEventRelay>();
        }

        public T GetActiveGameMode<T>() where T : GameModeBase
        {
            return ActiveGameMode as T;
        }

        public T GetPlayerState<T>(PlayerRef playerRef) where T : PlayerState
        {
            if (ActiveGameMode != null)
            {
                PlayerState playerState = ActiveGameMode.GetPlayerState<PlayerState>(playerRef);
                if (playerState)
                {
                    return playerState as T;
                }
            }
            return null;
        }

        public T GetLocalPlayerState<T>() where T : PlayerState
        {
            if (ActiveGameMode != null)
            {
                return ActiveGameMode.GetLocalPlayerState<T>();
            }
            return null;
        }

        #endregion

        #region Private Methods

        private void OnPlayerJoined(PlayerRef playerRef)
        {
            NetworkRunner runner = _networkingSystem.Runner;
            if (runner.IsServer || runner.IsSharedModeMasterClient)
            {
                ActiveGameMode.OnPlayerJoined(playerRef);
            }
        }

        private void OnPlayerLeft(PlayerRef playerRef)
        {
            NetworkRunner runner = _networkingSystem.Runner;
            if (runner.IsServer || runner.IsSharedModeMasterClient)
            {
                ActiveGameMode.OnPlayerLeft(playerRef);
            }
        }

        private void OnSceneLoadDone()
        {
            NetworkRunner runner = _networkingSystem.Runner;
            if (runner.IsServer || runner.IsSharedModeMasterClient)
            {
                if (ActiveGameMode == null && _defaultGameModePrefab != null)
                {
                    _activeGameMode = runner.Spawn(_defaultGameModePrefab);
                    Stratton.Core.Log.Warning(BaseLogChannel.Gameplay, "Using the default game mode");
                }
                if (ActiveGameMode == null)
                {
                    Stratton.Core.Log.Error(BaseLogChannel.Gameplay, "Game mode is not available");
                }
                else
                {
                    Stratton.Core.Log.Message(BaseLogChannel.Gameplay, $"Game mode: {ActiveGameMode.name}");
                }
            }
        }

        #endregion
    }
}
