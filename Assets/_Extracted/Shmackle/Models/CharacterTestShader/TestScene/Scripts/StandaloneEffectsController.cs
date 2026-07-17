using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class StandaloneEffectsController : MonoBehaviour
{
    public enum OutlineType { None, Player, BloodJman, ObjectReact }
    public enum XRayType { Standard, BloodJman }

    [Title("CORE MATERIALS")]
    [Tooltip("Vật liệu sẽ được thêm vào renderer để tạo hiệu ứng đóng băng.")]
    [SerializeField] private Material freezeEffectMaterial;

    [BoxGroup("Outline Effect")]
    [SerializeField] private Material playerOutlineMaterial;
    [SerializeField] private Material bloodJmanOutlineMaterial;
    [SerializeField] private Material objectReactOutlineMaterial;

    [BoxGroup("X-Ray Effect")]
    [SerializeField] private Material standardXRayMaterial;
    [SerializeField] private Material bloodJmanXRayMaterial;

    [Title("BLOODJMAN EFFECT")]
    [BoxGroup("BloodJman Effect")]
    [Header("Core References")]
    [Required][SerializeField] private Renderer[] playerRenderersForDissolve;
    [Required][SerializeField] private Renderer bloodJmanRenderer;
    [Required][SerializeField] private Transform scaleTarget;

    [BoxGroup("BloodJman Effect")]
    [Header("Configuration")]
    [SerializeField] private float targetScaleMultiplier = 1.5f;
    [SerializeField] private GameObject[] objectsToDisableOnTransform;

    private readonly Dictionary<Renderer, Material[]> _originalSharedMaterials = new();
    private Sequence _activeBloodJmanSequence;
    private bool _isBloodJmanActive = false;
    private Material _activeOutlineMaterial;
    private Material _activeXRayMaterial;

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private void Awake()
    {
        if (bloodJmanRenderer != null)
        {
            bloodJmanRenderer.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        _activeBloodJmanSequence?.Kill();
        RestoreAllOriginalMaterials();
    }

    #region Overlay Material Management

    public void SetOverlayState(VisualEffectType type, bool active, IEnumerable<Renderer> targetRenderers)
    {
        Material overlayMaterial = GetOverlayMaterialForType(type);
        if (overlayMaterial == null) return;

        // Reset a property to its default when applying the effect
        if (active)
        {
            SetOverlayFloatProperty(type, "_FreezeAmount", 0f); // Example for Freeze
        }

        foreach (var renderer in targetRenderers)
        {
            if (active)
            {
                ApplyEffectMaterialToRenderer(renderer, overlayMaterial);
            }
            else
            {
                RemoveEffectMaterialFromRenderer(renderer, overlayMaterial);
            }
        }
    }

    public void SetOverlayFloatProperty(VisualEffectType type, string propertyName, float value)
    {
        Material overlayMaterial = GetOverlayMaterialForType(type);
        if (overlayMaterial != null && !string.IsNullOrEmpty(propertyName))
        {
            overlayMaterial.SetFloat(Shader.PropertyToID(propertyName), value);
        }
    }

    private Material GetOverlayMaterialForType(VisualEffectType type)
    {
        return type switch
        {
            VisualEffectType.Freeze => freezeEffectMaterial,
            _ => null
        };
    }

    private void ApplyEffectMaterialToRenderer(Renderer renderer, Material effectMaterial)
    {
        if (renderer == null || effectMaterial == null) return;

        if (!_originalSharedMaterials.ContainsKey(renderer))
        {
            _originalSharedMaterials[renderer] = renderer.sharedMaterials;
        }

        var currentMaterials = renderer.sharedMaterials.ToList();
        if (!currentMaterials.Contains(effectMaterial))
        {
            currentMaterials.Add(effectMaterial);
            renderer.sharedMaterials = currentMaterials.ToArray();
        }
    }

    private void RemoveEffectMaterialFromRenderer(Renderer renderer, Material effectMaterial)
    {
        if (renderer == null || effectMaterial == null) return;

        if (_originalSharedMaterials.TryGetValue(renderer, out var originalMaterials))
        {
            renderer.sharedMaterials = originalMaterials;
            _originalSharedMaterials.Remove(renderer);
        }
        else
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Where(m => m != effectMaterial).ToArray();
        }
    }

    private void RestoreAllOriginalMaterials()
    {
        foreach (var kvp in _originalSharedMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sharedMaterials = kvp.Value;
            }
        }
        _originalSharedMaterials.Clear();
    }

    #endregion

    #region Specialized Effect Logic

    public void SetBloodJmanState(bool enable, float duration, Action onComplete = null)
    {
        if (enable == _isBloodJmanActive || playerRenderersForDissolve.Length == 0 || bloodJmanRenderer == null || scaleTarget == null) return;
        _isBloodJmanActive = enable;

        _activeBloodJmanSequence?.Kill();
        _activeBloodJmanSequence = DOTween.Sequence();

        Action onStartAction = enable ?
            (Action)(() =>
            {
                bloodJmanRenderer.gameObject.SetActive(true);
                foreach (var obj in objectsToDisableOnTransform) obj.SetActive(false);
            }) :
            () =>
            {
                foreach (var obj in objectsToDisableOnTransform) obj.SetActive(true);
            };

        Action onCompleteAction = enable ? onComplete :
            () =>
            {
                bloodJmanRenderer.gameObject.SetActive(false);
                onComplete?.Invoke();
            };

        float playerTargetDissolve = enable ? 1f : -1.5f;
        float jmanTargetDissolve = enable ? -1f : 2f;
        float targetScale = enable ? targetScaleMultiplier : 1f;

        _activeBloodJmanSequence.OnStart(() => onStartAction?.Invoke())
            .Append(scaleTarget.DOScale(targetScale, duration).SetEase(Ease.InOutSine))
            .OnComplete(() => onCompleteAction?.Invoke());

        foreach (var renderer in playerRenderersForDissolve)
        {
            if (renderer != null)
                _activeBloodJmanSequence.Join(renderer.material.DOFloat(playerTargetDissolve, DissolveAmountID, duration));
        }

        if (bloodJmanRenderer != null)
            _activeBloodJmanSequence.Join(bloodJmanRenderer.material.DOFloat(jmanTargetDissolve, DissolveAmountID, duration));

        _activeBloodJmanSequence.Play();
    }

    public void SetOutline(OutlineType type)
    {
        Material targetMaterial = GetMaterialFromOutlineType(type);
        if (targetMaterial == _activeOutlineMaterial) return;

        var activeRenderers = GetComponentsInChildren<Renderer>();
        if (_activeOutlineMaterial != null)
        {
            foreach (var r in activeRenderers) RemoveEffectMaterialFromRenderer(r, _activeOutlineMaterial);
        }
        if (targetMaterial != null)
        {
            foreach (var r in activeRenderers) ApplyEffectMaterialToRenderer(r, targetMaterial);
        }

        _activeOutlineMaterial = targetMaterial;
    }

    public void SetXRay(bool enable, XRayType type)
    {
        Material targetMaterial = GetMaterialFromXRayType(type);
        if (targetMaterial == null) return;

        var activeRenderers = GetComponentsInChildren<Renderer>();

        if (_activeXRayMaterial != null)
        {
            foreach (var r in activeRenderers) RemoveEffectMaterialFromRenderer(r, _activeXRayMaterial);
            _activeXRayMaterial = null;
        }

        if (enable)
        {
            foreach (var r in activeRenderers) ApplyEffectMaterialToRenderer(r, targetMaterial);
            _activeXRayMaterial = targetMaterial;
        }
    }

    private Material GetMaterialFromOutlineType(OutlineType type) => type switch
    {
        OutlineType.Player => playerOutlineMaterial,
        OutlineType.BloodJman => bloodJmanOutlineMaterial,
        OutlineType.ObjectReact => objectReactOutlineMaterial,
        _ => null,
    };

    private Material GetMaterialFromXRayType(XRayType type) => type switch
    {
        XRayType.Standard => standardXRayMaterial,
        XRayType.BloodJman => bloodJmanXRayMaterial,
        _ => null,
    };

    #endregion
}