using Cysharp.Threading.Tasks;
using Teabag.Authentication;
using Teabag.Core;
using Teabag.Economy;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConsumablePreview : MonoBehaviour
{
    public DataViewer dataViewer;
    string consumableName;
    int amount
    {
        get
        {
            InventoryItem? item = ConsumablesManager.GetItem(consumableName);

            if (!item.HasValue)
                return 0;

            return item.Value.RemainingUses;
        }
    }

    public void Initialise(string consumable)
    {
        consumableName = consumable;
        Render();
    }

    bool spawning = false;

    public async UniTaskVoid Use()
    {
        if (spawning)
            return;

        spawning = true;

        await ConsumablesManager.UseConsumable(consumableName);

        spawning = false;
        GetComponentInParent<ConsumablesTab>().Render();
    }

    public void Render()
    {
        string name = consumableName + (amount == 1 ? "" : "S");
        dataViewer.Show(new Dictionary<string, string>()
        {
            {
                "CONSUMABLE",
                name
            },
            {
                "AMOUNT",
                amount.ToString()
            }
        });
    }
}
