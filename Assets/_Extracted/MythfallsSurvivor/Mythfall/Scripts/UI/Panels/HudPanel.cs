using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BillGameCore;
using Mythfall.Events;
using Mythfall.Inventory;
using Mythfall.Player;

namespace Mythfall.UI.Panels
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: GameplayScene (Canvas Screen Space - Overlay)
     * Path:  Canvas → HudPanel (this GameObject, this script attached)
     *
     * Required hierarchy:
     *   HudPanel (RectTransform full-stretch, this script + CanvasGroup, GameObject ACTIVE)
     *   ├── HpBarRoot (anchor top-left, position 24, -24, size 480x60)
     *   │   ├── HpBarBg (Image, dark grey, full-stretch)
     *   │   └── HpBarFill (Image type=Filled, fill method Horizontal Left, fillAmount=1, color #D24747, full-stretch)
     *   │       ↳ drag this Image into hpBarFill
     *   ├── CharacterInfoRow (HorizontalLayoutGroup, anchor top-left under HpBarRoot, y=-100, spacing 12)
     *   │   ├── CharacterNameText (TMP_Text + LocalizedText component, key set at runtime by HudPanel,
     *   │   │                       leave key empty in Inspector — HudPanel.SetKey overrides on show)
     *   │   │   ↳ drag the TMP_Text into characterNameText
     *   │   │   ↳ drag the LocalizedText component into characterNameLocalized
     *   │   └── StarRow (HorizontalLayoutGroup, spacing 4)
     *   │       └── Star_1..Star_6 (Image, gold star sprite, size 24x24)
     *   │           ↳ drag the 6 Image components into starIcons[]
     *   └── PauseButton (Button + Image, anchor top-right, margin 24,24, size 60x60)
     *       ↳ drag into pauseButton
     *
     * Components on HudPanel root:
     *   - CanvasGroup (auto-added by MythfallPanelBase RequireComponent)
     *
     * Serialized fields to assign in Inspector:
     *   - hpBarFill              (Image, type Filled)
     *   - characterNameText      (TMP_Text)
     *   - characterNameLocalized (LocalizedText) — same GO as characterNameText
     *   - starIcons              (Image[6])
     *   - pauseButton            (Button)
     *
     * Behavior:
     *   - OnPanelShown: read InventoryService → update name LocalizedText key + star visibility,
     *     find player in scene → read PlayerHealth.CurrentHP/MaxHP → set initial fillAmount,
     *     subscribe PlayerDamagedEvent.
     *   - On PlayerDamagedEvent: update fillAmount = currentHP / maxHP.
     *   - OnPanelHidden: unsubscribe PlayerDamagedEvent.
     *   - PauseButton: no-op log only in Day 3 (Sprint 4 polish).
     *
     * ============================================================ */

    public class HudPanel : MythfallPanelBase
    {
        [Header("HP Bar")]
        [SerializeField] Image hpBarFill;

        [Header("Character Header")]
        [SerializeField] TMP_Text characterNameText;
        [SerializeField] Mythfall.UI.LocalizedText characterNameLocalized;
        [SerializeField] Image[] starIcons;

        [Header("Buttons")]
        [SerializeField] Button pauseButton;

        bool _subscribed;

        protected override void Awake()
        {
            base.Awake();

            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
        }

        void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
            // Unsubscribe defensively — OnPanelHidden may not have run if panel is destroyed mid-show.
            // Gate on Bill.IsReady because OnDestroy may fire after service teardown
            // (stop Play); the Bill.Events getter logs an error before returning null.
            if (_subscribed && Bill.IsReady)
            {
                Bill.Events.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
                Bill.Events.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
            }
            _subscribed = false;
        }

        protected override void OnPanelShown()
        {
            // Bind character name + stars from current selection.
            var inv = ServiceLocator.Get<InventoryService>();
            var charData = inv?.GetCurrentCharacterData();
            if (charData != null)
            {
                if (characterNameLocalized != null) characterNameLocalized.SetKey(charData.nameKey);
                else if (characterNameText != null) characterNameText.text = charData.GetDisplayName();

                UpdateStarVisuals(charData.starTier);
            }
            else
            {
                Debug.LogWarning("[HudPanel] CharacterDataSO unavailable — name/stars not bound.");
            }

            // Initial HP from active player (if already spawned). Subsequent updates come via event.
            SyncFromActivePlayer();

            if (!_subscribed)
            {
                Bill.Events.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
                Bill.Events.Subscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
                _subscribed = true;
            }
        }

        protected override void OnPanelHidden()
        {
            if (_subscribed)
            {
                Bill.Events.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
                Bill.Events.Unsubscribe<CharacterSpawnedEvent>(OnCharacterSpawned);
                _subscribed = false;
            }
        }

        void OnPlayerDamaged(PlayerDamagedEvent e)
        {
            if (hpBarFill == null) return;
            hpBarFill.fillAmount = e.maxHP > 0f ? Mathf.Clamp01(e.currentHP / e.maxHP) : 0f;
        }

        void OnCharacterSpawned(CharacterSpawnedEvent e)
        {
            // Player just spawned (e.g. retry flow) — bind HP bar to fresh PlayerHealth.
            if (hpBarFill == null || e.player == null) return;
            var hp = e.player.Health;
            hpBarFill.fillAmount = (hp != null && hp.MaxHP > 0f) ? Mathf.Clamp01(hp.CurrentHP / hp.MaxHP) : 1f;
        }

        void SyncFromActivePlayer()
        {
            if (hpBarFill == null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // Player not spawned yet — leave fill at 1 (full) until first damage event.
                hpBarFill.fillAmount = 1f;
                return;
            }

            var health = player.GetComponent<PlayerHealth>();
            if (health == null || health.MaxHP <= 0f)
            {
                hpBarFill.fillAmount = 1f;
                return;
            }

            hpBarFill.fillAmount = Mathf.Clamp01(health.CurrentHP / health.MaxHP);
        }

        void UpdateStarVisuals(int tier)
        {
            if (starIcons == null) return;
            int clamped = Mathf.Clamp(tier, 0, starIcons.Length);
            for (int i = 0; i < starIcons.Length; i++)
                if (starIcons[i] != null) starIcons[i].enabled = (i < clamped);
        }

        void OnPauseClicked()
        {
            // TODO Sprint 4: pause overlay implementation (Time.timeScale=0, show pause UI, resume on close).
            Debug.Log("[HudPanel] Pause clicked — TODO Sprint 4");
        }
    }
}
