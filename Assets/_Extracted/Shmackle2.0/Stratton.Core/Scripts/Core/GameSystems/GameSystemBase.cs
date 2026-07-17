using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Stratton.Core
{
    public abstract class GameSystemBase : MonoBehaviour, IInitializableAsync
    {
        #region Properties

        public virtual bool IsReady { get; protected set; }

        #endregion

        #region Public Methods

        public virtual async UniTask<InitializationResult> Init()
        {
            IsReady = true;
            await UniTask.CompletedTask;
            return InitializationResult.Success;
        }

        public virtual async UniTask<DeinitializationResult> DeInit()
        {
            IsReady = false;
            await UniTask.CompletedTask;
            return DeinitializationResult.Success;
        }

        public abstract void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder);

        #endregion
    }
}