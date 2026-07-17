using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.Core
{
    public class AssetLabelReferenceComponent<TComponent> : AddressableComponent<AssetLabelReference, TComponent>
    {
        protected override AsyncOperationHandle<TComponent> LoadCachedAssetAsync()
        {
            return Addressables.ResourceManager.CreateChainOperation<TComponent, TComponent>(Addressables.LoadAssetAsync<TComponent>(_assetSource), AssetReady);
        }
    }

}
