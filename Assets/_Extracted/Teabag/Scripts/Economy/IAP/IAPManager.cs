using Teabag.Authentication;
using Oculus.Platform;
using Oculus.Platform.Models;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Services;
using UnityEngine;

namespace Teabag.Services
{
    public class IAPManager : IIAPManager
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly Dictionary<string, Product> _products = new Dictionary<string, Product>();
        private PurchaseList _purchases;

        public IAPManager(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async UniTask ConsumePurchasesAsync()
        {
            var inventory = await GetInventoryAsync();
            foreach (Purchase purchase in inventory)
            {
                if (!purchase.Sku.EndsWith("_004"))
                {
                    bool isCurrency = purchase.Sku.StartsWith("currency");
                    GameLogger.Debug("Found non-consumed item: SKU-" + purchase.Sku);
                    if (isCurrency)
                        await PurchaseWithPlayFabAsync(purchase.Sku);
                    else
                    {
                        Pack pack = PacksManager.GetPackFromSku(purchase.Sku);
                        if (pack != null && !pack.owns) await PurchaseWithPlayFabAsync(purchase.Sku);
                    }
                }
                else
                    GameLogger.Warning("Found invalid item: SKU-" + purchase.Sku);
            }
        }

        private async UniTask<PurchaseList> GetInventoryAsync()
        {
            UniTaskCompletionSource<PurchaseList> tcs = new UniTaskCompletionSource<PurchaseList>();
            if (_purchases != null) tcs.TrySetResult(_purchases);

            IAP.GetViewerPurchases()
                .OnComplete(p =>
                {
                    if (p.IsError)
                    {
                        GameLogger.Error("Failed to get viewer purchases");
                        tcs.TrySetResult(null); // or throw
                        return;
                    }

                    _purchases = p.GetPurchaseList();
                    tcs.TrySetResult(p.GetPurchaseList());
                });
            return await tcs.Task;
        }

        public async UniTask<IAPProductResult> GetProductAsync(string sku)
        {
            UniTaskCompletionSource<IAPProductResult> tcs = new UniTaskCompletionSource<IAPProductResult>();
            if (_products.TryGetValue(sku, out var product))
                tcs.TrySetResult(new IAPProductResult(product, false));

            IAP.GetProductsBySKU(new[] { sku })
                .OnComplete(p =>
                {
                    if (p.IsError)
                        tcs.TrySetResult(new IAPProductResult(null, p.IsError));
                    else
                    {
                        if (p.GetProductList().Count > 0)
                        {
                            Product prod = p.GetProductList()[0];
                            _products.TryAdd(sku, prod);
                            tcs.TrySetResult(new IAPProductResult(_products[sku], p.IsError));
                        }
                        else
                            tcs.TrySetResult(new IAPProductResult(null, true));
                    }
                });
            return await tcs.Task;
        }

        public async UniTask<IAPPurchaseResult> PurchaseAsync(string sku)
        {
            if (!_products.ContainsKey(sku))
                await GetProductAsync(sku);
            var msg = await LaunchCheckoutFlowAsync(sku);

            if (msg.IsError)
            {
                GameLogger.Error("User canceled purchase");
                return new IAPPurchaseResult(msg, new ExecuteCloudScriptResult(), new PlayFabError
                {
                    Error = PlayFabErrorCode.OperationCanceled,
                    ErrorMessage = "Purchase cancelled"
                }, true);
            }

            if (_authenticationService.OrgScopedId == 0) // Used to check AuthenticationManager.platformId, now checking OrgScopedId or similar
            {
                GameLogger.Error("Platform identity is invalid");
                return new IAPPurchaseResult(msg, new ExecuteCloudScriptResult(), new PlayFabError
                {
                    Error = PlayFabErrorCode.UserisNotValid,
                    ErrorMessage = "Invalid platform"
                }, true);
            }

            var result = await PurchaseWithPlayFabAsync(sku);
            return new IAPPurchaseResult(msg, new ExecuteCloudScriptResult(), new PlayFabError(), result);
        }

        public async UniTask<Message<Purchase>> LaunchCheckoutFlowAsync(string sku)
        {
            UniTaskCompletionSource<Message<Purchase>> tcs = new UniTaskCompletionSource<Message<Purchase>>();
            IAP.LaunchCheckoutFlow(sku)
                .OnComplete(msg =>
                {
                    tcs.TrySetResult(msg);
                });
            return await tcs.Task;
        }

        public async UniTask<bool> PurchaseWithPlayFabAsync(string sku)
        {
            GameLogger.Debug("executing purchase with PlayFab");
            bool isCurrency = sku.StartsWith("currency");

            var purchaseCloudScript = await PlayFabAsyncClientAPI.ExecuteCloudScriptAsync(
                new ExecuteCloudScriptRequest()
                {
                    FunctionName = isCurrency ? "PurchaseVirtualCurrency" : "PurchasePack",
                    FunctionParameter = new
                    {
                        sku,
                        id = _authenticationService.OrgScopedId.ToString() // Replaced platformId with OrgScopedId
                    }
                });

            if (purchaseCloudScript.IsError)
            {
                GameLogger.Error($"Failed to purchase 'SKU-{sku}': {purchaseCloudScript.Error.ErrorMessage}");
                return true;
            }

            GameLogger.Info($"Successfully purchased {sku}");
            try
            {
                if (isCurrency)
                    AuthenticationUtils.currency += Int32.Parse(purchaseCloudScript.Result.FunctionResult.ToString());
                else
                {
                    PackResult packResult = JsonUtility.FromJson<PackResult>(purchaseCloudScript.Result.FunctionResult.ToString());
                    if (packResult.success)
                    {
                        AuthenticationUtils.currency += packResult.amount;
                        await AuthenticationUtils.GetCosmeticsAsync();
                    }
                    else return true;
                }
            }
            catch (FormatException ex)
            {
                GameLogger.Error($"Failed to add currency or pack due to invalid format (Message={ex.Message})");
                return true;
            }
            catch (OverflowException ex)
            {
                GameLogger.Error($"Failed to add currency or pack due to numeric overflow (Message={ex.Message})");
                return true;
            }
            catch (ArgumentException ex)
            {
                GameLogger.Error($"Failed to add currency or pack due to invalid data (Message={ex.Message})");
                return true;
            }

            return false;
        }

        [Serializable]
        public class PurchaseResult
        {
            public bool success;
            public int amount;
        }

        [Serializable]
        public class PackResult : PurchaseResult
        {
            public List<ItemInstance> cosmetics;
        }
    }
}
