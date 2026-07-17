#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using UnityEngine;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.NPC
{
    /// <summary>
    /// Manages spawning and tracking of loot drop GameObjects in the game world.
    /// Listens to the loot_drop table for insert/delete events.
    /// </summary>
    public class LootSpawner : MonoBehaviour
    {
        public static LootSpawner Instance { get; private set; }

        private readonly Dictionary<uint, GameObject> _lootDrops = new Dictionary<uint, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable() { RegisterCallbacks(); }
        private void OnDisable() { UnregisterCallbacks(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void RegisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.LootDrop.OnInsert += OnLootInsert;
            gm.Connection.Db.LootDrop.OnDelete += OnLootDelete;

            // Spawn existing loot
            if (gm.SubscriptionReady)
            {
                foreach (var loot in gm.Connection.Db.LootDrop.Iter())
                {
                    if (!_lootDrops.ContainsKey(loot.DropId))
                        SpawnLoot(loot);
                }
            }
        }

        private void UnregisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.LootDrop.OnInsert -= OnLootInsert;
            gm.Connection.Db.LootDrop.OnDelete -= OnLootDelete;
        }

        private void SpawnLoot(SpacetimeDB.Types.LootDrop lootData)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.LootDropPrefab == null) return;

            Vector3 pos = new Vector3(lootData.PosX, lootData.PosY, 0f);
            GameObject obj = Instantiate(gm.LootDropPrefab, pos, Quaternion.identity);
            obj.name = $"[Loot:{lootData.DropId}]";

            // Look up item definition for name, rarity, and sprite info
            string itemName = "Unknown Item";
            int rarity = 0;
            string spritePath = "";
            if (gm.Connection != null)
            {
                var itemDef = gm.Connection.Db.ItemDef.ItemId.Find(lootData.ItemId);
                if (itemDef != null)
                {
                    itemName = itemDef.Name;
                    rarity = itemDef.Rarity;
                    // SpriteIndex is an int; convert to a resource path if needed
                    spritePath = itemDef.SpriteIndex > 0 ? $"Items/Item_{itemDef.SpriteIndex}" : "";
                }
            }

            var lootDrop = obj.GetComponent<NPC.LootDrop>();
            if (lootDrop != null)
            {
                lootDrop.Initialize(lootData.DropId, lootData.ItemId, itemName, rarity, spritePath);
            }

            _lootDrops[lootData.DropId] = obj;

            if (Bill.IsReady)
            {
                Bill.Events.Fire(new LootDropSpawnedEvent { LootObject = obj });
            }
        }

        private void OnLootInsert(EventContext ctx, SpacetimeDB.Types.LootDrop row)
        {
            if (!_lootDrops.ContainsKey(row.DropId))
                SpawnLoot(row);
        }

        private void OnLootDelete(EventContext ctx, SpacetimeDB.Types.LootDrop row)
        {
            if (_lootDrops.TryGetValue(row.DropId, out var obj))
            {
                if (obj != null) Destroy(obj);
                _lootDrops.Remove(row.DropId);
            }
        }

        public int ActiveLootCount => _lootDrops.Count;
    }
}

#endif // STDB_BINDINGS
