using Fusion;
using Teabag.Player;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Teabag.Core;

namespace Teabag.Gameplay
{
public class NonGrabbableItem : Grabbable
{
    [Networked]
    public NonGrabbableBackpackItem item { get; set; }

    public List<TextMeshPro> amountTexts;
    protected override void Awake()
    {
        base.Awake();
        takeStateOnGrab = false;
    }

    public override void Render()
    {
        base.Render();
        foreach (TextMeshPro amountText in amountTexts)
        {
            amountText.text = item.amount.ToString();
        }
    }

    public override async void OnGrab(Grabber holster)
    {
        base.OnGrab(holster);

        canGrab = false;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = false;

        if (holster.isMine)
        {
            if (!HasStateAuthority)
                await RequestStateAuthorityAsync();

            if (HasStateAuthority)
            {
                AddToBackpack();
                Runner.Despawn(Object);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!canGrab && HasStateAuthority)
            Runner.Despawn(Object);
    }

    public void AddToBackpack()
    {
        Backpack.myBackpack.AddNonGrabbable(item);
        GameServices.DisplayPopup?.Invoke("+" + item.amount, transform.position, 0.3f);
    }
}
}
