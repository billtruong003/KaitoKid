using Teabag.Authentication;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

// Rarity lives in global namespace so all scripts can use it without a 'using' directive,
// matching its previous location in VRRigCosmeticSlot.cs.
public enum Rarity : byte
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

// CosmeticSlot moved from VRRigCosmeticSlot.cs so Core.asmdef (PlayerData) can reference it.
public enum CosmeticSlot
{
    Head = 0,
    Face = 1,
    Nuts = 5
}

// Cosmetic struct moved from VRRigCosmeticSlot.cs (Player) to Core so that
// Authentication/ and other Core scripts can reference it without depending on Player.
[Serializable]
public struct Cosmetic
{
    public CosmeticSlot category;
    public string cosmetic;
    public Rarity rarity;

    /// <summary>
    /// Wired by CosmeticUtils (Player assembly) to resolve a cosmetic name to its CosmeticSlot.
    /// Falls back to CosmeticSlot.Head if not wired.
    /// </summary>
    public static Func<string, CosmeticSlot> ResolveCosmeticType;

    public Cosmetic(CosmeticSlot Category, string Cosmetic)
    {
        category = Category;
        cosmetic = Cosmetic;
        rarity = 0;
    }

    public Cosmetic(CosmeticSlot Category, string Cosmetic, int Rarity)
    {
        category = Category;
        cosmetic = Cosmetic;
        rarity = (Rarity)Rarity;
    }

    public Cosmetic(InventoryItem Item)
    {
        category = ResolveCosmeticType?.Invoke(Item.Name) ?? CosmeticSlot.Head;
        cosmetic = Item.Name;
        rarity = Item.Rarity;
    }

    public Cosmetic(CatalogItem Item)
    {
        Cosmetic c = new Cosmetic(new InventoryItem(Item));
        category = c.category;
        cosmetic = c.cosmetic;
        rarity = c.rarity;
    }

    public override string ToString()
    {
        return $"Category: {category.ToString()} Cosmetic: {cosmetic} Rarity: {rarity}";
    }
}

namespace Teabag.Authentication
{
    // Moved from AuthenticationUtils.cs so that Player.asmdef scripts can reference
    // InventoryItem without depending on Assembly-CSharp.
    [Serializable]
    public class CatalogItemCustomData
    {
        public string Rarity;
    }

    public struct InventoryItem
    {
        public string Id;
        public string Name;
        public string Catalog;
        public uint Price;
        public Rarity Rarity;

        // Consumable
        public string InstanceId;
        public int RemainingUses;

        public InventoryItem(CatalogItem catalogItem, ItemInstance itemInstance = null)
        {
            Id = catalogItem.ItemId;
            Name = catalogItem.DisplayName;
            Catalog = catalogItem.CatalogVersion;

            if (itemInstance != null)
            {
                InstanceId = itemInstance.ItemInstanceId;
                if (itemInstance.RemainingUses.HasValue)
                    RemainingUses = itemInstance.RemainingUses.Value;
                else
                    RemainingUses = 1;
            }
            else
            {
                InstanceId = "";
                RemainingUses = 0;
            }

            if (catalogItem.VirtualCurrencyPrices.TryGetValue("BA", out uint price))
                Price = price;
            else
                Price = 0;

            Rarity = 0;
            if (catalogItem.CustomData != null)
            {
                CatalogItemCustomData data = JsonUtility.FromJson<CatalogItemCustomData>(catalogItem.CustomData);
                if (int.TryParse(data.Rarity, out int result))
                    Rarity = (Rarity)result;
                else
                    Debug.LogError($"Failed to parse Rarity \"{data.Rarity}\"");
            }
            else
                Debug.LogError($"\"{catalogItem.DisplayName}\" CustomData is null");
        }

        public override string ToString()
        {
            return $"{Name}: {InstanceId} ({Catalog} B${Price})";
        }
    }

    public static class InventoryExtensions
    {
        public static bool InventoryContains(this List<InventoryItem> items, string id)
        {
            foreach (InventoryItem item in items)
            {
                if (item.Id == id)
                    return true;
            }
            return false;
        }
    }
}
