using System;
using System.Collections.Generic;
using UnityEngine;
using BillGameCore;
using Newtonsoft.Json.Linq;

namespace Mythfall.Localization
{
    /// <summary>
    /// JSON-based localization with nested-key flattening (e.g. ui.menu.play).
    /// Loads current language + English fallback. Persists choice via PlayerPrefs.
    /// Registered in GameBootstrap; resolved via ServiceLocator.Get&lt;LocalizationService&gt;().
    /// </summary>
    public class LocalizationService : IService, IInitializable
    {
        public const string DefaultLanguage = "vi";
        public const string FallbackLanguage = "en";
        const string ResourcePath = "Localization/lang_";
        const string PrefKey = "mythfall.language";

        Dictionary<string, string> _current;
        Dictionary<string, string> _fallback;
        string _currentLanguage;

        public string CurrentLanguage => _currentLanguage;
        public event Action<string> OnLanguageChanged;

        public void Initialize()
        {
            string saved = PlayerPrefs.GetString(PrefKey, DefaultLanguage);
            LoadLanguageInto(saved, isFallback: false);

            if (_currentLanguage != FallbackLanguage)
                LoadLanguageInto(FallbackLanguage, isFallback: true);
        }

        public void SetLanguage(string langCode)
        {
            if (langCode == _currentLanguage) return;

            LoadLanguageInto(langCode, isFallback: false);
            PlayerPrefs.SetString(PrefKey, _currentLanguage);
            PlayerPrefs.Save();

            OnLanguageChanged?.Invoke(_currentLanguage);
            Bill.Events.Fire(new LanguageChangedEvent { newLanguage = _currentLanguage });
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";

            if (_current != null && _current.TryGetValue(key, out var value))
                return value;

            if (_fallback != null && _fallback.TryGetValue(key, out var fb))
                return fb;

            Debug.LogWarning($"[Localization] Missing key: {key}");
            return $"[{key}]";
        }

        public string GetFormatted(string key, params object[] args)
        {
            string template = Get(key);
            if (args == null || args.Length == 0) return template;
            try { return string.Format(template, args); }
            catch (FormatException e)
            {
                Debug.LogWarning($"[Localization] Format error for '{key}': {e.Message}");
                return template;
            }
        }

        public bool HasKey(string key)
            => _current != null && _current.ContainsKey(key);

        // ----- internal -----

        void LoadLanguageInto(string langCode, bool isFallback)
        {
            var asset = Resources.Load<TextAsset>($"{ResourcePath}{langCode}");
            if (asset == null)
            {
                if (isFallback)
                {
                    Debug.LogError($"[Localization] Fallback '{langCode}' not found at Resources/{ResourcePath}{langCode}.json");
                    return;
                }
                Debug.LogWarning($"[Localization] '{langCode}' not found, falling back to '{FallbackLanguage}'");
                if (langCode != FallbackLanguage)
                    LoadLanguageInto(FallbackLanguage, isFallback: false);
                return;
            }

            var flat = ParseAndFlatten(asset.text);
            if (isFallback) _fallback = flat;
            else
            {
                _current = flat;
                _currentLanguage = langCode;
            }

            Debug.Log($"[Localization] Loaded {(isFallback ? "fallback" : "language")} '{langCode}' — {flat.Count} keys");
        }

        static Dictionary<string, string> ParseAndFlatten(string jsonText)
        {
            var result = new Dictionary<string, string>(64);
            var root = JObject.Parse(jsonText);
            FlattenInto(root, "", result);
            return result;
        }

        static void FlattenInto(JObject obj, string prefix, Dictionary<string, string> result)
        {
            foreach (var prop in obj.Properties())
            {
                if (prop.Name.StartsWith("_")) continue; // skip _meta and other reserved

                string key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                switch (prop.Value.Type)
                {
                    case JTokenType.Object:
                        FlattenInto((JObject)prop.Value, key, result);
                        break;
                    case JTokenType.String:
                    case JTokenType.Integer:
                    case JTokenType.Float:
                    case JTokenType.Boolean:
                        result[key] = prop.Value.ToString();
                        break;
                    // Arrays/null intentionally skipped — not a string-leaf
                }
            }
        }
    }
}
