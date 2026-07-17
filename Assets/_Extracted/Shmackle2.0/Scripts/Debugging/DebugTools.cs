using UnityEngine;

namespace Shmackle.Debugging
{
    public class DebugTools : Stratton.Debugging.DebugTools
    {
        #region Serialized Fields

        [SerializeField] private DebugRuntimeInfo _debugRuntimeInfo;

        #endregion

        #region Fields

        private DebugRuntimeInfo _debugRuntimeInfoInstance;

        #endregion

        #region Properties

        public DebugRuntimeInfo DebugRuntimeInfo => _debugRuntimeInfoInstance;

        #endregion

        #region Public Methods

        public override void Init()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR || DEBUG
            _debugRuntimeInfoInstance = Instantiate(_debugRuntimeInfo, transform, true);
#endif
            base.Init();
        }

        #endregion
    }
}