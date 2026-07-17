using System.Text;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab.ClientModels;
using System.Text.RegularExpressions;
using Teabag.Authentication;
using Teabag.Core;

public class ValueCalculator : MonoBehaviour
{
    public string packName;
    string basePriceSku = "currency_001";
    public TMP_Text cosmetics;
    public TMP_Text values;
    public List<string> rarities = new List<string>();

    private async void Awake()
    {
        if (GameServices.GetIAPProductAsync == null)
            return;

        var result = await GameServices.GetIAPProductAsync(basePriceSku);
        if (result.isError)
            return;

        string formattedPrice = result.formattedPrice;
        string priceString = Regex.Replace(formattedPrice, "[^0-9.]", "");
        string irlCurrencyName = formattedPrice.Replace(priceString, "");
        float basePrice = float.Parse(priceString);

        StringBuilder cosmeticsBuilder = new StringBuilder();
        StringBuilder valuesBuilder = new StringBuilder();

        Pack pack = PacksManager.GetPack(packName);
        List<CatalogItem> v = pack.GetCatalogItems();

        float fullValue = 0;

        foreach (CatalogItem item in v)
        {
            InventoryItem i = new InventoryItem(item);
            cosmeticsBuilder.Append(rarities[(int)i.Rarity]);

            string name = i.Name;
            // Clean up the name
            name = name.Replace("EARLY ACCESS", "");
            name = name.Replace("EARLY SUPPORTER", "");
            name = name.Replace("PAYLOAD", "");
            name = name.Replace("CHRISTMAS 2024", "");
            cosmeticsBuilder.AppendLine(name);

            float value = i.Price / 1000f * basePrice;
            fullValue += value;

            valuesBuilder.AppendLine($"{irlCurrencyName}{value}");
        }

        if (pack.currency > 0)
        {
            cosmeticsBuilder.Append(rarities[4]);
            cosmeticsBuilder.AppendLine($"{pack.currency} BANANA-BUCKS");
            float value = pack.currency / 1000f * basePrice;
            fullValue += value;
            valuesBuilder.AppendLine($"{irlCurrencyName}{value}");
        }

        valuesBuilder.AppendLine($"VALUE: <s>{irlCurrencyName}{fullValue}");
        cosmetics.text = cosmeticsBuilder.ToString();
        values.text = valuesBuilder.ToString();
    }
}
