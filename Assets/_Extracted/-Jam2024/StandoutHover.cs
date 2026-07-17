using UnityEngine;

public class StandoutHover : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 2f;
    public float hoverForce = 50f;
    public LayerMask groundLayer;

    [Header("Stabilization Settings")]
    public float stability = 0.5f;
    public float stabilizationSpeed = 2f;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 100f;

    [Header("Physics Settings")]
    public float gravityMultiplier = 2f;

    [Header("References")]
    public Rigidbody rb;  // Tham chiếu đến Rigidbody mà script sẽ hoạt động trên nó

    void Start()
    {
        if (rb == null)
        {
            Debug.LogError("Rigidbody reference is not set. Please assign a Rigidbody in the inspector.");
            return;
        }

        rb.useGravity = false; // Vô hiệu hóa trọng lực của Unity, chúng ta sẽ quản lý điều này thủ công
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        HandleHover();
        HandleMovement();
        ApplyGravity();
    }

    void HandleHover()
    {
        Ray ray = new Ray(rb.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, hoverHeight, groundLayer))
        {
            float proportionalHeight = (hoverHeight - hit.distance) / hoverHeight;
            Vector3 appliedHoverForce = Vector3.up * proportionalHeight * hoverForce;
            rb.AddForce(appliedHoverForce, ForceMode.Acceleration);

            // Stabilize hoverboard
            Vector3 desiredUp = hit.normal;
            Vector3 torque = Vector3.Cross(rb.transform.up, desiredUp);
            rb.AddTorque(torque * stability * stabilizationSpeed);
        }
    }

    void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        Vector3 forwardMovement = rb.transform.forward * moveInput * moveSpeed;
        rb.AddForce(forwardMovement, ForceMode.Acceleration);

        Quaternion turnRotation = Quaternion.Euler(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    void ApplyGravity()
    {
        Vector3 gravity = gravityMultiplier * Physics.gravity;
        rb.AddForce(gravity, ForceMode.Acceleration);
    }

    void OnDrawGizmos()
    {
        if (rb == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(rb.position, rb.position - Vector3.up * hoverHeight);
    }
}
