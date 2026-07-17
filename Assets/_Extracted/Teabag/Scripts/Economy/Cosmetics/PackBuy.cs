using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using Teabag.Core;

public class PackBuy : MonoBehaviour
{
    public string packName;
    public PackShelf shelf;

    [Header("Toggle")]
    public List<GameObject> notOwned = new List<GameObject>();
    public List<GameObject> owned = new List<GameObject>();

    [Header("Info")]
    public TextMeshPro text;
    public TextMeshPro price;

    [Header("Visuals")]
    public MoneyParticles particles;

    Pack pack;
    bool doing = false;

    private void Awake()
    {
        if (PlayerData.loggedIn) Load();
        else PlayerData.OnLogin += Load;

    }

    public async void Load()
    {
        pack = PacksManager.GetPack(packName);
        CheckOwnership();

        if (!(GameServices.IsPlatformInitialized?.Invoke() ?? false))
            return;

        if (GameServices.GetIAPProductAsync == null)
            return;

        var i = await GameServices.GetIAPProductAsync(pack.sku);
        if (i.isError)
        {
            price.text = "ERROR";
            return;
        }
        price.text = i.formattedPrice;
    }

    public void CheckOwnership()
    {
        bool owns = pack.owns;
        if (owns) doing = true;
        if (owns) text.text = "OWNED";

        foreach (GameObject obj in owned) obj.SetActive(owns);
        foreach (GameObject obj in notOwned) obj.SetActive(!owns);
    }

    public async UniTaskVoid Buy()
    {
        if (doing) return;
        doing = true;

        text.text = "LOADING";

        if (GameServices.PurchaseIAPAsync == null)
        {
            text.text = "ERROR";
            doing = false;
            return;
        }

        bool isError = await GameServices.PurchaseIAPAsync(pack.sku);
        if (isError)
        {
            text.text = "ERROR";
            await UniTask.Delay(2000);
            text.text = "BUY";
            doing = false;
            return;
        }

        if (particles && PacksManager.GetPack(packName).currency > 0)
            particles.Play(0);

        CheckOwnership();
    }

    private void OnDestroy()
    {
        PlayerData.OnLogin -= Load;
    }
}
