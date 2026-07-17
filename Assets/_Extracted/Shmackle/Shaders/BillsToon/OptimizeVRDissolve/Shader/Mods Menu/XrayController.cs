using UnityEngine;

public class XRayController : MonoBehaviour
{
    public enum XRayType { Standard, BloodJman, Hunter }

    [Header("Effect Materials")]
    [SerializeField] private Material standardXRayMaterial;
    [SerializeField] private Material bloodJmanMaterial;
    [SerializeField] private Material xrayPropHunt;


    [Header("Dependencies")]
    [SerializeField]
    private MaterialEffectApplier effectApplier;

    private Material _activeXRayMaterial;

    public void SetXRayActive(bool enable, XRayType type)
    {
        Material targetMaterial = GetMaterialFromType(type);
        if (targetMaterial == null) return;

        if (enable)
        {
            if (_activeXRayMaterial == targetMaterial) return;

            if (_activeXRayMaterial != null)
            {
                effectApplier.RemoveEffectMaterial(_activeXRayMaterial);
            }

            effectApplier.ApplyEffectMaterial(targetMaterial);
            _activeXRayMaterial = targetMaterial;
        }
        else
        {
            if (_activeXRayMaterial != targetMaterial) return;

            effectApplier.RemoveEffectMaterial(targetMaterial);
            _activeXRayMaterial = null;
        }
    }

    private Material GetMaterialFromType(XRayType type)
    {
        switch (type)
        {
            case XRayType.Standard: return standardXRayMaterial;
            case XRayType.BloodJman: return bloodJmanMaterial;
            case XRayType.Hunter: return xrayPropHunt;
            default: return null;
        }
    }

    private void OnValidate()
    {
        if (effectApplier == null)
        {
            effectApplier = GetComponent<MaterialEffectApplier>();
        }
    }
}