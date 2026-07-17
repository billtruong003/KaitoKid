// Assets/_Shmackle/Scripts/Standalone/StandaloneMaterialController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Shmackle.Data;
using _Shmackle.Scripts.Player;

[System.Serializable]
public class BodyPartRendererMapping
{
    public CharacterPartType partType;
    public Renderer partRenderer;
}

public sealed class StandaloneMaterialController : MonoBehaviour
{
    [SerializeField]
    private List<BodyPartRendererMapping> bodyPartMappings;

    private readonly Dictionary<CharacterPartType, Renderer> _renderers = new();
    private readonly Dictionary<CharacterPartType, DripData_Runtime> _activeDripRuntimes = new();

    private void Awake()
    {
        InitializeRenderers();
    }

    private void InitializeRenderers()
    {
        foreach (var mapping in bodyPartMappings)
        {
            if (mapping.partRenderer != null)
            {
                _renderers[mapping.partType] = mapping.partRenderer;
                mapping.partRenderer.gameObject.SetActive(false);
            }
        }
    }

    public void ApplyDripRuntimes(Dictionary<CharacterPartType, DripData_Runtime> runtimesToApply)
    {
        _activeDripRuntimes.Clear();
        foreach (var renderer in _renderers.Values)
        {
            renderer.gameObject.SetActive(false);
        }

        foreach (var runtimeKvp in runtimesToApply)
        {
            if (_renderers.TryGetValue(runtimeKvp.Key, out var targetRenderer))
            {
                ApplyRuntimeToRenderer(targetRenderer, runtimeKvp.Value);
                _activeDripRuntimes[runtimeKvp.Key] = runtimeKvp.Value;
            }
        }
    }

    public void ApplyMaterialToActiveParts(System.Func<DripData_Runtime, Material> materialSelector)
    {
        foreach (var activeRuntimeKvp in _activeDripRuntimes)
        {
            if (_renderers.TryGetValue(activeRuntimeKvp.Key, out var targetRenderer))
            {
                var material = materialSelector(activeRuntimeKvp.Value);
                if (material != null)
                {
                    targetRenderer.sharedMaterial = material;
                }
            }
        }
    }

    public void ApplyDefaultDripMaterials()
    {
        ApplyMaterialToActiveParts(runtime => runtime.CharacterPartMaterial);
    }

    public IEnumerable<Renderer> GetActiveRenderers()
    {
        return _activeDripRuntimes.Keys
            .Where(partType => _renderers.ContainsKey(partType))
            .Select(partType => _renderers[partType]);
    }

    private void ApplyRuntimeToRenderer(Renderer renderer, DripData_Runtime runtime)
    {
        if (renderer == null || runtime == null) return;

        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            skinnedMeshRenderer.sharedMesh = runtime.CharacterPartMesh;
        }
        else if (renderer.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            meshFilter.sharedMesh = runtime.CharacterPartMesh;
        }

        renderer.sharedMaterial = runtime.CharacterPartMaterial;
        renderer.gameObject.SetActive(true);
    }
}