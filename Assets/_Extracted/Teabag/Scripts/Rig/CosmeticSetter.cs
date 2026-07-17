using Teabag.Player.Cosmetics;
using Teabag.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teabag.Player
{
public class CosmeticSetter : MonoBehaviour
{
    public bool preview = false;
    public List<VRRigCosmeticSlot> cosmeticSlots = new List<VRRigCosmeticSlot>();
    [HideInInspector] public Dictionary<CosmeticSlot, VRRigCosmeticSlot> slots
    {
        get
        {
            Dictionary<CosmeticSlot, VRRigCosmeticSlot> slots = new Dictionary<CosmeticSlot, VRRigCosmeticSlot>();
            foreach (VRRigCosmeticSlot slot in cosmeticSlots)
                slots.Add(slot.slot, slot);
            return slots;
        }
    }

    private Dictionary<SkinnedMeshRenderer, (Mesh mesh, Material mat)> _originalReplacementData = new();

    private void Awake()
    {
        foreach (VRRigCosmeticSlot slot in cosmeticSlots)
        {
            if (slot.type == CosmeticSlotType.Replacement && slot.renderers != null)
            {
                foreach (SkinnedMeshRenderer target in slot.renderers)
                {
                    if (target != null && !_originalReplacementData.ContainsKey(target))
                    {
                        _originalReplacementData.Add(target, (target.sharedMesh, target.sharedMaterial));
                    }
                }
            }
        }
    }

    public List<Cosmetic> GetCosmetics()
    {
        List<Cosmetic> cosmetics = new List<Cosmetic>();
        foreach (VRRigCosmeticSlot slot in cosmeticSlots)
        {
            if (!string.IsNullOrEmpty(slot.currentCosmetic.cosmetic))
            {
                cosmetics.Add(slot.currentCosmetic);
            }
        }

        return cosmetics;
    }

    public void SetCosmetic(Cosmetic cosmetic)
    {
        for (int i = 0; i < cosmeticSlots.Count; i++)
        {
            VRRigCosmeticSlot slot = cosmeticSlots[i];
            if (slot.slot == cosmetic.category)
            {
                // Update tracked data
                slot.currentCosmetic = cosmetic;
                cosmeticSlots[i] = slot;

                if (slot.type == CosmeticSlotType.Replacement)
                {
                    GameObject prefab = CosmeticUtils.Load(cosmetic.category.ToString(), cosmetic.cosmetic);

                    if (prefab != null)
                    {
                        SkinnedMeshRenderer source = prefab.GetComponentInChildren<SkinnedMeshRenderer>();

                        if (slot.renderers != null && source != null)
                        {
                            foreach (SkinnedMeshRenderer target in slot.renderers)
                            {
                                if (target == null) continue;
                                target.sharedMesh = source.sharedMesh;
                                target.sharedMaterial = source.sharedMaterial;
                            }
                        }
                    }
                    else
                    {
                        // Restore original mesh and material for Replacement slots if no prefab found (e.g. unequip)
                        if (slot.renderers != null)
                        {
                            foreach (SkinnedMeshRenderer target in slot.renderers)
                            {
                                if (target != null && _originalReplacementData.TryGetValue(target, out var data))
                                {
                                    target.sharedMesh = data.mesh;
                                    target.sharedMaterial = data.mat;
                                }
                            }
                        }
                        
                        // Clear tracking data if un-equipping
                        slot.currentCosmetic = new Cosmetic();
                        cosmeticSlots[i] = slot;
                    }
                    continue;
                }

                // Attachment logic
                if (slot.slotParent != null)
                {
                    // Remove current child if un-equipping or switching
                    foreach (Transform t in slot.slotParent)
                        CosmeticUtils.DestroyCosmetic(t.gameObject);

                    if (!string.IsNullOrEmpty(cosmetic.cosmetic))
                    {
                        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                        GameObject obj = CosmeticUtils.InstantiateCosmetic(
                            cosmetic, slot.slotParent, preview ? string.Empty : "DEFAULT");
                        
                        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                        SetPreview(obj);
                    }
                }
            }
        }
    }

    public void SetCosmetic(string cosmetic)
    {
        CosmeticSlot type = CosmeticUtils.CosmeticType(cosmetic);
        SetCosmetic(new Cosmetic(type, cosmetic));
    }

    public void SetCosmetic(string cosmetic, Vector3 offset)
    {
        CosmeticSlot type = CosmeticUtils.CosmeticType(cosmetic);
        SetCosmetic(new Cosmetic(type, cosmetic));

        // Offset only applies to attachments
        for (int i = 0; i < cosmeticSlots.Count; i++)
        {
            if (cosmeticSlots[i].slot == type && cosmeticSlots[i].type == CosmeticSlotType.Attachment && cosmeticSlots[i].slotParent.childCount > 0)
            {
                cosmeticSlots[i].slotParent.GetChild(0).localPosition = offset;
            }
        }
    }

    public void SetCosmetics(Dictionary<int, string> cosmetics)
    {
        foreach (KeyValuePair<int, string> cosmetic in cosmetics)
        {
            SetCosmetic(new Cosmetic((CosmeticSlot)cosmetic.Key, cosmetic.Value));
        }
    }

    public void SetCosmetics(List<Cosmetic> cosmetics)
    {
        foreach (Cosmetic cosmetic in cosmetics)
        {
            SetCosmetic(cosmetic);
        }
    }

    public void SetPreview(GameObject obj)
    {
        if (obj != null)
        {
            InteractiveCosmetic interactive = obj.GetComponentInChildren<InteractiveCosmetic>();
            if (interactive != null)
            {
                interactive.preview = preview;
                //Debug.Log("Set " + obj.name + "'s preview to be " + preview);
            }
        }
    }

    public float GetNameOffset()
    {
        float max = 0;
        foreach (VRRigCosmeticSlot slot in cosmeticSlots)
        {
            if (slot.slotParent == null) continue;
            InteractiveCosmetic interactive = slot.slotParent.GetComponentInChildren<InteractiveCosmetic>();
            if (interactive != null)
            {
                if (interactive.nameOffset > max)
                {
                    //Debug.Log(interactive.name + ": " + interactive.nameOffset);
                    max = interactive.nameOffset;
                }
            }
        }
        return max;
    }

    public float GetHealthOffset()
    {
        float max = 0;
        foreach (VRRigCosmeticSlot slot in cosmeticSlots)
        {
            if (slot.slotParent == null) continue;
            InteractiveCosmetic interactive = slot.slotParent.GetComponentInChildren<InteractiveCosmetic>();
            if (interactive != null)
            {
                if (interactive.healthOffset > max)
                {
                    //Debug.Log(interactive.name + ": " + interactive.nameOffset);
                    max = interactive.healthOffset;
                }
            }
        }
        return max;
    }

    public void ResetCosmetics()
    {
        for (int i = 0; i < cosmeticSlots.Count; i++)
        {
            VRRigCosmeticSlot slot = cosmeticSlots[i];
            
            // Clear tracking data
            slot.currentCosmetic = new Cosmetic();
            cosmeticSlots[i] = slot;

            if (slot.type == CosmeticSlotType.Replacement && slot.renderers != null)
            {
                foreach (SkinnedMeshRenderer target in slot.renderers)
                {
                    if (target != null && _originalReplacementData.TryGetValue(target, out var data))
                    {
                        target.sharedMesh = data.mesh;
                        target.sharedMaterial = data.mat;
                    }
                }
                continue;
            }

            if (slot.slotParent != null)
            {
                foreach (Transform t in slot.slotParent)
                {
                    CosmeticUtils.DestroyCosmetic(t.gameObject);
                }
            }
        }
    }
}
}
