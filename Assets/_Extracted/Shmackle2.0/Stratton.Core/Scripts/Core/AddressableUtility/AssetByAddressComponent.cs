using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.Core
{
    public class AssetByAddressComponent<TComponent> : AddressableComponent<string, TComponent>
    {
        protected override AsyncOperationHandle<TComponent> LoadCachedAssetAsync()
        {
            return Addressables.ResourceManager.CreateChainOperation<TComponent, TComponent>(Addressables.LoadAssetAsync<TComponent>(_assetSource), AssetReady);
        }
    }

}
