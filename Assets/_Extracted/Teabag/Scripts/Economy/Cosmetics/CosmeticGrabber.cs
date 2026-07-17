using Fusion;
using Teabag.Networking;
using Teabag.Player.Rig;
using System;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;
using WebSocketSharp;
using Teabag.Player;
using Teabag.Gameplay;

public class CosmeticGrabber : NetworkBehaviour
{
    //public static Dictionary<bool, CosmeticGrabber> grabbers = new Dictionary<bool, CosmeticGrabber>();
    public GorillaHand hand;
    public CosmeticSetter cosmeticSetter;

    [Networked, OnChangedRender(nameof(OnCosmeticSwitched))]
    public NetworkString<_32> cosmetic { get; set; }
    CosmeticSlot slot;

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority)
            return;

        if (string.IsNullOrEmpty(cosmetic.ToString()))
        {
            foreach (CosmeticPreview preview in CosmeticPreview.Previews)
            {
                if (Vector3.Distance(preview.head.position, transform.position) < 0.25f)
                {
                    //Debug.Log("Distance: " + preview.name);
                    if (VRInputHandler.GetInputDown(hand.isLeftHand, InputType.Grip))
                    {
                        Debug.Log("Grab");
                        Grab(preview);
                    }
                }
            }
        }
        else
        {
            CheckCosmeticDistance();

            if (!VRInputHandler.GetInputDown(hand.isLeftHand, InputType.Grip))
            {
                Release();
            }
        }
    }

    void Grab(CosmeticPreview preview)
    {
        if (!string.IsNullOrEmpty(cosmetic.ToString()))
            return;

        if (hand.isGrabbed)
            return;

        if (preview.GetComponentInParent<PurchaseStand>() != null)
        {
            if (preview.GetComponentInParent<PurchaseStand>().button.state != CosmeticBuyButton.State.OWNED && preview.GetComponentInParent<PurchaseStand>().button.state != CosmeticBuyButton.State.BOUGHT)
                return;
        }

        cosmetic = preview.currentCosmetic.cosmetic;
        slot = preview.currentCosmetic.category;
        hand.isGrabbed = true;
    }

    void Release()
    {
        cosmetic = string.Empty;
        hand.isGrabbed = false;
    }

    public void OnCosmeticSwitched()
    {
        if (!string.IsNullOrEmpty(cosmetic.ToString()))
            cosmeticSetter.SetCosmetic(cosmetic.ToString());
        else cosmeticSetter.ResetCosmetics();
    }

    private bool CheckCosmeticDistance()
    {
        if (NearPoint(slot))
        {
            PlayerData.SetCosmetic?.Invoke(slot, cosmetic.ToString());
            cosmetic = string.Empty;
            return true;
        }

        return false;
    }

    public bool NearPoint(CosmeticSlot slot)
    {
        Gorilla gorilla = GetComponentInParent<Gorilla>();
        switch (slot)
        {
            case CosmeticSlot.Head:
            case CosmeticSlot.Face:
                return Vector3.Distance(gorilla.headTransform.position, transform.position) < 0.25f;
            case CosmeticSlot.Nuts:
                return Vector3.Distance(gorilla.headTransform.position + new Vector3(0, -0.5f, 0), transform.position) < 0.3f;
            default:
                return false;
        }
    }
}
