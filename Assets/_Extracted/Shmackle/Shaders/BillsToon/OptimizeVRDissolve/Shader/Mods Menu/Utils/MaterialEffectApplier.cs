// Assets/_Shmackle/Scripts/Effects/Utils/MaterialEffectApplier.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MaterialEffectApplier : MonoBehaviour
{
    [Header("Target Renderers")]
    [SerializeField]
    private List<Renderer> targetRenderers = new List<Renderer>();

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    private readonly HashSet<Material> _activeEffectMaterials = new HashSet<Material>();

    #region "All Renderers"

    public void ApplyEffectMaterial(Material effectMaterial)
    {
        if (effectMaterial == null || !_activeEffectMaterials.Add(effectMaterial))
        {
            return;
        }

        if (_originalMaterials.Count == 0)
        {
            CacheOriginalMaterials();
        }

        UpdateAllRendererMaterials();
    }

    public void RemoveEffectMaterial(Material effectMaterial)
    {
        if (effectMaterial == null || !_activeEffectMaterials.Remove(effectMaterial))
        {
            return;
        }

        UpdateAllRendererMaterials();

        if (_activeEffectMaterials.Count == 0)
        {
            RestoreAndClearOriginalMaterials();
        }
    }

    #endregion

    #region "Single Body Renderer"
    public void ApplyEffectMaterialToBody(Material effectMaterial)
    {
        if (effectMaterial == null || !_activeEffectMaterials.Add(effectMaterial))
        {
            return;
        }

        if (_originalMaterials.Count == 0)
        {
            CacheOriginalMaterials();
        }

        UpdateOnlyBodyRenderer();
    }

    public void RemoveEffectMaterialFromBody(Material effectMaterial)
    {
        if (effectMaterial == null || !_activeEffectMaterials.Remove(effectMaterial))
        {
            return;
        }

        UpdateOnlyBodyRenderer();
        if (_activeEffectMaterials.Count == 0)
        {
            RestoreAndClearOriginalMaterials();
        }
    }

    #endregion

    private void CacheOriginalMaterials()
    {
        _originalMaterials.Clear();
        foreach (var renderer in targetRenderers)
        {
            if (renderer != null)
            {
                _originalMaterials[renderer] = renderer.sharedMaterials;
            }
        }
    }

    private void UpdateOnlyBodyRenderer()
    {
        if (targetRenderers.Count > 0 && targetRenderers[0] != null)
        {
            var bodyRenderer = targetRenderers[0];
            if (_originalMaterials.TryGetValue(bodyRenderer, out var baseMaterials))
            {
                bodyRenderer.materials = baseMaterials.Concat(_activeEffectMaterials).ToArray();
            }
        }
    }

    private void UpdateAllRendererMaterials()
    {
        foreach (var renderer in targetRenderers)
        {
            if (renderer != null && _originalMaterials.ContainsKey(renderer))
            {
                var baseMaterials = _originalMaterials[renderer];
                renderer.materials = baseMaterials.Concat(_activeEffectMaterials).ToArray();
            }
        }
    }

    private void RestoreAndClearOriginalMaterials()
    {
        foreach (var renderer in targetRenderers)
        {
            if (renderer != null && _originalMaterials.TryGetValue(renderer, out var originalMats))
            {
                renderer.materials = originalMats;
            }
        }
        _originalMaterials.Clear();
    }
}