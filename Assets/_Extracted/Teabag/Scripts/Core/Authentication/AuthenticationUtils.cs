using Fusion;
using Fusion.Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using Squido.JungleXRKit.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Teabag.Core;
using Teabag.Services;
using UnityEngine;

// refactored as of 16/05/2025
namespace Teabag.Authentication
{
    public static class AuthenticationUtils
    {
        public static Dictionary<string, string> titleData = new Dictionary<string, string>();
        public static List<InventoryItem> inventory = new List<InventoryItem>(); // items that the user owns
        public static List<CatalogItem> catalogItems = new List<CatalogItem>(); // items in catalog
        public static int currency;

        [Networked, OnChangedRender(nameof(OnCurrencyChanged))]
        private static int GetCurrency => currency; 

        public static Action CurrencyChanged;
        private static void OnCurrencyChanged()
        {
            CurrencyChanged?.Invoke();
        }

        // returns the total value of the users inventory
        public static uint CosmeticsValue => cosmeticsValue;

        [Obsolete("Use CosmeticsValue instead")]
        public static uint cosmeticsValue
        {
            get
            {
                uint value = 0;

                foreach (InventoryItem i in inventory)
                {
                    if (i.Catalog == "Cosmetics")
                        value += i.Price;
                }

                return value;
            }
        }

        public static Dictionary<CosmeticSlot, string> Cosmetics => cosmetics;

        [Obsolete("Use Cosmetics instead")]
        public static Dictionary<CosmeticSlot, string> cosmetics
        {
            get
            {
                var authManager = ServiceLocator.Get<IAuthenticationService>();
                if (authManager == null || !authManager.Initialised || !PlayerData.initialised)
                {
                    GameLogger.Debug("authentication manager is not initialized, returning empty cosmetics list");
                    return new Dictionary<CosmeticSlot, string>();
                }

                if (GameServices.HasNetworkRunner?.Invoke() != true || GameServices.IsSceneManagerBusy?.Invoke() == true)
                {
                    GameLogger.Debug("no network runner or scene manager is busy, returning empty cosmetics list");
                    return new Dictionary<CosmeticSlot, string>();
                }

                string[] enumNames = Enum.GetNames(typeof(CosmeticSlot));
                Dictionary<CosmeticSlot, string> cos = new Dictionary<CosmeticSlot, string>();

                for (int i = 0; i < enumNames.Length; i++)
                {
                    var cosmeticSlotToName = ServiceLocator.Get<IDataPersistenceService>()?.LoadData<string>($"Cosmetic{enumNames[i]}", "") ?? "";
                    cos.Add((CosmeticSlot)i, cosmeticSlotToName);
                }
                return cos;
            }
        }

        // TODO: we should probably deprecate this and start using
        //       SyncedTime.serverTime instead, as this implementation
        //       is just slowing us down, its quicker if we use SyncedTime.serverTime
        public static DateTime serverTime { get { return SyncedTime.serverTime; } }


        public static void SetCosmetic(Cosmetic cosmetic) => SetCosmetic((int)cosmetic.category, cosmetic.cosmetic);

        public static void SetCosmetic(CosmeticSlot slot, string cosmeticName) => SetCosmetic((int)slot, cosmeticName);

        public static void SetCosmetic(int slot, string cosmeticName)
        {
            string slotName = Enum.GetName(typeof(CosmeticSlot), slot);
            ServiceLocator.Get<IDataPersistenceService>()?.TrySaveData($"Cosmetic{slotName}", cosmeticName);
            GameServices.LoadLocalCosmetics?.Invoke();
        }

        public static async UniTask<bool> SubtractCurrencyAsync(int amount)
        {
            if (currency < amount)
                return false;

            var result = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(new ExecuteCloudScriptRequest()
            {
                FunctionName = "SubtractCurrency",
                FunctionParameter = new { Amount = amount }
            });

            if (result.IsError || !(bool)result.Result.FunctionResult)
                return false;

            currency -= amount;
            return true;
        }

        public static async UniTask<PlayFabAsyncResult<UpdateUserTitleDisplayNameResult>> SetDisplayNameAsync(
            string displayName)
        {
            if (ModerationUtils.CheckBadWord(displayName))
            {
                return new PlayFabAsyncResult<UpdateUserTitleDisplayNameResult>(
                    null,
                    new PlayFabError
                    {
                        Error = PlayFabErrorCode.ProfaneDisplayName,
                        ErrorMessage = "Name contains profanity"
                    });
            }

            string normalizedDisplayName = displayName.ToUpperInvariant();

#if UNITY_EDITOR || DEVELOPMENT_BUILD || SKIP_PLATFORM_AUTH
            if (ShouldUseLocalDisplayNameFallback())
            {
                PlayerData.displayName = normalizedDisplayName;
                GameServices.SetLocalPlayerName?.Invoke(normalizedDisplayName);

                return new PlayFabAsyncResult<UpdateUserTitleDisplayNameResult>(
                    new UpdateUserTitleDisplayNameResult
                    {
                        DisplayName = normalizedDisplayName
                    },
                    null);
            }
#endif

            var result = await PlayFabAsyncClientAPI.UpdateUserTitleDisplayNameAsync(new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = normalizedDisplayName
            });

            if (result.IsError) return result;
            GameLogger.Info("Updated display name");

            PlayerData.displayName = result.Result.DisplayName;
            GameServices.SetLocalPlayerName?.Invoke(result.Result.DisplayName);

            return result;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || SKIP_PLATFORM_AUTH
        private static bool ShouldUseLocalDisplayNameFallback()
        {
            if (!ServiceLocator.TryGet<IAuthenticationService>(out var authenticationService) || authenticationService == null)
                return true;

            return !authenticationService.LoggedIn;
        }
#endif


        public static async UniTask<UserTitleInfo> GetAccountInfoAsync(string playFabId)
        {
            var result = await PlayFabAsyncClientAPI.GetAccountInfoAsync(new GetAccountInfoRequest
            {
                PlayFabId = playFabId
            });

            if (result.IsError) return null;

            PlayerData.displayName = result.Result.AccountInfo.TitleInfo.DisplayName;

            return result.Result.AccountInfo.TitleInfo;
        }

        public static async UniTask<GorillaUser> GetGorillaUserAsync(string playFabId)
        {
            var result = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(new ExecuteCloudScriptRequest()
            {
                FunctionName = "GetGorillaUser",
                FunctionParameter = new { PlayFabId = playFabId }
            });

            if (result.IsError)
            {
                Debug.LogError("Failed to get Gorilla User: " + playFabId);
                return null;
            }

            if (result.Result == null)
            {
                Debug.LogError("Gorilla user is null");
                return null;
            }

            return new GorillaUser(result.Result);
        }

        public static async UniTask<PlayFabAsyncResult<GetPhotonAuthenticationTokenResult>> RequestFusionTokenAsync()
        {
            GameLogger.Info("Authenticating with Fusion");
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            var photonResult = await PlayFabAsyncClientAPI.GetPhotonAuthenticationTokenAsync(new GetPhotonAuthenticationTokenRequest
            {
                PhotonApplicationId = PhotonAppSettings.Global.AppSettings.AppIdFusion
            });

            GameLogger.Debug("Finished authentication");
            if (photonResult.IsError)
            {
                GameLogger.Error($"Failed to authenticate: '{photonResult.Error.ErrorMessage}'");
                return photonResult;
            }

            return photonResult;
        }

        public static async UniTask<bool> GetCatalogAsync(string catalog)
        {
            var catalogResult = await PlayFabAsyncClientAPI.GetCatalogItemsAsync(new GetCatalogItemsRequest
            {
                CatalogVersion = catalog
            });

            if (catalogResult.IsError)
            {
                GameLogger.Error($"Failed to get catalog: '{catalogResult.Error}'");
                return false;
            }

            catalogItems.AddRange(catalogResult.Result.Catalog);
            PlayerData.catalogItems = catalogItems;
            GameServices.RefreshCosmeticSelectors?.Invoke();
            return true;
        }

        // only use this if you need to load multiple catalogs at once
        // this is experimental!
        public static async UniTask<bool> GetCatalogBulkAsync(params string[] catalogs)
        {
            var tasks = catalogs.Select(async str =>
            {
                var asyncResult = await PlayFabAsyncClientAPI.GetCatalogItemsAsync(
                    new GetCatalogItemsRequest
                    {
                        CatalogVersion = str
                    });

                if (asyncResult.IsError)
                {
                    GameLogger.Error($"Failed to get catalog in bulk '{str}': {asyncResult.Error}");
                    return null;
                }

                GameLogger.Debug($"loaded catalog in bulk '{str}'");
                return asyncResult.Result;
            });

            var catalogResults = await UniTask.WhenAll(tasks);
            var successfulResults = catalogResults.Where(r => r != null);
            catalogItems.AddRange(successfulResults.SelectMany(r => r.Catalog));
            PlayerData.catalogItems = catalogItems;
            GameServices.RefreshCosmeticSelectors?.Invoke();
            return true;
        }

        public static async UniTask<bool> GetCosmeticsAsync()
        {
            var inventoryResult = await PlayFabAsyncClientAPI.GetUserInventoryAsync(new GetUserInventoryRequest());
            if (inventoryResult.IsError || inventoryResult.Result == null || inventoryResult.Result.Inventory == null)
                return false;
            inventoryResult.Result.VirtualCurrency.TryGetValue("BA", out currency);

            inventory = new List<InventoryItem>();
            if (catalogItems.Count <= 0) // load all catalogs
                await GetCatalogBulkAsync("Cosmetics", "Items", "Packs");

            foreach (CatalogItem catalogItem in catalogItems)
            {
                foreach (ItemInstance instance in inventoryResult.Result.Inventory)
                {
                    try
                    {
                        if (instance.ItemId == catalogItem.ItemId)
                        {
                            AddToInventory(new(catalogItem, instance));

                            var packItems = GameServices.GetPackInventoryItems?.Invoke(instance.DisplayName);
                            if (packItems != null)
                            {
                                foreach (InventoryItem item in packItems)
                                    AddToInventory(item);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameLogger.Error($"Error while loading cosmetics: {ex.Message}");
                    }
                }
            }

            // sync inventory to PlayerData bridge for named assemblies
            PlayerData.inventory = inventory;

            // validate cosmetics and set cosmetics if they dont own them
            foreach (var cosmetic in Cosmetics.Where(cosmetic => !string.IsNullOrEmpty(cosmetic.Value) && !OwnsCosmetic(cosmetic.Value)))
                SetCosmetic(cosmetic.Key, string.Empty);

            GameServices.RefreshCosmeticSelectors?.Invoke();
            GameServices.RefreshPurchaseStands?.Invoke();
            GameServices.LoadLocalCosmetics?.Invoke();
            return true;
        }

        private static void AddToInventory(InventoryItem item)
        {
            if (GameServices.CosmeticExists?.Invoke(new Cosmetic(item)) == true || item.Catalog != "Cosmetics")
                inventory.Add(item);
        }

        public static bool OwnsCosmetic(string cosmeticName) => inventory.Any(i => i.Name == cosmeticName);

        public static UniTask<GorillaProgression> SubmitKillsAsync(int kills, bool won)
            => SubmitMatchResultAsync(kills, won, 0, won ? 1 : 0);

        public static async UniTask<GorillaProgression> SubmitMatchResultAsync(int kills, bool won, int teabagRips, int place)
        {
            if (GameServices.IsRoomModded?.Invoke() == true)
                return null;

            if (kills == 0 && won == false && teabagRips == 0 && place <= 0)
            {
                GameServices.SetLevel?.Invoke(GameServices.GetObsoleteLevel?.Invoke() ?? 0, GameServices.GetObsoleteXp?.Invoke() ?? 0, 0);
                return null;
            }

            GameLogger.Info($"Game over -- kills: {kills}, won: {won}");
            var result = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(new ExecuteCloudScriptRequest()
            {
                FunctionName = "GameOver",
                FunctionParameter = new { Kills = kills, Won = won, TeabagRips = teabagRips, Place = place }
            });

            if (result.IsError)
            {
                GameLogger.Error("Failed to upload session end data");
                return null;
            }

            GorillaProgression progression = new GorillaProgression(result.Result);
            GameServices.SetLevel?.Invoke(progression.Level, progression.Xp, progression.Reward);
            return progression;
        }

        // InventoryContains and GetItem extension methods now live in Core assembly:
        // InventoryExtensions (Engine/PlayerTypes.cs) and CatalogItemExtensions (Engine/PlayerData.cs)

        public static Cosmetic? GetCosmetic(string name)
        {
            foreach (CatalogItem item in catalogItems)
            {
                if (item.DisplayName == name)
                    return new Cosmetic(item);
            }

            return null;
        }

        public static Cosmetic? GetCosmetic(this CatalogItem item)
        {
            if (item == null)
                return null;
            return new Cosmetic(item);
        }
    }

    [Serializable]
    public class GorillaUser
    {
        public string PlayFabId = "";
        public string DisplayName = "";
        public string CreatedAt = DateTime.UtcNow.ToString();
        public int Level = 0;
        public int Xp = 0;

        public GorillaUser(ExecuteCloudScriptResult cloudScriptResult)
        {
            try
            {
                GorillaUser user = JsonUtility.FromJson<GorillaUser>(cloudScriptResult.FunctionResult.ToString());
                PlayFabId = user.PlayFabId;
                DisplayName = user.DisplayName;
                CreatedAt = user.CreatedAt;
                Level = user.Level;
                Xp = user.Xp;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse GorillaUser: {ex.Message}\n{cloudScriptResult.FunctionResult}");
            }
        }

        public override string ToString()
        {
            return $"Gorilla User {DisplayName} ({PlayFabId}) Level: {Level} XP: {Xp} Created At: {CreatedAt}";
        }
    }

    [Serializable]
    public class GorillaProgression
    {
        public int Level;
        public int Xp;
        public int Reward;

        public GorillaProgression(ExecuteCloudScriptResult cloudScriptResult)
        {
            GorillaProgression progression = JsonUtility.FromJson<GorillaProgression>(cloudScriptResult.FunctionResult.ToString());
            Level = progression.Level;
            Xp = progression.Xp;
            Reward = progression.Reward;
        }
    }
}
