using System.Collections.Generic;
using UnityEngine;
using BillGameCore;
using Mythfall.Characters;
using Mythfall.Localization;

namespace Mythfall.Inventory
{
    /// <summary>
    /// Owns the persisted PlayerData blob. Loads on Initialize, persists every mutation.
    ///
    /// Registered in GameBootstrap alongside LocalizationService.
    /// Bill.Save (JsonUtility under PlayerPrefs) backs persistence — first launch
    /// returns null and we seed defaults; subsequent launches restore the last save.
    /// PlayerData is the canonical source of truth for language; LocalizationService's
    /// own PlayerPrefs is reconciled at Initialize.
    /// </summary>
    public class InventoryService : IService, IInitializable
    {
        const string SaveKey = "player";
        const string CharacterDataPathPrefix = "Characters/";

        PlayerData _data;
        readonly Dictionary<string, CharacterDataSO> _charDataCache = new(4);

        public PlayerData Data => _data;

        public void Initialize()
        {
            _data = Bill.Save.Get<PlayerData>(SaveKey);

            if (_data == null)
            {
                _data = new PlayerData();
                Persist();
                Debug.Log($"[InventoryService] First-launch save — character={_data.currentCharacterId}, lang={_data.preferredLanguage}");
            }
            else
            {
                if (_data.schemaVersion != PlayerData.CURRENT_VERSION)
                    MigrateData(_data);
                Debug.Log($"[InventoryService] Loaded save — character={_data.currentCharacterId}, lang={_data.preferredLanguage}, owned={_data.ownedCharacterIds?.Count ?? 0}");
            }

            // Defensive — guard against malformed save reaching live game code.
            if (_data.ownedCharacterIds == null || _data.ownedCharacterIds.Count == 0)
            {
                Debug.LogWarning("[InventoryService] ownedCharacterIds empty — restoring defaults.");
                _data.ownedCharacterIds = new List<string> { "kai", "lyra" };
                Persist();
            }
            if (!_data.ownedCharacterIds.Contains(_data.currentCharacterId))
            {
                Debug.LogWarning($"[InventoryService] currentCharacterId '{_data.currentCharacterId}' not in owned list — resetting to '{_data.ownedCharacterIds[0]}'.");
                _data.currentCharacterId = _data.ownedCharacterIds[0];
                Persist();
            }

            // Language reconciliation: PlayerData wins over LocalizationService's own PlayerPref.
            var loc = ServiceLocator.Get<LocalizationService>();
            if (loc != null && loc.CurrentLanguage != _data.preferredLanguage)
                loc.SetLanguage(_data.preferredLanguage);
        }

        public void SetCurrentCharacter(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[InventoryService] SetCurrentCharacter called with null/empty id.");
                return;
            }
            if (!_data.ownedCharacterIds.Contains(id))
            {
                Debug.LogError($"[InventoryService] '{id}' not in ownedCharacterIds — selection rejected.");
                return;
            }
            if (_data.currentCharacterId == id) return;

            _data.currentCharacterId = id;
            Persist();
        }

        public void SetPreferredLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return;
            if (_data.preferredLanguage == langCode) return;

            _data.preferredLanguage = langCode;
            Persist();

            var loc = ServiceLocator.Get<LocalizationService>();
            loc?.SetLanguage(langCode);
        }

        /// <summary>Resolve current character's CharacterDataSO from Resources. Null if missing.</summary>
        public CharacterDataSO GetCurrentCharacterData() => GetCharacterData(_data.currentCharacterId);

        public CharacterDataSO GetCharacterData(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_charDataCache.TryGetValue(id, out var cached)) return cached;

            // Convention: Resources/Characters/{Id}_Data, where {Id} is capitalized first letter.
            // currentCharacterId="kai" → "Characters/Kai_Data"
            string capitalized = char.ToUpperInvariant(id[0]) + id.Substring(1);
            string path = $"{CharacterDataPathPrefix}{capitalized}_Data";

            var so = Resources.Load<CharacterDataSO>(path);
            if (so == null)
                Debug.LogError($"[InventoryService] CharacterDataSO not found at Resources/{path}.asset");
            else
                _charDataCache[id] = so;

            return so;
        }

        // ----- internal -----

        void Persist()
        {
            Bill.Save.Set(SaveKey, _data);
            Bill.Save.Flush();
        }

        /// <summary>
        /// Forward-compatible save migration. Called when loaded blob's schemaVersion
        /// != PlayerData.CURRENT_VERSION. Day 3 (v1) has nothing to migrate — keeping
        /// the call site locks in the pattern for Sprint 5+ schema bumps.
        /// </summary>
        void MigrateData(PlayerData old)
        {
            Debug.Log($"[InventoryService] Migrating save schema {old.schemaVersion} → {PlayerData.CURRENT_VERSION}");
            // Sprint 5+: populate new fields from old fields when schema bumps.
            old.schemaVersion = PlayerData.CURRENT_VERSION;
            Persist();
        }
    }
}
