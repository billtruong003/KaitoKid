using Stratton.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.Core
{
    public class AssetReferenceComponent<TComponent> : AddressableComponent<AssetReference,TComponent>
    {
        protected override AsyncOperationHandle<TComponent> LoadCachedAssetAsync()
        {
            return Addressables.ResourceManager.CreateChainOperation<TComponent, TComponent>(_assetSource.LoadAssetAsync<TComponent>(), AssetReady);
        }
    }
}
