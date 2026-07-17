using Teabag.Player.Rig;
using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using UnityEngine.UI;
using Teabag.Gameplay;

public class Scope : MonoBehaviour
{
    [SerializeField] private float _distanceDetect = 20;
    [SerializeField] private float _distanceDefault = 2;
    [SerializeField] private LayerMask _detectLayer;

    private static readonly List<Scope> _scopes = new List<Scope>();

    private readonly RaycastHit[] _raycastHits = new RaycastHit[1];
    public Firearm firearm;
    public Image scope;
    public ScopeState state;
    float t = 0;
    Vector3 originalScale;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (!_scopes.Contains(this))
            _scopes.Add(this);
    }

    private void OnDisable()
    {
        _scopes.Remove(this);
    }

    void Update()
    {
        if (t < Mathf.PI)
            t += Time.deltaTime * 20;
        else
            t = Mathf.PI;

        state = ScopeState.Normal;
        scope.sprite = CurrenStateSprite();

        Vector3 targetDirection = firearm.shootPoint.forward;
        Vector3 startPoint = firearm.shootPoint.position;
        Vector3 targetPosition = startPoint + targetDirection * _distanceDefault;

        if (Physics.RaycastNonAlloc(startPoint, targetDirection, _raycastHits, _distanceDetect, _detectLayer) > 0)
        {
            targetPosition = _raycastHits[0].point;
        }

        transform.position = targetPosition;
        var rig = LocalHardwareRig;
        if (rig != null)
            transform.forward = (transform.position - rig.Headset.Position).normalized;
        transform.localScale = originalScale * (1 + Mathf.Sin(t));
    }

    public void Fire()
    {
        t = 0;
    }

    public static Scope GetScope(Firearm f)
    {
        foreach (Scope scope in _scopes)
        {
            if (scope.firearm == f)
                return scope;
        }

        return null;
    }

    Sprite CurrenStateSprite()
    {
        foreach (ScopeManager.ScopeStateOption option in ScopeManager.instance.options)
        {
            if (option.state == state)
            {
                return option.sprite;
            }
        }

        return null;
    }
}
