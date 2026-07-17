using Teabag.Player.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Teabag.Gameplay;

public class ScopeManager : MonoBehaviour
{
    //public static List<Firearm> scopes = new List<Firearm>();
    public static ScopeManager instance;
    public List<ScopeStateOption> options = new List<ScopeStateOption>();
    public Scope scopePrefab;
    public FirearmDisplay firearmDisplayPrefab;

    private void Awake()
    {
        instance = this;

        // Wire GameServices bridges
        Teabag.Core.GameServices.CreateScope = (firearm) => CreateScope((Firearm)firearm);
        Teabag.Core.GameServices.RemoveScope = (firearm) => RemoveScope((Firearm)firearm);
        Teabag.Core.GameServices.ScopeFire = (firearm) =>
        {
            Scope s = Scope.GetScope((Firearm)firearm);
            if (s != null) s.Fire();
        };
        Teabag.Core.GameServices.CreateFirearmDisplay = (firearm, parent) =>
        {
            FirearmDisplay display = Instantiate(firearmDisplayPrefab, parent);
            display.firearm = (Firearm)firearm;
            return display;
        };
        Teabag.Core.GameServices.DestroyFirearmDisplay = (parent) =>
        {
            FirearmDisplay display = parent.GetComponentInChildren<FirearmDisplay>();
            if (display != null) Destroy(display.gameObject);
        };
    }

    public void CreateScope(Firearm firearm)
    {
        var scope = Scope.GetScope(firearm);
        if (scope != null)
        {
            scope.gameObject.SetActive(true);
            return;
        }

        scope = Instantiate(scopePrefab);
        scope.firearm = firearm;
    }

    public void RemoveScope(Firearm firearm)
    {
        Scope scope = Scope.GetScope(firearm);
        if (scope != null)
        {
            //Only hide for reusable purpose.
            scope.gameObject.SetActive(false);
        }
    }

    /*
    public Vector3 targetPosition;
    public Vector3 targetDirection;
    public Transform scopeTransform;
    public Image scope;
    public static ScopeState state;
    public float t = 0;
    Vector3 originalScale;

    private void Awake()
    {
        instance = this;
        originalScale = scopeTransform.localScale;
    }

    public static void SetScope(Vector3 position, Vector3 direction)
    {
        instance.targetPosition = position;
        instance.targetDirection = direction;
    }

    public static void Fire()
    {
        instance.t = 0;
    }

    private void Update()
    {
        if (t < Mathf.PI)
            t += Time.deltaTime * 20;
        else
            t = Mathf.PI;

        if (scopes.Count > 0 && state == ScopeState.None)
            state = ScopeState.Normal;

        foreach (Firearm firearm in scopes)
        {
            if ((DateTime.UtcNow - firearm.reloadStart).TotalMilliseconds < firearm.msReloadTime)
            {
                state = ScopeState.Reload;
                SelectFirearm(firearm);
                break;
            }

            if (Physics.Raycast(firearm.transform.position, firearm.shootPoint.forward, out RaycastHit hit, 5, LayerMask.GetMask("VRRig")))
            {
                state = hit.transform.GetComponentInParent<VRRig>() != null ? ScopeState.Firing : ScopeState.Normal;
                SelectFirearm(firearm);
                break;
            }

            state = ScopeState.Normal;
            SelectFirearm(firearm);
        }

        if (scopes.Count < 1)
            state = ScopeState.None;

        scope.sprite = CurrenStateSprite();
        scope.transform.position = targetPosition + targetDirection * 2;
        scope.transform.forward = (scope.transform.position - GorillaServiceUtils.LocalGorillaPlayer.headCollider.transform.position).normalized;
        scope.transform.localScale = originalScale * (1 + Mathf.Sin(t));
    }

    public void SelectFirearm(Firearm firearm)
    {
        targetPosition = firearm.shootPoint.position;
        targetDirection = firearm.shootPoint.forward;
    }

    Sprite CurrenStateSprite()
    {
        foreach (ScopeStateOption option in options)
        {
            if (option.state == state)
            {
                return option.sprite;
            }
        }

        return null;
    }
    */

    [Serializable]
    public class ScopeStateOption
    {
        public ScopeState state;
        public Sprite sprite;
    }
}

public enum ScopeState
{
    None,
    Normal,
    Firing,
    Reload
}
