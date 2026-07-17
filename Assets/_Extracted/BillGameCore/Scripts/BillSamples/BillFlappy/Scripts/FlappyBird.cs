using UnityEngine;
using BillGameCore;

namespace BillSamples.Flappy
{
    /// <summary>
    /// Bird controller. Handles tap input, gravity, rotation, collision.
    /// Attach to the Bird GameObject (created by FlappySetup).
    /// </summary>
    public class FlappyBird : MonoBehaviour
    {
        [Header("Physics")]
        public float gravity = -9.8f;
        public float tapForce = 5.5f;
        public float maxFallSpeed = -8f;

        [Header("Rotation")]
        public float noseUpAngle = 30f;
        public float noseDownAngle = -90f;
        public float rotationLerpSpeed = 8f;

        [Header("Collision")]
        public float hitRadius = 0.35f;
        public LayerMask obstacleLayer;

        [Header("Visuals — assign your model/sprite")]
        public Transform modelRoot;
        public Animator animator; // Optional: if you have bird animations

        // Runtime
        private float _velocityY;
        private bool _alive;
        private bool _started;
        private float _startY;

        // Animation hashes (optional, works without Animator too)
        private static readonly int AnimFlap = Animator.StringToHash("Flap");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimIdle = Animator.StringToHash("Idle");

        void Awake()
        {
            _startY = transform.position.y;
            if (modelRoot == null) modelRoot = transform;
        }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            Bill.Events.Subscribe<GameStartEvent>(OnGameStart);
        }

        void OnDisable()
        {
            if (!Application.isPlaying) return;
            Bill.Events.Unsubscribe<GameStartEvent>(OnGameStart);
        }

        // ─── Public API ───────────────────────────

        public void ResetBird()
        {
            _alive = false;
            _started = false;
            _velocityY = 0f;
            transform.position = new Vector3(0f, _startY, 0f);
            SetRotation(0f);
            if (animator) animator.SetTrigger(AnimIdle);
        }

        /// <summary>Called from idle menu state — gentle bob animation via Tween.</summary>
        public void StartIdleBob()
        {
            BillTween.LocalMoveY(transform, _startY + 0.3f, 0.6f)
                .SetEase(EaseType.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void StopIdleBob()
        {
            BillTween.KillTarget(transform);
            transform.localPosition = new Vector3(0, _startY, 0);
        }

        // ─── Core Loop ───────────────────────────

        void Update()
        {
            if (!_alive) return;

            // Input: tap / space / click
            bool tapped = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

            if (tapped && _started)
            {
                Tap();
            }

            // Gravity
            _velocityY += gravity * Time.deltaTime;
            _velocityY = Mathf.Max(_velocityY, maxFallSpeed);

            // Move
            Vector3 pos = transform.position;
            pos.y += _velocityY * Time.deltaTime;
            transform.position = pos;

            // Rotation based on velocity
            float targetAngle = _velocityY > 0 ? noseUpAngle : Mathf.Lerp(0f, noseDownAngle, -_velocityY / Mathf.Abs(maxFallSpeed));
            float currentZ = modelRoot.eulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;
            float newZ = Mathf.Lerp(currentZ, targetAngle, rotationLerpSpeed * Time.deltaTime);
            SetRotation(newZ);

            // Death: floor or ceiling
            if (pos.y < -5f || pos.y > 6f)
            {
                Die();
            }
        }

        void Tap()
        {
            _velocityY = tapForce;
            Bill.Audio.Play("sfx_flap");
            Bill.Events.Fire(new BirdTapEvent());

            if (animator) animator.SetTrigger(AnimFlap);

            // Quick scale punch on model
            BillTween.KillTarget(modelRoot);
            BillTween.ScaleY(modelRoot, 1.15f, 0.06f);
            BillTween.ScaleY(modelRoot, 1f, 0.08f).SetDelay(0.06f);
        }

        // ─── Collision ───────────────────────────

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_alive) return;

            if (other.CompareTag("Obstacle"))
            {
                Die();
            }
            else if (other.CompareTag("ScoreZone"))
            {
                // Score trigger passed
                Bill.Events.Fire(new ScoreChangedEvent { Score = 1 }); // Delta
                Bill.Audio.Play("sfx_score");
            }
        }

        // Also support 3D colliders
        void OnTriggerEnter(Collider other)
        {
            if (!_alive) return;

            if (other.CompareTag("Obstacle"))
            {
                Die();
            }
            else if (other.CompareTag("ScoreZone"))
            {
                Bill.Events.Fire(new ScoreChangedEvent { Score = 1 });
                Bill.Audio.Play("sfx_score");
            }
        }

        // ─── Death ───────────────────────────────

        void Die()
        {
            if (!_alive) return;
            _alive = false;

            Bill.Audio.Play("sfx_hit");
            Bill.Events.Fire(new BirdDiedEvent());

            if (animator) animator.SetTrigger(AnimHit);

            // Death tween: pop up then fall
            BillTween.KillTarget(transform);
            BillTween.KillTarget(modelRoot);
            BillTween.MoveY(transform, transform.position.y + 1.5f, 0.25f)
                .SetEase(EaseType.OutQuad)
                .OnComplete(() =>
                {
                    BillTween.MoveY(transform, -6f, 0.6f).SetEase(EaseType.InQuad);
                    BillTween.RotateZ(modelRoot, -90f, 0.4f);
                });
        }

        // ─── Events ──────────────────────────────

        void OnGameStart(GameStartEvent _)
        {
            StopIdleBob();
            _alive = true;
            _started = true;
            _velocityY = tapForce; // First tap launches bird
        }

        // ─── Helpers ─────────────────────────────

        void SetRotation(float z)
        {
            modelRoot.rotation = Quaternion.Euler(0, 0, z);
        }
    }
}
