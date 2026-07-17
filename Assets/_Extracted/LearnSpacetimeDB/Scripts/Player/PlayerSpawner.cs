#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using UnityEngine;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline
{
    /// <summary>
    /// Manages the spawning and lifecycle of all player GameObjects in the game world.
    /// Listens to the player_position table for insert/update/delete events and
    /// instantiates the appropriate prefab (local vs remote). Maintains a dictionary
    /// mapping Identity to GameObject for efficient lookups.
    /// </summary>
    public class PlayerSpawner : MonoBehaviour
    {
        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        /// <summary>Maps player Identity to their spawned GameObject.</summary>
        private readonly Dictionary<Identity, GameObject> _players = new Dictionary<Identity, GameObject>();

        /// <summary>Reference to the local player GameObject (also in dictionary).</summary>
        public GameObject LocalPlayerObject { get; private set; }

        public static PlayerSpawner Instance { get; private set; }

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
        // Callback Registration
        // -------------------------------------------------------

        private void RegisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.PlayerPosition.OnInsert += OnPlayerPositionInsert;
            gm.Connection.Db.PlayerPosition.OnUpdate += OnPlayerPositionUpdate;
            gm.Connection.Db.PlayerPosition.OnDelete += OnPlayerPositionDelete;

            // Also listen for stats updates to keep HP/MP in sync
            gm.Connection.Db.PlayerStats.OnUpdate += OnPlayerStatsUpdate;

            // Listen for appearance updates
            gm.Connection.Db.PlayerAppearance.OnInsert += OnPlayerAppearanceInsert;
            gm.Connection.Db.PlayerAppearance.OnUpdate += OnPlayerAppearanceUpdate;

            // Spawn all players already in the table (in case we loaded into an active world)
            SpawnExistingPlayers();
        }

        private void UnregisterCallbacks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            gm.Connection.Db.PlayerPosition.OnInsert -= OnPlayerPositionInsert;
            gm.Connection.Db.PlayerPosition.OnUpdate -= OnPlayerPositionUpdate;
            gm.Connection.Db.PlayerPosition.OnDelete -= OnPlayerPositionDelete;
            gm.Connection.Db.PlayerStats.OnUpdate -= OnPlayerStatsUpdate;
            gm.Connection.Db.PlayerAppearance.OnInsert -= OnPlayerAppearanceInsert;
            gm.Connection.Db.PlayerAppearance.OnUpdate -= OnPlayerAppearanceUpdate;
        }

        /// <summary>
        /// On scene load, spawn any players that are already in the subscription cache.
        /// </summary>
        private void SpawnExistingPlayers()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.SubscriptionReady || gm.Connection == null) return;

            foreach (var pos in gm.Connection.Db.PlayerPosition.Iter())
            {
                if (!_players.ContainsKey(pos.Owner))
                {
                    SpawnPlayer(pos);
                }
            }
        }

        // -------------------------------------------------------
        // Spawn / Destroy
        // -------------------------------------------------------

        private void SpawnPlayer(PlayerPosition pos)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool isLocal = pos.Owner == gm.LocalIdentity;
            GameObject prefab = isLocal ? gm.LocalPlayerPrefab : gm.RemotePlayerPrefab;

            if (prefab == null)
            {
                Debug.LogError($"[PlayerSpawner] {(isLocal ? "Local" : "Remote")} player prefab is null!");
                return;
            }

            Vector3 spawnPos = new Vector3(pos.PosX, pos.PosY, 0f);
            GameObject playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            playerObj.name = isLocal ? "[LocalPlayer]" : $"[RemotePlayer:{pos.Owner}]";

            _players[pos.Owner] = playerObj;

            if (isLocal)
            {
                LocalPlayerObject = playerObj;

                // Initialize the local player controller
                var localCtrl = playerObj.GetComponent<LocalPlayerController>();
                if (localCtrl != null)
                {
                    localCtrl.OnServerPositionUpdate(pos.PosX, pos.PosY, pos.FacingRight, pos.AnimState);
                }

                Debug.Log("[PlayerSpawner] Local player spawned.");
            }
            else
            {
                // Initialize the remote player controller
                var remoteCtrl = playerObj.GetComponent<RemotePlayerController>();
                if (remoteCtrl != null)
                {
                    string playerName = GetPlayerName(pos.Owner);
                    remoteCtrl.Initialize(pos.Owner, playerName, pos.PosX, pos.PosY, pos.FacingRight, pos.AnimState);
                }

                Debug.Log($"[PlayerSpawner] Remote player spawned: {pos.Owner}");
            }

            // Apply appearance if available
            ApplyAppearanceToPlayer(pos.Owner, playerObj);
        }

        private void DestroyPlayer(Identity identity)
        {
            if (_players.TryGetValue(identity, out var playerObj))
            {
                if (playerObj != null)
                {
                    Destroy(playerObj);
                }

                _players.Remove(identity);

                var gm = GameManager.Instance;
                if (gm != null && identity == gm.LocalIdentity)
                {
                    LocalPlayerObject = null;
                }

                Debug.Log($"[PlayerSpawner] Player removed: {identity}");
            }
        }

        // -------------------------------------------------------
        // Table Callbacks
        // -------------------------------------------------------

        private void OnPlayerPositionInsert(EventContext ctx, PlayerPosition row)
        {
            if (!_players.ContainsKey(row.Owner))
            {
                SpawnPlayer(row);
            }
        }

        private void OnPlayerPositionUpdate(EventContext ctx, PlayerPosition oldRow, PlayerPosition newRow)
        {
            if (!_players.TryGetValue(newRow.Owner, out var playerObj)) return;
            if (playerObj == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            if (newRow.Owner == gm.LocalIdentity)
            {
                // Local player: reconcile
                var localCtrl = playerObj.GetComponent<LocalPlayerController>();
                if (localCtrl != null)
                {
                    localCtrl.OnServerPositionUpdate(newRow.PosX, newRow.PosY, newRow.FacingRight, newRow.AnimState);
                }
            }
            else
            {
                // Remote player: update target
                var remoteCtrl = playerObj.GetComponent<RemotePlayerController>();
                if (remoteCtrl != null)
                {
                    remoteCtrl.OnServerPositionUpdate(newRow.PosX, newRow.PosY, newRow.FacingRight, newRow.AnimState);
                }
            }
        }

        private void OnPlayerPositionDelete(EventContext ctx, PlayerPosition row)
        {
            DestroyPlayer(row.Owner);
        }

        private void OnPlayerStatsUpdate(EventContext ctx, PlayerStats oldRow, PlayerStats newRow)
        {
            if (!_players.TryGetValue(newRow.Owner, out var playerObj)) return;
            if (playerObj == null) return;

            // Update remote player stats for targeting UI
            var remoteCtrl = playerObj.GetComponent<RemotePlayerController>();
            if (remoteCtrl != null)
            {
                remoteCtrl.UpdateStats(newRow.CurrentHp, newRow.MaxHp);
            }
        }

        private void OnPlayerAppearanceInsert(EventContext ctx, PlayerAppearance row)
        {
            if (_players.TryGetValue(row.Owner, out var playerObj) && playerObj != null)
            {
                var visualSync = playerObj.GetComponent<CharacterVisualSync>();
                if (visualSync != null)
                {
                    visualSync.ApplyAppearance(row);
                }
            }
        }

        private void OnPlayerAppearanceUpdate(EventContext ctx, PlayerAppearance oldRow, PlayerAppearance newRow)
        {
            if (_players.TryGetValue(newRow.Owner, out var playerObj) && playerObj != null)
            {
                var visualSync = playerObj.GetComponent<CharacterVisualSync>();
                if (visualSync != null)
                {
                    visualSync.ApplyAppearance(newRow);
                }
            }
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private void ApplyAppearanceToPlayer(Identity identity, GameObject playerObj)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            var appearance = gm.Connection.Db.PlayerAppearance.Owner.Find(identity);
            if (appearance != null)
            {
                var visualSync = playerObj.GetComponent<CharacterVisualSync>();
                if (visualSync != null)
                {
                    visualSync.ApplyAppearance(appearance);
                }
            }
        }

        private string GetPlayerName(Identity identity)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return "Unknown";

            // Use unique index to find player by owner identity
            var player = gm.Connection.Db.Player.Owner.Find(identity);
            if (player != null)
            {
                return player.Username;
            }
            return "Unknown";
        }

        /// <summary>
        /// Get the GameObject for a given player identity.
        /// </summary>
        public GameObject GetPlayerObject(Identity identity)
        {
            _players.TryGetValue(identity, out var obj);
            return obj;
        }

        /// <summary>
        /// Get all currently tracked player objects.
        /// </summary>
        public IReadOnlyDictionary<Identity, GameObject> GetAllPlayers()
        {
            return _players;
        }
    }
}

#endif // STDB_BINDINGS
