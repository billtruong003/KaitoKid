using Teabag.Authentication;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using Cysharp.Threading.Tasks;
using Teabag.Player;

public class PurchaseStand : MonoBehaviour
{
    public Cosmetic cosmetic => button.cosmetic;
    CosmeticPreview preview;

    public string cosmeticName;
    public CosmeticBuyButton button;

    private void OnEnable()
    {
        preview = GetComponentInChildren<CosmeticPreview>();
        LoadCosmetic(cosmeticName);
    }

    public void LoadCosmetic(string name)
    {
        if (preview == null)
            preview = GetComponentInChildren<CosmeticPreview>();

        cosmeticName = name;
        button.LoadCosmetic(name);
        preview.Set(button.cosmetic);
    }

    /*
    private void Update()
    {
        if (button.item == null)
            return;

        Transform t = GorillaServiceUtils.LocalGorillaPlayer.headCollider.transform;
        Debug.DrawRay(t.position, t.forward);
    }
    */
}
