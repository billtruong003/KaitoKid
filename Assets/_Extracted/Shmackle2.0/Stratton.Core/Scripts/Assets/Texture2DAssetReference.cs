using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
    [Serializable]
    public class Texture2DAssetReference : AssetReferenceBase
    {
        [SerializeField] AssetReferenceTexture2D _addressableReference;

        [JsonIgnore] public override Type AssetType => typeof(Texture2D);
        [JsonIgnore] public override AssetReference AddressableReference => _addressableReference;
        [JsonIgnore] public AssetReferenceTexture2D TextureAddressableReference { get { return _addressableReference; } set { _addressableReference = value; } }
    }
}