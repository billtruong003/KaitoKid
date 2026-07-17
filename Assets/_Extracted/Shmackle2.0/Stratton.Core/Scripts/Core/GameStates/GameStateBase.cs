using System.Reflection;
using MessagePipe;

namespace Stratton.Core
{
    public abstract class GameStateBase
    {
        #region Private Methods

        /// <summary>
        /// Register state's logic events
        /// </summary>
        protected virtual void RegisterEvents() { }

        /// <summary>
        /// Unregister state's logic events
        /// </summary>
        protected virtual void UnregisterEvents() { }

        #endregion

        #region Public Methods

        public virtual void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
        }

        /// <summary>
        /// Enables state's logic
        /// </summary>
        public virtual void Enter()
        {
            RegisterEvents();
        }

        /// <summary>
        /// Disables state's logic
        /// </summary>
        public virtual void Exit()
        {
            UnregisterEvents();
        }

        public GameStateType GetStateType()
        {
            PropertyInfo property = GetType().GetProperty("Type", BindingFlags.Public | BindingFlags.Static);
            if (property != null)
            {
                return (GameStateType)property.GetValue(null);
            }
            Log.Error(BaseLogChannel.GameStates, $"Can't find the Type property in the game state of type {GetType()}");
            return BaseGameStateType.None;
        }

        #endregion
    }
}