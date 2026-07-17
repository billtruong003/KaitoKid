using MessagePipe;
using UnityEngine;

namespace Stratton.Core
{
    public abstract class AppServiceBase : MonoBehaviour, IInitializable
    {
        #region Properties

        public virtual bool IsReady { get; protected set; }

        #endregion

        #region Public Methods

        public virtual void Init()
        {
            IsReady = true;
        }

        public virtual void DeInit()
        {
            IsReady = false;
        }

        public abstract void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder);

        #endregion
    }
}