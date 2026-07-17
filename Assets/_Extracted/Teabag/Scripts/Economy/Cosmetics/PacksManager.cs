using Teabag.Authentication;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;

public class PacksManager : MonoBehaviour
{
    public static PacksManager instance;
    public List<Pack> packs = new List<Pack>();

    private void Awake()
    {
        instance = this;
    }

    public static Pack GetPack(string name)
    {
        foreach (Pack pack in instance.packs)
        {
            if (pack.name == name)
                return pack;
        }

        return null;
    }

    public static Pack GetPackFromSku(string sku)
    {
        foreach (Pack pack in instance.packs)
        {
            if (pack.sku == sku)
                return pack;
        }

        return null;
    }
}

[Serializable]
public class Pack
{
    public string name;
    public string id;
    public string sku;
    public int currency;
    public List<string> cosmetics = new List<string>();

    public bool owns
    {
        get
        {
            return GetInstance().HasValue;
        }
    }

    public InventoryItem? GetInstance()
    {
        foreach (InventoryItem inventory in AuthenticationUtils.inventory)
        {
            if (inventory.Id == id)
                return inventory;
        }

        return null;
    }

    public List<InventoryItem> GetInventoryItems()
    {
        List<InventoryItem> result = new List<InventoryItem>();
        InventoryItem? instance = GetInstance();
        if (!instance.HasValue)
            return result;

        foreach (string cosmetic in cosmetics)
        {
            CatalogItem item = AuthenticationUtils.catalogItems.GetItem(cosmetic);
            //Debug.Log(item.DisplayName);
            InventoryItem i = new InventoryItem(item);
            if (instance.HasValue)
                i.InstanceId = instance.Value.InstanceId;
            result.Add(i);
        }

        return result;
    }

    public List<CatalogItem> GetCatalogItems()
    {
        List<CatalogItem> result = new List<CatalogItem>();

        foreach (string cosmetic in cosmetics)
        {
            CatalogItem item = AuthenticationUtils.catalogItems.GetItem(cosmetic);
            result.Add(item);
        }

        return result;
    }
}

/*
[Serializable]
public struct PackCosmetic
{
    public string cosmetic;
    public Rarity rarity;
    public int value;

    public Cosmetic GetCosmetic()
    {
        CosmeticSlot slot = CosmeticUtils.CosmeticType(cosmetic);
        return new Cosmetic(slot, cosmetic, (int)rarity);
    }

    public InventoryItem GetInventoryItem()
    {
        return new InventoryItem()
        {
            Id = cosmetic.Replace(" ", ""),
            Name = cosmetic,
            Catalog = "Cosmetics",
            Price = (uint)value,
            Rarity = rarity,
            InstanceId = DateTime.UtcNow.ToLongDateString()
        };
    }
}
*/