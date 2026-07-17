using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
    [Serializable]
    public class GameObjectAssetReference : AssetReferenceBase
    {
        [SerializeField] AssetReferenceGameObject _addressableReference;

        [JsonIgnore] public override Type AssetType => typeof(GameObject);
        [JsonIgnore] public override AssetReference AddressableReference => _addressableReference;
        [JsonIgnore] public AssetReferenceGameObject GameObjectAddressableReference { get { return _addressableReference; } set { _addressableReference = value; } }
    }
}