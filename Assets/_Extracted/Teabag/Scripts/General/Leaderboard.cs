using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab.ClientModels;
using Squido.JungleXRKit.Core;
using Teabag.Authentication;
using Teabag.Core;
using Teabag.Services;

public class Leaderboard : MonoBehaviour
{
    public string title;
    public string leaderboardName;
    TextMeshProUGUI text;
    private IAuthenticationService _authenticationService;
    private void Awake()
    {
        _authenticationService = ServiceLocator.Get<IAuthenticationService>();
        if (text == null)
        {
            GameLogger.Warning(this, "No text was associated with this leaderboard, auto setting component");
            text = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        if (_authenticationService.LoggedIn)
        {
            GameLogger.Info(this, "Leaderboard is loading");
            GetLeaderboardAsync();
        }
    }

    public async UniTaskVoid GetLeaderboardAsync()
    {
        text.text = "<size=80><b>Loading leaderboard</b></size>";
        var leaderboardResult = await PlayFabAsyncClientAPI.GetLeaderboardAsync(
            new GetLeaderboardRequest()
            {
                StatisticName = leaderboardName,
                MaxResultsCount = 10
            });

        if (leaderboardResult.IsError)
        {
            GameLogger.Error(this, $"Failed to get leaderboard of key '{leaderboardName}'");
            text.text += $"\n<color=red>ERROR: {leaderboardResult.Error.ErrorMessage}</color>";
            return;
        }
        LoadLeaderboard(leaderboardResult.Result.Leaderboard);
    }

    public async UniTaskVoid GetLeaderboardAroundPlayerAsync()
    {
        text.text = "<size=80><b>Loading leaderboard</b></size>";
        var leaderboardResult = await PlayFabAsyncClientAPI.GetLeaderboardAroundPlayerAsync(
            new GetLeaderboardAroundPlayerRequest()
            {
                StatisticName = leaderboardName,
                MaxResultsCount = 10
            });

        if (leaderboardResult.IsError)
        {
            GameLogger.Error(this, $"Failed to get leaderboard of key '{leaderboardName}'");
            text.text += $"\n<color=red>ERROR: {leaderboardResult.Error.ErrorMessage}</color>";
            return;
        }
        LoadLeaderboard(leaderboardResult.Result.Leaderboard);
    }

    public void LoadLeaderboard(List<PlayerLeaderboardEntry> entries)
    {
        text.text = $"<size=100><b>{title}</b></size>";
        foreach (PlayerLeaderboardEntry entry in entries)
        {
            string beginning = $"{entry.Position + 1}.";
            if (entry.Position + 1 != 4) beginning += " ";

            string line = $"{CreateSpaces(4, beginning)} | {CreateSpaces(16, entry.DisplayName)} | {entry.StatValue}";
            if (entry.PlayFabId == ServiceLocator.Get<IAuthenticationService>().PlayFabId)
                line = $"<color=green>{line}</color>";

            text.text += $"\n{line}";
        }
    }

    private string CreateSpaces(int amount, string str)
        => str + new string(' ', Mathf.Clamp(amount - str.Length, 0, int.MaxValue));
}
