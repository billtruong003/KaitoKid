using System.Collections.Generic;

using Teabag.Authentication;
using PlayFab.ClientModels;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;
using UnityEngine;

public class WinRequirement : MonoBehaviour
{
    public int wins;
    public int requriedWins;
    public List<GameObject> gameObjects;
    private IAuthenticationService _authenticationService;

    public void Awake()
    {
        _authenticationService = ServiceLocator.Get<IAuthenticationService>();

        if (_authenticationService.LoggedIn)
        {
            GetWins();
        }
        else
        {
            _authenticationService.OnLogin += GetWins;
        }
    }

    private void OnDestroy()
    {
        if (_authenticationService != null)
            _authenticationService.OnLogin -= GetWins;
    }

    public async void GetWins()
    {
        var leaderboardResult = await PlayFabAsyncClientAPI.GetLeaderboardAroundPlayerAsync(new GetLeaderboardAroundPlayerRequest()
        {
            StatisticName = "BattleRoyaleWins",
            PlayFabId = _authenticationService.PlayFabId,
            MaxResultsCount = 1
        });

        if (leaderboardResult.IsError)
            return;

        if (leaderboardResult.Result.Leaderboard.Count < 0)
            return;

        wins = leaderboardResult.Result.Leaderboard[0].StatValue;
    }

    public void Update()
    {
        foreach (GameObject go in gameObjects)
        {
            go.SetActive(wins >= requriedWins);
        }
    }
}
