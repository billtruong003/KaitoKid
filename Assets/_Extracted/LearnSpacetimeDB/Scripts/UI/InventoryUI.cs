#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.UI
{
    /// <summary>
    /// Inventory panel UI with a 6x5 grid of item slots, equipment slots,
    /// drag-and-drop, and a right-click context menu. Toggled with Tab.
    /// Listens to inventory_slot and equipment table changes.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Inventory Grid")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("Equipment Slots")]
        [SerializeField] private InventorySlotUI weaponSlot;
        [SerializeField] private InventorySlotUI armorSlot;
        [SerializeField] private InventorySlotUI helmetSlot;

        [Header("Context Menu")]
        [SerializeField] private GameObject contextMenu;
        [SerializeField] private Button contextEquipButton;
        [SerializeField] private Button contextDropButton;
        [SerializeField] private Button contextUseButton;
        [SerializeField] private TMP_Text contextItemName;

        [Header("Item Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TMP_Text tooltipNameText;
        [SerializeField] private TMP_Text tooltipDescText;
        [SerializeField] private TMP_Text tooltipStatsText;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private readonly List<InventorySlotUI> _slots = new List<InventorySlotUI>();
        private bool _isOpen;
        private int _contextSlotIndex = -1;

        // Drag state
        private InventorySlotUI _dragSource;
        private GameObject _dragIcon;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Start()
        {
            // Build the inventory grid
            BuildGrid();

            // Setup context menu buttons
            if (contextEquipButton != null) contextEquipButton.onClick.AddListener(OnContextEquip);
            if (contextDropButton != null) contextDropButton.onClick.AddListener(OnContextDrop);
            if (contextUseButton != null) contextUseButton.onClick.AddListener(OnContextUse);

            // Hide panels
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (contextMenu != null) contextMenu.SetActive(false);
            if (tooltipPanel != null) tooltipPanel.SetActive(false);

            // Subscribe to inventory change events
            if (Bill.IsReady)
            {
                Bill.Events.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
                Bill.Events.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);
            }
        }

        private void OnDestroy()
        {
            if (Bill.IsReady)
            {
                Bill.Events.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
                Bill.Events.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
            }
        }

        private void Update()
        {
            // Toggle with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleInventory();
            }

            // Close context menu on click outside
            if (_isOpen && contextMenu != null && contextMenu.activeSelf)
            {
                if (Input.GetMouseButtonDown(0) && !IsPointerOverContextMenu())
                {
                    contextMenu.SetActive(false);
                }
            }

            // Handle drag
            if (_dragIcon != null)
            {
                _dragIcon.transform.position = Input.mousePosition;
            }
        }

        // -------------------------------------------------------
        // Grid Construction
        // -------------------------------------------------------

        private void BuildGrid()
        {
            if (slotContainer == null || slotPrefab == null) return;

            _slots.Clear();

            for (int i = 0; i < NetworkConfig.INVENTORY_SLOT_COUNT; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                slotObj.name = $"Slot_{i}";

                var slotUI = slotObj.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                {
                    slotUI = slotObj.AddComponent<InventorySlotUI>();
                }

                int slotIndex = i; // Capture for closure
                slotUI.Initialize(i);
                slotUI.OnRightClicked = () => ShowContextMenu(slotIndex);
                slotUI.OnBeginDragAction = () => OnBeginDrag(slotIndex);
                slotUI.OnEndDragAction = (targetIndex) => OnEndDrag(slotIndex, targetIndex);
                slotUI.OnHoverEnter = () => ShowTooltip(slotIndex);
                slotUI.OnHoverExit = () => HideTooltip();

                _slots.Add(slotUI);
            }
        }

        // -------------------------------------------------------
        // Toggle
        // -------------------------------------------------------

        public void ToggleInventory()
        {
            _isOpen = !_isOpen;

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(_isOpen);
            }

            if (_isOpen)
            {
                RefreshAllSlots();
                RefreshEquipment();
                AnimateOpen();
            }
            else
            {
                if (contextMenu != null) contextMenu.SetActive(false);
                if (tooltipPanel != null) tooltipPanel.SetActive(false);
            }
        }

        private void AnimateOpen()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                BillTween.Fade(panelCanvasGroup, 1f, 0.2f)
                    ?.SetEase(EaseType.OutQuad);
            }
        }

        // -------------------------------------------------------
        // Refresh
        // -------------------------------------------------------

        private void OnInventoryChanged(InventoryChangedEvent evt)
        {
            if (_isOpen) RefreshAllSlots();
        }

        private void OnEquipmentChanged(EquipmentChangedEvent evt)
        {
            if (_isOpen) RefreshEquipment();
        }

        private void RefreshAllSlots()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            // Clear all slots first
            foreach (var slot in _slots)
            {
                slot.SetEmpty();
            }

            // Fill from the InventorySlot table using ByOwner index
            foreach (var invSlot in gm.Connection.Db.InventorySlot.ByOwner.Filter(gm.LocalIdentity))
            {
                int index = invSlot.SlotIndex;
                if (index < 0 || index >= _slots.Count) continue;

                // Look up item name from ItemDef
                string itemName = "Item";
                var itemDef = gm.Connection.Db.ItemDef.ItemId.Find(invSlot.ItemId);
                if (itemDef != null) itemName = itemDef.Name;

                // Load item sprite from item definitions
                Sprite itemSprite = LoadItemSprite(invSlot.ItemId);
                _slots[index].SetItem(invSlot.ItemId, itemSprite, invSlot.Quantity, itemName);
            }
        }

        private void RefreshEquipment()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            // Look up equipment for local player via unique index
            var equip = gm.Connection.Db.Equipment.Owner.Find(gm.LocalIdentity);
            if (equip == null)
            {
                // No equipment row - clear all equipment slots
                if (weaponSlot != null) weaponSlot.SetEmpty();
                if (armorSlot != null) armorSlot.SetEmpty();
                if (helmetSlot != null) helmetSlot.SetEmpty();
                return;
            }

            // Equipment stores SlotId references, not ItemId directly.
            // Look up the InventorySlot to find the actual item.
            if (weaponSlot != null && equip.WeaponSlotId > 0)
            {
                var invSlot = gm.Connection.Db.InventorySlot.SlotId.Find(equip.WeaponSlotId);
                if (invSlot != null)
                {
                    Sprite sprite = LoadItemSprite(invSlot.ItemId);
                    string name = "Weapon";
                    var def = gm.Connection.Db.ItemDef.ItemId.Find(invSlot.ItemId);
                    if (def != null) name = def.Name;
                    weaponSlot.SetItem(invSlot.ItemId, sprite, 1, name);
                }
                else
                {
                    weaponSlot.SetEmpty();
                }
            }
            else if (weaponSlot != null)
            {
                weaponSlot.SetEmpty();
            }

            if (armorSlot != null && equip.ArmorSlotId > 0)
            {
                var invSlot = gm.Connection.Db.InventorySlot.SlotId.Find(equip.ArmorSlotId);
                if (invSlot != null)
                {
                    Sprite sprite = LoadItemSprite(invSlot.ItemId);
                    string name = "Armor";
                    var def = gm.Connection.Db.ItemDef.ItemId.Find(invSlot.ItemId);
                    if (def != null) name = def.Name;
                    armorSlot.SetItem(invSlot.ItemId, sprite, 1, name);
                }
                else
                {
                    armorSlot.SetEmpty();
                }
            }
            else if (armorSlot != null)
            {
                armorSlot.SetEmpty();
            }

            if (helmetSlot != null && equip.HelmetSlotId > 0)
            {
                var invSlot = gm.Connection.Db.InventorySlot.SlotId.Find(equip.HelmetSlotId);
                if (invSlot != null)
                {
                    Sprite sprite = LoadItemSprite(invSlot.ItemId);
                    string name = "Helmet";
                    var def = gm.Connection.Db.ItemDef.ItemId.Find(invSlot.ItemId);
                    if (def != null) name = def.Name;
                    helmetSlot.SetItem(invSlot.ItemId, sprite, 1, name);
                }
                else
                {
                    helmetSlot.SetEmpty();
                }
            }
            else if (helmetSlot != null)
            {
                helmetSlot.SetEmpty();
            }
        }

        private Sprite LoadItemSprite(uint itemId)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return null;

            // Look up item definition via unique index
            var itemDef = gm.Connection.Db.ItemDef.ItemId.Find(itemId);
            if (itemDef == null) return null;

            // Use SpriteIndex to load sprite from a sprite atlas or indexed resource
            if (itemDef.SpriteIndex >= 0)
            {
                // Attempt to load from a sprite atlas resource path
                Sprite[] sprites = Resources.LoadAll<Sprite>("Items/ItemAtlas");
                if (sprites != null && itemDef.SpriteIndex < sprites.Length)
                {
                    return sprites[itemDef.SpriteIndex];
                }
            }
            return null;
        }

        // -------------------------------------------------------
        // Context Menu
        // -------------------------------------------------------

        private void ShowContextMenu(int slotIndex)
        {
            if (contextMenu == null) return;
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            var slot = _slots[slotIndex];
            if (slot.IsEmpty) return;

            _contextSlotIndex = slotIndex;

            if (contextItemName != null)
            {
                contextItemName.text = slot.ItemName;
            }

            contextMenu.SetActive(true);
            contextMenu.transform.position = Input.mousePosition;
        }

        private void OnContextEquip()
        {
            if (_contextSlotIndex < 0 || _contextSlotIndex >= _slots.Count) return;
            var slot = _slots[_contextSlotIndex];
            if (slot.IsEmpty) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.IsConnected)
            {
                // EquipItem takes a uint slotId, so look up the actual SlotId from InventorySlot
                uint slotId = FindSlotIdByIndex(_contextSlotIndex);
                if (slotId > 0) gm.Connection.Reducers.EquipItem(slotId);
            }

            if (contextMenu != null) contextMenu.SetActive(false);
        }

        private void OnContextDrop()
        {
            if (_contextSlotIndex < 0 || _contextSlotIndex >= _slots.Count) return;
            var slot = _slots[_contextSlotIndex];
            if (slot.IsEmpty) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.IsConnected)
            {
                // DropItem takes a uint slotId, so look up the actual SlotId from InventorySlot
                uint slotId = FindSlotIdByIndex(_contextSlotIndex);
                if (slotId > 0) gm.Connection.Reducers.DropItem(slotId);
            }

            if (contextMenu != null) contextMenu.SetActive(false);
        }

        private void OnContextUse()
        {
            if (_contextSlotIndex < 0 || _contextSlotIndex >= _slots.Count) return;
            var slot = _slots[_contextSlotIndex];
            if (slot.IsEmpty) return;

            // UseItem reducer does not exist on the server.
            // Log a warning for now; this feature requires a server-side reducer.
            Debug.LogWarning("[InventoryUI] UseItem is not available - no server-side reducer exists.");

            if (contextMenu != null) contextMenu.SetActive(false);
        }

        /// <summary>
        /// Look up the actual InventorySlot.SlotId for the given slot index owned by the local player.
        /// </summary>
        private uint FindSlotIdByIndex(int slotIndex)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return 0;

            foreach (var invSlot in gm.Connection.Db.InventorySlot.ByOwner.Filter(gm.LocalIdentity))
            {
                if (invSlot.SlotIndex == slotIndex)
                {
                    return invSlot.SlotId;
                }
            }
            return 0;
        }

        // -------------------------------------------------------
        // Drag & Drop
        // -------------------------------------------------------

        private void OnBeginDrag(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _slots.Count) return;
            var slot = _slots[sourceIndex];
            if (slot.IsEmpty) return;

            _dragSource = slot;

            // Create drag icon at mouse position
            _dragIcon = new GameObject("DragIcon");
            _dragIcon.transform.SetParent(transform, false);

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _dragIcon.transform.SetParent(canvas.transform, false);
            }

            var img = _dragIcon.AddComponent<Image>();
            img.sprite = slot.ItemSprite;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0.7f);

            var rt = _dragIcon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48f, 48f);
        }

        private void OnEndDrag(int sourceIndex, int targetIndex)
        {
            if (_dragIcon != null)
            {
                Destroy(_dragIcon);
                _dragIcon = null;
            }

            _dragSource = null;

            if (sourceIndex == targetIndex) return;
            if (sourceIndex < 0 || targetIndex < 0) return;
            if (sourceIndex >= _slots.Count || targetIndex >= _slots.Count) return;

            // SwapInventorySlots reducer does not exist on the server.
            // Log a warning for now; this feature requires a server-side reducer.
            Debug.LogWarning("[InventoryUI] SwapInventorySlots is not available - no server-side reducer exists.");
        }

        // -------------------------------------------------------
        // Tooltip
        // -------------------------------------------------------

        private void ShowTooltip(int slotIndex)
        {
            if (tooltipPanel == null) return;
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;

            var slot = _slots[slotIndex];
            if (slot.IsEmpty) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            // Look up item definition via unique index
            var itemDef = gm.Connection.Db.ItemDef.ItemId.Find(slot.ItemId);
            if (itemDef != null)
            {
                if (tooltipNameText != null) tooltipNameText.text = itemDef.Name;
                if (tooltipDescText != null) tooltipDescText.text = itemDef.Description;
                if (tooltipStatsText != null) tooltipStatsText.text = FormatItemStats(itemDef);

                tooltipPanel.SetActive(true);
                tooltipPanel.transform.position = Input.mousePosition + new Vector3(80f, 0f, 0f);
            }
        }

        private void HideTooltip()
        {
            if (tooltipPanel != null) tooltipPanel.SetActive(false);
        }

        private string FormatItemStats(ItemDef item)
        {
            var sb = new System.Text.StringBuilder();
            if (item.BonusAtk > 0) sb.AppendLine($"ATK: +{item.BonusAtk}");
            if (item.BonusDef > 0) sb.AppendLine($"DEF: +{item.BonusDef}");
            if (item.BonusHp > 0) sb.AppendLine($"HP: +{item.BonusHp}");
            if (item.BonusMp > 0) sb.AppendLine($"MP: +{item.BonusMp}");
            if (item.BonusSpeed > 0) sb.AppendLine($"SPD: +{item.BonusSpeed}");
            return sb.ToString();
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private bool IsPointerOverContextMenu()
        {
            if (contextMenu == null) return false;
            var rt = contextMenu.GetComponent<RectTransform>();
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition);
        }

        public bool IsOpen => _isOpen;
    }
}

#endif // STDB_BINDINGS
