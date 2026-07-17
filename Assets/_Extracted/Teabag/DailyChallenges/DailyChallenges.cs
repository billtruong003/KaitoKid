using Teabag.Authentication;
using Teabag.GameMode;
using Teabag.Networking;
using Teabag.Player.Rig;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Teabag.Core;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Public;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Player;
using Teabag.Services;
using Teabag.UI;

public static class DailyChallenges
{
    static DailyChallenges()
    {
        GameServices.ScoreChallengeAsync = async (type) => await ScoreAsync((ChallengeType)type);
    }

    public static DailyChallengesDay day = new DailyChallengesDay();
    public static List<DailyChallenge> challenges
    {
        get => day.Challenges;
        set => day.Challenges = value;
    }
    public static bool loading;

    public static async UniTask<int> ClaimAsync(ChallengeType challenge)
    {
        if (loading) return 0;
        loading = true;

        var response = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(
            new ExecuteCloudScriptRequest()
            {
                FunctionName = "ClaimDailyChallenge",
                FunctionParameter = new { challenge = (int)challenge }
            });

        loading = false;
        if (response.IsError)
        {
            GameLogger.Error("Failed to claim daily challenge");
            return 0;
        }
        DailyChallengesDay challengeDay = JsonUtility.FromJson<DailyChallengesDay>(response.Result.FunctionResult.ToString());

        int result = 0;
        for (int i = 0; i < challengeDay.Challenges.Count && i < challenges.Count; i++)
        {
            DailyChallenge now = challengeDay.Challenges[i];
            DailyChallenge old = challenges[i];
            if (now.Claimed && !old.Claimed)
                result += now.Reward;
        }

        day = challengeDay;
        AuthenticationUtils.currency += result;

        GameObject.FindObjectOfType<MapBoard>()
            .OpenScreen("ATM");
        return result;
    }

    public static async UniTask ScoreAsync(ChallengeType challenge)
    {
        if (loading) return;
        var authManager = ServiceLocator.Get<IAuthenticationService>();
        if (authManager == null || !authManager.LoggedIn || authManager.IsError)
        {
            GameLogger.Error("Failed to increment score -- authentication error");
            return;
        }

        MapType map = MapType.None;
        var networkManager = ServiceLocator.Get<INetworkManager>();
        if (networkManager.IsBattleRoyale)
            map = MapType.BattleRoyale;

        DailyChallenge c = GetDailyChallenge(challenge);
        if (c == null || c.Map != MapType.None && c.Map != map || c.Score >= c.Amount || c.Claimed)
            return;

        loading = true;
        var response = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(
            new ExecuteCloudScriptRequest()
            {
                FunctionName = "ScoreDailyChallenge",
                FunctionParameter = new
                {
                    challenge = (int)challenge,
                    map = (int)map
                }
            });

        loading = false;
        if (response.IsError)
        {
            GameLogger.Error("Failed to score daily challenge");
            return;
        }

        GameLogger.Info($"Score result: '{response.Result.FunctionResult}'");
        day = JsonUtility.FromJson<DailyChallengesDay>(response.Result.FunctionResult.ToString());
    }

    public static async UniTask<List<DailyChallenge>> GetDailyChallengesAsync()
    {
        GameLogger.Debug("getting daily challenges");
        var response = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(
            new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetDailyChallenges"
            });

        if (response.IsError)
        {
            GameLogger.Error("Failed to get daily challenges");
            return null;
        }

        day = JsonUtility.FromJson<DailyChallengesDay>(response.Result.FunctionResult.ToString());
        return challenges;
    }

    public static DailyChallenge GetDailyChallenge(ChallengeType challenge)
    {
        foreach (DailyChallenge c in challenges)
        {
            if (c.Challenge == challenge)
                return c;
        }

        return null;
    }

    [Serializable]
    public class DailyChallengesDay
    {
        public DateTime Date;
        public List<DailyChallenge> Challenges = new List<DailyChallenge>();
    }
}

[Serializable]
public class DailyChallenge
{
    public ChallengeType Challenge;
    public MapType Map;
    public int Amount;
    public int Score;
    public int Reward;
    public bool Claimed;

    public override string ToString()
    {
        string s = "";
        if (Amount != 1)
            s = "s";

        string mapText = "";
        switch (Map)
        {
            case MapType.BattleRoyale:
                mapText = "in Battle Royale";
                break;
            case MapType.Deathmatch:
                mapText = "in Deathmatch";
                break;
        }

        string text = "None";

        switch (Challenge)
        {
            case ChallengeType.Login:
                text = $"Login {Amount} time{s} today";
                break;
            case ChallengeType.Play:
                text = $"Play for {Amount} minute{s}";
                break;
            case ChallengeType.Kill:
                text = $"Get {Amount} kill{s}";
                break;
            case ChallengeType.Win:
                text = $"Get {Amount} win{s}";
                break;
            case ChallengeType.HitDistance:
                text = $"Get {Amount} hit{s} from 50 meters away";
                break;
            case ChallengeType.KillGrenade:
                text = $"Get {Amount} grenade kill{s}";
                break;
            case ChallengeType.Game:
                text = $"Play {Amount} game{s}";
                break;
        }

        return $"{text} {mapText}".ToUpper();
    }

    public DailyChallenge(ChallengeType type, MapType map, int amount, int reward)
    {
        Challenge = type;
        Map = map;
        Amount = amount;
        Reward = reward;
    }
}

public enum ChallengeType
{
    None = -1,
    Login,
    Play,
    Kill,
    Win,
    HitDistance,
    KillGrenade,
    Game
}

public enum MapType
{
    None = -1,
    BattleRoyale,
    Deathmatch
}
