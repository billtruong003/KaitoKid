using UnityEngine;

namespace ModularTopDown.Locomotion
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterLocomotion : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private CharacterAnimator characterAnimator;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float airControlSpeed = 5f;

        [Header("Rotation & Gravity")]
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float gravity = -20.0f;

        [Header("Physics & Feel Improvements")]
        [SerializeField] private float movementSmoothTime = 0.1f;
        [SerializeField] private float slopeSlideSpeed = 8f;
        [SerializeField] private float coyoteTime = 0.15f;

        [Header("Ground Check Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 1.1f;

        private CharacterController controller;
        private Transform camTransform;
        private Vector3 playerVelocity;
        private Vector3 moveDampVelocity;
        private float coyoteTimeCounter;
        private bool isCurrentlyGrounded;
        private Vector3 groundNormal;
        private int jumpsLeft;
        private int currentMaxJumps;

        public Vector3 PlayerVelocity => playerVelocity;

        // === MYTHFALL ADDITIVE — Sprint 1 (2026-04-28) ===
        // Reason: top-down combat needs locomotion + facing decoupled (PlayerFacing handles rotation).
        // Backward compat: ExternalRotationControl defaults false → existing consumers unchanged.
        // Five additions in this file:
        //   1. ExternalRotationControl property (below)
        //   2. HorizontalVelocity getter (below)
        //   3. RunSpeed getter (below)
        //   4. Null-safe characterAnimator in HandleGroundedMovement (search "MYTHFALL ADDITIVE")
        //   5. Rotation guard in HandleGroundedMovement + HandleAirborneMovement (search "MYTHFALL ADDITIVE")
        public bool ExternalRotationControl { get; set; }
        public Vector3 HorizontalVelocity => new Vector3(playerVelocity.x, 0f, playerVelocity.z);
        public float RunSpeed => runSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            camTransform = Camera.main.transform;
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<CharacterAnimator>();
        }

        void Update()
        {
            PerformGroundCheck();
            HandleGravity();
        }

        public void ConfigureJumps(bool allowDoubleJump)
        {
            currentMaxJumps = allowDoubleJump ? 2 : 1;
            jumpsLeft = currentMaxJumps;
        }

        private void PerformGroundCheck()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (!isCurrentlyGrounded)
                {
                    groundNormal = hit.normal;
                    jumpsLeft = currentMaxJumps;
                }
                isCurrentlyGrounded = true;
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                isCurrentlyGrounded = false;
                coyoteTimeCounter -= Time.deltaTime;
            }
        }

        public bool IsGrounded()
        {
            return isCurrentlyGrounded || controller.isGrounded;
        }

        public bool PerformJump(float jumpHeight, float doubleJumpHeight, out bool isDoubleJump)
        {
            isDoubleJump = false;
            if (coyoteTimeCounter > 0f)
            {
                jumpsLeft = currentMaxJumps - 1;
                coyoteTimeCounter = 0f;
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                return true;
            }

            if (jumpsLeft > 0)
            {
                isDoubleJump = true;
                jumpsLeft--;
                playerVelocity.y = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);
                return true;
            }

            return false;
        }

        private void HandleGravity()
        {
            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = -2f;
            }
            else
            {
                playerVelocity.y += gravity * Time.deltaTime;
            }
        }

        private Vector3 CalculateCameraRelativeMoveDirection(Vector2 moveInput)
        {
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            return (camForward.normalized * moveInput.y + camRight.normalized * moveInput.x).normalized;
        }

        public void HandleGroundedMovement(Vector2 moveInput, bool isRunning)
        {
            float targetMaxSpeed = isRunning ? runSpeed : walkSpeed;
            Vector3 targetMoveVector = CalculateCameraRelativeMoveDirection(moveInput) * targetMaxSpeed;

            Vector3 horizontalVelocity = new Vector3(playerVelocity.x, 0, playerVelocity.z);
            horizontalVelocity = Vector3.SmoothDamp(horizontalVelocity, targetMoveVector, ref moveDampVelocity, movementSmoothTime);
            playerVelocity.x = horizontalVelocity.x;
            playerVelocity.z = horizontalVelocity.z;

            Vector3 finalMove = HandleSlopeSlide(playerVelocity);
            float currentPhysicalSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;

            // MYTHFALL ADDITIVE — null-safe (silent fallback) + rotation guard
            if (characterAnimator != null) characterAnimator.UpdateMoveSpeed(currentPhysicalSpeed / runSpeed);
            if (!ExternalRotationControl) HandleRotation(targetMoveVector);
            controller.Move(finalMove * Time.deltaTime);
        }

        private Vector3 HandleSlopeSlide(Vector3 currentVelocity)
        {
            if (!isCurrentlyGrounded) return currentVelocity;

            float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
            if (slopeAngle > controller.slopeLimit)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized * slopeSlideSpeed;
                return new Vector3(slideDirection.x, currentVelocity.y, slideDirection.z);
            }
            return currentVelocity;
        }

        public void HandleAirborneMovement(Vector2 moveInput)
        {
            Vector3 moveDirection = CalculateCameraRelativeMoveDirection(moveInput);

            playerVelocity.x = Mathf.Lerp(playerVelocity.x, moveDirection.x * airControlSpeed, Time.deltaTime * airControlSpeed * 0.5f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, moveDirection.z * airControlSpeed, Time.deltaTime * airControlSpeed * 0.5f);

            // MYTHFALL ADDITIVE — rotation guard
            if (!ExternalRotationControl) HandleRotation(moveDirection);
            controller.Move(playerVelocity * Time.deltaTime);
        }

        public void HandleRotation(Vector3 moveDirection)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        public void HandleDash(float dashSpeed)
        {
            Vector3 dashDirection = transform.forward;
            Vector3 currentMoveDirection = new Vector3(playerVelocity.x, 0, playerVelocity.z);
            if (currentMoveDirection.magnitude > 0.1f)
            {
                dashDirection = currentMoveDirection.normalized;
            }
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
        }

        // --- HÀM MỚI: VẼ GIZMOS ĐỂ DEBUG ---
        // Chỉ thị #if UNITY_EDITOR đảm bảo code này sẽ bị loại bỏ hoàn toàn khi build game,
        // không gây ảnh hưởng tới hiệu năng của sản phẩm cuối.
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Vẽ Vector vận tốc
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up, playerVelocity);

            // Vẽ tia dò đất (Ground Check Ray)
            Vector3 rayStartPoint = transform.position + Vector3.up * 0.1f;

            // Thay đổi màu sắc dựa trên trạng thái isCurrentlyGrounded (chỉ hoạt động ở Play Mode)
            // hoặc thực hiện một Raycast mô phỏng để có feedback ngay trong Edit Mode.
            bool hasHit = Physics.Raycast(rayStartPoint, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer);

            Gizmos.color = (Application.isPlaying ? isCurrentlyGrounded : hasHit) ? Color.green : Color.red;
            Gizmos.DrawRay(rayStartPoint, Vector3.down * groundCheckDistance);

            // Nếu tia chạm đất, vẽ thêm thông tin chi tiết
            if (hasHit)
            {
                // Vẽ điểm va chạm
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(hit.point, 0.05f);

                // Vẽ pháp tuyến của mặt đất
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(hit.point, hit.normal * 0.5f);
            }
        }
#endif
        // --- KẾT THÚC HÀM MỚI ---
    }
}