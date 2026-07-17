using Fusion;
using Teabag.Player.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;

namespace Teabag.Player
{
public class GorillaCosmetics : NetworkBehaviour
{
    public static event Action OnCosmeticsChanged;
    public CosmeticSetter cosmeticSetter;
    
    [Networked, OnChangedRender(nameof(InternalOnCosmeticsChanged)), Capacity(10)]
    public NetworkDictionary<CosmeticSlot, NetworkString<_32>> cosmetics { get; }
    
    public override void Spawned()
    {
        base.Spawned();

        // start loading
        LoadCosmetics();
        InternalOnCosmeticsChanged();
    }

    public void LoadCosmetics()
    {
        // only initialize if we have state authority
        if (!HasStateAuthority) return;
        
        cosmetics.Clear();
        foreach (var kv in PlayerCosmeticsHelper.GetSavedCosmetics())
            cosmetics.Set(kv.Key, kv.Value);
    }

    public void InternalOnCosmeticsChanged()
    {
        foreach (var kv in cosmetics)
            cosmeticSetter.SetCosmetic(new Cosmetic(kv.Key, kv.Value.ToString()));

        foreach (VRRigCosmeticSlot slot in cosmeticSetter.cosmeticSlots)
        {
            foreach (Renderer childRenderer in slot.slotParent.GetComponentsInChildren<Renderer>())
            {
                if (HasStateAuthority)
                {
                    List<CosmeticSlot> shownSlots = new List<CosmeticSlot>() { CosmeticSlot.Nuts };
                    childRenderer.gameObject.layer = shownSlots.Contains(slot.slot) ? LayerMask.NameToLayer("Cosmetics") : LayerMask.NameToLayer("Culled");
                }
                else childRenderer.gameObject.layer = LayerMask.NameToLayer("Cosmetics");
            }
        }

        OnCosmeticsChanged?.Invoke();
    }
}
}
