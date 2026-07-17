using Teabag.Core;
using UnityEngine;

public class SpawnWeaponfromDataManager : MonoBehaviour
{
    [SerializeField] private string[] idItems;
    [SerializeField] private float delayTime = 0;
    [SerializeField] private SpawnObjectType spawnType;

    float _currentTime = 0;
    bool _canSpawn = true;

    private IItemSettings _itemSettings;
    private IItemManagerInstance _itemManagerInstance;
    private GameObject _itemManager;

    private void Start()
    {
        _itemManager = FindObjectOfType<ItemManagerInstance>().gameObject;
        _itemSettings = ItemSettingsAsset.InstanceAsset.Settings;
        _itemManagerInstance = _itemManager.GetComponent<IItemManagerInstance>();
        _itemManagerInstance.SetData(_itemSettings.ItemManagerDataObject);
    }

    private void Update()
    {
        if (_canSpawn || _itemManagerInstance == null)
        {
            return;
        }

        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            return;
        }
        _canSpawn = true;
    }

    public void SpawnItem()
    {
        if (!GameServices.GetRunner())
        {
            return;
        }

        if(!_canSpawn)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up + transform.forward;

        for (int i = 0; i < idItems.Length; i++)
        {
            switch(spawnType)
            {
                case SpawnObjectType.Weapon:
                    {
                        _itemManagerInstance.SpawnWeapon(idItems[i], Rarity.Common, spawnPosition, Vector3.one);
                        _itemManagerInstance.SpawnWeapon(idItems[i], Rarity.Uncommon, spawnPosition, Vector3.one);
                        _itemManagerInstance.SpawnWeapon(idItems[i], Rarity.Rare, spawnPosition, Vector3.one);
                        _itemManagerInstance.SpawnWeapon(idItems[i], Rarity.Epic, spawnPosition, Vector3.one);
                        _itemManagerInstance.SpawnWeapon(idItems[i], Rarity.Legendary, spawnPosition, Vector3.one);
                        break;
                    }
                case SpawnObjectType.Ammo:
                    {
                        _itemManagerInstance.SpawnAmmoFromFirearm(idItems[i], 5, spawnPosition, Vector3.one);
                        break;
                    }
                case SpawnObjectType.Object:
                    {
                        _itemManagerInstance.SpawnObject(idItems[i], spawnPosition, Vector3.one);
                        break;
                    }
            }
        }

        _currentTime = delayTime;
        _canSpawn = false;
    }
}

public enum SpawnObjectType
{
    Weapon,
    Ammo,
    Object
}
