using Cysharp.Threading.Tasks;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Authentication;
using Teabag.Networking;
using Teabag.Core;
using UnityEngine;
using UnityEngine.Events;
using Teabag.UI;

public class FictionalPurchaseButton : GorillaButton
{
    [Header("Purchase")]
    public int amount;
    public UnityEvent onPurchase;

    [Header("Spawn")]
    public Transform spawnPoint;
    public NetworkObject spawnObject;

    public override void OnPress()
    {
        Purchase();
    }

    public async UniTaskVoid Purchase()
    {
        interactable = false;
        bool result = await AuthenticationUtils.SubtractCurrencyAsync(amount);
        interactable = true;
        var networkManager = ServiceLocator.Get<NetworkManager>();
        if (result)
        {
            onPurchase.Invoke();
            networkManager.Runner.Spawn(spawnObject, spawnPoint.transform.position);
        }
    }
}
