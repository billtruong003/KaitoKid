using UnityEngine;

namespace GameSystem.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class FirstPersonFacade : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCamera playerCamera;
        [SerializeField] private PlayerHeadbob headbob;
        [SerializeField] private PlayerFootstep footstep;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private PlayerBlink blink;
        [SerializeField] private PlayerModeStateMachine modeStateMachine;

        private void Awake()
        {
            Rigidbody rb = GetComponent<Rigidbody>();

            movement.Initialize(rb);
            playerCamera.Initialize();
            footstep.Initialize();
            interaction.Initialize(playerCamera.CameraTransform);
            blink.Initialize();
            modeStateMachine.Initialize(rb);

            // Khi mode thay đổi → cập nhật movement + camera
            modeStateMachine.OnModeChanged += HandleModeChanged;

            // Áp dụng mode ban đầu (Seated)
            ApplyCurrentMode();
        }

        private void OnDestroy()
        {
            modeStateMachine.OnModeChanged -= HandleModeChanged;
        }

        private void Update()
        {
            // StateMachine tick trước (xử lý transition, input chuyển mode)
            modeStateMachine.Tick();

            // Khoá mọi thứ khi đang transition
            if (modeStateMachine.IsInTransition) return;

            // Camera luôn hoạt động (trừ transition)
            UpdateCameraLimits();
            playerCamera.HandleLook();

            // Movement input chỉ khi Walking hoặc Portal
            movement.HandleInput();

            // Interaction chỉ khi không Portal
            if (modeStateMachine.CurrentMode != PlayerMode.Portal)
            {
                interaction.CheckInteraction();
            }

            blink.HandleBlinkLogic();
        }

        private void FixedUpdate()
        {
            if (modeStateMachine.IsInTransition) return;

            movement.ProcessMovement();

            // Headbob + Footstep chỉ khi đang đi
            if (modeStateMachine.CurrentMode == PlayerMode.Walking
             || modeStateMachine.CurrentMode == PlayerMode.Portal)
            {
                headbob.ProcessHeadbob(movement.CurrentVelocity, movement.IsGrounded);
                footstep.ProcessFootstep(headbob.CurrentPhase, headbob.IsLowestPoint, transform.position);
            }
        }

        // ──────────────────────────────────────────────────────
        //  Mode change handling
        // ──────────────────────────────────────────────────────

        private void HandleModeChanged(PlayerMode previous, PlayerMode current)
        {
            ApplyCurrentMode();
        }

        private void ApplyCurrentMode()
        {
            PlayerMode mode = modeStateMachine.CurrentMode;

            // Movement chỉ bật khi Walking hoặc Portal
            bool canMove = mode == PlayerMode.Walking || mode == PlayerMode.Portal;
            movement.MovementEnabled = canMove;

            // Snap baseYaw khi vào mode có giới hạn góc nhìn
            bool snapYaw = mode == PlayerMode.Seated || mode == PlayerMode.Peephole;
            modeStateMachine.GetCameraLimits(out float yawLimit, out float pitchLimit);
            playerCamera.SetLimits(yawLimit, pitchLimit, snapYaw);
        }

        private void UpdateCameraLimits()
        {
            // Cập nhật liên tục (phòng trường hợp giá trị thay đổi runtime)
            modeStateMachine.GetCameraLimits(out float yawLimit, out float pitchLimit);
            playerCamera.SetLimits(yawLimit, pitchLimit);
        }
    }
}
