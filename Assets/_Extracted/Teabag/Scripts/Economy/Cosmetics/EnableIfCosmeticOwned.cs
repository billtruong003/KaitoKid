using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;

public class EnableIfCosmeticOwned : MonoBehaviour
{
    public new GameObject gameObject;
    public GameObject notGameObject;
    public string cosmeticId;

    private void Update() => ChangeVisibility();

    public void ChangeVisibility()
    {
        bool owned = false;
        if (cosmeticId.Contains(","))
        {
            string[] cosmetics = cosmeticId.Split(',');
            foreach (var id in cosmetics)
            {
                if (AuthenticationUtils.inventory.InventoryContains(id.Trim()))
                {
                    owned = true;
                    break;
                }
            }
        }
        else
            owned = AuthenticationUtils.inventory.InventoryContains(cosmeticId);

        if (gameObject) gameObject.SetActive(owned);
        if (notGameObject) notGameObject.SetActive(!owned);
    }
}
