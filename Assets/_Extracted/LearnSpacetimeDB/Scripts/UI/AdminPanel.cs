#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.UI
{
    /// <summary>
    /// Admin debug panel accessible with F12. Only visible to players
    /// flagged as admin. Provides controls for spawning mobs, giving items,
    /// and performing world-wide administrative actions.
    /// </summary>
    public class AdminPanel : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject adminPanelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Mob Spawner")]
        [SerializeField] private TMP_Dropdown mobTypeDropdown;
        [SerializeField] private Slider mobCountSlider;
        [SerializeField] private TMP_Text mobCountText;
        [SerializeField] private Button spawnMobsButton;

        [Header("Item Giver")]
        [SerializeField] private TMP_Dropdown playerDropdown;
        [SerializeField] private TMP_Dropdown itemDropdown;
        [SerializeField] private Slider itemQuantitySlider;
        [SerializeField] private TMP_Text itemQuantityText;
        [SerializeField] private Button giveItemButton;

        [Header("World Controls")]
        [SerializeField] private Button killAllMobsButton;
        [SerializeField] private Button healAllButton;
        [SerializeField] private Button toggleSpawnZonesButton;
        [SerializeField] private TMP_Text spawnZoneStatusText;

        [Header("Stats Display")]
        [SerializeField] private TMP_Text onlinePlayersText;
        [SerializeField] private TMP_Text activeMobsText;
        [SerializeField] private TMP_Text lootCountText;
        [SerializeField] private TMP_Text serverTimeText;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private bool _isOpen;
        private bool _isAdmin;
        private bool _spawnZonesActive = true;
        private float _statsRefreshTimer;
        private const float STATS_REFRESH_INTERVAL = 2f;

        // Cached dropdown data
        private readonly List<string> _mobTypes = new List<string>();
        private readonly List<string> _playerNames = new List<string>();
        private readonly List<Identity> _playerIdentities = new List<Identity>();
        private readonly List<string> _itemNames = new List<string>();
        private readonly List<uint> _itemIds = new List<uint>();

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Start()
        {
            // Hide panel initially
            if (adminPanelRoot != null) adminPanelRoot.SetActive(false);

            // Setup button listeners
            if (spawnMobsButton != null) spawnMobsButton.onClick.AddListener(OnSpawnMobs);
            if (giveItemButton != null) giveItemButton.onClick.AddListener(OnGiveItem);
            if (killAllMobsButton != null) killAllMobsButton.onClick.AddListener(OnKillAllMobs);
            if (healAllButton != null) healAllButton.onClick.AddListener(OnHealAll);
            if (toggleSpawnZonesButton != null) toggleSpawnZonesButton.onClick.AddListener(OnToggleSpawnZones);

            // Setup sliders
            if (mobCountSlider != null)
            {
                mobCountSlider.minValue = 1;
                mobCountSlider.maxValue = 50;
                mobCountSlider.wholeNumbers = true;
                mobCountSlider.value = 5;
                mobCountSlider.onValueChanged.AddListener(v =>
                {
                    if (mobCountText != null) mobCountText.text = v.ToString("F0");
                });
            }

            if (itemQuantitySlider != null)
            {
                itemQuantitySlider.minValue = 1;
                itemQuantitySlider.maxValue = 99;
                itemQuantitySlider.wholeNumbers = true;
                itemQuantitySlider.value = 1;
                itemQuantitySlider.onValueChanged.AddListener(v =>
                {
                    if (itemQuantityText != null) itemQuantityText.text = v.ToString("F0");
                });
            }

            // Check admin status
            CheckAdminStatus();
        }

        private void Update()
        {
            // Toggle with F12
            if (Input.GetKeyDown(KeyCode.F12))
            {
                TogglePanel();
            }

            // Refresh stats periodically when open
            if (_isOpen)
            {
                _statsRefreshTimer += Time.deltaTime;
                if (_statsRefreshTimer >= STATS_REFRESH_INTERVAL)
                {
                    _statsRefreshTimer = 0f;
                    RefreshStats();
                }
            }
        }

        // -------------------------------------------------------
        // Admin Check
        // -------------------------------------------------------

        private void CheckAdminStatus()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null)
            {
                _isAdmin = false;
                return;
            }

            var player = gm.Connection.Db.Player.Owner.Find(gm.LocalIdentity);
            _isAdmin = player != null && player.IsAdmin;
        }

        // -------------------------------------------------------
        // Toggle
        // -------------------------------------------------------

        private void TogglePanel()
        {
            // Re-check admin status each time
            CheckAdminStatus();

            if (!_isAdmin)
            {
                Debug.Log("[AdminPanel] Access denied - not an admin.");
                return;
            }

            _isOpen = !_isOpen;

            if (adminPanelRoot != null)
            {
                adminPanelRoot.SetActive(_isOpen);
            }

            if (_isOpen)
            {
                PopulateDropdowns();
                RefreshStats();
                AnimateOpen();
            }
        }

        private void AnimateOpen()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                BillTween.Fade(panelCanvasGroup, 1f, 0.25f)
                    ?.SetEase(EaseType.OutQuad);
            }
        }

        // -------------------------------------------------------
        // Dropdown Population
        // -------------------------------------------------------

        private void PopulateDropdowns()
        {
            PopulateMobTypes();
            PopulatePlayers();
            PopulateItems();
        }

        private void PopulateMobTypes()
        {
            _mobTypes.Clear();

            // Predefined mob types (should match server-side mob definitions)
            _mobTypes.Add("Slime");
            _mobTypes.Add("Skeleton");
            _mobTypes.Add("Goblin");
            _mobTypes.Add("Orc");
            _mobTypes.Add("Dragon");

            if (mobTypeDropdown != null)
            {
                mobTypeDropdown.ClearOptions();
                mobTypeDropdown.AddOptions(_mobTypes);
            }
        }

        private void PopulatePlayers()
        {
            _playerNames.Clear();
            _playerIdentities.Clear();

            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            foreach (var player in gm.Connection.Db.Player.Iter())
            {
                _playerNames.Add(player.Username);
                _playerIdentities.Add(player.Owner);
            }

            if (playerDropdown != null)
            {
                playerDropdown.ClearOptions();
                playerDropdown.AddOptions(_playerNames);
            }
        }

        private void PopulateItems()
        {
            _itemNames.Clear();
            _itemIds.Clear();

            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            foreach (var item in gm.Connection.Db.ItemDef.Iter())
            {
                _itemNames.Add(item.Name);
                _itemIds.Add(item.ItemId);
            }

            if (itemDropdown != null)
            {
                itemDropdown.ClearOptions();
                itemDropdown.AddOptions(_itemNames);
            }
        }

        // -------------------------------------------------------
        // Actions
        // -------------------------------------------------------

        private void OnSpawnMobs()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            int count = mobCountSlider != null ? Mathf.RoundToInt(mobCountSlider.value) : 1;

            // Use AdminSpawnRandomWave to spawn mobs near the player's position
            float centerX = 0f, centerY = 0f;
            if (gm.LocalPlayerPosition != null)
            {
                centerX = gm.LocalPlayerPosition.PosX;
                centerY = gm.LocalPlayerPosition.PosY;
            }

            gm.Connection.Reducers.AdminSpawnRandomWave(count, centerX, centerY, 5f);
            Debug.Log($"[AdminPanel] Spawning {count} mobs near ({centerX:F1}, {centerY:F1})");
        }

        private void OnGiveItem()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            int playerIndex = playerDropdown != null ? playerDropdown.value : 0;
            int itemIndex = itemDropdown != null ? itemDropdown.value : 0;
            int quantity = itemQuantitySlider != null ? Mathf.RoundToInt(itemQuantitySlider.value) : 1;

            if (playerIndex >= _playerIdentities.Count || itemIndex >= _itemIds.Count) return;

            Identity targetPlayer = _playerIdentities[playerIndex];
            uint itemId = _itemIds[itemIndex];

            gm.Connection.Reducers.AdminGiveItem(targetPlayer, itemId, quantity);
            Debug.Log($"[AdminPanel] Giving {quantity}x item {_itemNames[itemIndex]} to {_playerNames[playerIndex]}");
        }

        private void OnKillAllMobs()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            gm.Connection.Reducers.AdminKillAllMobs();
            Debug.Log("[AdminPanel] Kill all mobs command sent.");
        }

        private void OnHealAll()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            gm.Connection.Reducers.AdminHealAllPlayers();
            Debug.Log("[AdminPanel] Heal all players command sent.");
        }

        private void OnToggleSpawnZones()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            _spawnZonesActive = !_spawnZonesActive;

            // Toggle all spawn configs
            foreach (var config in gm.Connection.Db.SpawnConfig.Iter())
            {
                gm.Connection.Reducers.AdminToggleSpawnConfig(config.ConfigId, _spawnZonesActive);
            }

            if (spawnZoneStatusText != null)
            {
                spawnZoneStatusText.text = _spawnZonesActive ? "Spawn Zones: ON" : "Spawn Zones: OFF";
                spawnZoneStatusText.color = _spawnZonesActive ? Color.green : Color.red;
            }

            Debug.Log($"[AdminPanel] Spawn zones: {(_spawnZonesActive ? "ON" : "OFF")}");
        }

        // -------------------------------------------------------
        // Stats Refresh
        // -------------------------------------------------------

        private void RefreshStats()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            // Count online players
            int playerCount = 0;
            foreach (var _ in gm.Connection.Db.PlayerPosition.Iter()) playerCount++;
            if (onlinePlayersText != null)
            {
                onlinePlayersText.text = $"Online Players: {playerCount}";
            }

            // Count active mobs
            int mobCount = 0;
            foreach (var _ in gm.Connection.Db.MobInstance.Iter()) mobCount++;
            if (activeMobsText != null)
            {
                activeMobsText.text = $"Active Mobs: {mobCount}";
            }

            // Count loot drops
            int lootCount = 0;
            foreach (var _ in gm.Connection.Db.LootDrop.Iter()) lootCount++;
            if (lootCountText != null)
            {
                lootCountText.text = $"Loot Drops: {lootCount}";
            }

            // Server time
            if (serverTimeText != null)
            {
                serverTimeText.text = $"Local Time: {System.DateTime.Now:HH:mm:ss}";
            }
        }

        public bool IsOpen => _isOpen;
    }
}

#endif // STDB_BINDINGS
