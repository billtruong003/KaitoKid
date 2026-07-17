using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using Teabag.Core;
using UnityEngine;

public class PackShelf : MonoBehaviour
{
    public string packName;
    public List<PurchaseStand> stands = new List<PurchaseStand>();

    private void Awake() => InternalLoadShelf();

    private void InternalLoadShelf()
    {
        Pack pack = PacksManager.GetPack(packName);

        List<CatalogItem> v = pack.GetCatalogItems();
        for (int i = 0; i < stands.Count && i < v.Count; i++)
        {
            if (v[i] == null || v[i].Description == null)
            {
                GameLogger.Warning(this, "Catalog item not found: " + i);
                continue;
            }
            stands[i].LoadCosmetic(v[i].DisplayName);
        }


        GameLogger.Info(this, $"Packed a total of {v.Count} catalog items");
    }
}
