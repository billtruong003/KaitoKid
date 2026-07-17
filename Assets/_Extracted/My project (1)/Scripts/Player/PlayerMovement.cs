using UnityEngine;

namespace GameSystem.Player
{
    [System.Serializable]
    public class PlayerMovement
    {
        [Header("Walk")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float dragOnGround = 6f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform orientation;

        [Header("Sprint")]
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float sprintStaminaCost = 15f;   // trừ stamina/giây khi chạy
        [SerializeField] private float staminaRegen = 8f;         // hồi stamina/giây khi không chạy
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float sprintWakefulnessDrain = 1f; // trừ tỉnh táo/giây khi sprint

        private Rigidbody rb;
        private Vector2 input;
        private Vector3 moveDirection;
        private float currentStamina;
        private bool isSprinting;
        private bool sprintLocked; // khoá sprint khi stamina = 0, mở lại khi > 20%

        // ── Public properties ──────────────────────────────────
        public float CurrentVelocity => rb.linearVelocity.magnitude;
        public bool IsGrounded => Physics.Raycast(rb.position, Vector3.down, 1.1f, groundMask);
        public bool IsSprinting => isSprinting;
        public bool IsMoving => input.sqrMagnitude > 0.01f;
        public float StaminaNormalized => currentStamina / maxStamina;

        /// <summary>
        /// Event để WakelfulnessSystem lắng nghe, trừ tỉnh táo khi sprint.
        /// Gửi lượng drain mỗi frame (đã nhân deltaTime).
        /// </summary>
        public System.Action<float> OnSprintDrainWakefulness;

        // ── Cho phép bật/tắt movement từ StateMachine ─────────
        public bool MovementEnabled { get; set; } = true;

        // ── Init ───────────────────────────────────────────────
        public void Initialize(Rigidbody rigidbody)
        {
            rb = rigidbody;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            currentStamina = maxStamina;
        }

        // ── Update (gọi trong Update) ─────────────────────────
        public void HandleInput()
        {
            if (!MovementEnabled)
            {
                input = Vector2.zero;
                isSprinting = false;
                return;
            }

            input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).normalized;

            // Sprint input
            bool wantSprint = Input.GetKey(KeyCode.LeftShift) && IsMoving && IsGrounded;

            // Khoá sprint khi cạn stamina, mở lại khi hồi > 20%
            if (currentStamina <= 0f)
                sprintLocked = true;
            if (sprintLocked && currentStamina >= maxStamina * 0.2f)
                sprintLocked = false;

            isSprinting = wantSprint && !sprintLocked && currentStamina > 0f;

            // Stamina tick
            if (isSprinting)
            {
                currentStamina -= sprintStaminaCost * Time.deltaTime;
                currentStamina = Mathf.Max(currentStamina, 0f);

                // Báo cho Wakefulness trừ tỉnh táo
                OnSprintDrainWakefulness?.Invoke(sprintWakefulnessDrain * Time.deltaTime);
            }
            else
            {
                currentStamina += staminaRegen * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        // ── FixedUpdate (gọi trong FixedUpdate) ───────────────
        public void ProcessMovement()
        {
            if (!MovementEnabled) return;

            rb.linearDamping = IsGrounded ? dragOnGround : 0f;
            moveDirection = orientation.forward * input.y + orientation.right * input.x;

            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 force = moveDirection * targetSpeed * acceleration;
            rb.AddForce(force, ForceMode.Acceleration);

            LimitSpeed(targetSpeed);
        }

        private void LimitSpeed(float maxSpeed)
        {
            Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVelocity.magnitude > maxSpeed)
            {
                Vector3 limited = flatVelocity.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
            }
        }
    }
}
