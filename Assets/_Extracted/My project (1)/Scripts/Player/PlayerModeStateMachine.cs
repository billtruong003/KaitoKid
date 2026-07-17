using UnityEngine;
using System;

namespace GameSystem.Player
{
    public enum PlayerMode
    {
        Seated,     // Ngồi ghế bảo vệ – mouse look giới hạn, không di chuyển
        Walking,    // FPS tự do trong hành lang / sảnh
        Portal,     // FPS trong sub-scene portal, có timer 30s
        Peephole    // Nhìn qua lỗ cửa, fisheye, góc nhìn hẹp
    }

    /// <summary>
    /// Quản lý chuyển đổi giữa các mode.
    /// Gắn vào cùng GameObject với FirstPersonFacade.
    /// </summary>
    [System.Serializable]
    public class PlayerModeStateMachine
    {
        // ── Tuỳ chỉnh trong Inspector ──────────────────────────

        [Header("Seated Settings")]
        [SerializeField] private Transform seatAnchor;          // vị trí ghế bảo vệ
        [SerializeField] private float seatedYawLimit = 120f;   // ±120° ngang
        [SerializeField] private float seatedPitchLimit = 40f;  // ±40° dọc

        [Header("Peephole Settings")]
        [SerializeField] private Transform peepholeAnchor;      // vị trí lỗ nhìn trên cửa
        [SerializeField] private float peepholeYawLimit = 25f;  // góc rất hẹp
        [SerializeField] private float peepholePitchLimit = 15f;

        [Header("Walking Settings")]
        [SerializeField] private float walkingPitchLimit = 85f;

        [Header("Portal Settings")]
        [SerializeField] private float portalTimeLimit = 30f;
        [SerializeField] private float portalCooldown = 45f;

        [Header("Transition")]
        [SerializeField] private float standUpDuration = 0.4f;  // thời gian đứng dậy khỏi ghế
        [SerializeField] private float sitDownDuration = 0.3f;

        [Header("References")]
        [SerializeField] private Transform playerBody;
        [SerializeField] private Rigidbody playerRigidbody;

        // ── State ──────────────────────────────────────────────
        private PlayerMode currentMode = PlayerMode.Seated;
        private float portalTimer;
        private float portalCooldownTimer;
        private float transitionTimer;
        private bool isTransitioning;
        private Vector3 transitionStartPos;
        private Quaternion transitionStartRot;
        private Vector3 transitionEndPos;
        private Quaternion transitionEndRot;
        private PlayerMode pendingMode;

        // ── Public ─────────────────────────────────────────────
        public PlayerMode CurrentMode => currentMode;
        public bool IsInTransition => isTransitioning;
        public float PortalTimeRemaining => Mathf.Max(0f, portalTimeLimit - portalTimer);
        public float PortalCooldownRemaining => Mathf.Max(0f, portalCooldownTimer);
        public bool PortalOnCooldown => portalCooldownTimer > 0f;

        /// <summary> Gửi khi mode thay đổi xong (sau transition). </summary>
        public event Action<PlayerMode, PlayerMode> OnModeChanged;

        /// <summary> Gửi khi hết 30s portal mà chưa thoát. </summary>
        public event Action OnPortalTimeout;

        // ── Init ───────────────────────────────────────────────
        public void Initialize(Rigidbody rb)
        {
            playerRigidbody = rb;
            EnterSeated(immediate: true);
        }

        // ──────────────────────────────────────────────────────
        //  PUBLIC API – gọi từ bên ngoài để request chuyển mode
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// Player bấm phím đứng dậy (VD: phím F hoặc đi về phía cửa).
        /// Từ Seated → Walking.
        /// </summary>
        public bool RequestStandUp()
        {
            if (currentMode != PlayerMode.Seated || isTransitioning) return false;
            StartTransition(PlayerMode.Walking, standUpDuration);
            return true;
        }

        /// <summary>
        /// Player quay về ghế và ngồi xuống.
        /// Từ Walking → Seated. Cần đứng gần ghế.
        /// </summary>
        public bool RequestSitDown()
        {
            if (currentMode != PlayerMode.Walking || isTransitioning) return false;

            float distToSeat = Vector3.Distance(playerBody.position, seatAnchor.position);
            if (distToSeat > 1.5f) return false; // phải đứng gần ghế

            StartTransition(PlayerMode.Seated, sitDownDuration);
            return true;
        }

        /// <summary>
        /// Player nhìn qua lỗ cửa (từ Seated, click vào peephole).
        /// Seated → Peephole.
        /// </summary>
        public bool RequestPeephole()
        {
            if (currentMode != PlayerMode.Seated || isTransitioning) return false;
            StartTransition(PlayerMode.Peephole, 0.2f);
            return true;
        }

        /// <summary>
        /// Thoát peephole quay lại ghế.
        /// Peephole → Seated.
        /// </summary>
        public bool RequestExitPeephole()
        {
            if (currentMode != PlayerMode.Peephole || isTransitioning) return false;
            StartTransition(PlayerMode.Seated, 0.2f);
            return true;
        }

        /// <summary>
        /// Rọi đèn pin vào CRT → vào portal.
        /// Seated → Portal (nếu không đang cooldown).
        /// </summary>
        public bool RequestEnterPortal()
        {
            if (currentMode != PlayerMode.Seated || isTransitioning) return false;
            if (PortalOnCooldown) return false;

            portalTimer = 0f;
            StartTransition(PlayerMode.Portal, 0.5f);
            return true;
        }

        /// <summary>
        /// Bấm [E] thoát portal hoặc hết giờ.
        /// Portal → Seated.
        /// </summary>
        public bool RequestExitPortal()
        {
            if (currentMode != PlayerMode.Portal || isTransitioning) return false;
            portalCooldownTimer = portalCooldown;
            StartTransition(PlayerMode.Seated, 0.5f);
            return true;
        }

        // ──────────────────────────────────────────────────────
        //  TICK – gọi mỗi frame từ Facade
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// Trả về config camera giới hạn cho mode hiện tại.
        /// PlayerCamera dùng giá trị này mỗi frame.
        /// </summary>
        public void GetCameraLimits(out float yawLimit, out float pitchLimit)
        {
            switch (currentMode)
            {
                case PlayerMode.Seated:
                    yawLimit = seatedYawLimit;
                    pitchLimit = seatedPitchLimit;
                    break;
                case PlayerMode.Peephole:
                    yawLimit = peepholeYawLimit;
                    pitchLimit = peepholePitchLimit;
                    break;
                case PlayerMode.Walking:
                case PlayerMode.Portal:
                default:
                    yawLimit = 360f; // không giới hạn
                    pitchLimit = walkingPitchLimit;
                    break;
            }
        }

        public void Tick()
        {
            // Transition lerp
            if (isTransitioning)
            {
                TickTransition();
                return; // khoá mọi input khi đang chuyển
            }

            // Portal timer
            if (currentMode == PlayerMode.Portal)
            {
                portalTimer += Time.deltaTime;
                if (portalTimer >= portalTimeLimit)
                {
                    OnPortalTimeout?.Invoke();
                    RequestExitPortal();
                }
            }

            // Portal cooldown (chạy mọi lúc)
            if (portalCooldownTimer > 0f)
            {
                portalCooldownTimer -= Time.deltaTime;
            }

            // Input shortcuts
            HandleModeInput();
        }

        // ──────────────────────────────────────────────────────
        //  PRIVATE
        // ──────────────────────────────────────────────────────

        private void HandleModeInput()
        {
            switch (currentMode)
            {
                case PlayerMode.Seated:
                    // F = đứng dậy
                    if (Input.GetKeyDown(KeyCode.F))
                        RequestStandUp();
                    // Chuột phải = peephole (hoặc click vào cửa – tuỳ interaction system)
                    break;

                case PlayerMode.Walking:
                    // F gần ghế = ngồi lại
                    if (Input.GetKeyDown(KeyCode.F))
                        RequestSitDown();
                    break;

                case PlayerMode.Peephole:
                    // Chuột phải hoặc ESC = thoát
                    if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                        RequestExitPeephole();
                    break;

                case PlayerMode.Portal:
                    // E = thoát portal
                    if (Input.GetKeyDown(KeyCode.E))
                        RequestExitPortal();
                    break;
            }
        }

        private void StartTransition(PlayerMode target, float duration)
        {
            isTransitioning = true;
            transitionTimer = 0f;
            pendingMode = target;

            transitionStartPos = playerBody.position;
            transitionStartRot = playerBody.rotation;

            // Xác định vị trí đích
            switch (target)
            {
                case PlayerMode.Seated:
                    transitionEndPos = seatAnchor.position;
                    transitionEndRot = seatAnchor.rotation;
                    break;
                case PlayerMode.Peephole:
                    transitionEndPos = peepholeAnchor.position;
                    transitionEndRot = peepholeAnchor.rotation;
                    break;
                default:
                    // Walking / Portal: giữ nguyên vị trí
                    transitionEndPos = playerBody.position;
                    transitionEndRot = playerBody.rotation;
                    break;
            }
        }

        private void TickTransition()
        {
            transitionTimer += Time.deltaTime;
            float duration = GetTransitionDuration();
            float t = Mathf.Clamp01(transitionTimer / duration);

            // Smooth ease-in-out
            float smooth = t * t * (3f - 2f * t);

            playerBody.position = Vector3.Lerp(transitionStartPos, transitionEndPos, smooth);
            playerBody.rotation = Quaternion.Slerp(transitionStartRot, transitionEndRot, smooth);

            if (t >= 1f)
            {
                isTransitioning = false;
                PlayerMode previousMode = currentMode;
                currentMode = pendingMode;
                ApplyModeSettings();
                OnModeChanged?.Invoke(previousMode, currentMode);
            }
        }

        private float GetTransitionDuration()
        {
            switch (pendingMode)
            {
                case PlayerMode.Seated: return sitDownDuration;
                case PlayerMode.Walking: return standUpDuration;
                default: return 0.3f;
            }
        }

        private void EnterSeated(bool immediate)
        {
            currentMode = PlayerMode.Seated;
            if (immediate && seatAnchor != null)
            {
                playerBody.position = seatAnchor.position;
                playerBody.rotation = seatAnchor.rotation;
            }
            ApplyModeSettings();
        }

        private void ApplyModeSettings()
        {
            // Khoá rigidbody khi ngồi/peephole
            bool lockPhysics = currentMode == PlayerMode.Seated
                            || currentMode == PlayerMode.Peephole;

            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = lockPhysics;
            }
        }
    }
}
