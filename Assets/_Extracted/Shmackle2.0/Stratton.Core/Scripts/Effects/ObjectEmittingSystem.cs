using Cysharp.Threading.Tasks;
using Stratton.Core;
using UnityEngine;

namespace Stratton.Effects
{
    public interface IObjectEmittingSystem<T> where T : ObjectEmitter
    {
        public T Play(string effectKey, Transform cameraTransform = null);
        public T Play(string effectKey, Vector3 position, Transform cameraTransform = null);
        public T Play(string effectKey, Transform parent, Transform cameraTransform = null);
        public void Stop(T emitter);
        public void Stop(T emitter, float fadeOutTime);
    }

    public abstract class ObjectEmittingSystem<T> : GameSystemBase, IObjectEmittingSystem<T> where T : ObjectEmitter
    {
        public async override UniTask<InitializationResult> Init()
        {
            CreatePool();
            IsReady = true;
            await UniTask.CompletedTask;
            return InitializationResult.Success;
        }

        public async override UniTask<DeinitializationResult> DeInit()
        {
            IsReady = false;
            await UniTask.CompletedTask;
            return DeinitializationResult.Success;
        }

        protected abstract void CreatePool();

        public abstract T Play(string effectKey, Transform cameraTransform = null);

        public abstract T Play(string effectKey, Vector3 position, Transform cameraTransform = null);

        public abstract T Play(string effectKey, Transform parent, Transform cameraTransform = null);

        public abstract void Stop(T emitter);

        public abstract void Stop(T emitter, float fadeOutTime);
    }
}