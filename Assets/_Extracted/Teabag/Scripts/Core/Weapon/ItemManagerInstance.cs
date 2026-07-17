using System.Collections.Generic;
using Fusion;
using Microsoft.Extensions.Logging;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Random = UnityEngine.Random;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Teabag.Core
{
    public class ItemManagerInstance : NetworkBehaviour, IItemManagerInstance
    {
        private static readonly ILogger _logger = JungleXRLogger.GetLogger();

        [SerializeField] private ItemManagerDataObject itemData;

        public Dictionary<string, ItemsByRarity> ItemDataBase { private set; get; } = new Dictionary<string, ItemsByRarity>();
        public Dictionary<string, GameObject> ObjectDataBase { private set; get; } = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        public override void Spawned()
        {
            if (itemData != null)
                SetData(itemData);
        }

        public void SetData(ItemManagerDataObject data)
        {
            for (int i = 0; i < data.ItemDatas.Length; i++)
            {
                if (ItemDataBase.ContainsKey(data.ItemDatas[i].ID))
                {
                    continue;
                }
                ItemDataBase.Add(data.ItemDatas[i].ID, data.ItemDatas[i].ItemPrefabs);
            }

            for (int i = 0; i < data.ObjectDatas.Length; i++)
            {
                if (ObjectDataBase.ContainsKey(data.ObjectDatas[i].ID))
                {
                    continue;
                }
                ObjectDataBase.Add(data.ObjectDatas[i].ID, data.ObjectDatas[i].ObjectPrefab);
            }
        }

        public NetworkObject SpawnWeapon(string id, Rarity rarity, Vector3 position, Vector3 spawnZoneExtents)
        {
            _logger.LogInformation("Spawning: " + id + " with rarity: " + rarity);

            if (!GameServices.GetRunner())
            {
                _logger.LogError("No game runner found");
                return null;
            }

            if (!ItemDataBase.ContainsKey(id))
            {
                _logger.LogWarning("item database doesnt contain id: " + id);
                return null;
            }

            Vector3 _min = position - (spawnZoneExtents / 2);
            Vector3 _max = position + (spawnZoneExtents / 2);
            Vector3 _final = new Vector3(Random.Range(_min.x, _max.x), Random.Range(_min.y, _max.y), Random.Range(_min.z, _max.z));

            GameObject[] prefabs = GetWeaponData(id, rarity);

            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            int randIdx = Random.Range(0, prefabs.Length);
            var _object = GameServices.GetRunner().Spawn(prefabs[randIdx]);

            SetPosition(_object.gameObject, _final);

            return _object;
        }

        public NetworkObject SpawnObject(string id, Vector3 position, Vector3 spawnZoneExtents)
        {
            if (!GameServices.GetRunner())
            {
                Debug.Log("No game runner found");
                return null;
            }

            if (!ObjectDataBase.ContainsKey(id))
            {
                Debug.Log("object database doesnt contain id: " + id);
                return null;
            }

            Vector3 _min = position - (spawnZoneExtents / 2);
            Vector3 _max = position + (spawnZoneExtents / 2);
            Vector3 _final = new Vector3(Random.Range(_min.x, _max.x), Random.Range(_min.y, _max.y), Random.Range(_min.z, _max.z));

            GameObject ObjectPrefab = GetObjectData(id);

            if (ObjectPrefab == null)
            {
                return null;
            }

            var _object = GameServices.GetRunner().Spawn(ObjectPrefab);

            SetPosition(_object.gameObject, _final);

            return _object;
        }

        public bool SpawnAmmoFromFirearm(string id, int amountToSpawn, Vector3 position, Vector3 radius)
        {
            if (!GameServices.GetRunner())
            {
                return false;
            }

            if (!ItemDataBase.ContainsKey(id))
            {
                return false;
            }

            ItemsByRarity _data = null;
            if (ItemDataBase.TryGetValue(id, out _data))
            {
                GameObject ammo = _data.Ammo;

                if (ammo == null)
                {
                    Debug.Log("No ammo associated with: " + id);
                    return false;
                }

                Vector3 _min = position - (radius / 2);
                Vector3 _max = position + (radius / 2);
                Vector3 _final = new Vector3(Random.Range(_min.x, _max.x), Random.Range(_min.y, _max.y), Random.Range(_min.z, _max.z));

                for (int i = 0; i < amountToSpawn; i++)
                {
                    var _object = GameServices.GetRunner().Spawn(ammo);
                    SetPosition(_object.gameObject, _final);
                }
                return true;
            }
            return false;
        }

        public GameObject[] GetWeaponData(string id, Rarity rarity = Rarity.Common)
        {
            ItemsByRarity _data = null;
            if (ItemDataBase.TryGetValue(id, out _data))
            {
                switch (rarity)
                {
                    case Rarity.Common:
                        return _data.Common;
                    case Rarity.Uncommon:
                        return _data.Uncommon;
                    case Rarity.Rare:
                        return _data.Rare;
                    case Rarity.Epic:
                        return _data.Epic;
                    case Rarity.Legendary:
                        return _data.Legendary;
                }
            }
            return null;
        }

        public GameObject GetAmmoFromType(string id)
        {
            ItemsByRarity _data = null;
            if (ItemDataBase.TryGetValue(id, out _data))
            {
                return _data.Ammo;
            }
            return null;
        }

        public GameObject GetObjectData(string id)
        {
            GameObject _data = null;
            if (ObjectDataBase.TryGetValue(id, out _data))
            {
                return _data;
            }
            Debug.Log("Could not get object data");
            return null;
        }

        private void SetPosition(GameObject targetSpawn, Vector3 position)
        {
            RoyaleNetworkRigidbody _netRB = null;
            Rigidbody _rb = null;
            if (targetSpawn.TryGetComponent(out _netRB))
            {
                _netRB.Rigidbody.angularVelocity = Vector3.zero;
                _netRB.Rigidbody.linearVelocity = Vector3.zero;
                _netRB.Rigidbody.position = position;
                _netRB.Transmit();
                return;
            }

            if (targetSpawn.TryGetComponent(out _rb))
            {
                _rb.angularVelocity = Vector3.zero;
                _rb.linearVelocity = Vector3.zero;
                _rb.position = position;
            }
        }
    }
}
