using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Teabag.BattlePass.Models;
using Teabag.Authentication;
using Newtonsoft.Json;
using Teabag.Core;
using System;
using Cysharp.Threading.Tasks;
using Teabag.Networking;
using PlayFab.ClientModels;
using Teabag.Player;

// ReSharper disable once CheckNamespace
namespace Teabag.BattlePass
{
    public enum BattlePassIncrementContext
    {
        PRE_GAME,
        DURING_GAME,
        POST_GAME
    }
    
    public static class BattlePassManager
    {
        public static event Action OnPassUpdated;
        public static event Action<int, int> OnGameOverProcessed;

        private static Models.BattlePass s_BattlePass;

        public static Models.BattlePass BattlePass
        {
            get => s_BattlePass;
            private set
            {
                s_BattlePass = value;
                OnPassUpdated?.Invoke();
            }
        }
        
        private static int s_Xp = 0;
        public static int Xp
        {
            get => s_Xp;
            private set => s_Xp = value;
        }

        private static int s_CurrentTier;
        public static int CurrentTier
        {
            get => s_CurrentTier;
            private set => s_CurrentTier = value;
        }

        public static void GetBattlePass()
        {
            if (AuthenticationUtils.titleData.TryGetValue("BattlePass", out string value))
            {
                GameLogger.Info("Deserializing BattlePass data from Title Data");
                BattlePass = JsonConvert.DeserializeObject<Models.BattlePass>(value);
                
                // we need some way of getting the current XP of the player to set
                // our vars like s_Xp and s_CurrentTier, so we use a cloud script
                InitializeXpAsync().Forget();
            }
            else
                GameLogger.Error("Failed to get BattlePass field from Title Data");
        }

        private static async UniTaskVoid InitializeXpAsync()
        {
            try
            {
                int xp = await RequestCurrentXpAsync();
                s_Xp = xp;
                s_CurrentTier = InternalCalculateCurrentTier();
                OnPassUpdated?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Initialization was canceled (for example, when shutting down); no error log needed.
            }
            catch (Exception ex)
            {
                GameLogger.Error("Failed to get current XP (for more information check the logs above): {0}", ex);
            }
        }

        // ReSharper disable once AsyncVoidMethod
        public static async UniTask<int> RequestCurrentXpAsync()
        {
            var cloudScriptResult = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync<int>(
                new ExecuteCloudScriptRequest
                {
                    FunctionName = "BattlePassCurrentXp",
                    FunctionParameter = new {}
                });

            if (cloudScriptResult.IsError && cloudScriptResult.Error != null)
            {
                GameLogger.Error("Failed to get current XP: {0}", cloudScriptResult.Error);
                return 0;
            }
            if (cloudScriptResult.Result.FunctionResult is int currentXpResponse)
                return currentXpResponse; 
            
            GameLogger.Error("Failed to get current XP: server failed to send parsable response data to client");
            return 0;
        }
        
        // ReSharper disable once AsyncVoidMethod
        public static async UniTaskVoid GameOverAsync(int kills, bool won)
        {
            try
            {
                if (kills < 0)
                {
                    GameLogger.Debug("kills cannot be negative");
                    return;
                }

                var cloudScriptResult = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync<Dictionary<string, object>>(
                    new ExecuteCloudScriptRequest
                    {
                        FunctionName = "BattlePassGameOver",
                        FunctionParameter = new
                        {
                            Kills = kills,
                            Won = won
                        }
                    });

                if (cloudScriptResult.IsError && cloudScriptResult.Error != null)
                {
                    GameLogger.Error("Failed to process GameOver: {0}", cloudScriptResult.Error);
                    return;
                }

                if (cloudScriptResult.Result.FunctionResult is Dictionary<string, object> gameOverData)
                {
                    if (gameOverData.TryGetValue("Level", out var levelObj) &&
                        gameOverData.TryGetValue("Xp", out var xpObj))
                    {
                        int newLevel = Convert.ToInt32(levelObj);
                        int newXp = Convert.ToInt32(xpObj);

                        // add currency
                        if (gameOverData.TryGetValue("Reward", out var rewardObj))
                        {
                            int rewardAmount = Convert.ToInt32(rewardObj);
                            AuthenticationUtils.currency += rewardAmount;
                        }

                        if (newXp > s_Xp && newLevel > s_CurrentTier)
                        {
                            s_Xp = newXp;
                            s_CurrentTier = newLevel - 1;

                            if (s_CurrentTier != InternalCalculateCurrentTier())
                            {
                                GameLogger.Error("Internal mismatch -- tier sent by server does not match the tier that the client has after changing!");
                                return;
                            }

                            OnPassUpdated?.Invoke();
                            OnGameOverProcessed?.Invoke(newLevel, newXp);
                        }
                    }
                    else
                        GameLogger.Error("GameOver response missing required fields.");
                }
                else
                    GameLogger.Error("GameOver cloud script returned an unparseable response.");
            }
            catch (Exception ex)
            {
                GameLogger.Error($"BattlePassManager.GameOverAsync failed: {ex.Message}");
            }
        }

        public static bool HasPass(string pass)
            => string.IsNullOrEmpty(pass) || AuthenticationUtils.inventory.InventoryContains(pass);

        private static int InternalCalculateCurrentTier()
        {
            int final = 0;
            foreach (BattlePassTier tier in BattlePass.Tiers)
            {
                if (Xp >= tier.RequiredXP)
                    final++;
                else break;
            }
                
            // tiers are 0-indexed, so we subtract 1 to get the correct index
            return final - 1;
        }
    }
}
