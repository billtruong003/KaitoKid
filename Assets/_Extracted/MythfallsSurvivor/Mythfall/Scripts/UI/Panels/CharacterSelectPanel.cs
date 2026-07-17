using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using Mythfall.Characters;
using Mythfall.Events;
using Mythfall.Inventory;
using Mythfall.States;

namespace Mythfall.UI.Panels
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: MenuScene
     * Path:  Canvas → CharacterSelectPanel (this GameObject, this script attached)
     *
     * Required hierarchy:
     *   CharacterSelectPanel (RectTransform full-stretch, this script + CanvasGroup, GameObject ACTIVE)
     *   ├── Background (Image, full-stretch, color #0F1218 alpha 0.95, raycastTarget = true)
     *   ├── Title (TMP_Text + LocalizedText key="ui.character_select.title", anchor top-center, y=-100)
     *   ├── CardRow (HorizontalLayoutGroup, anchor center, size 1400x600, spacing=80, child alignment Middle Center)
     *   │   ├── KaiCard (Button + Image bg, size 380x520) — drag this Button into cards[0].selectButton
     *   │   │   ├── SelectedHighlight (GameObject — Image with bright border sprite, INACTIVE by default)
     *   │   │   ├── Portrait (Image, anchor top-center, size 320x320)
     *   │   │   ├── NameText (TMP_Text + LocalizedText key="character.kai.name", anchor center, y=-180)
     *   │   │   ├── TitleText (TMP_Text + LocalizedText key="character.kai.title", anchor center, y=-230, smaller font)
     *   │   │   └── StarRow (HorizontalLayoutGroup anchor bottom-center, y=40, spacing 4)
     *   │   │       └── Star_1..Star_6 (Image, gold star sprite, size 32x32) — drag Image components into cards[0].starIcons
     *   │   └── LyraCard (same structure as KaiCard, with character.lyra.name + character.lyra.title keys)
     *   ├── ConfirmButton (Button + Image, anchor bottom-center, y=80, size 320x80)
     *   │   └── Label (TMP_Text + LocalizedText key="ui.character_select.enter_stage", center)
     *   └── BackButton (Button + Image, anchor top-left, margin 24,24, size 80x80)
     *       └── Label (TMP_Text + LocalizedText key="ui.character_select.back", center)
     *
     * Optional: 2-3 LockedCard slots in CardRow showing "???" + lock icon (key "ui.character_select.locked").
     * Per Sprint doc Definition of Done. Wire as cards entries with data=null + interactable=false.
     *
     * Serialized fields to assign in Inspector:
     *   - cards (size 2):
     *       [0] data = Kai_Data (Resources/Characters/Kai_Data),
     *           selectButton = KaiCard Button,
     *           selectedHighlight = KaiCard/SelectedHighlight,
     *           starIcons = drag the 6 Star_N Image components in order
     *       [1] data = Lyra_Data, ... same fields for LyraCard
     *   - confirmButton (Button) → ConfirmButton
     *   - backButton (Button) → BackButton
     *
     * Star display: starIcons array can hold up to 6 entries. Cards with starTier=4
     * (Kai/Lyra default) will visible-toggle starIcons[0..3], hide [4..5].
     *
     * State flow:
     *   - CharacterSelectState.Enter shows this panel via MythfallPanelRegistry.
     *   - Card click → highlight selection locally (no commit yet).
     *   - ConfirmButton click → InventoryService.SetCurrentCharacter + fire
     *     CharacterSelectedEvent → Bill.State.GoTo<InRunState> → Bill.Scene.Load("GameplayScene").
     *   - BackButton click → Bill.State.GoTo<MainMenuState>.
     *
     * ============================================================ */

    public class CharacterSelectPanel : MythfallPanelBase
    {
        [System.Serializable]
        public class CharacterCardWiring
        {
            public CharacterDataSO data;
            public Button selectButton;
            public GameObject selectedHighlight;
            public Image[] starIcons;
        }

        [Header("Cards (Kai = [0], Lyra = [1])")]
        [SerializeField] CharacterCardWiring[] cards;

        [Header("Action Buttons")]
        [SerializeField] Button confirmButton;
        [SerializeField] Button backButton;

        string _selectedCharacterId = "kai";

        protected override void Awake()
        {
            base.Awake();

            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    var card = cards[i];
                    if (card == null || card.selectButton == null || card.data == null) continue;
                    string id = card.data.characterId;
                    card.selectButton.onClick.AddListener(() => OnCardSelected(id));
                }
            }

            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            else Debug.LogError($"[{name}] confirmButton not assigned.", this);

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            else Debug.LogWarning($"[{name}] backButton not assigned — flow back to menu unreachable.", this);
        }

        void OnDestroy()
        {
            if (cards != null)
            {
                foreach (var card in cards)
                    if (card != null && card.selectButton != null)
                        card.selectButton.onClick.RemoveAllListeners();
            }
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        }

        protected override void OnPanelShown()
        {
            // Pre-select last-saved character so user re-entering CharacterSelect sees their previous pick.
            var inv = ServiceLocator.Get<InventoryService>();
            _selectedCharacterId = inv?.Data?.currentCharacterId ?? "kai";

            UpdateSelectionVisuals();
            UpdateStarVisuals();
        }

        void OnCardSelected(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_selectedCharacterId == id) return;
            _selectedCharacterId = id;
            UpdateSelectionVisuals();
            Debug.Log($"[CharacterSelectPanel] Selected: {id}");
        }

        void OnConfirmClicked()
        {
            var inv = ServiceLocator.Get<InventoryService>();
            if (inv == null)
            {
                Debug.LogError("[CharacterSelectPanel] InventoryService missing — cannot confirm.");
                return;
            }
            inv.SetCurrentCharacter(_selectedCharacterId);
            Bill.Events.Fire(new CharacterSelectedEvent { characterId = _selectedCharacterId });

            Debug.Log($"[CharacterSelectPanel] Confirmed '{_selectedCharacterId}' → InRunState + load GameplayScene");

            // Order: state first, scene second. Setting desiredVisible[HudPanel]=true before scene
            // load means the new scene's HudPanel auto-shows on its OnEnable register.
            Bill.State.GoTo<InRunState>();
            Bill.Scene.Load("GameplayScene", TransitionType.Fade, 0.5f);
        }

        void OnBackClicked()
        {
            Debug.Log("[CharacterSelectPanel] Back → MainMenuState");
            Bill.State.GoTo<MainMenuState>();
        }

        // ----- visuals -----

        void UpdateSelectionVisuals()
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card == null || card.selectedHighlight == null || card.data == null) continue;
                bool isSelected = card.data.characterId == _selectedCharacterId;
                card.selectedHighlight.SetActive(isSelected);
            }
        }

        void UpdateStarVisuals()
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card == null || card.starIcons == null || card.data == null) continue;
                int tier = Mathf.Clamp(card.data.starTier, 0, card.starIcons.Length);
                for (int i = 0; i < card.starIcons.Length; i++)
                {
                    if (card.starIcons[i] != null) card.starIcons[i].enabled = (i < tier);
                }
            }
        }
    }
}
