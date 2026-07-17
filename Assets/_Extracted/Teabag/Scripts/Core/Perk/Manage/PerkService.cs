using System;
using System.Collections.Generic;
using System.Text.Json;
using Squido.JungleXRKit.Core;
using UnityEngine;

namespace Teabag.Core
{
    public class PerkService : IPerkService
    {
        private readonly IDataPersistenceService _persistence;
        private IPerkSettings _perkSettings;

        public event Action OnItemsChanged;

        Dictionary<string, BasePerkDataObject> _perkDataBase = new Dictionary<string, BasePerkDataObject>();

        public PerkService(IDataPersistenceService persistence)
        {
            _persistence = persistence;
        }

        public void Initialize()
        {
            _perkSettings = PerkSettingsAsset.InstanceAsset.Settings;
            SetDataBase();
        }

        public void Dispose()
        { }

        public BasePerkDataObject GetPerkDataObject(string id)
        {
            if (_perkDataBase.ContainsKey(id))
            {
                return _perkDataBase[id];
            }
            else
            {
                return null;
            }
        }

        private void SetDataBase()
        {
            for (int i = 0; i < _perkSettings.PerkDataBase.Length; i++)
            {
                if (_perkDataBase.ContainsKey(_perkSettings.PerkDataBase[i].ID))
                {
                    continue;
                }
                _perkDataBase.Add(_perkSettings.PerkDataBase[i].ID, _perkSettings.PerkDataBase[i]);
            }
        }

        public List<BasePerkDataObject> GetAllEquipPerks()
        {
            List<BasePerkDataObject> perks = new List<BasePerkDataObject>();

            foreach (var perk in PlayerData.perkEquip)
            {
                perks.Add(GetPerkDataObject(perk));
            }
            return perks;
        }

        public void SavePerk()
        {
            string jsonData = JsonSerializer.Serialize(PlayerData.perks);
            _persistence.TrySaveData("PerkUnlock", jsonData);
            Debug.Log(">> Save Perk Unlock: " + jsonData);
        }

        public void SavePerkEquipped()
        {
            string jsonData = JsonSerializer.Serialize(PlayerData.perkEquip);
            _persistence.TrySaveData("PerkEquipped", jsonData);
            Debug.Log(">> Save Perk Equipped: " + jsonData);
        }

        public void LoadPerk()
        {
            var json = _persistence.LoadData<string>("PerkUnlock", "");
            var data = string.IsNullOrEmpty(json) ? new Dictionary<string, int>() : JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            PlayerData.perks = data;
            Debug.Log(">> Load Perk Unlock: " + data.Count);
        }

        public void LoadPerkEquipped()
        {
            var json = _persistence.LoadData<string>("PerkEquipped", "");
            var data = string.IsNullOrEmpty(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            PlayerData.perkEquip = data;
            Debug.Log(">> Load Perk Equipped: " + data.Count);
        }
    }
}
