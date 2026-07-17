 using Teabag.Authentication;
using PlayFab.ClientModels;
using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using Teabag.Core;
using Teabag.UI;

public class CosmeticBuyButton : GorillaButton
{
    PurchaseStand stand;
    public TextMeshPro text;

    public string catalog = "Cosmetics";
    public string itemName;

    [NonSerialized]
    public Cosmetic cosmetic;
    [NonSerialized]
    public CatalogItem item;

    public State state = State.NONE;

    public override void Awake()
    {
        base.Awake();
        stand = GetComponentInParent<PurchaseStand>();


        if (PlayerData.loggedIn) LoadIfNotNull();
        else PlayerData.OnLogin += LoadIfNotNull;
    }

    private void OnDestroy()
    {
        PlayerData.OnLogin -= LoadIfNotNull;
    }

    public void LoadIfNotNull()
    {

    }

    public void LoadCosmetic(string name)
    {
        state = State.NONE;
        itemName = name;

        if (string.IsNullOrEmpty(name) || !PlayerData.loggedIn)
            return;

        item = AuthenticationUtils.catalogItems.GetItem(itemName);
        if (item == null)
        {
            state = State.ERROR;
            Debug.LogError("Failed to get item");
            return;
        }

        if (catalog == "Cosmetics")
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Cosmetic? itemCosmetic = AuthenticationUtils.GetCosmetic(item);
            if (itemCosmetic.HasValue) cosmetic = itemCosmetic.Value;
        }

        if (AuthenticationUtils.inventory.InventoryContains(item.ItemId) && !item.IsStackable)
            state = State.OWNED;
    }

    public override void OnPress() => _ = Purchase();

    private void Update() => RefreshText();

    public async UniTask Purchase()
    {
        if (AuthenticationUtils.inventory.InventoryContains(item.ItemId) && !item.IsStackable)
        {
            state = State.OWNED;
            return;
        }

        if (state != State.NONE)
            return;
        state = State.BUYING;

        if (AuthenticationUtils.currency < (int)item.VirtualCurrencyPrices["BA"])
        {
            GameLogger.Error($"Not enough currency (owns = {AuthenticationUtils.currency}, price = {(int)item.VirtualCurrencyPrices["BA"]}");
            state = State.NOT_ENOUGH;
            await UniTask.Delay(3000);
            state = State.NONE;
            return;
        }

        var purchase = await PlayFabAsyncClientAPI.PurchaseItemAsync(new PurchaseItemRequest()
        {
            ItemId = item.ItemId,
            CatalogVersion = catalog,
            VirtualCurrency = "BA",
            Price = (int)item.VirtualCurrencyPrices["BA"]
        });

        if (purchase.IsError)
        {
            state = State.ERROR;
            await UniTask.Delay(3000);
            state = State.NONE;
            Debug.LogError("Failed to purchase: " + purchase.Error.ErrorMessage);
            return;
        }

        AuthenticationUtils.currency -= (int)item.VirtualCurrencyPrices["BA"];
        AuthenticationUtils.inventory.Add(new InventoryItem(item, purchase.Result.Items[0]));
        state = State.BOUGHT;

        await UniTask.Delay(catalog == "Cosmetics" ? 10000 : 1000);
        state = !item.IsStackable ? State.OWNED : State.NONE;

        foreach (CosmeticsSelector selector in FindObjectsOfType<CosmeticsSelector>())
            selector.Render();
        foreach (ConsumablesTab tab in FindObjectsOfType<ConsumablesTab>())
            tab.Render();
    }

    private void RefreshText()
    {
        switch (state)
        {
            case State.NONE:
                text.text = "NOT PURCHASABLE";
                if (item != null)
                {
                    if (!item.VirtualCurrencyPrices.ContainsKey("BA"))
                        break;
                    text.text = catalog == "Cosmetics"
                        ? $"{item.DisplayName} - B${(int)item.VirtualCurrencyPrices["BA"]}"
                        : $"<size=100>{item.DisplayName}</size>\nB${(int)item.VirtualCurrencyPrices["BA"]}";
                }
                break;
            case State.NOT_ENOUGH:
                text.text = "CARD DECLINED.";
                break;
            case State.BUYING:
                text.text = "PURCHASING.";
                break;
            case State.ERROR:
                text.text = "SOMETHING WENT WRONG :(";
                break;
            case State.BOUGHT:
                text.text = catalog == "Cosmetics"
                    ? "SUCCESS!\nGRAB THE COSMETIC!"
                    : "SUCCESS!";
                break;
            case State.OWNED:
                text.text = "OWNED";
                break;
        }
    }

    public enum State
    {
        NONE,
        NOT_ENOUGH,
        BUYING,
        ERROR,
        BOUGHT,
        OWNED
    }
}
