using Fusion;
using Teabag.Authentication;
using PlayFab.ClientModels;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Teabag.Core;
using Teabag.UI;

public class BunnyPresser : NetworkBehaviour
{
    public static IReadOnlyList<BunnyPresser> Pressers => _pressers;
    private static readonly List<BunnyPresser> _pressers = new List<BunnyPresser>();

    public GorillaButton button;
    public float checkRadius = 0.5f;

    public Animator animator;
    public AudioSource audioSource;

    [Networked]
    public Bunny targetBunny { get; set; }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public State state { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        OnStateChanged();
    }

    private void OnEnable()
    {
        if (!_pressers.Contains(this))
            _pressers.Add(this);
    }

    private void OnDisable()
    {
        _pressers.Remove(this);
    }

    public void OnStateChanged()
    {
        switch (state)
        {
            case State.Ready:
                if (targetBunny != null)
                    button.interactable = !AuthenticationUtils.OwnsCosmetic(targetBunny.race.cosmetic);
                break;
            case State.Converting:
                animator.SetTrigger("Press");
                audioSource.Play();
                button.interactable = false;
                break;
            default:
                button.interactable = false;
                break;
        }
    }

    public void Press()
    {
        if (state != State.Ready)
            return;

        RPCPress();
    }

    [Rpc(targets: RpcTargets.All, sources: RpcSources.StateAuthority)]
    public async void RPCReportCovnert(PlayerRef target)
    {
        string cosmeticName = targetBunny.race.cosmetic;
        targetBunny.RPCDestroy();

        if (target != Runner.LocalPlayer)
            return;

        if (AuthenticationUtils.OwnsCosmetic(cosmeticName))
        {
            Debug.LogError("Already owns cosmetic");
            return;
        }

        CatalogItem item = AuthenticationUtils.catalogItems.GetItem(cosmeticName);

        if (item == null)
        {
            Debug.LogError("Failed to get item: " + cosmeticName);
            return;
        }

        var purchase = await PlayFabAsyncClientAPI.PurchaseItemAsync(new PurchaseItemRequest()
        {
            ItemId = item.ItemId,
            CatalogVersion = "Cosmetics",
            VirtualCurrency = "BA",
            Price = 0
        });

        if (purchase.IsError)
        {
            Debug.LogError("Failed to get bunny: " + purchase.Error.ErrorMessage);
            return;
        }

        Debug.Log("Got bunny cosmetic");
        AuthenticationUtils.inventory.Add(new InventoryItem(item, purchase.Result.Items[0]));
        AuthenticationUtils.SetCosmetic(CosmeticSlot.Head, cosmeticName);
        CosmeticOwnershipShower.RefreshAll();
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public async void RPCPress(RpcInfo info = default)
    {
        if (state != State.Ready)
            return;

        if (targetBunny == null)
            return;

        state = State.Converting;

        targetBunny.RPCConvertBegin();

        await UniTask.Delay(400);

        RPCReportCovnert(info.Source);

        state = State.Converted;

        await UniTask.Delay(1000);

        state = State.Idle;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (HasStateAuthority)
        {
            switch (state)
            {
                case State.Idle:
                    targetBunny = GetBunny();
                    if (targetBunny != null)
                        state = State.Ready;
                    break;
                case State.Ready:
                    targetBunny = GetBunny();
                    if (targetBunny == null)
                        state = State.Idle;
                    break;
                default:
                    break;
            }
        }
    }

    public Bunny GetBunny()
    {
        foreach (Bunny bunny in Bunny.Bunnies)
        {
            if (Vector3.Distance(transform.position, bunny.transform.position) < checkRadius)
                return bunny;
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }

    public enum State
    {
        Idle,
        Ready,
        Converting,
        Converted
    }
}
