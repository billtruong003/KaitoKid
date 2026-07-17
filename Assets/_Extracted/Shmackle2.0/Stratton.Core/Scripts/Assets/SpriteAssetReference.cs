using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
    [Serializable]
    public class SpriteAssetReference : AssetReferenceBase
    {
        [SerializeField] AssetReferenceSprite _addressableReference;

        [JsonIgnore] public override Type AssetType => typeof(Sprite);
        [JsonIgnore] public override AssetReference AddressableReference => _addressableReference;
        [JsonIgnore] public AssetReferenceSprite SpriteAddressableReference { get { return _addressableReference; } set { _addressableReference = value; } }
    }
}