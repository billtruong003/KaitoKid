using UnityEngine;
using BillGameCore;
using Mythfall.Events;
using Mythfall.Inventory;
using Mythfall.Player;

namespace Mythfall.Gameplay
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: GameplayScene
     * Path:  [GameplaySpawner] (empty GameObject at world origin, this script attached)
     *        └── (optional) SpawnPoint (child Transform, set Y rotation for initial facing)
     *
     * Serialized fields to assign in Inspector:
     *   - spawnPoint (Transform) — drag the SpawnPoint child if you want a non-origin
     *                               spawn location. Leave null to spawn at Vector3.zero.
     *
     * No other refs required — the spawner pulls everything from
     * InventoryService.Data.currentCharacterId → CharacterDataSO.characterPrefab.
     *
     * ============================================================ */

    /// <summary>
    /// Scene-resident player spawner. On Bill ready, reads
    /// <see cref="InventoryService"/>.Data.currentCharacterId, resolves the
    /// CharacterDataSO, Instantiates its characterPrefab at <see cref="spawnPoint"/>,
    /// and fires <see cref="CharacterSpawnedEvent"/> so HUD / enemies / camera can bind.
    ///
    /// Why Instantiate (not Bill.Pool):
    ///   Player spawn is 1x per scene load. Pool reuse would require PlayerBase
    ///   OnSpawn() re-init logic (HP, stats, iFrame timer, animator, locomotion
    ///   velocity reset) — extra complexity for no churn benefit. Scene reload via
    ///   Bill.Scene.Load destroys the old player; a fresh Instantiate gives clean
    ///   state guaranteed. See Docs/ARCHITECTURE_DECISIONS.md (2026-04-29).
    /// </summary>
    public class GameplaySpawner : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;

        void Start()
        {
            if (Bill.IsReady) Spawn();
            else Bill.Events?.SubscribeOnce<GameReadyEvent>(_ => Spawn());
        }

        void Spawn()
        {
            var inv = ServiceLocator.Get<InventoryService>();
            if (inv == null)
            {
                Debug.LogError("[GameplaySpawner] InventoryService unavailable — cannot resolve current character.");
                return;
            }

            string charId = inv.Data?.currentCharacterId ?? "kai";
            var charData = inv.GetCurrentCharacterData();
            if (charData == null)
            {
                Debug.LogError($"[GameplaySpawner] CharacterDataSO not found for '{charId}'.");
                return;
            }
            if (charData.characterPrefab == null)
            {
                Debug.LogError($"[GameplaySpawner] CharacterDataSO '{charId}' has no characterPrefab assigned. Run Sprint 2 setup or assign in Inspector.");
                return;
            }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            if (spawnPoint == null)
                Debug.LogWarning("[GameplaySpawner] spawnPoint not assigned — using world origin.");

            var go = Object.Instantiate(charData.characterPrefab, pos, rot);
            go.name = charData.characterPrefab.name; // strip "(Clone)" suffix

            var player = go.GetComponent<PlayerBase>();
            if (player == null)
            {
                Debug.LogError($"[GameplaySpawner] Spawned '{charId}' prefab has no PlayerBase component.");
                return;
            }

            Bill.Events.Fire(new CharacterSpawnedEvent
            {
                player = player,
                transform = go.transform,
                characterId = charId,
            });
            Debug.Log($"[GameplaySpawner] Spawned '{charId}' at {pos}.");
        }
    }
}
