using GorillaRoyale.Services;
using PlayFab.ClientModels;
using Squido.JungleXRKit.Core;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Teabag.Core;
using Teabag.Services;
using UnityEngine;
using UnityEngine.Serialization;

namespace Teabag.Authentication
{
    // refactored as of 15/05/2025
    public static class ModerationUtils
    {
        private const string DEFAULT_REPORT_MESSAGE = "Breaking the rules";

        public static readonly List<string> ReportedPlayers = new List<string>();

        // TODO: Remove default once PlayFab "BadWords" title data is configured.
        private static BadWordsResult s_BadWords = new BadWordsResult
        {
            badWords = DefaultBadWords.Words
        };

        // by default, the user should always have a report after initialization
        // however if the user has already exhausted their report count, this will fail
        // when the user calls ReportAsync
        private static int s_RemainingSubmissions = 3;

        public static void InitialiseBadWords(Dictionary<string, string> dictionary)
        {
            // changed from ContainsKey to TryGetValue
            if (!dictionary.TryGetValue("BadWords", out var words))
                return;

            Debug.Log($"Bad words: {words}");
            s_BadWords = JsonUtility.FromJson<BadWordsResult>(words);
        }

        // little helper just for cases where we dont need the BadWord
        // structure
        public static bool CheckBadWord(string str) => CheckBadWordEx(str) != null;

        // In the future do so it automatically checks for 1's instead of i's, 3's instead of e's, and so on...
        // Nig is type-able, since it's in night, this should be checked later on
        public static BadWord CheckBadWordEx(string check)
        {
            if (string.IsNullOrEmpty(check))
                return null;

            Debug.Log($"Checking \"{check}\" for bad words");

            foreach (BadWord b in s_BadWords.badWords)
            {
                if (check.ToLower().Contains(b.word.ToLower()))
                {
                    Debug.Log($"Bad word detected: {check} contains {b.word}!");
                    return b;
                }
            }

            return null;
        }

        // ReSharper disable once AsyncVoidMethod
        public static async UniTaskVoid BanAsync(
            [Optional, DefaultParameterValue(1)] int hours,
            [Optional, DefaultParameterValue(DEFAULT_REPORT_MESSAGE)]
            string reason,
            [Optional, DefaultParameterValue("")] string playerId)
        {
            // If another players PlayerId is null, it'll ban yourself
            if (string.IsNullOrEmpty(playerId))
                playerId = ServiceLocator.Get<IAuthenticationService>()?.PlayFabId ?? string.Empty;

            Debug.Log("Banning: " + playerId);
            var response = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(new ExecuteCloudScriptRequest()
            {
                FunctionName = "Ban",
                FunctionParameter = new
                {
                    playerId,
                    hours,
                    reason
                }
            });

            if (response.IsError)
            {
                GameLogger.Error($"Failed to ban: '{response.Error.ErrorMessage}'");
                return;
            }

            var authManager = ServiceLocator.Get<IAuthenticationService>();
            if (authManager != null && playerId == authManager.PlayFabId)
            {
                GameLogger.Error("Banned");
                Application.Quit();
            }
        }

        private static readonly object s_ReportPlayerAsyncLock = new object();

        public static async UniTask<bool> ReportPlayerAsync(string playFabId, string reason)
        {
            if (string.IsNullOrWhiteSpace(playFabId) || string.IsNullOrWhiteSpace(reason))
                return false;
            var authMgr = ServiceLocator.Get<IAuthenticationService>();
            lock (s_ReportPlayerAsyncLock)
            {
                if (ReportedPlayers.Contains(playFabId) && (authMgr == null || playFabId != authMgr.PlayFabId))
                {
                    GameLogger.Warning("Already have reported this player");
                    return false;
                }
            }

            var result = await PlayFabAsyncClientAPI.ReportPlayerAsync(new ReportPlayerClientRequest
            {
                ReporteeId = playFabId,
                Comment = reason
            });

            if (result.IsError)
            {
                if (authMgr == null || playFabId != authMgr.PlayFabId)
                    GameLogger.Error($"Failed to report '{playFabId}': '{result.Error.ErrorMessage}'");
                return false;
            }

            if (authMgr == null || playFabId != authMgr.PlayFabId)
            {
                lock (s_ReportPlayerAsyncLock)
                {
                    s_RemainingSubmissions = result.Result.SubmissionsRemaining;
                    ReportedPlayers.Add(playFabId);
                }

                GameLogger.Info(
                    $"Successfully reported '{playFabId}' for '{reason}' (Remaining: {s_RemainingSubmissions})");
            }

            return true;
        }
    }

    [System.Serializable]
    public class BadWordsResult
    {
        [FormerlySerializedAs("BadWords")]
        public BadWord[] badWords = new BadWord[0];
    }

    [System.Serializable]
    public class BadWord
    {
        [FormerlySerializedAs("Word")]
        public string word;

        [FormerlySerializedAs("Severity")]
        public int severity;

        public BadWord(string w, int s)
        {
            word = w;
            severity = s;
        }
    }
}
