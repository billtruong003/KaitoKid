using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using Mythfall.States;

namespace Mythfall.UI.Panels
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: MenuScene
     * Path:  Canvas (Screen Space - Overlay) → MainMenuPanel (this GameObject, this script attached)
     *
     * Required hierarchy:
     *   MainMenuPanel (RectTransform full-stretch, this script, GameObject ACTIVE — visibility via CanvasGroup, NOT GameObject.SetActive)
     *   ├── Background (Image, color #1A1A28, full-stretch, raycastTarget = false)
     *   ├── Title (TMP_Text + LocalizedText key="ui.menu.title", anchor top-center, y=-160, size 1100x140)
     *   ├── PlayButton (Button + Image, anchor center, size 360x96, position y=-40)
     *   │   └── Label (TMP_Text + LocalizedText key="ui.menu.play", center anchor, full-stretch)
     *   ├── SettingsButton (Button + Image, anchor top-right, size 80x80, margin 24,24)
     *   │   └── Icon (Image, settings gear sprite OR TMP_Text + LocalizedText key="ui.menu.settings")
     *   └── LockedFeatureRow (HorizontalLayoutGroup, anchor bottom-center, y=120, size 1200x140, spacing=24)
     *       ├── GachaButton    (Button interactable=false, child Label TMP_Text + LocalizedText key="ui.menu.gacha")
     *       ├── InventoryButton (Button interactable=false, child Label TMP_Text + LocalizedText key="ui.menu.inventory")
     *       ├── ShopButton     (Button interactable=false, child Label TMP_Text + LocalizedText key="ui.menu.shop")
     *       └── BattlePassButton (Button interactable=false, child Label TMP_Text + LocalizedText key="ui.menu.battle_pass")
     *
     * Serialized fields to assign in Inspector:
     *   - playButton           (Button) → drag PlayButton GameObject
     *   - settingsButton       (Button) → drag SettingsButton GameObject
     *
     * Locked buttons are interactable=false in their Button component — no script
     * reference required. "Coming Soon" tooltip is Sprint 4 polish.
     *
     * Scene requirements:
     *   - Canvas: Render Mode = Screen Space - Overlay; Pixel Perfect = false
     *   - Canvas Scaler: UI Scale Mode = Scale With Screen Size; Reference Resolution 1920x1080
     *   - GraphicRaycaster on Canvas (auto-added)
     *   - EventSystem GameObject must exist in scene (auto-added with first Canvas)
     *
     * State flow:
     *   - MainMenuState.Enter shows this panel via MythfallPanelRegistry
     *   - PlayButton click → Bill.State.GoTo<CharacterSelectState>()
     *   - SettingsButton click → Toggle SettingsOverlay panel (overlay sits at higher sort order)
     *
     * ============================================================ */

    public class MainMenuPanel : MythfallPanelBase
    {
        [Header("Buttons (UGUI)")]
        [SerializeField] Button playButton;
        [SerializeField] Button settingsButton;

        void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            else Debug.LogError($"[{name}] playButton not assigned.", this);

            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            else Debug.LogWarning($"[{name}] settingsButton not assigned — settings overlay unreachable.", this);
        }

        void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        void OnPlayClicked()
        {
            Debug.Log("[MainMenuPanel] Play → CharacterSelectState");
            Bill.State.GoTo<CharacterSelectState>();
        }

        void OnSettingsClicked()
        {
            Debug.Log("[MainMenuPanel] Settings → toggle SettingsOverlay");
            MythfallPanelRegistry.Toggle<SettingsOverlay>();
        }
    }
}
