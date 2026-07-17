using Teabag.Authentication;
using Teabag.Networking;
using Teabag.Core;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Teabag.Player;

namespace Teabag.Economy
{
    public struct PurchaseResult
    {
        public bool Success;
        public string Message;
    }

    public static class ConsumablesManager
    {
        public static string ConsumableCatalog = "Consumables";
        public static List<CatalogItem> catalogItems
        {
            get
            {
                List<CatalogItem> items = new List<CatalogItem>();
                foreach (CatalogItem item in AuthenticationUtils.catalogItems)
                {
                    if (item.CatalogVersion == ConsumableCatalog)
                        items.Add(item);
                }

                //TODO: Debug mock items, Deleted this when playfab is ready.
                if (items.Count == 0)
                {
                    items.Add(new CatalogItem { ItemId = "Mock_Banana", DisplayName = "BANANA", CatalogVersion = ConsumableCatalog, VirtualCurrencyPrices = new Dictionary<string, uint> { { "BA", 50 } } });
                    items.Add(new CatalogItem { ItemId = "Mock_Apple", DisplayName = "APPLE", CatalogVersion = ConsumableCatalog, VirtualCurrencyPrices = new Dictionary<string, uint> { { "BA", 75 } } });
                }

                return items;
            }
        }
        public static List<InventoryItem> inventory
        {
            get
            {
                List<InventoryItem> items = new List<InventoryItem>();
                foreach (InventoryItem item in AuthenticationUtils.inventory)
                {
                    if (item.Catalog == ConsumableCatalog)
                        items.Add(item);
                }
                return items;
            }
        }

        public static async UniTask<PurchaseResult> BuyConsumable(string consumable)
        {
            CatalogItem item = catalogItems.GetItem(consumable);
            if (item == null)
            {
                GameLogger.Error("Consumable not found in catalog: " + consumable);
                return new PurchaseResult { Success = false, Message = "ITEM NOT FOUND" };
            }

            // TODO: Mock enough BA, remove comment mock when PlayFab is ready.
            // if (Teabag.Authentication.AuthenticationUtils.currency < (int)item.VirtualCurrencyPrices["BA"])
            // {
            //     GameLogger.Warning("Not enough BA for " + consumable);
            //     return new PurchaseResult { Success = false, Message = "NOT ENOUGH BA" };
            // }

            /* TODO: Implement PlayFab purchase when backend is ready
            var purchase = await PlayFabAsyncClientAPI.PurchaseItemAsync(new PurchaseItemRequest
            {
                ItemId = item.ItemId,
                CatalogVersion = ConsumableCatalog,
                VirtualCurrency = "BA",
                Price = (int)item.VirtualCurrencyPrices["BA"]
            });

            if (purchase.IsError)
            {
                GameLogger.Error("Failed to purchase consumable: " + purchase.Error.ErrorMessage);
                return new PurchaseResult { Success = false, Message = purchase.Error.ErrorMessage };
            }
            Teabag.Authentication.AuthenticationUtils.inventory.Add(new Teabag.Authentication.InventoryItem(item, purchase.Result.Items[0]));


            Teabag.Authentication.AuthenticationUtils.currency -= (int)item.VirtualCurrencyPrices["BA"];
            Teabag.Authentication.AuthenticationUtils.inventory.Add(new Teabag.Authentication.InventoryItem(item));
            */

            return new PurchaseResult { Success = true, Message = "PURCHASE SUCCESSFUL!" };
        }

        public static async UniTask UseConsumable(string consumable)
        {
            GameLogger.Info("Attempting to use " + consumable);
            InventoryItem? item = GetItem(consumable);
            if (!item.HasValue)
                return;

            if (item.Value.RemainingUses <= 0)
            {
                GameLogger.Info("Not enough " + consumable);
                return;
            }

            /* TODO: Implement PlayFab consume when backend is ready
            var result = await PlayFabAsyncClientAPI.ConsumeItemAsync(new ConsumeItemRequest
            {
                ConsumeCount = 1,
                ItemInstanceId = item.Value.InstanceId
            });

            if (result.IsError)
            {
                GameLogger.Info("Failed to use consumable: " + result.Error.ErrorMessage);
                return;
            }
            */

            GameLogger.Info("Spawning consumable " + consumable);
            NetworkObjectsManager.Spawn($"Consumables/{consumable}", Vector3.zero, Quaternion.identity, Vector3.one);
        }

        public static InventoryItem? GetItem(string name)
        {
            foreach (InventoryItem item in inventory)
            {
                if (item.Name == name)
                    return item;
            }

            return null;
        }
    }
}
