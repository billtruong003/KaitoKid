using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using PlayFab.ClientModels;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;

namespace Teabag.Economy
{
    /// <summary>
    /// Concrete implementation of <see cref="IBundleService"/>.
    /// Reads bundle expiry dates from PlayFab TitleData (key "BundleTimers")
    /// and the player's first-login timestamp from PlayFab UserData
    /// (key "FirstLoginTimestamp").
    ///
    /// All time calculations use <see cref="SyncedTime.ServerTime"/> to
    /// prevent client-side clock manipulation.
    /// </summary>
    public sealed class BundleService : IBundleService
    {
        private const string TITLE_DATA_KEY = "BundleTimers";
        private const string USER_DATA_KEY = "FirstLoginTimestamp";

        private bool _isInitialized;
        private bool _isInitializing;

        /// <summary>Cached remote expiry dates keyed by bundle ID.</summary>
        private readonly Dictionary<string, DateTime> _remoteExpiryCache = new Dictionary<string, DateTime>();

        /// <summary>Cached first-login timestamp for the current player (null = not a first-time user or not yet fetched).</summary>
        private DateTime? _firstLoginTimestamp;

        // ── IBundleTimerService ─────────────────────────────────────────────

        public bool IsInitialized => _isInitialized;

        public async UniTask InitializeAsync()
        {
            if (_isInitialized || _isInitializing)
                return;

            _isInitializing = true;

            try
            {
                await FetchRemoteExpiryDatesAsync();
                await FetchFirstLoginTimestampAsync();
                _isInitialized = true;
                GameLogger.Info("[BundleService] Initialized successfully.");
            }
            catch (Exception ex)
            {
                GameLogger.Error($"[BundleService] InitializeAsync failed: {ex.Message}");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public TimeSpan GetRemainingTime(BundleConfig config)
        {
            if (config == null)
                return TimeSpan.MinValue;

            DateTime now = SyncedTime.ServerTime;

            if (config.IsFirstTimeUserBundle)
            {
                if (!_firstLoginTimestamp.HasValue)
                    return TimeSpan.MinValue;

                DateTime expiry = _firstLoginTimestamp.Value.AddDays(config.FirstTimeUserDurationDays);
                return expiry - now;
            }

            DateTime expiryDate = GetExpiryDate(config);
            if (expiryDate == DateTime.MinValue)
                return TimeSpan.MinValue;

            return expiryDate - now;
        }

        public bool IsBundleVisible(BundleConfig config)
        {
            if (config == null)
                return false;

            if (!_isInitialized)
                return false;

            TimeSpan remaining = GetRemainingTime(config);
            return remaining.TotalSeconds > 0;
        }

        // ── Private Helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Resolves the expiry date for a bundle, preferring remote TitleData
        /// when the config says to use it, otherwise falling back to the
        /// inspector-configured date.
        /// </summary>
        private DateTime GetExpiryDate(BundleConfig config)
        {
            if (config.UseRemoteExpiry)
            {
                if (_remoteExpiryCache.TryGetValue(config.BundleId, out DateTime remote))
                    return remote;

                // Remote was requested but not found — bundle stays hidden.
                GameLogger.Warning($"[BundleService] No remote expiry found for bundle '{config.BundleId}'.");
                return DateTime.MinValue;
            }

            return config.ExpiryDate != null ? config.ExpiryDate.dateTime : DateTime.MinValue;
        }

        /// <summary>
        /// Reads the "BundleTimers" key from TitleData (already cached in
        /// <see cref="AuthenticationUtils.titleData"/> after login).
        /// Expected JSON format:
        /// <code>
        /// {
        ///   "starter_pack": "2026-05-01T00:00:00Z",
        ///   "holiday_bundle": "2026-04-30T12:00:00Z"
        /// }
        /// </code>
        /// </summary>
        private UniTask FetchRemoteExpiryDatesAsync()
        {
#if UNITY_EDITOR
            // TODO: [BundleService] Remove this mock block once PlayFab TitleData is fully configured.
            _remoteExpiryCache["mock_bundle_1"] = SyncedTime.ServerTime.AddMinutes(1); // Expires in 1 minutes
            GameLogger.Info("[BundleService] UNITY_EDITOR: Using MOCK remote expiry data.");
            return UniTask.CompletedTask;
#endif

            _remoteExpiryCache.Clear();

            // TitleData is already fetched during the auth flow and stored here.
            if (AuthenticationUtils.titleData == null ||
                !AuthenticationUtils.titleData.TryGetValue(TITLE_DATA_KEY, out string json))
            {
                GameLogger.Info("[BundleService] No TitleData key 'BundleTimers' found — skipping remote expiry.");
                return UniTask.CompletedTask;
            }

            try
            {
                // Lightweight JSON parsing without pulling in Newtonsoft.
                // The format is a flat dict: { "key": "ISO8601", ... }
                Dictionary<string, string> entries = ParseSimpleJsonDict(json);
                foreach (var kvp in entries)
                {
                    if (DateTime.TryParse(kvp.Value, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                            out DateTime parsed))
                    {
                        _remoteExpiryCache[kvp.Key] = parsed;
                    }
                    else
                    {
                        GameLogger.Warning($"[BundleService] Could not parse date for bundle '{kvp.Key}': '{kvp.Value}'");
                    }
                }

                GameLogger.Info($"[BundleService] Loaded {_remoteExpiryCache.Count} remote expiry date(s).");
            }
            catch (Exception ex)
            {
                GameLogger.Error($"[BundleService] Failed to parse BundleTimers TitleData: {ex.Message}");
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Reads the player's "FirstLoginTimestamp" from PlayFab UserData.
        /// If the key does not exist, this is a first-time user — the current
        /// server time is written as their first-login timestamp.
        /// </summary>
        private async UniTask FetchFirstLoginTimestampAsync()
        {
#if UNITY_EDITOR
            // TODO: [BundleService] Remove this mock block once PlayFab UserData is fully configured.
            // Simulate that the user logged in for the first time 2 day and 59 minute ago.
            // Change AddDays(-1) to AddDays(-5) to test the bundle expiring!
            _firstLoginTimestamp = SyncedTime.ServerTime.AddDays(-2).AddMinutes(-59);
            PlayerData.firstLoginTimestamp = _firstLoginTimestamp;
            GameLogger.Info($"[BundleService] UNITY_EDITOR: Using MOCK first login timestamp: {_firstLoginTimestamp}");
            return;
#endif

            try
            {
                var request = new GetUserDataRequest
                {
                    Keys = new List<string> { USER_DATA_KEY }
                };

                var result = await PlayFabAsyncClientAPI.GetUserDataAsync(request);
                if (result.IsError)
                {
                    GameLogger.Error($"[BundleService] Failed to get UserData: {result.Error.ErrorMessage}");
                    return;
                }

                if (result.Result.Data != null &&
                    result.Result.Data.TryGetValue(USER_DATA_KEY, out UserDataRecord record) &&
                    !string.IsNullOrEmpty(record.Value))
                {
                    // Existing player — parse their stored timestamp.
                    if (DateTime.TryParse(record.Value, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                            out DateTime parsed))
                    {
                        _firstLoginTimestamp = parsed;
                        PlayerData.firstLoginTimestamp = parsed;
                        GameLogger.Info($"[BundleService] Existing player. First login: {parsed:O}");
                    }
                    else
                    {
                        GameLogger.Warning($"[BundleService] Stored FirstLoginTimestamp is malformed: '{record.Value}'");
                    }
                }
                else
                {
                    // First-time user — write their timestamp to PlayFab.
                    DateTime now = SyncedTime.ServerTime;
                    string isoNow = now.ToString("O", CultureInfo.InvariantCulture);

                    var writeRequest = new UpdateUserDataRequest
                    {
                        Data = new Dictionary<string, string>
                        {
                            { USER_DATA_KEY, isoNow }
                        }
                    };

                    var writeResult = await PlayFabAsyncClientAPI.UpdateUserDataAsync(writeRequest);
                    if (writeResult.IsError)
                    {
                        GameLogger.Error($"[BundleService] Failed to write FirstLoginTimestamp: {writeResult.Error.ErrorMessage}");
                        return;
                    }

                    _firstLoginTimestamp = now;
                    PlayerData.firstLoginTimestamp = now;
                    GameLogger.Info($"[BundleService] First-time user detected. Wrote timestamp: {isoNow}");
                }
            }
            catch (Exception ex)
            {
                GameLogger.Error($"[BundleService] FetchFirstLoginTimestampAsync failed: {ex.Message}");
            }
        }

        // ── Minimal JSON Dict Parser ────────────────────────────────────────

        /// <summary>
        /// Parses a flat JSON object into a string→string dictionary.
        /// Supports only the format: { "key": "value", ... }
        /// This avoids pulling in a full JSON library for a simple use case.
        /// </summary>
        private static Dictionary<string, string> ParseSimpleJsonDict(string json)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(json))
                return dict;

            // Strip outer braces
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            string[] pairs = json.Split(',');
            foreach (string pair in pairs)
            {
                int colonIndex = pair.IndexOf(':');
                if (colonIndex < 0) continue;

                string key = pair.Substring(0, colonIndex).Trim().Trim('"');
                string value = pair.Substring(colonIndex + 1).Trim().Trim('"');

                if (!string.IsNullOrEmpty(key))
                    dict[key] = value;
            }

            return dict;
        }
    }
}
