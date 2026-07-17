using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Teabag.Core
{
    public class ItemService : IItemService
    {
        private IItemSettings _itemSettings;
        private GameObject _itemManager;
        private IItemManagerInstance _itemManagerInstance;

        public event Action OnItemsChanged;
        public IReadOnlyList<string> AvailableItemTypes => throw new NotImplementedException();

        public void Initialize()
        {
            _itemSettings = ItemSettingsAsset.InstanceAsset.Settings;
            _itemManager = Object.Instantiate(_itemSettings.ItemManagerPrefab);
            _itemManagerInstance = _itemManager.GetComponent<IItemManagerInstance>();
            _itemManagerInstance.SetData(_itemSettings.ItemManagerDataObject);
            RegisterBridges();
        }

        public void Dispose()
        {
            _itemSettings = null;
            Object.Destroy(_itemManager);
        }

        private void RegisterBridges()
        {
            GameServices.SpawnWeaponData = data =>
            {
                return _itemManagerInstance.SpawnWeapon(data.Item1, data.Item2, data.Item3, data.Item4) != null;
            };

            GameServices.SpawnObjectData = data =>
            {
                return _itemManagerInstance.SpawnObject(data.Item1, data.Item2, data.Item3) != null;
            };

            GameServices.SpawnAmmoForFirearm = data =>
            {
                return _itemManagerInstance.SpawnAmmoFromFirearm(data.Item1, data.Item2, data.Item3, data.Item4);
            };

            GameServices.GetWeaponData = data =>
            {
                return _itemManagerInstance.GetWeaponData(data.Item1, data.Item2);
            };

            GameServices.GetObjectData = data =>
            {
                return _itemManagerInstance.GetObjectData(data);
            };
            GameServices.GetSpawnableIds = () => _itemManagerInstance.ItemDataBase.Keys;

            GameServices.GetAmmoFromType = data =>
            {
                return _itemManagerInstance.GetAmmoFromType(data);
            };
            GameServices.GetSpawnableIds = () => _itemManagerInstance.ItemDataBase.Keys;
        }

        public bool IsItemAvailable(string itemType)
        {
            if (_itemManagerInstance.ItemDataBase == null)
            {
                return false;
            }

            return _itemManagerInstance.ItemDataBase.ContainsKey(itemType);
        }
    }
}
