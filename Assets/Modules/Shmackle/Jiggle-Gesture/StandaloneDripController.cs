using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Shmackle.Data;
using Shmackle.Data.ScriptableObjects;
using Sirenix.OdinInspector;
using DG.Tweening;
using _Shmackle.Scripts.Player;
using System;

public enum VisualEffectType
{
    None,
    Gert,
    GenericDissolve,
    BloodJman,
    Freeze
}

[System.Serializable]
public class VisualEffectSettings
{
    public VisualEffectType EffectType;

    [Tooltip("Thời gian chuyển đổi của hiệu ứng.")]
    public float TransitionDuration = 1.5f;

    [Header("Capabilities")]
    [Tooltip("Hiệu ứng này có thêm một lớp material lên trên không? (Ví dụ: Freeze)")]
    public bool UsesOverlayMaterial;

    [Tooltip("Hiệu ứng này có điều khiển một chuỗi hành động phức tạp không? (Ví dụ: BloodJman)")]
    public bool IsComplexSequence;

    [Header("Threshold Configuration")]
    [Tooltip("Tên thuộc tính trong Shader để điều khiển ngưỡng trên MATERIAL GỐC. (Ví dụ: _DissolveAmount)")]
    [HideIf("UsesOverlayMaterial")]
    public string BaseMaterialShaderProperty;

    [Tooltip("Tên thuộc tính trong Shader để điều khiển ngưỡng trên MATERIAL OVERLAY. (Ví dụ: _FreezeAmount)")]
    [ShowIf("UsesOverlayMaterial")]
    public string OverlayMaterialShaderProperty;

    public float StartValue = 0f;
    public float EndValue = 1f;
}

[System.Serializable]
public class BodyPartMapping
{
    public CharacterPartType partType;
    public Renderer partRenderer;
}

public sealed class StandaloneDripController : MonoBehaviour
{
    [Title("Core Dependencies")]
    [Required][SerializeField] private DripDataContainer dripDataContainer;
    [Required("Cần tham chiếu đến controller hiệu ứng chính.")]
    [SerializeField] private StandaloneEffectsController effectsController;

    [Title("Effects Configuration")]
    [InfoBox("Định nghĩa TẤT CẢ các hiệu ứng và cách chúng được thực thi tại đây.")]
    [SerializeField]
    private List<VisualEffectSettings> visualEffectSettings;

    [Tooltip("Map each character part type to a Renderer in the scene.")]
    [SerializeField]
    [ListDrawerSettings(IsReadOnly = true)]
    private List<BodyPartMapping> bodyPartMappings;

    [Title("Fallback Materials")]
    [SerializeField] private Material defaultBodyDissolveMaterial;

    private readonly Dictionary<DripPack, DripData> _equippedDrips = new();
    private readonly Dictionary<CharacterPartType, Renderer> _partRenderers = new();
    private readonly Dictionary<CharacterPartType, DripData_Runtime> _activeRuntimeParts = new();
    private readonly Dictionary<VisualEffectType, Tween> _activeTweens = new();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> _propertyBlocks = new();

    private void OnDestroy()
    {
        foreach (var tween in _activeTweens.Values)
        {
            tween?.Kill();
        }
        _activeTweens.Clear();
    }

    #region Public API for Effects (For UI Buttons)

    public void SetGertState(bool isGert) => SetEffectState(VisualEffectType.Gert, isGert);
    public void SetGenericDissolve(bool enable) => SetEffectState(VisualEffectType.GenericDissolve, enable);
    public void SetBloodJmanState(bool isBloodJman) => SetEffectState(VisualEffectType.BloodJman, isBloodJman);
    public void SetFreezeState(bool isFrozen) => SetEffectState(VisualEffectType.Freeze, isFrozen);

    public void SetOutlineState(StandaloneEffectsController.OutlineType type) => effectsController?.SetOutline(type);
    public void SetXRayState(bool enable, StandaloneEffectsController.XRayType type) => effectsController?.SetXRay(enable, type);

    #endregion

    #region Unified Effect Logic

    private void SetEffectState(VisualEffectType effectType, bool enable)
    {
        VisualEffectSettings settings = visualEffectSettings.FirstOrDefault(e => e.EffectType == effectType);
        if (settings == null)
        {
            Debug.LogError($"Configuration for effect '{effectType}' not found.");
            return;
        }

        if (_activeTweens.TryGetValue(effectType, out Tween existingTween))
        {
            existingTween?.Kill();
        }

        if (settings.IsComplexSequence)
        {
            ExecuteComplexSequence(settings, enable);
            return;
        }

        if (enable)
        {
            PrepareMaterialsForEffect(settings);
        }

        float startValue = enable ? settings.StartValue : settings.EndValue;
        float endValue = enable ? settings.EndValue : settings.StartValue;
        float currentValue = startValue;

        Tween newTween = DOTween.To(() => currentValue, x => currentValue = x, endValue, settings.TransitionDuration)
            .SetEase(Ease.Linear)
            .OnUpdate(() => UpdateEffectValue(settings, currentValue))
            .OnComplete(() =>
            {
                if (!enable)
                {
                    CleanupMaterialsAfterEffect(settings);
                }
                _activeTweens.Remove(effectType);
            });

        _activeTweens[effectType] = newTween;
    }

    private void PrepareMaterialsForEffect(VisualEffectSettings settings)
    {
        if (settings.UsesOverlayMaterial)
        {
            effectsController?.SetOverlayState(settings.EffectType, true, GetActiveRenderers());
        }
        else
        {
            Action materialSwapAction = settings.EffectType switch
            {
                VisualEffectType.Gert => ApplyGertMaterials,
                VisualEffectType.GenericDissolve => ApplyDissolveMaterials,
                _ => null
            };
            materialSwapAction?.Invoke();
        }
    }

    private void UpdateEffectValue(VisualEffectSettings settings, float value)
    {
        if (settings.UsesOverlayMaterial && !string.IsNullOrEmpty(settings.OverlayMaterialShaderProperty))
        {
            effectsController?.SetOverlayFloatProperty(settings.EffectType, settings.OverlayMaterialShaderProperty, value);
        }
        else if (!string.IsNullOrEmpty(settings.BaseMaterialShaderProperty))
        {
            SetMaterialPropertyOnAllParts(settings.BaseMaterialShaderProperty, value);
        }
    }

    private void CleanupMaterialsAfterEffect(VisualEffectSettings settings)
    {
        if (settings.UsesOverlayMaterial)
        {
            effectsController?.SetOverlayState(settings.EffectType, false, GetActiveRenderers());
        }
        else
        {
            ApplyDefaultMaterials();
        }
    }

    private void ExecuteComplexSequence(VisualEffectSettings settings, bool enable)
    {
        if (effectsController != null && settings.EffectType == VisualEffectType.BloodJman)
        {
            if (enable)
            {
                ApplyDissolveMaterials();
                effectsController.SetBloodJmanState(true, settings.TransitionDuration);
            }
            else
            {
                effectsController.SetBloodJmanState(false, settings.TransitionDuration, ApplyDefaultMaterials);
            }
        }
    }

    private void SetMaterialPropertyOnAllParts(string propertyName, float value)
    {
        int propertyID = Shader.PropertyToID(propertyName);
        foreach (var partType in _activeRuntimeParts.Keys)
        {
            if (_partRenderers.TryGetValue(partType, out var renderer))
            {
                if (!_propertyBlocks.ContainsKey(renderer))
                {
                    _propertyBlocks[renderer] = new MaterialPropertyBlock();
                }
                renderer.GetPropertyBlock(_propertyBlocks[renderer]);
                _propertyBlocks[renderer].SetFloat(propertyID, value);
                renderer.SetPropertyBlock(_propertyBlocks[renderer]);
            }
        }
    }

    private IEnumerable<Renderer> GetActiveRenderers()
    {
        return _activeRuntimeParts.Keys
            .Where(partType => _partRenderers.ContainsKey(partType))
            .Select(partType => _partRenderers[partType]);
    }

    #endregion

    #region Material & Visual Application
    private void ApplyAllVisuals()
    {
        _propertyBlocks.Clear();
        foreach (var renderer in _partRenderers.Values)
        {
            renderer.gameObject.SetActive(false);
            renderer.SetPropertyBlock(null);
        }
        _activeRuntimeParts.Clear();

        var runtimesToApply = new Dictionary<CharacterPartType, DripData_Runtime>();
        _equippedDrips.Values.OrderBy(drip => drip.pack == DripPack.Outfit ? 0 : 1)
            .SelectMany(drip => drip.runtimeCollection)
            .ToList()
            .ForEach(runtime => runtimesToApply[runtime.CharacterPartType] = runtime);

        foreach (var kvp in runtimesToApply)
        {
            if (_partRenderers.TryGetValue(kvp.Key, out var partRenderer))
            {
                ApplyRuntimeToPart(partRenderer, kvp.Value);
                _activeRuntimeParts[kvp.Key] = kvp.Value;
            }
        }
    }

    private void ApplyRuntimeToPart(Renderer partRenderer, DripData_Runtime runtime)
    {
        if (partRenderer is SkinnedMeshRenderer smr)
        {
            smr.sharedMesh = runtime.CharacterPartMesh;
        }
        else if (partRenderer.TryGetComponent<MeshFilter>(out var mf))
        {
            mf.mesh = runtime.CharacterPartMesh;
        }

        partRenderer.sharedMaterial = runtime.CharacterPartMaterial;
        partRenderer.gameObject.SetActive(true);
    }

    private void ApplyMaterialToActiveParts(Func<DripData_Runtime, Material> materialSelector)
    {
        _propertyBlocks.Clear();
        foreach (var kvp in _activeRuntimeParts)
        {
            if (_partRenderers.TryGetValue(kvp.Key, out var partRenderer))
            {
                var material = materialSelector(kvp.Value);
                if (material != null)
                {
                    partRenderer.sharedMaterial = material;
                }
                partRenderer.SetPropertyBlock(null);
            }
        }
    }

    private void ApplyDefaultMaterials() => ApplyMaterialToActiveParts(runtime => runtime.CharacterPartMaterial);
    private void ApplyGertMaterials() => ApplyMaterialToActiveParts(runtime => runtime.CharacterGertMaterial);
    private void ApplyDissolveMaterials() => ApplyMaterialToActiveParts(runtime => runtime.CharacterDissolveDripMachineMaterial ?? defaultBodyDissolveMaterial);
    #endregion

    #region Equip/Unequip Logic
    private void Awake()
    {
        InitializeBodyParts();
    }

    private void InitializeBodyParts()
    {
        if (bodyPartMappings == null || bodyPartMappings.Count == 0)
        {
#if UNITY_EDITOR
            AutoMapBodyPartsByName();
#endif
        }

        foreach (var mapping in bodyPartMappings)
        {
            if (mapping.partRenderer != null)
            {
                _partRenderers[mapping.partType] = mapping.partRenderer;
                mapping.partRenderer.gameObject.SetActive(false);
            }
        }
    }

    public bool EquipDrip(string dripId)
    {
        DripData dataToEquip = FindDripData(dripId);
        if (dataToEquip == null)
        {
            Debug.LogError($"Drip with ID or Name '{dripId}' not found.");
            return false;
        }

        if (dataToEquip.pack == DripPack.Outfit)
        {
            HandleOutfitEquip(dataToEquip);
        }
        else
        {
            if (_equippedDrips.ContainsKey(dataToEquip.pack))
            {
                UnequipDrip(_equippedDrips[dataToEquip.pack].id);
            }
            if (!IsCompatibleWithCurrentBody(dataToEquip))
            {
                Debug.LogWarning($"Cannot equip '{dataToEquip.name}'. Incompatible with current outfit.");
                return false;
            }
        }

        _equippedDrips[dataToEquip.pack] = dataToEquip;
        ApplyAllVisuals();
        return true;
    }

    public void UnequipDrip(string dripId)
    {
        DripData dataToUnequip = _equippedDrips.Values.FirstOrDefault(d => d.id == dripId);
        if (dataToUnequip == null) return;

        _equippedDrips.Remove(dataToUnequip.pack);
        ApplyAllVisuals();
    }

    public void UnequipAll()
    {
        _equippedDrips.Clear();
        ApplyAllVisuals();
    }

    public string GetEquippedDripIdForPack(DripPack pack)
    {
        return _equippedDrips.TryGetValue(pack, out DripData dripData) ? dripData.id : null;
    }

    public DripData FindDripData(string nameOrId)
    {
        return dripDataContainer.collection.FirstOrDefault(d =>
            d.id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) ||
            d.name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
    }
    #endregion

    #region Compatibility & Auto-mapping
    private void HandleOutfitEquip(DripData outfitData)
    {
        var itemsToRemove = new List<DripData>();
        var outfitBody = outfitData.runtimeCollection.FirstOrDefault(r => r.CharacterPartType == CharacterPartType.Body);

        foreach (var equippedItem in _equippedDrips.Values)
        {
            if (equippedItem.pack == DripPack.Outfit || (outfitBody != null && !IsPartAllowedByBody(outfitBody, equippedItem)))
            {
                itemsToRemove.Add(equippedItem);
            }
        }
        itemsToRemove.ForEach(item => UnequipDrip(item.id));
    }

    private bool IsCompatibleWithCurrentBody(DripData item)
    {
        return !_activeRuntimeParts.TryGetValue(CharacterPartType.Body, out var bodyRuntime) || IsPartAllowedByBody(bodyRuntime, item);
    }

    private bool IsPartAllowedByBody(DripData_Runtime bodyRuntime, DripData item)
    {
        return item.runtimeCollection.All(runtimePart => (bodyRuntime.CharacterPartAllows & runtimePart.CharacterPartType.ToFlag()) != 0);
    }

#if UNITY_EDITOR
    [BoxGroup("Automation")]
    [Button("Auto-map Body Parts From Children", ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
    private void AutoMapBodyPartsByName()
    {
        bodyPartMappings.Clear();
        var childrenRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (CharacterPartType partType in Enum.GetValues(typeof(CharacterPartType)))
        {
            var matchedRenderer = childrenRenderers.FirstOrDefault(r => r.name == partType.ToString());
            if (matchedRenderer != null)
            {
                bodyPartMappings.Add(new BodyPartMapping { partType = partType, partRenderer = matchedRenderer });
            }
        }
        bodyPartMappings = bodyPartMappings.OrderBy(m => (int)m.partType).ToList();
    }
#endif
    #endregion
}

public static class CharacterPartTypeExtensions
{
    public static CharacterPartFlags ToFlag(this CharacterPartType partType)
    {
        return (CharacterPartFlags)(1 << (int)partType);
    }
}