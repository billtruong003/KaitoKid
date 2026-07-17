using System;
using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using System.Collections;
using System.Collections.Generic;
using Teabag.Core;
using UnityEngine;

namespace Teabag.Gameplay
{
public class Zone : MonoBehaviour
{
    public bool inZone;
    public bool checkHands = true;
    //Vector3 touchPoint;
    Collider[] colliders = Array.Empty<Collider>();

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public virtual void Awake()
    {
        OnLeave();
        foreach (Zone zone in GetComponentsInChildren<Zone>(true))
        {
            zone.OnLeave();
        }

        colliders = GetComponents<Collider>();

        Check();
    }

    public virtual void LateUpdate()
    {
        Check();
    }

    public void Check()
    {
        bool inside = false;
        IHardwareRig hardwareRig = LocalHardwareRig;
        if (hardwareRig == null)
            return;

        Vector3 headPos = hardwareRig.Headset.Position;

        foreach (Collider col in colliders)
        {
            if (!col)
                continue;

            if (Vector3.Distance(col.bounds.center, headPos) > 5f + col.bounds.extents.magnitude)
                continue;

            if (Vector3.Distance(col.ClosestPoint(headPos), headPos) > 5)
                continue;

            bool b = CheckBounds(col, headPos);

            if (checkHands && !b)
                b = CheckBounds(col, hardwareRig.LocomotionController.PlayerRigidbody.position) || CheckBounds(col, hardwareRig.LeftHand.HandTransform) || CheckBounds(col, hardwareRig.RightHand.HandTransform);

            if (b)
            {
                inside = true;
                break;
            }
        }

        if (inside && !inZone)
            OnEnter();
        else if (!inside && inZone)
            OnLeave();

        inZone = inside;
    }

    public bool CheckBounds(Collider collider, Collider other)
    {
        return CheckBounds(collider, other.ClosestPoint(collider.transform.position));
        //return collider.bounds.Contains(other.ClosestPoint(collider.transform.position));
        //touchPoint = collider.bounds.ClosestPoint(other.transform.position);
        //return collider.bounds.Contains(other.transform.position);
    }

    public bool CheckBounds(Collider collider, Component component)
    {
        return CheckBounds(collider, component.transform.position);
        //return collider.bounds.Contains(component.transform.position);
    }

    public bool CheckBounds(Collider collider, Vector3 position)
    {
        return collider.bounds.Contains(position);
    }

    public virtual void OnEnter()
    {
        //Debug.Log("Player entered zone: " + gameObject.name);
        //Debug.Log("Player entered zone: " + StaticRoyaleObject.FullName(gameObject));
    }

    public virtual void OnLeave()
    {
        //Debug.Log("Player left zone: " + gameObject.name);
        //Debug.Log("Player left zone: " + StaticRoyaleObject.FullName(gameObject));
    }

    /*
    private void OnDrawGizmos()
    {
        if (touchPoint != Vector3.zero)
        {
            Gizmos.DrawSphere(touchPoint, 0.5f);
        }
    }
    */
}
}
