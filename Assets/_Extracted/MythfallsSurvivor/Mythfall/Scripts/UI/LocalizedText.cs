using TMPro;
using UnityEngine;
using BillGameCore;
using Mythfall.Localization;

namespace Mythfall.UI
{
    /// <summary>
    /// Binds a TMP_Text to a localization key. Auto-refreshes on language change.
    /// Drop on any GameObject with TMP_Text component, set localizationKey in inspector.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string localizationKey;
        [Tooltip("Optional format args (string.Format). Leave empty if not a format template.")]
        [SerializeField] string[] formatArgs;

        TMP_Text _text;
        LocalizationService _localization;

        void Awake() => _text = GetComponent<TMP_Text>();

        void OnEnable()
        {
            _localization = ServiceLocator.Get<LocalizationService>();
            if (_localization != null)
            {
                _localization.OnLanguageChanged += OnLanguageChanged;
                Refresh();
            }
            else
            {
                // Service not ready yet — wait for GameReadyEvent
                Bill.Events?.SubscribeOnce<GameReadyEvent>(OnGameReady);
            }
        }

        void OnDisable()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= OnLanguageChanged;
        }

        void OnGameReady(GameReadyEvent _)
        {
            _localization = ServiceLocator.Get<LocalizationService>();
            if (_localization != null)
            {
                _localization.OnLanguageChanged += OnLanguageChanged;
                Refresh();
            }
        }

        void OnLanguageChanged(string _) => Refresh();

        public void SetKey(string key)
        {
            localizationKey = key;
            Refresh();
        }

        public void SetFormatArgs(params string[] args)
        {
            formatArgs = args;
            Refresh();
        }

        void Refresh()
        {
            if (_text == null || string.IsNullOrEmpty(localizationKey) || _localization == null)
                return;

            if (formatArgs != null && formatArgs.Length > 0)
                _text.text = _localization.GetFormatted(localizationKey, formatArgs);
            else
                _text.text = _localization.Get(localizationKey);
        }

        void OnValidate()
        {
            if (Application.isPlaying) return;
            if (_text == null) _text = GetComponent<TMP_Text>();
            if (_text != null && !string.IsNullOrEmpty(localizationKey))
                _text.text = $"[{localizationKey}]";
        }
    }
}
