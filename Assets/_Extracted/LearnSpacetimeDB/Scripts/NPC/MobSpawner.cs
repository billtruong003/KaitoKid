#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using UnityEngine;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.NPC
{
    /// <summary>
    /// Manages spawning and tracking of mob GameObjects in the game world.
    /// Listens to the mob_instance table for insert/update/delete events.
    /// </summary>
    public class MobSpawner : MonoBehaviour
    {
        // -------------------------------------------------------
        // Singleton
        // -------------------------------------------------------

        public static MobSpawner Instance { get; private set; }

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private readonly Dictionary<uint, GameObject> _mobs = new Dictionary<uint, GameObject>();

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // -------------------------------------------------------
        // Callbacks
        // -------------------------------------------------------

        private void RegisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.MobInstance.OnInsert += OnMobInsert;
            gm.Connection.Db.MobInstance.OnUpdate += OnMobUpdate;
            gm.Connection.Db.MobInstance.OnDelete += OnMobDelete;

            // Spawn existing mobs
            SpawnExistingMobs();
        }

        private void UnregisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.MobInstance.OnInsert -= OnMobInsert;
            gm.Connection.Db.MobInstance.OnUpdate -= OnMobUpdate;
            gm.Connection.Db.MobInstance.OnDelete -= OnMobDelete;
        }

        private void SpawnExistingMobs()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.SubscriptionReady || gm.Connection == null) return;

            foreach (var mob in gm.Connection.Db.MobInstance.Iter())
            {
                if (!_mobs.ContainsKey(mob.MobId))
                {
                    SpawnMob(mob);
                }
            }
        }

        // -------------------------------------------------------
        // Spawn / Destroy
        // -------------------------------------------------------

        private void SpawnMob(MobInstance mobData)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.MobPrefab == null) return;

            Vector3 pos = new Vector3(mobData.PosX, mobData.PosY, 0f);
            GameObject mobObj = Instantiate(gm.MobPrefab, pos, Quaternion.identity);
            mobObj.name = $"[Mob:{mobData.MobDefId}:{mobData.MobId}]";

            var controller = mobObj.GetComponent<MobController>();
            if (controller != null)
            {
                controller.Initialize(mobData);
            }

            _mobs[mobData.MobId] = mobObj;
        }

        private void DestroyMob(uint mobId)
        {
            if (_mobs.TryGetValue(mobId, out var mobObj))
            {
                if (mobObj != null)
                {
                    Destroy(mobObj);
                }
                _mobs.Remove(mobId);
            }
        }

        // -------------------------------------------------------
        // Table Callbacks
        // -------------------------------------------------------

        private void OnMobInsert(EventContext ctx, MobInstance row)
        {
            if (!_mobs.ContainsKey(row.MobId))
            {
                SpawnMob(row);
            }
        }

        private void OnMobUpdate(EventContext ctx, MobInstance oldRow, MobInstance newRow)
        {
            if (_mobs.TryGetValue(newRow.MobId, out var mobObj) && mobObj != null)
            {
                var controller = mobObj.GetComponent<MobController>();
                if (controller != null)
                {
                    controller.OnServerUpdate(newRow);
                }
            }
        }

        private void OnMobDelete(EventContext ctx, MobInstance row)
        {
            DestroyMob(row.MobId);
        }

        // -------------------------------------------------------
        // Public
        // -------------------------------------------------------

        public GameObject GetMobObject(uint mobId)
        {
            _mobs.TryGetValue(mobId, out var obj);
            return obj;
        }

        public int ActiveMobCount => _mobs.Count;
    }
}

#endif // STDB_BINDINGS
