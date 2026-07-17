using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
    [Serializable]
    public class MeshAssetReference : AssetReferenceBase
    {
        [SerializeField] AssetReferenceT<Mesh> _addressableReference;

        [JsonIgnore] public override Type AssetType => typeof(Mesh);
        [JsonIgnore] public override AssetReference AddressableReference => _addressableReference;
        [JsonIgnore] public AssetReferenceT<Mesh> MeshAddressableReference { get { return _addressableReference; } set { _addressableReference = value; } }
    }
}