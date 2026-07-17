using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using MessagePipe;

namespace Stratton.Core
{
    public class GameStateSystem : GameSystemBase
    {
        #region Fields

        private IPublisher<GameStateChangedEvent> _gameStateChangedEventPublisher;

        private Dictionary<GameStateType, GameStateBase> _states;
        private GameStateBase _currentState;
        private GameStateBase _previousState;
        private GameStateType _incomingStateType;

        #endregion

        #region Properties

        public GameStateBase PreviousState => _previousState;
        public GameStateType PreviousStateType => _previousState != null ? _previousState.GetStateType() : BaseGameStateType.None;
        public GameStateBase CurrentState => _currentState;
        public GameStateType CurrentStateType => _currentState != null ? _currentState.GetStateType() : BaseGameStateType.None;
        public GameStateType IncomingStateType => _incomingStateType;

        #endregion

        #region Public Methods

        public void Awake()
        {
            _states = CreateStates();
        }

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            foreach (var state in _states)
            {
                state.Value.InstallMessageBrokers(builder);
            }
            builder.AddMessageBroker<GameStateChangedEvent>();
        }

        public override async UniTask<InitializationResult> Init()
        {
            _gameStateChangedEventPublisher = GlobalMessagePipe.GetPublisher<GameStateChangedEvent>();

            IsReady = true;
            GoToState(BaseGameStateType.Initializing);
            await UniTask.CompletedTask;
            return InitializationResult.Success;
        }

        public override async UniTask<DeinitializationResult> DeInit()
        {
            GoToState(BaseGameStateType.DeInitializing);
            if (_currentState != null)
            {
                _currentState.Exit();
            }
            _currentState = null;
            IsReady = false;
            await UniTask.CompletedTask;
            return DeinitializationResult.Success;
        }

        public virtual GameStateBase GetState(GameStateType type)
        {
            if (_states.ContainsKey(type))
            {
                return _states[type];
            }
            Log.Error(BaseLogChannel.GameStates, $"Can't find state of type {type}");
            return null;
        }

        /// <summary>
        /// Disables current game state and enables a new one
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual GameStateBase GoToState(GameStateType type, bool force = false)
        {
            GameStateBase newState = GetState(type);
            if (newState == null) return null;
            _incomingStateType = type;
            if (_currentState != null)
            {

                if (_currentState.GetStateType() == type && !force)
                {
                    return _currentState;
                }

                _currentState.Exit();
            }
            _previousState = _currentState;
            _currentState = newState;
            
            Log.Message(BaseLogChannel.GameStates, $"Entered state: {_currentState.GetType()}");

            _currentState.Enter();

            _gameStateChangedEventPublisher.Publish(new GameStateChangedEvent() { From = PreviousStateType, To = type });

            _incomingStateType = BaseGameStateType.None;

            return _currentState;
        }

        #endregion

        #region Private Methods

        protected Dictionary<GameStateType, GameStateBase> CreateStates()
        {
            var states = new Dictionary<GameStateType, GameStateBase>();
            List<Type> types = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(asm.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract && t.IsSubclassOf(typeof(GameStateBase))));
            }
            foreach (var t in types)
            {
                PropertyInfo property = t.GetProperty("Type", BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                {
                    GameStateType value = (GameStateType)property.GetValue(null);
                    var state = (GameStateBase)Activator.CreateInstance(t);
                    states.Add(value, state);
                }
                else
                {
                    Log.Error(BaseLogChannel.GameStates, $"Can't find the Type property in the game state of type {t}");
                }
            }
            return states;
        }

        #endregion
    }
}