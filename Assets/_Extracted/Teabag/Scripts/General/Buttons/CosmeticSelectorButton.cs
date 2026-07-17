using System.Collections.Generic;
using Teabag.Authentication;
using Teabag.Core;
using Teabag.UI;

public class CosmeticSelectorButton : GorillaButton
{
    public bool isArrow;
    public int data;

    public override void OnPress()
    {
        CosmeticsSelector selector = GetComponentInParent<CosmeticsSelector>();
        if (isArrow)
        {
            selector.currentPage += data;
            List<Cosmetic> filtered = CosmeticsSelector.FilterCosmetics(AuthenticationUtils.inventory, selector.currentSlot);
            if (selector.currentPage >= filtered.Count)
                selector.currentPage--;

            if (selector.currentPage < 0)
                selector.currentPage = 0;
        }
        else
        {
            selector.currentSlot = (CosmeticSlot)data;
            selector.currentPage = 0;
        }

        selector.Render();
    }
}
