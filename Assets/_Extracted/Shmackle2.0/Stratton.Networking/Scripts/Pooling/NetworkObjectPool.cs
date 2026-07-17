using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Networking
{
    public interface INetworkObjectPool : INetworkObjectProvider
    {
        bool TryGetNetworkObjectPrefab(NetworkObjectType networkObjectType, out NetworkObjectGuid networkObjectGuid);
        NetworkObjectLibrary NetworkObjectLibrary();
    }

    public class NetworkObjectPool : NetworkObjectProviderDefault, INetworkObjectPool
    {
        private class NetworkObjectDataEntry
        {
            public int Spawned;
            public NetworkObjectData NetworkObjectData;
        }

        #region Fields

        private NetworkObjectLibrary _networkObjectLibrary;
        private readonly Dictionary<NetworkPrefabId, NetworkObjectDataEntry> _library = new Dictionary<NetworkPrefabId, NetworkObjectDataEntry>();
        private readonly Dictionary<NetworkPrefabId, Stack<NetworkObject>> _cached = new Dictionary<NetworkPrefabId, Stack<NetworkObject>>(32);
        private readonly Dictionary<NetworkObject, NetworkPrefabId> _pooled = new Dictionary<NetworkObject, NetworkPrefabId>();
        private bool _isInitialized = false;

        #endregion

        #region Public Methods

        public void Init(NetworkObjectLibrary networkObjectLibrary)
        {
            _networkObjectLibrary = networkObjectLibrary;
        }

        #endregion

        #region INetworkObjectProvider Methods

        public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject instance)
        {
            instance = null;

            if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy)
            {
                return NetworkObjectAcquireResult.Retry;
            }

            if (!_isInitialized)
            {
                Initialize(runner);
            }

            if (_isInitialized)
            {
                if (_library.TryGetValue(context.PrefabId, out var networkObjectDataEntry))
                {
                    if (networkObjectDataEntry.NetworkObjectData.Poolable)
                    {
                        if (!_cached.TryGetValue(context.PrefabId, out var objects))
                        {
                            objects = _cached[context.PrefabId] = new Stack<NetworkObject>();
                        }

                        if (objects.Count > 0)
                        {
                            var oldInstance = objects.Pop();
                            _pooled[oldInstance] = context.PrefabId;

                            oldInstance.gameObject.SetActive(true);

                            instance = oldInstance;
                            runner.Prefabs.AddInstance(context.PrefabId);
                            return NetworkObjectAcquireResult.Success;
                        }

                        if (networkObjectDataEntry.NetworkObjectData.MaxInstances > 0)
                        {
                            if (networkObjectDataEntry.Spawned >= networkObjectDataEntry.NetworkObjectData.MaxInstances)
                            {
                                Core.Log.Error(NetworkingLogChannel.ObjectPool, $"Max instances of {networkObjectDataEntry.NetworkObjectData.NetworkObjectType.Name} limit exceeded!");
                                instance = null;
                                return NetworkObjectAcquireResult.Failed;
                            }
                            networkObjectDataEntry.Spawned++;
                        }

                        NetworkObject prefab;
                        try
                        {
                            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to load prefab: {ex}");
                            return NetworkObjectAcquireResult.Failed;
                        }

                        if (!prefab)
                        {
                            // this is ok, as long as Fusion does not require the prefab to be loaded immediately;
                            // if an instance for this prefab is still needed, this method will be called again next update
                            return NetworkObjectAcquireResult.Retry;
                        }

                        var newInstance = InstantiatePrefab(runner, prefab);
                        _pooled[newInstance] = context.PrefabId;
                        instance = newInstance;
                        Assert.Check(instance);
                        if (context.DontDestroyOnLoad)
                        {
                            runner.MakeDontDestroyOnLoad(instance.gameObject);
                        }
                        else
                        {
                            runner.MoveToRunnerScene(instance.gameObject);
                        }
                        runner.Prefabs.AddInstance(context.PrefabId);
                        return NetworkObjectAcquireResult.Success;
                    }
                }
                else
                {
                    Core.Log.Warning(NetworkingLogChannel.ObjectPool, $"Spawned NetworkObject {context.PrefabId} not found in NetworkObjectLibrary - add prefab to library for consistency");
                }
            }

            return base.AcquirePrefabInstance(runner, in context, out instance);
        }

        public override void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
        {
            var instance = context.Object;

            if (!context.IsBeingDestroyed)
            {
                if (context.TypeId.IsPrefab)
                {
                    if (_pooled.TryGetValue(instance, out var prefabId))
                    {
                        _pooled.Remove(instance);
                        AddInstanceToCache(prefabId, instance);
                    }
                    else
                    {
                        DestroyPrefabInstance(runner, context.TypeId.AsPrefabId, instance);
                    }
                }
                else if (context.TypeId.IsSceneObject)
                {
                    DestroySceneObject(runner, context.TypeId.AsSceneObjectId, instance);
                }
                else if (context.IsNestedObject)
                {
                    DestroyPrefabNestedObject(runner, instance);
                }
                else
                {
                    throw new NotImplementedException($"Unknown type id {context.TypeId}");
                }
            }

            if (context.TypeId.IsPrefab)
            {
                runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
            }
        }

        #endregion

        #region INetworkObjectPool Methods

        public NetworkObjectLibrary NetworkObjectLibrary() => _networkObjectLibrary;

        public bool TryGetNetworkObjectPrefab(NetworkObjectType networkObjectType, out NetworkObjectGuid networkObjectGuid)
        {
            return _networkObjectLibrary.TryGetNetworkObjectPrefab(networkObjectType, out networkObjectGuid);
        }

        #endregion

        #region Private Methods

        private void Initialize(NetworkRunner runner)
        {
            Dictionary<NetworkObjectGuid, NetworkPrefabId> prefabIdsInTableByNetworkTypeId = new();
            foreach (var prefabSource in NetworkProjectConfig.Global.PrefabTable.Prefabs)
            {
                var source = prefabSource as NetworkPrefabSourceResource;
                var prefabId = NetworkProjectConfig.Global.PrefabTable.GetId(prefabSource.AssetGuid);
                prefabIdsInTableByNetworkTypeId.Add(prefabSource.AssetGuid, prefabId);
            }
            var library = _networkObjectLibrary.NetworkObjectsData;
            if (library.Count > 0)
            {
                foreach (var item in library)
                {
                    var networkObjectGuid = new NetworkObjectGuid(item.NetworkObjectGuid);
                    if (!prefabIdsInTableByNetworkTypeId.ContainsKey(networkObjectGuid))
                    {
                        Core.Log.Warning(NetworkingLogChannel.ObjectPool, $"Prefab not in table: {item.NetworkObjectGuid}");
                        continue;
                    }

                    var prefabId = prefabIdsInTableByNetworkTypeId[networkObjectGuid];
                    _library.Add(prefabId, new NetworkObjectDataEntry
                    {
                        Spawned = 0,
                        NetworkObjectData = item
                    });

                    if (item.PrewarmedInstances <= 0) continue;

                    var prefab = runner.Prefabs.Load(prefabId, isSynchronous: true);
                    if (!prefab)
                    {
                        Core.Log.Warning(NetworkingLogChannel.ObjectPool, $"Could not load prefab for {prefabId}");
                        continue;
                    }

                    if (!_cached.ContainsKey(prefabId))
                        _cached[prefabId] = new Stack<NetworkObject>();

                    for (int i = 0; i < item.PrewarmedInstances; i++)
                    {
                        var instance = InstantiatePrefab(runner, prefab);
                        AddInstanceToCache(prefabId, instance);
                    }
                }
                _isInitialized = true;
            }
            else
            {
                Core.Log.Warning(NetworkingLogChannel.ObjectPool, $"NetworkObjectLibrary is empty - NetworkObjectPool can't be initialized");
            }
        }

        private void AddInstanceToCache(NetworkPrefabId prefabId, NetworkObject instance)
        {
            _cached[prefabId].Push(instance);
            instance.gameObject.SetActive(false);
            instance.transform.parent = null;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        #endregion
    }
}