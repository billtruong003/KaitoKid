#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using UnityEngine;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.NPC
{
    /// <summary>
    /// Represents a loot drop in the game world.
    /// Provides a floating bob animation, rarity-based glow, click-to-pickup,
    /// and hover name display. Controlled by the LootSpawner which listens to the loot table.
    /// </summary>
    public class LootDrop : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Visual")]
        [SerializeField] private SpriteRenderer itemSprite;
        [SerializeField] private SpriteRenderer glowSprite;
        [SerializeField] private TMP_Text nameLabel;

        [Header("Bob Animation")]
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobDuration = 1.0f;

        [Header("Rarity Colors")]
        [SerializeField] private Color commonGlow = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        [SerializeField] private Color uncommonGlow = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color rareGlow = new Color(0.2f, 0.4f, 1f, 0.6f);
        [SerializeField] private Color epicGlow = new Color(0.7f, 0.2f, 1f, 0.7f);
        [SerializeField] private Color legendaryGlow = new Color(1f, 0.7f, 0f, 0.8f);

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        /// <summary>The server-side loot drop ID.</summary>
        public uint LootId { get; private set; }

        /// <summary>The item definition ID for this loot.</summary>
        public uint ItemId { get; private set; }

        /// <summary>Display name of the item.</summary>
        public string ItemName { get; private set; }

        /// <summary>Item rarity (0=common, 1=uncommon, 2=rare, 3=epic, 4=legendary).</summary>
        public int Rarity { get; private set; }

        private Tween _bobTween;
        private bool _isHovered;
        private float _baseY;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Start()
        {
            _baseY = transform.position.y;

            // Hide name label by default (shown on hover)
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(false);
            }

            StartBobAnimation();
        }

        private void OnDestroy()
        {
            StopBobAnimation();
        }

        // -------------------------------------------------------
        // Initialization
        // -------------------------------------------------------

        /// <summary>
        /// Initialize the loot drop from server data.
        /// </summary>
        public void Initialize(uint lootId, uint itemId, string itemName, int rarity, string spritePath)
        {
            LootId = lootId;
            ItemId = itemId;
            ItemName = itemName;
            Rarity = rarity;

            // Set item sprite
            if (itemSprite != null && !string.IsNullOrEmpty(spritePath))
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite != null)
                {
                    itemSprite.sprite = sprite;
                }
            }

            // Set name label
            if (nameLabel != null)
            {
                nameLabel.text = itemName;
                nameLabel.color = GetRarityColor(rarity);
            }

            // Set glow color based on rarity
            if (glowSprite != null)
            {
                glowSprite.color = GetGlowColor(rarity);
                glowSprite.gameObject.SetActive(rarity > 0); // No glow for common
            }

            _baseY = transform.position.y;
        }

        // -------------------------------------------------------
        // Bob Animation
        // -------------------------------------------------------

        private void StartBobAnimation()
        {
            // Floating bob: move up and down in a yoyo loop using BillTween
            _bobTween = BillTween.MoveY(transform, _baseY + bobHeight, bobDuration)
                ?.SetEase(EaseType.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this);
        }

        private void StopBobAnimation()
        {
            if (!Bill.IsReady) return;
            if (_bobTween != null)
            {
                BillTween.Kill(_bobTween);
                _bobTween = null;
            }
            BillTween.KillTarget(this);
        }

        // -------------------------------------------------------
        // Interaction
        // -------------------------------------------------------

        private void OnMouseDown()
        {
            // Pickup the loot
            PickupLoot();
        }

        private void OnMouseEnter()
        {
            _isHovered = true;

            // Show name label
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(true);
            }

            // Scale up slightly for hover feedback
            BillTween.Scale(transform, 1.15f, 0.15f)
                ?.SetEase(EaseType.OutBack)
                .SetTarget(transform);
        }

        private void OnMouseExit()
        {
            _isHovered = false;

            // Hide name label
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(false);
            }

            // Scale back to normal
            BillTween.Scale(transform, 1f, 0.1f)
                ?.SetEase(EaseType.InOutSine)
                .SetTarget(transform);
        }

        private void PickupLoot()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            // Call the server reducer to pick up this loot
            gm.Connection.Reducers.PickupLoot(LootId);

            Debug.Log($"[LootDrop] Attempting to pick up: {ItemName} (ID: {LootId})");

            // Optimistic: play pickup animation
            StopBobAnimation();

            // Scale down and fade out
            BillTween.Scale(transform, 0f, 0.3f)
                ?.SetEase(EaseType.InBack)
                .SetTarget(this);

            if (itemSprite != null)
            {
                BillTween.Fade(itemSprite, 0f, 0.3f)
                    ?.SetTarget(this);
            }

            // Note: actual destruction happens when the server deletes the loot row,
            // which triggers LootSpawner to destroy this object.
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        private Color GetGlowColor(int rarity)
        {
            return rarity switch
            {
                0 => commonGlow,
                1 => uncommonGlow,
                2 => rareGlow,
                3 => epicGlow,
                4 => legendaryGlow,
                _ => commonGlow
            };
        }

        private Color GetRarityColor(int rarity)
        {
            return rarity switch
            {
                0 => Color.white,
                1 => new Color(0.2f, 0.9f, 0.2f),
                2 => new Color(0.3f, 0.5f, 1f),
                3 => new Color(0.8f, 0.3f, 1f),
                4 => new Color(1f, 0.8f, 0f),
                _ => Color.white
            };
        }
    }
}

#endif // STDB_BINDINGS
