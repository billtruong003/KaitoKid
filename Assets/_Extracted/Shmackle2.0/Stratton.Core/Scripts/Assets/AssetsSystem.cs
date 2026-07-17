using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Stratton.Core;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Stratton.Assets
{
    public struct LoadSceneHandler
    {
        public AssetReference AssetReference;
        public UniTask<SceneResult> UniTask;

        public event Action<LoadSceneHandler> OnCanceled;

        public void Cancel()
        {
            OnCanceled?.Invoke(this);
        }
    }

    public struct GlobalAddressablesStatus
    {
        public bool IsAllRemoteAddressablesCached { get; private set; }
        public GlobalAddressablesStatus(bool isAllAddressablesCached)
        {
            IsAllRemoteAddressablesCached = isAllAddressablesCached;
        }
    }

    public struct DownloadAddressablesStatus
    {
        public string AddressOrLabel { get; private set; }
        public bool IsCached { get; private set; }
        public long BytesToDownload { get; private set; }

        public DownloadAddressablesStatus(string addressOrLabel, bool isCached, long bytesToDownload)
        {
            AddressOrLabel = addressOrLabel;
            IsCached = isCached;
            BytesToDownload = bytesToDownload;
        }
    }

    public struct MemoryManagedAddressableAsset<T> where T : class
    {
        public T Component { get; private set; }
        public AsyncOperationHandle AsyncOperationHandle { get; private set; }

        public MemoryManagedAddressableAsset(T component, AsyncOperationHandle asyncOperationHandle)
        {
            Component = component;
            AsyncOperationHandle = asyncOperationHandle;
        }
    }

    public class AssetsSystem : GameSystemBase
    {
        #region Serialized Fields

        [SerializeField] protected List<AssetLabelReference> _remoteLabels;

        #endregion

        #region Fields

        private IPublisher<AddressablesInitializedEvent> _addressablesInitializedEventPublisher;
        private IPublisher<RemoteAddressablesCachedEvent> _remoteAddressablesCachedEventPublisher;

        protected IObjectPool _objectPool;

        protected IResourceLocator _resourceLocator;

        protected bool _isAllAddressablesCached;
        protected long _totalBytesToDownload;

        protected readonly List<object> _assetKeysInAddressables = new List<object>();

        #endregion

        #region Properties

        public bool IsAllAddressablesCached => _isAllAddressablesCached;
        public long TotalBytesToDownload => _totalBytesToDownload;

        /// <summary>
        ///     Temporary here... will be rearranged later.
        /// </summary>
        public IObjectPool ObjectPool
        {
            get { return _objectPool; }
        }

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
            builder.AddMessageBroker<AddressablesInitializedEvent>();
            builder.AddMessageBroker<RemoteAddressablesCachedEvent>();
        }

        public override async UniTask<InitializationResult> Init()
        {
            _addressablesInitializedEventPublisher = GlobalMessagePipe.GetPublisher<AddressablesInitializedEvent>();
            _remoteAddressablesCachedEventPublisher = GlobalMessagePipe.GetPublisher<RemoteAddressablesCachedEvent>();
            
            _isAllAddressablesCached = true;
            _objectPool = new ObjectPool();
            _objectPool.Init();

            await Addressables.InitializeAsync().Task;

            var cacheResult = await IsDownloadDependenciesNeeded(_remoteLabels);
            foreach (var item in cacheResult)
            {
                _isAllAddressablesCached &= item.IsCached;
                _totalBytesToDownload += item.BytesToDownload;
            }
            _addressablesInitializedEventPublisher.Publish(new() { GlobalAddressablesStatus = new GlobalAddressablesStatus(_isAllAddressablesCached) });

            try
            {
                foreach (var l in Addressables.ResourceLocators)
                {
                    foreach (var key in l.Keys)
                    {
                        if (!_assetKeysInAddressables.Contains(key))
                        {
                            _assetKeysInAddressables.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(BaseLogChannel.Assets, $"Exception occured while making a list of local assets: {ex.Message}");
            }

            IsReady = true;
            return InitializationResult.Success;
        }

        public override async UniTask<DeinitializationResult> DeInit()
        {
            IsReady = false;
            return DeinitializationResult.Success;
        }

        public IObjectPool GetObjectPool()
        {
            return _objectPool;
        }

        public virtual async UniTask<DownloadAssetsResult> DownloadAllRemoteAssets(IProgress<float> progress = null)
        {
            var handle = Addressables.DownloadDependenciesAsync(_remoteLabels.Select(l => l.labelString), Addressables.MergeMode.Intersection);
            float lastPercent = 0;
            while (!handle.IsDone)
            {
                DownloadStatus downloadStatus = handle.GetDownloadStatus();
                if (downloadStatus.Percent != lastPercent)
                {
                    lastPercent = downloadStatus.Percent;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            DownloadAssetsResult result;
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress?.Report(1f);
                _isAllAddressablesCached = true;
                result = DownloadAssetsResult.Success;
            }
            else
            {
                _isAllAddressablesCached = false;
                result = DownloadAssetsResult.Error(DataLoadErrorCode.Unknown, "Failed to download!");
            }

            Addressables.Release(handle);
            return result;
        }

        public virtual async UniTask<DownloadAssetsResult> DownloadRemoteAssetsByLabel(string key, IProgress<float> progress = null)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(key);

            while (!sizeHandle.IsDone)
            {
                await UniTask.Delay(1);
            }
            if (sizeHandle.IsValid() && sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                if (sizeHandle.Result == 0.0f)
                {
                    Addressables.Release(sizeHandle);
                    return DownloadAssetsResult.Success;
                }
            }

            var loadhandle = Addressables.LoadResourceLocationsAsync(key);

            float lastPercent = 0;
            while (!loadhandle.IsDone)
            {
                await UniTask.Delay(1);
            }

            DownloadAssetsResult result;
            if (loadhandle.IsValid() && loadhandle.Status == AsyncOperationStatus.Succeeded)
            {
                var handle = Addressables.DownloadDependenciesAsync(key);
                while (!handle.IsDone)
                {
                    DownloadStatus downloadStatus = handle.GetDownloadStatus();
                    if (downloadStatus.Percent != lastPercent)
                    {
                        lastPercent = downloadStatus.Percent;
                        progress?.Report(lastPercent);
                    }
                    await UniTask.Delay(1);
                }
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    progress?.Report(1f);
                    result = DownloadAssetsResult.Success;
                }
                else
                {
                    result = DownloadAssetsResult.Error(DataLoadErrorCode.Unknown, "Failed to download!");
                }
                Addressables.Release(handle);
            }
            else
            {

                result = DownloadAssetsResult.Error(DataLoadErrorCode.Unknown, $"Failed to load resources for label {key}!");
            }

            Addressables.Release(sizeHandle);
            Addressables.Release(loadhandle);
            return result;
        }

        public virtual async UniTask<DownloadAssetsResult> DownloadRemoteAssetsByLabel(AssetLabelReference label, IProgress<float> progress = null)
        {
            return await DownloadRemoteAssetsByLabel(label.labelString, progress);
        }

        public virtual async UniTask<DownloadAssetsResult> DownloadRemoteAsset(string key, IProgress<float> progress = null)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(key);

            while (!sizeHandle.IsDone)
            {
                await UniTask.Delay(1);
            }
            if (sizeHandle.IsValid() && sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                if (sizeHandle.Result == 0.0f)
                {
                    Addressables.Release(sizeHandle);
                    return DownloadAssetsResult.Success;
                }
            }
            var handle = Addressables.DownloadDependenciesAsync(key);
            float lastPercent = 0;
            while (!handle.IsDone)
            {
                DownloadStatus downloadStatus = handle.GetDownloadStatus();
                if (downloadStatus.Percent != lastPercent)
                {
                    lastPercent = downloadStatus.Percent;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }

            DownloadAssetsResult result;
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress?.Report(1f);
                result = DownloadAssetsResult.Success;
            }
            else
            {
                result = DownloadAssetsResult.Error(DataLoadErrorCode.Unknown, "Failed to download!");
            }

            Addressables.Release(sizeHandle);
            Addressables.Release(handle);
            return result;  
        }

        public virtual async UniTask<DownloadAssetsResult> DownloadRemoteAsset(AssetReference assetReference, IProgress<float> progress = null)
        {
            if (!assetReference.RuntimeKeyIsValid())
            {
                return DownloadAssetsResult.Error(DataLoadErrorCode.NotFound, $"AssetReference key {assetReference.RuntimeKey} is not valid");
            }

            return await DownloadRemoteAsset(assetReference.RuntimeKey.ToString(), progress);
        }

        public virtual async UniTask<DownloadAssetsResult> DownloadRemoteAssets(List<AssetReference> group, IProgress<float> progress = null)
        {
            var resourceLocations = new List<IResourceLocation>();
            foreach (var asset in group)
            {
                if (!asset.RuntimeKeyIsValid())
                {
                    return DownloadAssetsResult.Error(DataLoadErrorCode.NotFound, $"AssetReference key {asset.RuntimeKey} is not valid");
                }

                string guid = asset.AssetGUID;

                IResourceLocation resourceLocation = new ResourceLocationBase(
                    guid,
                    guid,
                    asset.RuntimeKey.ToString(),
                    typeof(UnityEngine.Object),
                    new IResourceLocation[] { }
                );

                resourceLocations.Add(resourceLocation);
            }

            var sizeHandle = Addressables.GetDownloadSizeAsync(resourceLocations);

            while (!sizeHandle.IsDone)
            {
                await UniTask.Delay(1);
            }
            if (sizeHandle.IsValid() && sizeHandle.Status == AsyncOperationStatus.Succeeded)
            {
                if (sizeHandle.Result == 0.0f)
                {
                    Addressables.Release(sizeHandle);
                    return DownloadAssetsResult.Success;
                }
            }

            var handle = Addressables.DownloadDependenciesAsync(resourceLocations);
            float lastPercent = 0;
            while (!handle.IsDone)
            {
                DownloadStatus downloadStatus = handle.GetDownloadStatus();
                if (downloadStatus.Percent != lastPercent)
                {
                    lastPercent = downloadStatus.Percent;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }

            DownloadAssetsResult result;
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress?.Report(1f);
                result = DownloadAssetsResult.Success;
            }
            else
            {
                result = DownloadAssetsResult.Error(DataLoadErrorCode.Unknown, "Failed to download!");
            }

            Addressables.Release(sizeHandle);
            Addressables.Release(handle);
            return result;
        }

        public virtual GlobalAddressablesStatus AddAssetReferenceAddressable()
        {
            return new GlobalAddressablesStatus(_isAllAddressablesCached);
        }

        public virtual async UniTask<IList<IResourceLocation>> LoadAssetsLocations(string addressOrLabel)
        {
            return await Addressables.LoadResourceLocationsAsync(addressOrLabel).ToUniTask();
        }

        public virtual async UniTask<IList<IResourceLocation>> LoadAssetsLocations<T>(string addressOrLabel)
        {
            return await Addressables.LoadResourceLocationsAsync(addressOrLabel, typeof(T)).ToUniTask();
        }

        public virtual LoadSceneHandler LoadScene(LoadSceneMode loadMode, AssetReference assetReference, IProgress<float> progress)
        {
            var loadSceneHandler = new LoadSceneHandler
            {
                AssetReference = assetReference,
                UniTask = LoadScene(assetReference, loadMode, progress)
            };
            return loadSceneHandler;
        }

        public virtual async UniTask<SceneResult> LoadScene(AssetReference assetReference, LoadSceneMode loadMode, IProgress<float> progress = null)
        {
            if (!assetReference.RuntimeKeyIsValid())
            {
                return SceneResult.Error($"Runtime key is not valid!");
            }
            float lastPercent = 0;
            if (assetReference.OperationHandle.IsValid())
            {
                // If loading is not already completed
                if (!assetReference.OperationHandle.IsDone)
                {
                    while (!assetReference.OperationHandle.IsDone)
                    {
                        if (assetReference.OperationHandle.PercentComplete != lastPercent)
                        {
                            lastPercent = assetReference.OperationHandle.PercentComplete;
                            progress?.Report(lastPercent);
                        }
                        await UniTask.Delay(1);
                    }
                    if (assetReference.OperationHandle.IsValid() &&
                        assetReference.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        progress?.Report(1f);
                        return new SceneResult()
                        {
                            SceneInstance = assetReference.OperationHandle.Convert<SceneInstance>().Result,
                            OperationHandle = assetReference.OperationHandle
                        };
                    }
                    return SceneResult.Error($"Failed to load scene!");
                }
                // If loading is completed
                else
                {
                    progress?.Report(1f);
                    return new SceneResult()
                    {
                        SceneInstance = assetReference.OperationHandle.Convert<SceneInstance>().Result,
                        OperationHandle = assetReference.OperationHandle
                    };
                }
            }
            else
            {
                var handle = assetReference.LoadSceneAsync(loadMode);
                while (!handle.IsDone)
                {
                    if (handle.PercentComplete != lastPercent)
                    {
                        lastPercent = handle.PercentComplete;
                        progress?.Report(lastPercent);
                    }
                    await UniTask.Delay(1);
                }
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    progress?.Report(1f);
                    return new SceneResult()
                    {
                        SceneInstance = handle.Result,
                        OperationHandle = handle
                    };
                }
                return SceneResult.Error($"Failed to load scene!");
            }
        }

        public virtual async UniTask<SceneResult> LoadScene(string key, LoadSceneMode loadMode, IProgress<float> progress = null)
        {
            var handle = Addressables.LoadSceneAsync(key, loadMode);
            float lastPercent = 0;
            while (!handle.IsDone)
            {
                if (handle.PercentComplete != lastPercent)
                {
                    lastPercent = handle.PercentComplete;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress?.Report(1f);
                return new SceneResult()
                {
                    SceneInstance = handle.Result,
                    OperationHandle = handle
                };
            }
            return SceneResult.Error($"Failed to load scene!");
        }

        public virtual async UniTask<SceneResult> UnloadScene(AssetReference assetReference)
        {
            if (!assetReference.RuntimeKeyIsValid())
            {
                return SceneResult.Error($"Runtime key is not valid!");
            }
            if (assetReference.OperationHandle.IsValid())
            {
                // If loading is not already completed
                if (!assetReference.OperationHandle.IsDone)
                {
                    await assetReference.OperationHandle.Task;
                    if (assetReference.OperationHandle.IsValid() &&
                        assetReference.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        return SceneResult.Success;
                    }
                    return SceneResult.Warning($"Failed to unload scene!");
                }
                // If loading is completed
                else
                {
                    return SceneResult.Success;
                }
            }
            else
            {
                var handle = assetReference.UnLoadScene();
                await handle.Task;
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return SceneResult.Success;
                }
                return SceneResult.Warning($"Failed to unload scene!");
            }
        }

        public virtual async UniTask<SceneResult> UnloadScene(SceneInstance sceneInstance)
        {
            var handle = Addressables.UnloadSceneAsync(sceneInstance);
            await handle.Task;
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                return SceneResult.Success;
            }
            return SceneResult.Warning($"Failed to unload scene!");
        }

        public virtual async UniTask<CommonResult> LoadInternalScene(string sceneName, LoadSceneMode loadMode, IProgress<float> progress = null)
        {
            var handle = SceneManager.LoadSceneAsync(sceneName, loadMode);
            float lastPercent = 0;
            while (!handle.isDone)
            {
                if (handle.progress != lastPercent)
                {
                    lastPercent = handle.progress;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            progress?.Report(1f);
            return CommonResult.Success;
        }

        public virtual async UniTask<CommonResult> UnloadInternalScene(string sceneName, IProgress<float> progress = null)
        {
            var handle = SceneManager.UnloadSceneAsync(sceneName);
            float lastPercent = 0;
            while (!handle.isDone)
            {
                if (handle.progress != lastPercent)
                {
                    lastPercent = handle.progress;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            progress?.Report(1f);
            return CommonResult.Success;
        }

        public virtual async UniTask<AssetResult<T>> LoadAsset<T>(AssetReference assetReference, IProgress<float> progress = null) where T : UnityEngine.Object
        {
            if (!assetReference.RuntimeKeyIsValid())
            {
                AssetResult<T>.Error($"Runtime key is not valid!");
            }
            if (assetReference.Asset != null)
            {
                return new AssetResult<T>() { Asset = assetReference.Asset as T };
            }

            float lastPercent = 0;
            if (assetReference.OperationHandle.IsValid())
            {
                // If loading is not already completed
                if (!assetReference.OperationHandle.IsDone)
                {
                    while (!assetReference.OperationHandle.IsDone)
                    {
                        if (assetReference.OperationHandle.PercentComplete != lastPercent)
                        {
                            lastPercent = assetReference.OperationHandle.PercentComplete;
                            progress?.Report(lastPercent);
                        }
                        await UniTask.Delay(1);
                    }
                    if (assetReference.OperationHandle.IsValid() &&
                        assetReference.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        progress?.Report(1f);
                        return new AssetResult<T>()
                        {
                            Asset = assetReference.OperationHandle.Convert<T>().Result,
                            OperationHandle = assetReference.OperationHandle
                        };
                    }
                    return AssetResult<T>.Error($"Failed to load asset!");
                }
                // If loading is completed
                else
                {
                    progress?.Report(1f);
                    return new AssetResult<T>()
                    {
                        Asset = assetReference.OperationHandle.Convert<T>().Result,
                        OperationHandle = assetReference.OperationHandle
                    };
                }
            }
            else
            {
                var handle = assetReference.LoadAssetAsync<T>();
                _ = handle.Task;
                while (!handle.IsDone)
                {
                    if (handle.PercentComplete != lastPercent)
                    {
                        lastPercent = handle.PercentComplete;
                        progress?.Report(lastPercent);
                    }
                    await UniTask.Delay(1);
                }
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    progress?.Report(1f);
                    return new AssetResult<T>()
                    {
                        Asset = handle.Result,
                        OperationHandle = handle
                    };
                }
                return AssetResult<T>.Error($"Failed to load asset!");
            }
        }

        public virtual async UniTask<AssetResult<T>> LoadAsset<T>(string key, IProgress<float> progress = null) where T : UnityEngine.Object
        {
            if (!IsAssetExists<T>(key))
            {
                return AssetResult<T>.Error($"Asset with key {key} not exist");
            }
            var handle = Addressables.LoadAssetAsync<T>(key);
            _ = handle.Task;
            float lastPercent = 0;
            while (!handle.IsDone)
            {
                if (handle.PercentComplete != lastPercent)
                {
                    lastPercent = handle.PercentComplete;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress?.Report(1f);
                return new AssetResult<T>()
                {
                    Asset = handle.Result,
                    OperationHandle = handle
                };
            }
            return AssetResult<T>.Error($"Failed to load asset!");
        }

        public virtual void ReleaseAsset<T>(T obj) where T : UnityEngine.Object
        {
            Addressables.Release(obj);
        }

        public virtual void ReleaseAsset(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        public virtual async UniTask<AssetResult<GameObject>> Instantiate(AssetReference assetReference, IProgress<float> progress = null)
        {
            if (!assetReference.RuntimeKeyIsValid())
            {
                AssetResult<GameObject>.Error($"Runtime key is not valid!");
            }
            if (assetReference.Asset != null)
            {
                return new AssetResult<GameObject>() { Asset = assetReference.Asset as GameObject };
            }

            float lastPercent = 0;
            if (assetReference.OperationHandle.IsValid())
            {
                // If loading is not already completed
                if (!assetReference.OperationHandle.IsDone)
                {
                    while (!assetReference.OperationHandle.IsDone)
                    {
                        if (assetReference.OperationHandle.PercentComplete != lastPercent)
                        {
                            lastPercent = assetReference.OperationHandle.PercentComplete;
                            progress?.Report(lastPercent);
                        }
                        await UniTask.Delay(1);
                    }
                    if (assetReference.OperationHandle.IsValid() &&
                        assetReference.OperationHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        progress?.Report(1f);
                        return new AssetResult<GameObject>()
                        {
                            Asset = assetReference.OperationHandle.Convert<GameObject>().Result,
                            OperationHandle = assetReference.OperationHandle
                        };
                    }
                    return AssetResult<GameObject>.Error($"Failed to load asset!");
                }
                // If loading is completed
                else
                {
                    progress?.Report(1f);
                    return new AssetResult<GameObject>()
                    {
                        Asset = assetReference.OperationHandle.Convert<GameObject>().Result,
                        OperationHandle = assetReference.OperationHandle
                    };
                }
            }
            else
            {
                var handle = assetReference.InstantiateAsync();
                _ = handle.Task;
                while (!handle.IsDone)
                {
                    if (handle.PercentComplete != lastPercent)
                    {
                        lastPercent = handle.PercentComplete;
                        progress?.Report(lastPercent);
                    }
                    await UniTask.Delay(1);
                }
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    progress?.Report(1f);
                    return new AssetResult<GameObject>()
                    {
                        Asset = handle.Result,
                        OperationHandle = handle
                    };
                }
                return AssetResult<GameObject>.Error($"Failed to load asset!");
            }
        }

        public virtual async UniTask<AssetResult<GameObject>> Instantiate(string key, IProgress<float> progress = null)
        {
            if (!IsAssetExists<GameObject>(key))
            {
                return AssetResult<GameObject>.Error($"Asset with key {key} not exist");
            }
            var handle = Addressables.InstantiateAsync(key);
            _ = handle.Task;
            float lastPercent = 0;
            while (!handle.IsDone)
            {
                if (handle.PercentComplete != lastPercent)
                {
                    lastPercent = handle.PercentComplete;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
            {
                progress?.Report(1f);
                return new AssetResult<GameObject>()
                {
                    Asset = handle.Result,
                    OperationHandle = handle
                };
            }
            return AssetResult<GameObject>.Error($"Failed to instantiate asset!");
        }

        public virtual bool DestroyInstance(GameObject gameObject)
        {
            return Addressables.ReleaseInstance(gameObject);
        }

        public virtual void ClearAssetBundleCache()
        {
            Caching.ClearCache();
            Log.Message(BaseLogChannel.Assets, "AssetBundle cache cleared!");
        }

        public virtual bool IsAssetExists<T>(object key) where T : UnityEngine.Object
        {
            foreach (var l in Addressables.ResourceLocators)
            {
                IList<IResourceLocation> locs;
                if (l.Locate(key, typeof(T), out locs))
                    return true;
            }
            return false;
        }

        public virtual bool IsAssetExistsInAddressables(object key)
        {
            return _assetKeysInAddressables.Contains(key);
        }

        public virtual bool IsAssetExistsInAddressables(AssetReferenceBase assetReference)
        {
            if (assetReference?.AddressableReference == null)
            {
                return false;
            }
            return IsAssetExistsInAddressables(assetReference.AddressableReference.RuntimeKey);
        }

        #endregion

        #region Private Methods

        protected virtual async UniTask<string> RetrievePrimaryKeyForAddressableReference(AssetReference assetReference)
        {
            // TODO: resources has to be earlier than calling this method.
            if (_resourceLocator == null)
            {
                while (_resourceLocator == null)
                {
                    await UniTask.Delay(25);
                }
            }

            if (_resourceLocator.Locate(assetReference.RuntimeKey, null, out var locations))
            {
                return locations[0].PrimaryKey;
            }
            else
            {
                return string.Empty;
            }
        }

        protected virtual async UniTask<DownloadAddressablesStatus[]> IsDownloadDependenciesNeeded(IEnumerable<string> addresesOrLabels)
        {
            List<UniTask<DownloadAddressablesStatus>> tasks = new List<UniTask<DownloadAddressablesStatus>>();
            foreach (var item in addresesOrLabels)
            {
                tasks.Add(IsDownloadDependenciesNeeded(item));
            }

            var result = await UniTask.WhenAll(tasks);
            return result;
        }

        protected virtual async UniTask<DownloadAddressablesStatus[]> IsDownloadDependenciesNeeded(IEnumerable<AssetLabelReference> addresesOrLabels)
        {
            List<UniTask<DownloadAddressablesStatus>> tasks = new List<UniTask<DownloadAddressablesStatus>>();
            foreach (var item in addresesOrLabels)
            {
                tasks.Add(IsDownloadDependenciesNeeded(item.labelString));
            }

            var result = await UniTask.WhenAll(tasks);
            return result;
        }

        protected virtual async UniTask<DownloadAddressablesStatus> IsDownloadDependenciesNeeded(string addressOrLabel)
        {
            var task = Addressables.GetDownloadSizeAsync(addressOrLabel).Task;
            var amountToDownload = await task;
            task.Dispose();
            return new DownloadAddressablesStatus(addressOrLabel, amountToDownload == 0, amountToDownload);
        }

        protected virtual async UniTask<IResourceLocator> LoadCatalog(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            AsyncOperationHandle<IResourceLocator> operation = Addressables.LoadContentCatalogAsync(path);
            IResourceLocator modLocator = await operation.Task;
            return modLocator;
        }

        #endregion
    }
}