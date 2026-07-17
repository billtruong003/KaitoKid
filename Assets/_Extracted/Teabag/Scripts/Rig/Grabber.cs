using Fusion;
using Teabag.Player;
using Teabag.Player.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;
using Squido.JungleXRKit.Core;

namespace Teabag.Player
{
//[OrderAfter(typeof(VRRigHand))]
public class Grabber : NetworkBehaviour
{
    [NonSerialized] public GorillaHand hand;
    [Networked, OnChangedRender(nameof(OnGrab))]
    public Grabbable grabbable { get; set; }
    private Grabbable lastGrabbable;
    private IGorillaService _gorillaService;
    private Collider _collider;

    //public Material previewMaterial;

    [NonSerialized] public bool releaseOld = true;

    [SerializeField]
    private Transform grabPoint;
    public Transform GrabPoint
    {
        get
        {
            if (grabPoint == null)
                return transform;

            return grabPoint;
        }
    }

    public bool isMine
    {
        get
        {
            return HasStateAuthority;
        }
    }

    public GameObject preview;
    private void Awake()
    {
        hand = GetComponent<GorillaHand>();
        _collider = GetComponent<Collider>();
        Grabbable.onGrabbableSpawned += OnGrabbableSpawned;
    }

    public override void Spawned()
    {
        base.Spawned();
        OnGrab();
    }

    public void OnGrabbableSpawned(Grabbable _grabbable)
    {
        if (Object == null || !Object.IsValid)
            return;

        if (grabbable == _grabbable)
            _grabbable.grabber = this;
    }

    public void OnGrab()
    {
        //Debug.Log($"{this} is performing an action");
        //Debug.Log(grabbable);

        StopPreview();

        Grabbable currentGrabbable = grabbable;

        if (currentGrabbable != null)
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            var grabLocal = _gorillaService?.LocalGorilla as Gorilla;
            if (grabLocal != null)
            {
                foreach (var grabber in grabLocal.GetComponentsInChildren<Grabber>())
                {
                    if (grabber.grabbable == currentGrabbable && grabber != this)
                    {
                        // If another grabber has the same grabbable, release it first before taking it
                        // Debug.Log($"Another grabber ({grabber}) already has the grabbable {currentGrabbable}. Releasing it.");
                        if (grabber.HasStateAuthority)
                        {
                            grabber.Release();
                        }
                    }
                }
            }

            // Take it after ensuring it has been released by other grabbers
            currentGrabbable.grabber = this;

            if (currentGrabbable != grabbable)
                return;

            if (currentGrabbable.GetComponent("Grenade") != null)
            {
                if (currentGrabbable.colliders != null)
                {
                    foreach (Collider collider in currentGrabbable.colliders)
                        collider.enabled = hand != null;
                }
            }
        }
        if (lastGrabbable != null && lastGrabbable != grabbable && releaseOld)
        {
            // When released
            if (lastGrabbable.grabber == this)
            {
                if (hand == null)
                {
                    if (lastGrabbable.colliders != null)
                    {
                        foreach (Collider collider in lastGrabbable.colliders)
                            collider.enabled = true;
                    }
                }

                lastGrabbable.grabber = null;

                // This is just so no weird issues happen
                // if (VRRig.rigs != null)
                // {
                //     foreach (VRRig rig in VRRig.rigs)
                //     {
                //         if (rig == null)
                //             continue;
                //
                //         foreach (var grabber in rig.GetComponentsInChildren<Grabber>())
                //         {
                //             if (grabber.grabbable == lastGrabbable)
                //                 lastGrabbable.grabber = grabber;
                //         }
                //     }
                // }
            }
        }

        releaseOld = true;
        lastGrabbable = grabbable;

        StopPreview();

        if (lastGrabbable != null)
        {
            if (lastGrabbable.renderers != null)
            {
                foreach (Renderer renderer in lastGrabbable.renderers)
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
            }
        }

        if (_collider != null)
            _collider.enabled = grabbable == null;

    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (lastGrabbable != null && grabbable != null)
        {
            if (grabbable.grabber != this)
            {
                grabbable = null;
            }
        }

        if (grabbable != null)
            grabbable.UpdatePosition();
    }

    public virtual bool CanGrab(Grabbable grabbable)
    {
        return true;
    }

    public override void Render()
    {
        base.Render();
        if (grabbable != null)
            grabbable.UpdatePosition();
    }

    public void Release()
    {
        if (!Object.HasStateAuthority)
            Debug.LogError("You do not have authority to perform an action on this holster");

        StopPreview();

        if (grabbable != null)
        {
            grabbable.grabber = null;
            grabbable = null;
        }
    }

    public void Grab(Grabbable go)
    {
        if (Object == null)
        {
            Debug.LogError("Grabber object is null");
            return;
        }

        if (!Object.IsValid)
        {
            Debug.LogError("Grabber object is not valid");
            return;
        }

        if (!Object.HasStateAuthority)
        {
            Debug.LogError("You do not have authority to perform an action on this holster");
            return;
        }

        StopPreview();

        grabbable = go;
    }

    public void Preview(Grabbable go)
    {
        if (preview != null)
            preview.SetActive(true);
    }

    public void StopPreview()
    {
        if (preview != null)
            preview.SetActive(false);
    }

    public bool ChildContains<T>(GameObject gameObject) where T : Component
    {
        int amount = gameObject.GetComponentsInChildren<T>().Length;
        Debug.Log($"{gameObject.name} contains {typeof(T).Name} {amount} time(s)");
        return amount > 0;
    }

    private void OnDestroy()
    {
        if (lastGrabbable != null)
            lastGrabbable.grabber = null;

        Grabbable.onGrabbableSpawned -= OnGrabbableSpawned;
    }
}
}
