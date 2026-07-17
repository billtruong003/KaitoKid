using System;
using Cysharp.Threading.Tasks;
using Stratton.Assets;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.Core
{
    public abstract class AddressableComponent<TSource, TComponent> : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] protected TSource _assetSource;

        #endregion

        #region Fields

        protected AssetsSystem _assetsSystem;

        protected ISubscriber<AddressablesInitializedEvent> _addressablesInitializedEventSubscriber;
        protected ISubscriber<RemoteAddressablesCachedEvent> _remoteAddressablesCachedEventSubscriber;

        protected IDisposable _eventsBagDisposable;
        protected bool _isAssetCached;
        protected long _downloadSize = -1;
        protected AsyncOperationHandle<TComponent> _asyncOperationHandler;

        #endregion

        #region Public Methods

        public AsyncOperationHandle<TComponent> LoadAssetAsync()
        {
#if ADDRESSABLE_CACHE_VERIFICATION
            if (!_isAssetCached)
            {
                throw new NotCachedAddressableException($"[Addressables] Typeof: {this.GetType()} is attempting to use addressable {_assetSource.GetType()} that is not cached.");
            }
#endif
            return LoadCachedAssetAsync();
        }

        public void ReleaseInstance()
        {
            // Release the instance
            ReleaseAsset();
        }

        #endregion

        #region Private Methods

        protected void Awake()
        {
            Init().Forget();
        }

        protected void OnDestroy()
        {
            DeInit();
        }

        protected abstract AsyncOperationHandle<TComponent> LoadCachedAssetAsync();

        protected async UniTask Init()
        {
            _assetsSystem = GameSystemsManager.Instance.Get<AssetsSystem>();
            _addressablesInitializedEventSubscriber = GlobalMessagePipe.GetSubscriber<AddressablesInitializedEvent>();
            _remoteAddressablesCachedEventSubscriber = GlobalMessagePipe.GetSubscriber<RemoteAddressablesCachedEvent>();
            
            var currentGlobalStatus = _assetsSystem.AddAssetReferenceAddressable();
            _downloadSize = await Addressables.GetDownloadSizeAsync(_assetSource).Task;
            _isAssetCached = (currentGlobalStatus.IsAllRemoteAddressablesCached || _downloadSize == 0);

            var bag = DisposableBag.CreateBuilder();

            _addressablesInitializedEventSubscriber.Subscribe(e =>
            {
                _isAssetCached = e.GlobalAddressablesStatus.IsAllRemoteAddressablesCached || _downloadSize == 0;
            }).AddTo(bag);

            _remoteAddressablesCachedEventSubscriber.Subscribe(e =>
            {
                _isAssetCached = true;
            }).AddTo(bag);

            _eventsBagDisposable = bag.Build();
        }

        protected void DeInit()
        {
            _eventsBagDisposable?.Dispose();
        }

        protected virtual void ReleaseAsset()
        {
            if (!_asyncOperationHandler.IsValid())
            {
                Debug.LogError("Handler is not valid. It wont be released.");
                return;
            }
            var componentAsComponent = _asyncOperationHandler.Result as Component;
            if (componentAsComponent != null)
            {
                Addressables.ReleaseInstance(componentAsComponent.gameObject);
            }

            var componentAsGameObject = _asyncOperationHandler.Result as GameObject;
            if (componentAsComponent != null)
            {
                Addressables.ReleaseInstance(componentAsGameObject);
            }
            else
            {
                Addressables.Release(_asyncOperationHandler.Result); // Sprites, Textures, AudioFiles
            }

            // Release the handle
            Addressables.Release(_asyncOperationHandler);
        }

        protected AsyncOperationHandle<TComponent> AssetReady(AsyncOperationHandle<TComponent> arg)
        {
            _asyncOperationHandler = arg;
            return Addressables.ResourceManager.CreateCompletedOperation<TComponent>(arg.Result, string.Empty);
        }

        #endregion
    }
}