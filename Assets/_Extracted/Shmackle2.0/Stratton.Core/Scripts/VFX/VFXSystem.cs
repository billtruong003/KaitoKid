using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratton.Assets;
using Stratton.Core;
using Stratton.Effects;
using Stratton.VFX.Factories;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.VFX
{
    public class VFXSystem : ObjectEmittingSystem<VFXEmitter>
    {
        #region Serialized Fields

        [SerializeField] private VFXLibrary _vfxLibrary;
        [SerializeField] private AssetReferenceT<VFXLibrary> _vfxLibraryAssetReference;

        #endregion

        #region Injected Fields

        private AssetsSystem _assetsSystem;
        private VFXEmitterFactory _vfxEmitterFactory = new();
        private VFXPoolFactory _vfxPoolFactory = new();

        #endregion

        #region Fields

        private AsyncOperationHandle _vfxLibraryHandle;
        private Dictionary<string, VFXPool> _vfxPools = new Dictionary<string, VFXPool>();

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
        }

        public override async UniTask<InitializationResult> Init()
        {
            _assetsSystem = GameSystemsManager.Instance.Get<AssetsSystem>();
            return await base.Init();
        }

        public override async UniTask<DeinitializationResult> DeInit()
        {
            UnloadVFXLibrary();
            return await base.DeInit();
        }

        public virtual async UniTask LoadVFXLibrary()
        {
            var result = await _assetsSystem.LoadAsset<VFXLibrary>(_vfxLibraryAssetReference);
            _vfxLibraryHandle = result.OperationHandle;
            _vfxLibrary = result.Asset;
            CreatePool();
            Log.Message(BaseLogChannel.Assets, "VFX Library loaded");
        }

        public virtual void UnloadVFXLibrary()
        {
            _assetsSystem.ReleaseAsset(_vfxLibraryHandle);
            Log.Message(BaseLogChannel.Assets, "VFX Library unloaded");
        }

        public override VFXEmitter Play(string effectKey, Transform playerTransform)
        {
            if (_vfxLibrary == null)
            {
                if (_assetsSystem.IsAllAddressablesCached)
                {
                    Log.Error(BaseLogChannel.Assets, "VFX Library is not loaded yet!");
                }
                return null;
            }

            if (_vfxLibrary.TryGetVFXData(effectKey, out var data))
            {
                var vfxEmitter = _vfxPools[effectKey].GetVFXEmitter();
                vfxEmitter.SetMainCameraTransform(playerTransform);
                vfxEmitter.ObjectEmitterData = data;
                vfxEmitter.Play();
                return vfxEmitter;
            }

            Log.Error(BaseLogChannel.Assets, $"VFXData - {effectKey} not present in VFXLibrary");
            return null;
        }

        public override VFXEmitter Play(string effectKey, Vector3 position, Transform playerTransform)
        {
            var vfxEmitter = Play(effectKey, playerTransform);
            if (vfxEmitter != null)
            {
                vfxEmitter.SetPosition(position);
            }
            return vfxEmitter;
        }

        public override VFXEmitter Play(string effectKey, Transform parent, Transform playerTransform)
        {
            var vfxEmitter = Play(effectKey, playerTransform);
            if (vfxEmitter != null)
            {
                vfxEmitter.SetParent(parent);
                vfxEmitter.transform.localRotation = Quaternion.identity;
            }
            return vfxEmitter;
        }

        public void PlayOverTime(string effectKey, Transform parent, Transform playerTransform, float time)
        {
            var emitter = Play(effectKey, parent, playerTransform);
            if (emitter != null)
            {
                StartCoroutine(DelayedStopEffect(time, emitter));
            }
        }

        public override void Stop(VFXEmitter emitter)
        {
            emitter.Stop();
        }

        public override void Stop(VFXEmitter emitter, float fadeOutTime)
        {
            emitter.Stop();
        }

        #endregion

        #region Private Methods

        protected virtual void Update()
        {
            foreach (var pool in _vfxPools)
            {
                if (pool.Value != null)
                {
                    pool.Value.OnUpdate();
                }
            }
        }

        protected override void CreatePool()
        {
            if (!_vfxLibrary)
            {
                return;
            }
            foreach (var vfxData in _vfxLibrary.VFXData)
            {
                _vfxPools[vfxData.VFXKey] = _vfxPoolFactory.Create(_vfxEmitterFactory, vfxData.PoolSize, this.transform, vfxData.VFXObject);
                _vfxPools[vfxData.VFXKey].PrefillPool();
            }
        }

        private IEnumerator DelayedStopEffect(float time, VFXEmitter emitter)
        {
            yield return new WaitForSeconds(time);
            emitter.Stop();
        }

        #endregion
    }
}