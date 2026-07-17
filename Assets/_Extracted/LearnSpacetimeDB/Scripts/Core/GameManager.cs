#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
// The module_bindings/ folder will contain auto-generated types such as:
//   DbConnection, PlayerPosition, PlayerStats, PlayerAppearance,
//   MobInstance, DamageEvent, ChatMessage, InventorySlot, Equipment,
//   LootDrop, ItemDef, Reducers, etc.
// These are referenced below as if they already exist.

using System;
using System.Collections.Generic;
using UnityEngine;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline
{
    /// <summary>
    /// Singleton GameManager that owns the SpacetimeDB connection lifecycle.
    /// Persists across scenes via DontDestroyOnLoad.
    /// Connects to the server, subscribes to all relevant tables, and routes
    /// table callbacks into BillGameCore events for decoupled consumption.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // -------------------------------------------------------
        // Singleton
        // -------------------------------------------------------

        public static GameManager Instance { get; private set; }

        // -------------------------------------------------------
        // Public state
        // -------------------------------------------------------

        /// <summary>The active SpacetimeDB connection.</summary>
        public SpacetimeDB.Types.DbConnection Connection { get; private set; }

        /// <summary>The local client's identity, assigned by the server on connect.</summary>
        public Identity LocalIdentity { get; private set; }

        /// <summary>Whether the client is currently connected to the server.</summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Cached reference to the local player's position row.
        /// Set after subscription is applied and the player is found in the table.
        /// </summary>
        public PlayerPosition LocalPlayerPosition { get; private set; }

        /// <summary>
        /// Cached reference to the local player's stats row.
        /// </summary>
        public PlayerStats LocalPlayerStats { get; private set; }

        /// <summary>
        /// Cached reference to the local player's appearance row.
        /// </summary>
        public PlayerAppearance LocalPlayerAppearance { get; private set; }

        /// <summary>
        /// Whether the initial subscription has been applied (all table data received).
        /// </summary>
        public bool SubscriptionReady { get; private set; }

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Prefabs")]
        [Tooltip("Prefab for the local player character.")]
        [SerializeField] private GameObject localPlayerPrefab;

        [Tooltip("Prefab for remote player characters.")]
        [SerializeField] private GameObject remotePlayerPrefab;

        [Tooltip("Prefab for mob instances.")]
        [SerializeField] private GameObject mobPrefab;

        [Tooltip("Prefab for loot drops.")]
        [SerializeField] private GameObject lootDropPrefab;

        [Tooltip("Prefab for floating damage numbers.")]
        [SerializeField] private GameObject damagePopupPrefab;

        // -------------------------------------------------------
        // Accessors for prefabs (used by spawners)
        // -------------------------------------------------------

        public GameObject LocalPlayerPrefab => localPlayerPrefab;
        public GameObject RemotePlayerPrefab => remotePlayerPrefab;
        public GameObject MobPrefab => mobPrefab;
        public GameObject LootDropPrefab => lootDropPrefab;
        public GameObject DamagePopupPrefab => damagePopupPrefab;

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
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ConnectToServer();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Disconnect();
                Instance = null;
            }
        }

        // -------------------------------------------------------
        // Connection
        // -------------------------------------------------------

        /// <summary>
        /// Establish a connection to the SpacetimeDB server.
        /// Uses the persisted auth token if available, otherwise connects anonymously.
        /// </summary>
        public void ConnectToServer()
        {
            if (IsConnected)
            {
                Debug.LogWarning("[GameManager] Already connected.");
                return;
            }

            Debug.Log($"[GameManager] Connecting to {NetworkConfig.HOST}/{NetworkConfig.MODULE_NAME}...");

            string savedToken = AuthManager.LoadToken();

            try
            {
                var builder = SpacetimeDB.Types.DbConnection.Builder()
                    .WithUri(NetworkConfig.HOST)
                    .WithDatabaseName(NetworkConfig.MODULE_NAME)
                    .OnConnect(OnConnected)
                    .OnConnectError(OnConnectError)
                    .OnDisconnect(OnDisconnected);

                if (!string.IsNullOrEmpty(savedToken))
                {
                    builder = builder.WithToken(savedToken);
                }

                Connection = builder.Build();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Connection build failed: {e.Message}");
            }
        }

        /// <summary>
        /// Gracefully disconnect from the server.
        /// </summary>
        public void Disconnect()
        {
            if (Connection != null)
            {
                IsConnected = false;
                SubscriptionReady = false;
                try
                {
                    Connection.Disconnect();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GameManager] Error closing connection: {e.Message}");
                }
                Connection = null;
            }
        }

        // -------------------------------------------------------
        // SpacetimeDB Callbacks
        // -------------------------------------------------------

        private void OnConnected(SpacetimeDB.Types.DbConnection conn, Identity identity, string token)
        {
            Debug.Log($"[GameManager] Connected. Identity: {identity}");

            LocalIdentity = identity;
            IsConnected = true;

            // Persist the auth token for future sessions
            AuthManager.SaveToken(token);

            // Fire the connected event
            if (Bill.IsReady)
            {
                Bill.Events.Fire(new PlayerConnectedEvent { Identity = identity.ToString() });
            }

            // Register all table callbacks before subscribing
            RegisterTableCallbacks(conn);

            // Subscribe to all game tables
            conn.SubscriptionBuilder()
                .OnApplied(OnSubscriptionApplied)
                .SubscribeToAllTables();
        }

        private void OnConnectError(Exception error)
        {
            Debug.LogError($"[GameManager] Connection error: {error.Message}");

            // Retry connection after a delay
            if (Bill.IsReady)
            {
                Bill.Timer.Delay(3f, () =>
                {
                    Debug.Log("[GameManager] Retrying connection...");
                    ConnectToServer();
                });
            }
        }

        private void OnDisconnected(SpacetimeDB.Types.DbConnection conn, Exception error)
        {
            Debug.LogWarning($"[GameManager] Disconnected. Error: {error?.Message ?? "None"}");

            IsConnected = false;
            SubscriptionReady = false;
            LocalPlayerPosition = null;
            LocalPlayerStats = null;
            LocalPlayerAppearance = null;

            if (Bill.IsReady)
            {
                Bill.Events.Fire<PlayerDisconnectedEvent>();
            }
        }

        // -------------------------------------------------------
        // Subscription
        // -------------------------------------------------------

        private void OnSubscriptionApplied(SubscriptionEventContext ctx)
        {
            Debug.Log("[GameManager] Subscription applied - all table data received.");
            SubscriptionReady = true;

            // Check if our player already exists in the database
            bool playerExists = TryFindLocalPlayer();

            if (Bill.IsReady)
            {
                Bill.Events.Fire<SubscriptionAppliedEvent>();
            }

            if (!playerExists)
            {
                // Player does not exist yet -- go to character creation
                Debug.Log("[GameManager] No existing player found. Loading CharacterSelect scene.");
                if (Bill.IsReady)
                {
                    Bill.Scene.Load("CharacterSelect");
                }
            }
            else
            {
                // Player exists -- enter the game world
                Debug.Log("[GameManager] Existing player found. Entering world.");
                Connection.Reducers.EnterWorld();

                if (Bill.IsReady)
                {
                    Bill.Scene.Load("GameWorld");
                }
            }
        }

        /// <summary>
        /// Search the player_position table for our local identity.
        /// If found, cache the position, stats, and appearance rows.
        /// </summary>
        private bool TryFindLocalPlayer()
        {
            // Use unique index to find our player's position by identity
            LocalPlayerPosition = Connection.Db.PlayerPosition.Owner.Find(LocalIdentity);

            // Also cache stats and appearance if available
            LocalPlayerStats = Connection.Db.PlayerStats.Owner.Find(LocalIdentity);

            LocalPlayerAppearance = Connection.Db.PlayerAppearance.Owner.Find(LocalIdentity);

            return LocalPlayerPosition != null;
        }

        // -------------------------------------------------------
        // Table Callbacks Registration
        // -------------------------------------------------------

        private void RegisterTableCallbacks(SpacetimeDB.Types.DbConnection conn)
        {
            // Player Position
            conn.Db.PlayerPosition.OnInsert += OnPlayerPositionInsert;
            conn.Db.PlayerPosition.OnUpdate += OnPlayerPositionUpdate;
            conn.Db.PlayerPosition.OnDelete += OnPlayerPositionDelete;

            // Player Stats
            conn.Db.PlayerStats.OnInsert += OnPlayerStatsInsert;
            conn.Db.PlayerStats.OnUpdate += OnPlayerStatsUpdate;

            // Player Appearance
            conn.Db.PlayerAppearance.OnInsert += OnPlayerAppearanceInsert;
            conn.Db.PlayerAppearance.OnUpdate += OnPlayerAppearanceUpdate;

            // Mob Instance
            conn.Db.MobInstance.OnInsert += OnMobInstanceInsert;
            conn.Db.MobInstance.OnUpdate += OnMobInstanceUpdate;
            conn.Db.MobInstance.OnDelete += OnMobInstanceDelete;

            // Damage Event
            conn.Db.DamageEvent.OnInsert += OnDamageEventInsert;

            // Chat Message
            conn.Db.ChatMessage.OnInsert += OnChatMessageInsert;

            // Loot Drop
            conn.Db.LootDrop.OnInsert += OnLootDropInsert;
            conn.Db.LootDrop.OnDelete += OnLootDropDelete;

            // Inventory Slot
            conn.Db.InventorySlot.OnInsert += OnInventorySlotChanged;
            conn.Db.InventorySlot.OnUpdate += OnInventorySlotUpdated;
            conn.Db.InventorySlot.OnDelete += OnInventorySlotRemoved;

            // Equipment
            conn.Db.Equipment.OnInsert += OnEquipmentInsert;
            conn.Db.Equipment.OnUpdate += OnEquipmentUpdate;
        }

        // -------------------------------------------------------
        // Player Position Callbacks
        // -------------------------------------------------------

        private void OnPlayerPositionInsert(EventContext ctx, PlayerPosition row)
        {
            if (row.Owner == LocalIdentity)
            {
                LocalPlayerPosition = row;
            }
            // PlayerSpawner handles the visual spawning
        }

        private void OnPlayerPositionUpdate(EventContext ctx, PlayerPosition oldRow, PlayerPosition newRow)
        {
            if (newRow.Owner == LocalIdentity)
            {
                LocalPlayerPosition = newRow;
            }
            // PlayerSpawner handles position sync
        }

        private void OnPlayerPositionDelete(EventContext ctx, PlayerPosition row)
        {
            if (row.Owner == LocalIdentity)
            {
                LocalPlayerPosition = null;
            }
            // PlayerSpawner handles the visual cleanup
        }

        // -------------------------------------------------------
        // Player Stats Callbacks
        // -------------------------------------------------------

        private void OnPlayerStatsInsert(EventContext ctx, PlayerStats row)
        {
            if (row.Owner == LocalIdentity)
            {
                LocalPlayerStats = row;
            }
        }

        private void OnPlayerStatsUpdate(EventContext ctx, PlayerStats oldRow, PlayerStats newRow)
        {
            if (newRow.Owner == LocalIdentity)
            {
                LocalPlayerStats = newRow;
            }

            if (Bill.IsReady)
            {
                Bill.Events.Fire(new PlayerStatsUpdatedEvent { Identity = newRow.Owner.ToString() });
            }
        }

        // -------------------------------------------------------
        // Player Appearance Callbacks
        // -------------------------------------------------------

        private void OnPlayerAppearanceInsert(EventContext ctx, PlayerAppearance row)
        {
            if (row.Owner == LocalIdentity)
            {
                LocalPlayerAppearance = row;
            }
        }

        private void OnPlayerAppearanceUpdate(EventContext ctx, PlayerAppearance oldRow, PlayerAppearance newRow)
        {
            if (newRow.Owner == LocalIdentity)
            {
                LocalPlayerAppearance = newRow;
            }
        }

        // -------------------------------------------------------
        // Mob Callbacks (stub -- MobController listens directly)
        // -------------------------------------------------------

        private void OnMobInstanceInsert(EventContext ctx, MobInstance row) { }
        private void OnMobInstanceUpdate(EventContext ctx, MobInstance oldRow, MobInstance newRow) { }
        private void OnMobInstanceDelete(EventContext ctx, MobInstance row) { }

        // -------------------------------------------------------
        // Damage Event Callback
        // -------------------------------------------------------

        private void OnDamageEventInsert(EventContext ctx, DamageEvent row)
        {
            // CombatManager listens to this directly
            // Check if local player was hit for screen shake
            if (row.Victim == LocalIdentity && Bill.IsReady)
            {
                Bill.Events.Fire(new LocalPlayerDamagedEvent { Damage = row.Damage });
            }
        }

        // -------------------------------------------------------
        // Chat Callback
        // -------------------------------------------------------

        private void OnChatMessageInsert(EventContext ctx, ChatMessage row)
        {
            if (Bill.IsReady)
            {
                Bill.Events.Fire(new ChatMessageReceivedEvent
                {
                    Sender = row.SenderName,
                    Message = row.Content,
                    Channel = row.Channel
                });
            }
        }

        // -------------------------------------------------------
        // Loot Callbacks (stub -- NPC/LootDrop listens directly)
        // -------------------------------------------------------

        private void OnLootDropInsert(EventContext ctx, LootDrop row) { }
        private void OnLootDropDelete(EventContext ctx, LootDrop row) { }

        // -------------------------------------------------------
        // Inventory Callbacks
        // -------------------------------------------------------

        private void OnInventorySlotChanged(EventContext ctx, InventorySlot row)
        {
            if (row.Owner == LocalIdentity && Bill.IsReady)
            {
                Bill.Events.Fire<InventoryChangedEvent>();
            }
        }

        private void OnInventorySlotUpdated(EventContext ctx, InventorySlot oldRow, InventorySlot newRow)
        {
            if (newRow.Owner == LocalIdentity && Bill.IsReady)
            {
                Bill.Events.Fire<InventoryChangedEvent>();
            }
        }

        private void OnInventorySlotRemoved(EventContext ctx, InventorySlot row)
        {
            if (row.Owner == LocalIdentity && Bill.IsReady)
            {
                Bill.Events.Fire<InventoryChangedEvent>();
            }
        }

        // -------------------------------------------------------
        // Equipment Callbacks
        // -------------------------------------------------------

        private void OnEquipmentInsert(EventContext ctx, Equipment row)
        {
            if (row.Owner == LocalIdentity && Bill.IsReady)
            {
                Bill.Events.Fire<EquipmentChangedEvent>();
            }
        }

        private void OnEquipmentUpdate(EventContext ctx, Equipment oldRow, Equipment newRow)
        {
            if (newRow.Owner == LocalIdentity && Bill.IsReady)
            {
                Bill.Events.Fire<EquipmentChangedEvent>();
            }
        }

        // -------------------------------------------------------
        // Frame Update - pump SpacetimeDB
        // -------------------------------------------------------

        private void Update()
        {
            // SpacetimeDB requires calling ProcessMessages on the main thread
            // to dispatch callbacks. The Unity SDK typically handles this via
            // DbConnection being ticked, but if manual pumping is needed:
            Connection?.FrameTick();
        }
    }
}

#endif // STDB_BINDINGS
