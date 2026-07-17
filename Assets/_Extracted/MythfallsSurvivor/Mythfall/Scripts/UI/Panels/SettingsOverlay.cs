using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using Mythfall.Inventory;
using Mythfall.Localization;

namespace Mythfall.UI.Panels
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: MenuScene (could also live in GameplayScene later for pause settings)
     * Path:  Canvas (sort order ≥ MainMenuPanel canvas) → SettingsOverlay (this script)
     *
     * Why overlay (not state-driven): settings open ON TOP of MainMenuPanel without
     * hiding it. MainMenuState stays active. MainMenuPanel.SettingsButton →
     * MythfallPanelRegistry.Toggle<SettingsOverlay>(). Close button → Hide<SettingsOverlay>().
     *
     * Required hierarchy:
     *   SettingsOverlay (RectTransform full-stretch, this script + CanvasGroup, ACTIVE)
     *   ├── DimBackground (Image, full-stretch, color #000 alpha 0.6, raycastTarget = true)
     *   ├── PanelBox (Image, anchor center, size 720x720)
     *   │   ├── Title (TMP_Text + LocalizedText key="ui.settings.title", anchor top-center, y=-40)
     *   │   ├── LanguageGroup (anchor center, y=180)
     *   │   │   ├── Label (TMP_Text + LocalizedText key="ui.settings.language", anchor top-center)
     *   │   │   ├── ViButton (Button, with child Label TMP_Text + LocalizedText key="ui.settings.language_vi"
     *   │   │   │             AND a child SelectedHighlight GameObject toggled on/off)
     *   │   │   └── EnButton (Button, child Label key="ui.settings.language_en", same SelectedHighlight pattern)
     *   │   ├── MusicGroup (anchor center, y=20)
     *   │   │   ├── Label (TMP_Text + LocalizedText key="ui.settings.music_volume")
     *   │   │   └── MusicSlider (Slider component, value range 0..1)
     *   │   ├── SfxGroup (anchor center, y=-100)
     *   │   │   ├── Label (TMP_Text + LocalizedText key="ui.settings.sfx_volume")
     *   │   │   └── SfxSlider (Slider component, value range 0..1)
     *   │   └── CloseButton (Button + Image, anchor bottom-center, y=40, size 240x80)
     *   │       └── Label (TMP_Text + LocalizedText key="ui.settings.close", center)
     *
     * Components on root:
     *   - CanvasGroup (auto-added by MythfallPanelBase RequireComponent)
     *
     * Serialized fields to assign in Inspector:
     *   - viButton, viSelectedHighlight (Button, GameObject)
     *   - enButton, enSelectedHighlight (Button, GameObject)
     *   - musicSlider (Slider)
     *   - sfxSlider   (Slider)
     *   - closeButton (Button)
     *
     * Volume persistence: PlayerPrefs keys "mythfall.audio.music" / "mythfall.audio.sfx"
     * (float). On enable, slider initial value = PlayerPrefs (fallback Bill.Audio.GetVolume).
     * On change, write PlayerPrefs + Bill.Audio.SetVolume. Bill.Audio call is best-effort —
     * try/catch logs warning but slider still updates locally.
     *
     * Language: clicking ViButton/EnButton calls InventoryService.SetPreferredLanguage,
     * which persists in save AND syncs LocalizationService. All LocalizedText
     * components (including this overlay's own labels) auto-refresh via the
     * LocalizationService.OnLanguageChanged event.
     *
     * ============================================================ */

    public class SettingsOverlay : MythfallPanelBase
    {
        const string PrefMusicKey = "mythfall.audio.music";
        const string PrefSfxKey   = "mythfall.audio.sfx";

        [Header("Language")]
        [SerializeField] Button viButton;
        [SerializeField] GameObject viSelectedHighlight;
        [SerializeField] Button enButton;
        [SerializeField] GameObject enSelectedHighlight;

        [Header("Volume")]
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;

        [Header("Close")]
        [SerializeField] Button closeButton;

        LocalizationService _loc;
        bool _suppressSliderCallback;

        protected override void Awake()
        {
            base.Awake();

            if (viButton != null) viButton.onClick.AddListener(OnViClicked);
            if (enButton != null) enButton.onClick.AddListener(OnEnClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        void OnDestroy()
        {
            if (viButton != null) viButton.onClick.RemoveListener(OnViClicked);
            if (enButton != null) enButton.onClick.RemoveListener(OnEnClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

            if (_loc != null) _loc.OnLanguageChanged -= OnLanguageChanged;
        }

        protected override void OnPanelShown()
        {
            _loc = ServiceLocator.Get<LocalizationService>();
            if (_loc != null)
            {
                _loc.OnLanguageChanged -= OnLanguageChanged;
                _loc.OnLanguageChanged += OnLanguageChanged;
            }

            UpdateLanguageHighlights();
            LoadSliderValues();
        }

        protected override void OnPanelHidden()
        {
            if (_loc != null) _loc.OnLanguageChanged -= OnLanguageChanged;
        }

        // ----- language -----

        void OnViClicked() => SetLanguage(LocalizationService.DefaultLanguage); // "vi"
        void OnEnClicked() => SetLanguage(LocalizationService.FallbackLanguage); // "en"

        void SetLanguage(string lang)
        {
            var inv = ServiceLocator.Get<InventoryService>();
            if (inv != null) inv.SetPreferredLanguage(lang);
            else _loc?.SetLanguage(lang); // fallback if Inventory missing
        }

        void OnLanguageChanged(string newLang) => UpdateLanguageHighlights();

        void UpdateLanguageHighlights()
        {
            string current = _loc?.CurrentLanguage ?? LocalizationService.DefaultLanguage;
            if (viSelectedHighlight != null) viSelectedHighlight.SetActive(current == "vi");
            if (enSelectedHighlight != null) enSelectedHighlight.SetActive(current == "en");
        }

        // ----- volume -----

        void LoadSliderValues()
        {
            _suppressSliderCallback = true;
            try
            {
                if (musicSlider != null)
                {
                    float v = PlayerPrefs.GetFloat(PrefMusicKey, GetCurrentVolume(AudioChannel.Music));
                    musicSlider.SetValueWithoutNotify(v);
                    ApplyVolume(AudioChannel.Music, v);
                }
                if (sfxSlider != null)
                {
                    float v = PlayerPrefs.GetFloat(PrefSfxKey, GetCurrentVolume(AudioChannel.SFX));
                    sfxSlider.SetValueWithoutNotify(v);
                    ApplyVolume(AudioChannel.SFX, v);
                }
            }
            finally { _suppressSliderCallback = false; }
        }

        void OnMusicChanged(float v)
        {
            if (_suppressSliderCallback) return;
            PlayerPrefs.SetFloat(PrefMusicKey, v);
            PlayerPrefs.Save();
            ApplyVolume(AudioChannel.Music, v);
        }

        void OnSfxChanged(float v)
        {
            if (_suppressSliderCallback) return;
            PlayerPrefs.SetFloat(PrefSfxKey, v);
            PlayerPrefs.Save();
            ApplyVolume(AudioChannel.SFX, v);
        }

        static float GetCurrentVolume(AudioChannel channel)
        {
            try { return Bill.Audio?.GetVolume(channel) ?? 1f; }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SettingsOverlay] Bill.Audio.GetVolume({channel}) failed: {e.Message}");
                return 1f;
            }
        }

        static void ApplyVolume(AudioChannel channel, float v)
        {
            if (Bill.Audio == null) return;
            try { Bill.Audio.SetVolume(channel, v); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SettingsOverlay] Bill.Audio.SetVolume({channel}, {v:F2}) failed: {e.Message}");
            }
        }

        // ----- close -----

        void OnCloseClicked()
        {
            MythfallPanelRegistry.Hide<SettingsOverlay>();
        }
    }
}
