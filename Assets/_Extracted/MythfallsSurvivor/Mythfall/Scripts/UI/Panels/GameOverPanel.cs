using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using Mythfall.States;

namespace Mythfall.UI.Panels
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: GameplayScene (overlay on top of HudPanel — higher Canvas sort order recommended,
     *        OR same canvas with later sibling index. DefeatState handles Time.timeScale slow-mo.)
     * Path:  Canvas → GameOverPanel (this GameObject, this script attached)
     *
     * Required hierarchy:
     *   GameOverPanel (RectTransform full-stretch, this script + CanvasGroup, GameObject ACTIVE)
     *   ├── DimBackground (Image, full-stretch, color #000000 alpha 0.7, raycastTarget = true)
     *   ├── Title (TMP_Text + LocalizedText key="ui.game_over.title_defeat",
     *   │          anchor center, y=160, large red font)
     *   ├── Subtitle (TMP_Text + LocalizedText key="ui.game_over.subtitle_defeat",
     *   │             anchor center, y=80, smaller font)
     *   ├── RetryButton (Button + Image, anchor center, y=-40, size 320x80)
     *   │   └── Label (TMP_Text + LocalizedText key="ui.game_over.btn_retry", center)
     *   └── HubButton (Button + Image, anchor center, y=-140, size 320x80)
     *       └── Label (TMP_Text + LocalizedText key="ui.game_over.btn_hub", center)
     *
     * Components on root:
     *   - CanvasGroup (auto-added by MythfallPanelBase RequireComponent)
     *
     * Serialized fields to assign in Inspector:
     *   - retryButton (Button)
     *   - hubButton   (Button)
     *
     * Stats display (time/wave/kills/level — Sprint 4 polish):
     *   Add a stats panel between Subtitle and RetryButton when RunStatsTracker lands.
     *   Day 3 ships without stats — keep the layout slot free for future fields.
     *
     * State flow:
     *   - DefeatState.Enter shows this panel + sets Time.timeScale = 0.3f.
     *   - Retry → Bill.State.GoTo<InRunState> → Bill.Scene.Load("GameplayScene", Fade).
     *     Scene reload fully respawns player + clears enemies. State change first so
     *     HudPanel auto-shows on the new scene's panel registration.
     *   - Hub → Bill.State.GoTo<MainMenuState> → Bill.Scene.Load("MenuScene", Fade).
     *
     * ============================================================ */

    public class GameOverPanel : MythfallPanelBase
    {
        [SerializeField] Button retryButton;
        [SerializeField] Button hubButton;

        protected override void Awake()
        {
            base.Awake();

            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            else Debug.LogError($"[{name}] retryButton not assigned.", this);

            if (hubButton != null) hubButton.onClick.AddListener(OnHubClicked);
            else Debug.LogError($"[{name}] hubButton not assigned.", this);
        }

        void OnDestroy()
        {
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
            if (hubButton != null) hubButton.onClick.RemoveListener(OnHubClicked);
        }

        void OnRetryClicked()
        {
            Debug.Log("[GameOverPanel] Retry → InRunState + reload GameplayScene");
            // DefeatState.Exit restores Time.timeScale = 1.
            Bill.State.GoTo<InRunState>();
            Bill.Scene.Load("GameplayScene", TransitionType.Fade, 0.5f);
        }

        void OnHubClicked()
        {
            Debug.Log("[GameOverPanel] Hub → MainMenuState + load MenuScene");
            Bill.State.GoTo<MainMenuState>();
            Bill.Scene.Load("MenuScene", TransitionType.Fade, 0.5f);
        }
    }
}
