using UnityEngine;
using BillInspector;

/// <summary>
/// Gắn vào GameObject chứa bow mesh.
/// Pivot của GameObject phải nằm ở GIỮA cung (chỗ tay cầm).
/// Không cần paint vertex color — shader tự tính weight theo khoảng cách từ pivot.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class BowDrawComponent : MonoBehaviour
{
    // ──────────────────────────────────────────
    // Inspector
    // ──────────────────────────────────────────

    [Header("Bow Shape")]
    [Tooltip("Trục LOCAL dọc theo thân cung (spine). Ví dụ: cung dọc theo Y thì để (0,1,0)")]
    [SerializeField] private Vector3 spineAxis = Vector3.up;

    [Tooltip("Trục LOCAL mà limb sẽ bị uốn về phía nào khi kéo. Ví dụ: -Z = Vector3.back")]
    [SerializeField] private Vector3 bendAxis = Vector3.back;

    [Tooltip("Nửa chiều dài cung tính từ pivot ra đến tip (metres). Nhấn Auto Detect để tự tính.")]
    [SerializeField] private float halfBowLength = 0.5f;

    [Header("Draw Settings")]
    [Tooltip("Khoảng dịch chuyển tối đa ở tip khi kéo hết cỡ (metres)")]
    [SerializeField] private float maxDisplacement = 0.25f;

    [Tooltip("Độ cong của limb:\n1 = linear\n2 = parabolic (giống beam deflection thật)\n3 = cubic")]
    [SerializeField, Range(1f, 4f)] private float curveExponent = 2f;

    [Header("Spring Release")]
    [Tooltip("Độ cứng lò xo — cao hơn = snap về nhanh hơn")]
    [SerializeField] private float springStiffness = 200f;

    [Tooltip("Damping — thấp = bouncy nhiều, cao = tắt nhanh\nGợi ý:\n" +
             "  Rất bouncy  : 5-8\n" +
             "  Vừa phải   : 12-18\n" +
             "  Gần tắt ngay: 25+")]
    [SerializeField] private float springDamping = 12f;

    // ──────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private static readonly int ID_DrawAmount = Shader.PropertyToID("_DrawAmount");
    private static readonly int ID_MaxDisplacement = Shader.PropertyToID("_MaxDisplacement");
    private static readonly int ID_SpineAxisOS = Shader.PropertyToID("_SpineAxisOS");
    private static readonly int ID_BendAxisOS = Shader.PropertyToID("_BendAxisOS");
    private static readonly int ID_HalfBowLength = Shader.PropertyToID("_HalfBowLength");
    private static readonly int ID_CurveExponent = Shader.PropertyToID("_CurveExponent");

    private float _currentDraw;   // giá trị hiện tại truyền vào shader (có thể âm khi overshoot)
    private float _springVelocity; // vận tốc của spring simulation
    private bool _releasing;

    // ──────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        PushToShader();
    }

    private void Update()
    {
        if (!_releasing) return;

        // ── Spring-damper về target = 0 ──────────────────────────────
        // F = -k * x  -  c * v
        // Cho phép _currentDraw âm (overshoot) để tạo bouncy thật sự
        float springForce = -springStiffness * _currentDraw;
        float dampingForce = -springDamping * _springVelocity;
        float acceleration = springForce + dampingForce;

        _springVelocity += acceleration * Time.deltaTime;
        _currentDraw += _springVelocity * Time.deltaTime;

        // Dừng khi đủ gần 0 và velocity nhỏ
        if (Mathf.Abs(_currentDraw) < 0.001f && Mathf.Abs(_springVelocity) < 0.001f)
        {
            _currentDraw = 0f;
            _springVelocity = 0f;
            _releasing = false;
        }

        PushToShader();
    }

    // ──────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────

    /// <summary>
    /// Gọi mỗi frame khi player đang kéo cung. amount = 0..1
    /// </summary>
    public void Draw(float amount)
    {
        _releasing = false;
        _springVelocity = 0f;
        _currentDraw = Mathf.Clamp01(amount);
        PushToShader();
    }

    /// <summary>
    /// Set cung về trạng thái kéo tối đa — test trong Editor.
    /// </summary>
    [BillButton("Set To Draw", ButtonSize.Medium)]
    public void SetToDraw()
    {
        _releasing = false;
        _springVelocity = 0f;
        _currentDraw = 1f;
        PushToShader();
    }

    /// <summary>
    /// Nhả cung — limb spring back có bounce.
    /// Velocity ban đầu được set âm để "bắn" về phía 0 ngay lập tức.
    /// </summary>
    [BillButton("Release", ButtonSize.Medium)]
    public void Release()
    {
        // Kick velocity về phía 0 tỉ lệ với draw hiện tại
        // → kéo càng nhiều thì snap về càng mạnh → bounce càng lớn
        _springVelocity = -_currentDraw * Mathf.Sqrt(springStiffness) * 1.5f;
        _releasing = true;
    }

    /// <summary>
    /// Nhả tức thì, không spring.
    /// </summary>
    public void ResetImmediate()
    {
        _releasing = false;
        _springVelocity = 0f;
        _currentDraw = 0f;
        PushToShader();
    }

    /// <summary>
    /// Tự tính halfBowLength từ Mesh bounds dọc theo spineAxis.
    /// </summary>
    [BillButton("Auto Detect Length", ButtonSize.Small)]
    public void AutoDetectLength()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("BowDrawComponent: Không tìm thấy MeshFilter.");
            return;
        }

        Bounds b = mf.sharedMesh.bounds;
        Vector3 s = spineAxis.normalized;
        halfBowLength = Mathf.Abs(Vector3.Dot(b.extents, s));
        if (halfBowLength < 0.001f) halfBowLength = b.extents.magnitude;

        Debug.Log($"BowDrawComponent: halfBowLength auto = {halfBowLength:F3} m");
        PushToShader();
    }

    // ──────────────────────────────────────────
    // Internal
    // ──────────────────────────────────────────

    private void PushToShader()
    {
        _renderer.GetPropertyBlock(_mpb);
        // _currentDraw có thể âm (overshoot) → shader sẽ đẩy limb ra ngoài một chút
        // tạo visual bounce thật sự. Clamp nếu không muốn hiệu ứng này.
        _mpb.SetFloat(ID_DrawAmount, _currentDraw);
        _mpb.SetFloat(ID_MaxDisplacement, maxDisplacement);
        _mpb.SetFloat(ID_HalfBowLength, Mathf.Max(halfBowLength, 0.001f));
        _mpb.SetFloat(ID_CurveExponent, curveExponent);
        _mpb.SetVector(ID_SpineAxisOS, new Vector4(spineAxis.x, spineAxis.y, spineAxis.z, 0f));
        _mpb.SetVector(ID_BendAxisOS, new Vector4(bendAxis.x, bendAxis.y, bendAxis.z, 0f));
        _renderer.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 s = transform.TransformDirection(spineAxis.normalized) * halfBowLength;
        Vector3 b = transform.TransformDirection(bendAxis.normalized) * maxDisplacement;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position - s, transform.position + s);
        Gizmos.DrawWireSphere(transform.position + s, 0.02f);
        Gizmos.DrawWireSphere(transform.position - s, 0.02f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + b);
        Gizmos.DrawWireSphere(transform.position + b, 0.015f);
    }
#endif
}