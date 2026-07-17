using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
    [Serializable]
    public class TextAssetReference : AssetReferenceBase
    {
        [SerializeField] AssetReferenceT<TextAsset> _addressableReference;

        [JsonIgnore] public override Type AssetType => typeof(TextAsset);
        [JsonIgnore] public override AssetReference AddressableReference => _addressableReference;
        [JsonIgnore] public AssetReferenceT<TextAsset> TextAssetAddressableReference { get { return _addressableReference; } set { _addressableReference = value; } }
    }
}