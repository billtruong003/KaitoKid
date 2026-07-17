using Fusion;
using Teabag.Networking;
using Teabag.Player;
using Liv.Lck;
using Liv.Lck.Tablet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Gameplay;
using Teabag.Core;
using Squido.JungleXRKit.Core;

public class LCKGrabbable : Grabbable
{
    public LCKCameraController controller;
    public GameObject body;

    private IGorillaService _gorillaService;

    public override bool CanGrab(Grabber holster)
    {
        if (!canGrab) return false;
        if (Vector3.Distance(transform.position, holster.transform.position) > grabRange)
            return false;
        return !grabber || (grabber.isMine && !grabber.hand);
    }

    public override void Spawned()
    {
        base.Spawned();
        canGrab = HasStateAuthority;
        controller.gameObject.SetActive(HasStateAuthority);

        _gorillaService = ServiceLocator.Get<IGorillaService>();
    }

    public override void Render()
    {
        base.Render();

        if (!hand)
            return;

        var localGorilla = _gorillaService.LocalGorilla as Gorilla;
        if (!hand.gorilla || !localGorilla)
            return;

        if (!hand.gorilla.health || !localGorilla.health)
            return;

        body.SetActive(localGorilla.health.isDead || !hand.gorilla.health.isDead);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        if (HasStateAuthority)
        {
            var lckService = LckService.GetService();

            if (!lckService.Success)
            {
                Debug.LogError("Failed to get LCK Service");
                return;
            }

            if (lckService.Result.IsRecording().Result)
                lckService.Result.StopRecording();
        }
    }
}
