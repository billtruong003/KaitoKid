using UnityEngine;



public class OutlineController : MonoBehaviour
{
    public enum OutlineType
    {
        None,
        Player,
        BloodJman,
        ObjectReact
    }

    [Header("Effect Materials")]
    [SerializeField]
    private Material playerOutlineMaterial;
    [SerializeField]
    private Material bloodJmanOutlineMaterial;
    [SerializeField]
    private Material objectReactOutlineMaterial;

    [Header("Dependencies")]
    [SerializeField]
    private MaterialEffectApplier effectApplier;

    private Material _activeOutlineMaterial;

    public void SetOutline(OutlineType type)
    {
        if (type == OutlineType.None)
        {
            if (_activeOutlineMaterial != null)
            {
                effectApplier.RemoveEffectMaterial(_activeOutlineMaterial);
                _activeOutlineMaterial = null;
            }
            return;
        }

        Material targetMaterial = GetMaterialFromType(type);

        if (targetMaterial == _activeOutlineMaterial)
        {
            return;
        }


        if (_activeOutlineMaterial != null)
        {
            effectApplier.RemoveEffectMaterial(_activeOutlineMaterial);
        }

        if (targetMaterial != null)
        {
            effectApplier.ApplyEffectMaterial(targetMaterial);
        }

        _activeOutlineMaterial = targetMaterial;
    }

    private Material GetMaterialFromType(OutlineType type)
    {
        switch (type)
        {
            case OutlineType.Player:
                return playerOutlineMaterial;
            case OutlineType.BloodJman:
                return bloodJmanOutlineMaterial;
            case OutlineType.ObjectReact:
                return objectReactOutlineMaterial;
            case OutlineType.None:
            default:
                return null;
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