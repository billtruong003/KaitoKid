using System;
using System.Collections.Generic;
using System.IO;
using Stratton.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.Assets
{
    [Serializable]
    public abstract class AssetReferenceBase
    {
        [SerializeField] string _assetPath;

        private static Dictionary<string, Tuple<UnityEngine.Object, bool, AsyncOperationHandle>> _loadedAssetsByPaths = new Dictionary<string, Tuple<UnityEngine.Object, bool, AsyncOperationHandle>>();

        private UnityEngine.Object _loadedAsset;
        private bool _loadedByAddressables;
        private AsyncOperationHandle _loadHandle;

        [JsonIgnore] public abstract Type AssetType { get; }
        public string AssetPath { get => _assetPath; set => _assetPath = value; }

        [JsonIgnore] public abstract AssetReference AddressableReference { get; }

        [JsonIgnore]
        public object RuntimeKey
        {
            get
            {
                if (AssetPath.IsNotNullOrEmpty())
                {
                    return Path.GetFileNameWithoutExtension(AssetPath);
                }
                if (AddressableReference != null && AddressableReference.RuntimeKeyIsValid())
                {
                    return AddressableReference.RuntimeKey;
                }
                return string.Empty;
            }
        }

        [JsonIgnore]
        public string AssetName
        {
            get
            {
                if (AssetPath.IsNotNullOrEmpty())
                {
                    return Path.GetFileNameWithoutExtension(AssetPath);
                }
#if UNITY_EDITOR
                if (AddressableReference != null && AddressableReference.editorAsset != null)
                {
                    return AddressableReference.editorAsset.name;
                }
#endif
                return string.Empty;
            }
        }

        [JsonIgnore]
        public UnityEngine.Object Asset
        {
            get
            {
                if (_loadedAsset != null)
                {
                    return _loadedAsset;
                }
                if (AssetPath.IsNotNullOrEmpty() && _loadedAssetsByPaths.ContainsKey(AssetPath))
                {
                    var assetTuple = _loadedAssetsByPaths[AssetPath];
                    SetLoadedAsset(assetTuple.Item1, assetTuple.Item2, assetTuple.Item3);
                    return assetTuple.Item1;
                }
                if (AddressableReference != null && AddressableReference.RuntimeKeyIsValid())
                {
                    return AddressableReference.Asset;
                }
                return null;
            }
        }

        [JsonIgnore]
        public bool IsDone
        {
            get
            {
                if (_loadedAsset != null)
                {
                    return true;
                }
                if (AddressableReference != null && AddressableReference.RuntimeKeyIsValid())
                {
                    return AddressableReference.IsDone;
                }
                return false;
            }
        }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                return (AddressableReference != null && AddressableReference.RuntimeKeyIsValid()) || AssetPath.IsNotNullOrEmpty();
            }
        }

        public virtual void SetLoadedAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }
            _loadedAsset = asset;
            _loadedByAddressables = false;
            _loadHandle = new AsyncOperationHandle();
            if (AssetPath.IsNotNullOrEmpty())
            {
                _loadedAssetsByPaths[AssetPath] = new Tuple<UnityEngine.Object, bool, AsyncOperationHandle>(asset, false, new AsyncOperationHandle());
            }
        }

        public virtual void SetLoadedAsset(UnityEngine.Object asset, bool loadedByAddressables, AsyncOperationHandle handle)
        {
            if (asset == null)
            {
                return;
            }
            _loadedAsset = asset;
            _loadedByAddressables = loadedByAddressables;
            _loadHandle = handle;
            if (AssetPath.IsNotNullOrEmpty())
            {
                _loadedAssetsByPaths[AssetPath] = new Tuple<UnityEngine.Object, bool, AsyncOperationHandle>(asset, loadedByAddressables, handle);
            }
        }

        public virtual void ReleaseAsset()
        {
            if (AddressableReference != null && AddressableReference.Asset != null)
            {
                AddressableReference?.ReleaseAsset();
            }
            if (_loadedAsset != null)
            {
                if (_loadedByAddressables)
                {
                    if (_loadHandle.IsValid())
                    {
                        Addressables.Release(_loadHandle);
                    }
                }
                else
                {
                    UnityEngine.Object.Destroy(_loadedAsset);
                }
                _loadedAsset = null;
            }
            if (AssetPath.IsNotNullOrEmpty() && _loadedAssetsByPaths.ContainsKey(AssetPath))
            {
                _loadedAssetsByPaths.Remove(AssetPath);
            }
        }

        public override int GetHashCode()
        {
            var hash = 0;

            if (AssetPath != null)
            {
                hash = HashUtils.ConcatHash(hash, HashUtils.GetHash(AssetPath));
            }

            return hash;
        }
    }
}