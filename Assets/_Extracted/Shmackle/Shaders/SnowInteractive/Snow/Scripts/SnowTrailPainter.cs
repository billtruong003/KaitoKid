using UnityEngine;

[DisallowMultipleComponent]
public class SnowTrailPainter : MonoBehaviour
{
    [SerializeField]
    private BrushProfile brushProfile;

    [Header("Overrides (Optional)")]
    [Tooltip("Override the radius from the profile. Set to 0 to use profile default.")]
    [SerializeField, Min(0f)]
    private float overrideRadius = 0f;

    [Tooltip("Override the strength from the profile. Set to -1 to use profile default.")]
    [SerializeField, Range(-1, 1)]
    private float overrideStrength = -1f;

    [Header("Performance")]
    [Tooltip("Time in seconds between each trail update. Higher values improve performance.")]
    [SerializeField, Min(0.01f)]
    private float updateInterval = 0.033f;

    private Transform objectTransform;
    private PersistentSnowTrailManager trailManager;
    private float timeSinceLastUpdate;

    private void Awake()
    {
        objectTransform = transform;
    }

    private void Start()
    {
        trailManager = PersistentSnowTrailManager.Instance;
        if (trailManager != null && brushProfile != null && brushProfile.BrushTexture != null)
        {
            trailManager.RegisterBrush(brushProfile.BrushTexture);
        }
    }

    private void LateUpdate()
    {
        if (trailManager == null || brushProfile == null || brushProfile.BrushTexture == null)
        {
            return;
        }

        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            float radius = overrideRadius > 0 ? overrideRadius : brushProfile.Radius;
            float strength = overrideStrength >= 0 ? overrideStrength : brushProfile.Strength;

            trailManager.QueueDrawCommand(
                objectTransform.position,
                radius,
                strength,
                brushProfile.BrushTexture
            );
            timeSinceLastUpdate = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (brushProfile == null) return;
        float radius = overrideRadius > 0 ? overrideRadius : brushProfile.Radius;
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}