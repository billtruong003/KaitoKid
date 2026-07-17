using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SpumOnline.UI
{
    /// <summary>
    /// Individual inventory slot UI element. Handles display, click events,
    /// and drag & drop interactions for a single item slot.
    /// NOTE: This file is NOT behind #if STDB_BINDINGS so the prefab
    /// always has a valid script reference.
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;

        public int SlotIndex { get; private set; }
        public uint ItemId { get; private set; }
        public string ItemName { get; private set; }
        public Sprite ItemSprite { get; private set; }
        public bool IsEmpty => ItemId == 0;

        // Callbacks
        public System.Action OnRightClicked;
        public System.Action OnBeginDragAction;
        public System.Action<int> OnEndDragAction;
        public System.Action OnHoverEnter;
        public System.Action OnHoverExit;

        public void Initialize(int index)
        {
            SlotIndex = index;
            SetEmpty();
        }

        public void SetItem(uint itemId, Sprite sprite, int quantity, string name)
        {
            ItemId = itemId;
            ItemSprite = sprite;
            ItemName = name;

            if (iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.color = sprite != null ? Color.white : new Color(1, 1, 1, 0f);
                iconImage.enabled = sprite != null;
            }

            if (quantityText != null)
            {
                quantityText.text = quantity > 1 ? quantity.ToString() : "";
                quantityText.gameObject.SetActive(quantity > 1);
            }
        }

        public void SetEmpty()
        {
            ItemId = 0;
            ItemSprite = null;
            ItemName = "";

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0f);
                iconImage.enabled = false;
            }

            if (quantityText != null)
            {
                quantityText.text = "";
                quantityText.gameObject.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClicked?.Invoke();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty) return;
            OnBeginDragAction?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Drag icon follows mouse (handled by InventoryUI)
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Find the target slot under the mouse
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            int targetIndex = -1;
            foreach (var result in results)
            {
                var targetSlot = result.gameObject.GetComponent<InventorySlotUI>();
                if (targetSlot != null && targetSlot != this)
                {
                    targetIndex = targetSlot.SlotIndex;
                    break;
                }
            }

            OnEndDragAction?.Invoke(targetIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnHoverExit?.Invoke();
        }
    }
}
