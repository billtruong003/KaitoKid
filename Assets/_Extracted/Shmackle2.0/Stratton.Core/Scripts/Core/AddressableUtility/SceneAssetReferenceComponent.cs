using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Stratton.Core
{
    public class SceneAssetReferenceComponent : AssetReferenceComponent<SceneInstance>
    {
        [SerializeField]
        private LoadSceneMode _loadSceneMode;
        [SerializeField]
        private bool _activateOnLoad = true;
        [SerializeField]
        private int _priority = 100;
        protected override AsyncOperationHandle<SceneInstance> LoadCachedAssetAsync()
        {
            return Addressables.LoadSceneAsync(_assetSource, _loadSceneMode, _activateOnLoad, _priority);
        }

        protected override void ReleaseAsset()
        {
            Addressables.UnloadSceneAsync(_asyncOperationHandler).Completed += SceneAssetReferenceComponent_Completed;
        }

        private void SceneAssetReferenceComponent_Completed(AsyncOperationHandle<SceneInstance> obj)
        {
            Debug.Log($"Scene unloaded");
        }
    }

}
