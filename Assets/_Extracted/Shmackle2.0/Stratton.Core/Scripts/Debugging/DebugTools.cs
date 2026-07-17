using MessagePipe;
using Stratton.AppReloading;
using Stratton.Core;
using UnityEngine;

namespace Stratton.Debugging
{
    public class DebugTools : AppServiceBase
    {
        #region Serialized Fields

        [SerializeField] private DebugConsoleController _debugConsolePrefab;

        #endregion

        #region Fields

        private DebugConsoleController _debugConsoleInstance;

        #endregion

        #region Properties

        public DebugConsoleController DebugConsole => _debugConsoleInstance;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            builder.AddMessageBroker<AppReloadRequestedEvent>();
        }

        public override void Init()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR || DEBUG
            _debugConsoleInstance = Instantiate(_debugConsolePrefab, transform, true);
#endif
            base.Init();
        }

        #endregion
    }
}