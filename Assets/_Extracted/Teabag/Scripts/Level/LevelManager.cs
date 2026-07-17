using System;
using Teabag.Authentication;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Teabag.Core;
using UnityEngine;
using Newtonsoft.Json;
using Squido.JungleXRKit.Core;
using Teabag.Services;
using Teabag.UI;

namespace Teabag.Progression
{
    public class LevelManager : MonoBehaviour
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public static int CurrentLevel => s_obsoleteLevel;
        public static int CurrentXp => s_obsoleteXp;
        public static LevelProgressionData LevelProgressionData => s_levelProgressionData;

        [Obsolete("Use Level instead.")] public static int s_obsoleteLevel = 0;
        [Obsolete("Use Xp instead.")] public static int s_obsoleteXp = 0;

        [Obsolete("Use LevelProgressionData instead.")]
        public static LevelProgressionData s_levelProgressionData;

        public static async UniTask GetLevelAsync()
        {
            GorillaUser user = await AuthenticationUtils.GetGorillaUserAsync(ServiceLocator.Get<IAuthenticationService>().PlayFabId);
            if (user == null)
            {
                GameLogger.Warning("GetLevelAsync: Failed to get user data, using defaults");
                return;
            }
            s_obsoleteLevel = user.Level;
            s_obsoleteXp = user.Xp;
            LevelUpUI.instance?.Initialise(s_obsoleteLevel, s_obsoleteXp);
        }

        public static void SetLevelProgressionData(GetTitleDataResult titleData)
        {
            if (titleData.Data.TryGetValue("LevelProgression", out string json))
            {
                s_levelProgressionData = JsonConvert.DeserializeObject<LevelProgressionData>(json);
                Debug.Log(json);
                Debug.Log("New length: " + s_levelProgressionData.LevelProgression.Count);
            }
            else GameLogger.Error("Failed to get level progression data");
        }

        public static async void SetLevelAsync(int level, int xp, int reward)
        {
            GameLogger.Info($"Setting visual level (Level: {level}, Xp: {xp})");
            MapBoard.instance.OpenScreen("Levels");

            while (!LevelUpUI.instance.gameObject.activeInHierarchy)
                await UniTask.Yield(); // wait
            LevelUpUI.instance?.ShowXp(level, xp, reward);

            s_obsoleteLevel = level;
            s_obsoleteXp = xp;
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
