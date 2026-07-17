using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Stratton.Core
{
    public class GameSystemsManager : Singleton<GameSystemsManager>
    {
        #region Serialized Fields

        [SerializeField] private GameSystemBase[] _initializationOrder;

        #endregion

        #region Fields

        protected IPublisher<AllGameSystemsInitializedEvent> _allGameSystemsInitializedEventPublisher;
        protected IPublisher<GameSystemInitializationFailedEvent> _gameSystemInitializationFailedEventPublisher;
        protected IPublisher<GameSystemInitializationCancelledEvent> _gameSystemInitializationCancelledEventPublisher;
        protected IPublisher<GameSystemInitializationPausedEvent> _gameSystemInitializationPausedEventPublisher;

        protected readonly List<GameSystemBase> _gameSystems = new List<GameSystemBase>();
        protected readonly Dictionary<Type, GameSystemBase> _gameSystemsByType = new Dictionary<Type, GameSystemBase>();
        protected bool _isInitializing;

        #endregion

        #region Public Properties

        public bool IsReady { protected set; get; }

        #endregion

        #region Public Methods

        public virtual T Get<T>() where T : GameSystemBase
        {
            if (_gameSystemsByType.TryGetValue(typeof(T), out var gs))
            {
                return (T)gs;
            }
            foreach (var gameSystem in _gameSystems)
            {
                if (gameSystem is T)
                {
                    _gameSystemsByType.Add(typeof(T), gameSystem);
                    return (T)gameSystem;
                }
            }
            return default;
        }

        public void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            foreach (var gameSystem in _initializationOrder)
            {
                gameSystem.InstallMessageBrokers(builder);
            }
            builder.AddMessageBroker<AllGameSystemsInitializedEvent>();
            builder.AddMessageBroker<GameSystemInitializationFailedEvent>();
            builder.AddMessageBroker<GameSystemInitializationCancelledEvent>();
            builder.AddMessageBroker<GameSystemInitializationPausedEvent>();
        }

        public async UniTask Init(bool isRetry = false)
        {
            if (IsReady)
            {
                Log.Message(BaseLogChannel.Core, "Game systems are already initialized!");
                return;
            }
            if (_isInitializing)
            {
                Log.Message(BaseLogChannel.Core, "Game systems are already being initialized!");
                return;
            }
            Log.Message(BaseLogChannel.Core, "Initializing game systems...");
            _isInitializing = true;
            if (!isRetry)
            {
                _gameSystems.Clear();
                foreach (var gameSystem in _initializationOrder)
                {
                    _gameSystems.Add(gameSystem);
                }
            }
            foreach (var gameSystem in _gameSystems)
            {
                if (isRetry)
                {
                    if (gameSystem.IsReady)
                    {
                        continue;
                    }
                }
                var result = await gameSystem.Init();
                if (result.IsFailed)
                {
                    Log.Error(BaseLogChannel.Core, $"Initialization failed of {gameSystem.GetType()}: {result.ErrorMessage}");
                    _gameSystemInitializationFailedEventPublisher.Publish(new GameSystemInitializationFailedEvent() { Type = gameSystem.GetType(), ErrorCode = result.ErrorCode, ErrorMessage = result.ErrorMessage });
                }
                else if (result.IsCancelled)
                {
                    Log.Warning(BaseLogChannel.Core, $"Initialization cancelled of {gameSystem.GetType()}");
                    _gameSystemInitializationCancelledEventPublisher.Publish(new GameSystemInitializationCancelledEvent() { Type = gameSystem.GetType() });
                }
                else if (result.IsPaused)
                {
                    Log.Warning(BaseLogChannel.Core, $"Initialization paused of {gameSystem.GetType()}");
                    _gameSystemInitializationPausedEventPublisher.Publish(new GameSystemInitializationPausedEvent() { Type = gameSystem.GetType() });
                }
                if (!result.IsSuccess)
                {
                    _isInitializing = false;
                    return;
                }
            }

            _allGameSystemsInitializedEventPublisher = GlobalMessagePipe.GetPublisher<AllGameSystemsInitializedEvent>();
            _gameSystemInitializationFailedEventPublisher = GlobalMessagePipe.GetPublisher<GameSystemInitializationFailedEvent>();
            _gameSystemInitializationCancelledEventPublisher = GlobalMessagePipe.GetPublisher<GameSystemInitializationCancelledEvent>();
            _gameSystemInitializationPausedEventPublisher = GlobalMessagePipe.GetPublisher<GameSystemInitializationPausedEvent>();

            IsReady = true;
            _allGameSystemsInitializedEventPublisher.Publish(new AllGameSystemsInitializedEvent());
            _isInitializing = false;
            Log.Message(BaseLogChannel.Core, $"Game systems initialization completed!");
        }

        public virtual async UniTask DeInit()
        {
            Log.Message(BaseLogChannel.Core, "Deinitializing game systems...");
            for (int i = _gameSystems.Count - 1; i >= 0; i--)
            {
                var result = await _gameSystems[i].DeInit();
                if (result.IsError)
                {
                    Log.Error(BaseLogChannel.Core, $"Deinitialization failed of {_gameSystems[i].GetType()}: {result.ErrorMessage}");
                }
            }
            IsReady = false;
            Log.Message(BaseLogChannel.Core, $"Systems deinitialization completed!");
        }

        public virtual async UniTask ContinueInit()
        {
            Log.Message(BaseLogChannel.Core, "Game systems initialization continuation requested!");
            await Init(true);
        }

        public virtual async UniTask ReInit()
        {
            Log.Message(BaseLogChannel.Core, "Game systems reinitialization requested!");
            await DeInit();
            await Init();
        }

        #endregion
    }
}