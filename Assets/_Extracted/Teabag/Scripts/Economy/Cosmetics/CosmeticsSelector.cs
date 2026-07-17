using Teabag.Player.Rig;
using Teabag.Authentication;
using Teabag.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Networking;
using Teabag.Economy;
using System;
using Teabag.Core;
using Teabag.Player;
using Squido.JungleXRKit.Core;

public class CosmeticsSelector : MonoBehaviour
{
    private static Dictionary<CosmeticSlot, List<string>> s_FilterCache = new();

    [EditorButton(nameof(Render))]
    public List<CosmeticPreview> previews = new List<CosmeticPreview>();
    public DataViewer dataViewer;
    public DefaultButton button;

    public new Renderer renderer;
    private IGorillaService _gorillaService;

    [Header("Info")]
    public CosmeticSlot currentSlot;
    public int currentPage;
    bool hasDone;

    private MaterialPropertyBlock _mpb;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
    }

    private void OnEnable()
    {
        Render();
        button.visual = false;
        GorillaCosmetics.OnCosmeticsChanged += Render;

        PlayerData.OnLogin += Render;

    }

    private void OnDisable()
    {
        GorillaCosmetics.OnCosmeticsChanged -= Render;
        PlayerData.OnLogin -= Render;
    }

    public void Randomise()
    {
        string[] names = Enum.GetNames(typeof(CosmeticSlot));
        for (int i = 0; i < names.Length; i++)
        {
            CosmeticSlot category = (CosmeticSlot)i;

            GameLogger.Info("Randomising slot: " + category);
            List<Cosmetic> filtered = FilterCosmetics(AuthenticationUtils.inventory, category);
            if (filtered.Count <= 0)
            {
                AuthenticationUtils.SetCosmetic(i, string.Empty);
                GameLogger.Info("No cosmetics owned");
                continue;
            }

            Cosmetic cosmetic = filtered[UnityEngine.Random.Range(0, filtered.Count)];
            AuthenticationUtils.SetCosmetic(cosmetic);
        }

        // GorillaMaterial.RandomiseColour();
    }

    public void Render()
    {
        if (hasDone)
            return;

        foreach (CosmeticPreview slots in previews)
        {
            slots.currentCosmetic = new Cosmetic();
            slots.setter.ResetCosmetics();
        }

        List<Cosmetic> filtered = FilterCosmetics(AuthenticationUtils.inventory, currentSlot);
        if (AuthenticationUtils.Cosmetics.ContainsKey(currentSlot))
            button.SetMaterial(
                !string.IsNullOrEmpty(AuthenticationUtils.Cosmetics[currentSlot]));
        else button.SetMaterial(false);

        for (int i = 0; i < previews.Count && i < filtered.Count; i++)
        {
            var add = currentPage * previews.Count;
            if (add + i >= filtered.Count) continue;

            if (filtered[add + i].category == currentSlot)
                previews[i].Set(filtered[add + i]);
        }

        dataViewer.Show(new Dictionary<string, string>()
        {
            {
                "OWNED",
                (AuthenticationUtils.inventory.Count - ConsumablesManager.inventory.Count).ToString()
            },
            {
                "AMOUNT",
                (AuthenticationUtils.catalogItems.Count - ConsumablesManager.catalogItems.Count).ToString()
            },
            {
                "VALUE",
                AuthenticationUtils.cosmeticsValue.ToString()
            }
        });

        hasDone = true;
    }

    private void Update()
    {
        hasDone = false;

        var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
        if (localGorilla)
        {
            renderer.GetPropertyBlock(_mpb, 1);
            _mpb.SetColor(ColorProp, localGorilla.material.colour);
            renderer.SetPropertyBlock(_mpb, 1);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            foreach (RaycastHit hit in hits)
            {
                CosmeticSelectorButton button = hit.collider.GetComponent<CosmeticSelectorButton>();
                if (button != null)
                {
                    button.Press();
                }
                else
                {
                    DefaultButton defaultButton = hit.collider.GetComponent<DefaultButton>();
                    if (defaultButton != null)
                    {
                        defaultButton.Press();
                    }
                }
            }
        }
#endif
    }

    public void TakeOff()
    {
        AuthenticationUtils.SetCosmetic(currentSlot, "");
        Render();
    }

    public static List<Cosmetic> FilterCosmetics(List<InventoryItem> cosmetics, CosmeticSlot slot)
    {
        // TODO: Remove this unlock-all bypass once PlayFab is ready.
        // Currently PlayFab is not connected, so we scan Resources to unlock all cosmetics for testing.
        List<Cosmetic> allCosmetics = new List<Cosmetic>();
        List<string> allNames = CosmeticUtils.GetAllCosmeticNamesForSlot(slot);
        foreach (string name in allNames)
        {
            allCosmetics.Add(new Cosmetic(slot, name));
        }
        if (allCosmetics.Count > 0)
            return allCosmetics;
        // END TODO: unlock-all bypass

        if (!s_FilterCache.ContainsKey(slot))
            s_FilterCache.Add(slot, new List<string>());

        List<Cosmetic> c = new List<Cosmetic>();
        List<string> newCache = new List<string>();
        foreach (InventoryItem cosmetic in cosmetics)
        {
            if (s_FilterCache[slot].Contains(cosmetic.Name))
            {
                c.Add(new Cosmetic(cosmetic));
                continue;
            }

            if (CosmeticUtils.Exists(new Cosmetic(slot, cosmetic.Name)))
            {
                if (CosmeticUtils.CosmeticType(cosmetic.Name) == slot && PacksManager.GetPack(cosmetic.Name) == null)
                {
                    c.Add(new Cosmetic(cosmetic));
                    newCache.Add(cosmetic.Name);
                }
            }
        }

        s_FilterCache[slot].AddRange(newCache);
        return c;
    }
}
