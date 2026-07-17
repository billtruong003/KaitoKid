using UnityEngine;
using BillGameCore;

namespace BillSamples.Runner
{
    public enum PlayerAnimState { Idle, Run, JumpUp, JumpPeak, JumpDown, DoubleJump, Slide, Hurt, Death }

    /// <summary>
    /// Runner player controller. Auto-runs right, handles jump/slide/damage.
    /// </summary>
    public class RunnerPlayer : MonoBehaviour
    {
        [Header("Movement")]
        public float baseSpeed = 6f;
        public float maxSpeed = 9f;
        public float jumpForce = 12f;
        public float gravity = -30f;
        public float slideTime = 0.6f;

        [Header("Upgrades (loaded from save)")]
        public int maxHP = 1;
        public bool hasDoubleJump;
        public float magnetRange;

        [Header("Visuals — assign your character model")]
        public Transform modelRoot;
        public Animator animator;

        [Header("Colliders")]
        public Vector3 standingColliderCenter = new Vector3(0, 0.5f, 0);
        public Vector3 standingColliderSize = new Vector3(0.6f, 1f, 0.6f);
        public Vector3 slidingColliderCenter = new Vector3(0, 0.2f, 0);
        public Vector3 slidingColliderSize = new Vector3(0.6f, 0.4f, 0.6f);

        // Runtime state
        [HideInInspector] public float currentSpeed;
        [HideInInspector] public int currentHP;
        [HideInInspector] public int coinsCollected;
        [HideInInspector] public float distanceTraveled;
        [HideInInspector] public bool alive;

        private float _velocityY;
        private bool _grounded;
        private bool _usedDoubleJump;
        private bool _sliding;
        private float _slideTimer;
        private float _invincibleTimer;
        private bool _invincible;
        private PlayerAnimState _animState;
        private BoxCollider _collider3D;
        private BoxCollider2D _collider2D;
        private int _lastReportedDistance;

        // Active power-ups
        private bool _shieldActive;
        private bool _magnetActive;
        private bool _speedBoostActive;
        private bool _coinDoublerActive;
        private bool _tinyModeActive;

        // Anim hashes
        static readonly int AHash_Run = Animator.StringToHash("Run");
        static readonly int AHash_Jump = Animator.StringToHash("Jump");
        static readonly int AHash_Slide = Animator.StringToHash("Slide");
        static readonly int AHash_Hurt = Animator.StringToHash("Hurt");
        static readonly int AHash_Death = Animator.StringToHash("Death");
        static readonly int AHash_Grounded = Animator.StringToHash("Grounded");
        static readonly int AHash_Speed = Animator.StringToHash("Speed");

        void Awake()
        {
            if (modelRoot == null) modelRoot = transform;
            _collider3D = GetComponent<BoxCollider>();
            _collider2D = GetComponent<BoxCollider2D>();
        }

        void OnEnable()
        {
            Bill.Events.Subscribe<RunnerStartEvent>(OnGameStart);
            Bill.Events.Subscribe<ItemPickedUpEvent>(OnItemPickup);
            Bill.Events.Subscribe<ItemExpiredEvent>(OnItemExpired);
        }

        void OnDisable()
        {
            Bill.Events.Unsubscribe<RunnerStartEvent>(OnGameStart);
            Bill.Events.Unsubscribe<ItemPickedUpEvent>(OnItemPickup);
            Bill.Events.Unsubscribe<ItemExpiredEvent>(OnItemExpired);
        }

        // ─── Init ────────────────────────────────

        public void LoadUpgrades()
        {
            int hpLevel = Bill.Save.GetInt("runner_hp_level", 0);
            maxHP = 1 + hpLevel;
            hasDoubleJump = Bill.Save.GetBool("runner_has_doublejump", false);
            magnetRange = Bill.Save.GetBool("runner_has_magnet", false) ? 1.5f : 0f;
        }

        public void ResetPlayer()
        {
            alive = false;
            currentHP = maxHP;
            coinsCollected = 0;
            distanceTraveled = 0;
            currentSpeed = baseSpeed;
            _velocityY = 0;
            _grounded = true;
            _usedDoubleJump = false;
            _sliding = false;
            _invincible = false;
            _invincibleTimer = 0;
            _shieldActive = false;
            _magnetActive = false;
            _speedBoostActive = false;
            _coinDoublerActive = false;
            _tinyModeActive = false;
            _lastReportedDistance = 0;

            transform.position = new Vector3(0, 0.5f, 0);
            if (modelRoot) modelRoot.localScale = Vector3.one;
            SetColliderStanding();
            SetAnimState(PlayerAnimState.Idle);
        }

        // ─── Update ──────────────────────────────

        void Update()
        {
            if (!alive) return;

            float dt = Time.deltaTime;

            HandleInput();
            HandleSlide(dt);
            HandleGravity(dt);
            HandleMovement(dt);
            HandleInvincibility(dt);
            HandleMagnet();
            UpdateDistance(dt);
        }

        void HandleInput()
        {
            bool jumpInput = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            bool slideInput = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);

            // Mobile: swipe detection could be added here
            // For now: tap = jump on mobile (distinguished by touch position later)

            if (jumpInput)
            {
                if (_grounded && !_sliding)
                {
                    Jump();
                }
                else if (!_grounded && hasDoubleJump && !_usedDoubleJump)
                {
                    DoubleJump();
                }
            }

            if (slideInput && _grounded && !_sliding)
            {
                StartSlide();
            }
        }

        void Jump()
        {
            _velocityY = jumpForce;
            _grounded = false;
            _usedDoubleJump = false;
            SetAnimState(PlayerAnimState.JumpUp);
            Bill.Audio.Play("sfx_jump");

            // Jump dust VFX (pool)
            var dust = Bill.Pool.Spawn("vfx_dust", transform.position, Quaternion.identity);
            if (dust) Bill.Pool.Return(dust, 0.5f);
        }

        void DoubleJump()
        {
            _velocityY = jumpForce * 0.85f;
            _usedDoubleJump = true;
            SetAnimState(PlayerAnimState.DoubleJump);
            Bill.Audio.Play("sfx_doublejump");

            // Spin
            if (modelRoot)
            {
                BillTween.KillTarget(modelRoot);
                BillTween.RotateZ(modelRoot, 360f, 0.3f)
                    .OnComplete(() => modelRoot.localEulerAngles = Vector3.zero);
            }
        }

        void StartSlide()
        {
            _sliding = true;
            _slideTimer = slideTime;
            SetColliderSliding();
            SetAnimState(PlayerAnimState.Slide);
            Bill.Audio.Play("sfx_slide");
        }

        void HandleSlide(float dt)
        {
            if (!_sliding) return;
            _slideTimer -= dt;
            if (_slideTimer <= 0)
            {
                _sliding = false;
                SetColliderStanding();
                SetAnimState(_grounded ? PlayerAnimState.Run : PlayerAnimState.JumpDown);
            }
        }

        void HandleGravity(float dt)
        {
            if (_grounded) return;

            _velocityY += gravity * dt;

            float y = transform.position.y + _velocityY * dt;

            // Ground check
            if (y <= 0.5f)
            {
                y = 0.5f;
                _velocityY = 0;
                _grounded = true;
                _usedDoubleJump = false;
                SetAnimState(PlayerAnimState.Run);
                Bill.Audio.Play("sfx_land");
            }
            else
            {
                // Peak detection
                if (_animState == PlayerAnimState.JumpUp && _velocityY <= 0)
                    SetAnimState(PlayerAnimState.JumpPeak);
                else if (_animState == PlayerAnimState.JumpPeak && _velocityY < -2f)
                    SetAnimState(PlayerAnimState.JumpDown);
            }

            transform.position = new Vector3(transform.position.x, y, transform.position.z);

            // Fall death
            if (y < -3f) Die();
        }

        void HandleMovement(float dt)
        {
            float speed = _speedBoostActive ? currentSpeed * 1.5f : currentSpeed;
            transform.Translate(Vector3.right * speed * dt);
        }

        void HandleInvincibility(float dt)
        {
            if (!_invincible) return;
            _invincibleTimer -= dt;

            // Flash effect
            if (modelRoot)
            {
                var renderers = modelRoot.GetComponentsInChildren<Renderer>();
                bool visible = (Mathf.FloorToInt(_invincibleTimer * 10) % 2 == 0);
                foreach (var r in renderers) r.enabled = visible;
            }

            if (_invincibleTimer <= 0)
            {
                _invincible = false;
                if (modelRoot)
                    foreach (var r in modelRoot.GetComponentsInChildren<Renderer>()) r.enabled = true;
            }
        }

        void HandleMagnet()
        {
            float range = _magnetActive ? 5f : magnetRange;
            if (range <= 0) return;

            // Find coins in range and pull them
            var colliders = Physics.OverlapSphere(transform.position, range);
            foreach (var col in colliders)
            {
                if (col.CompareTag("Coin"))
                {
                    col.transform.position = Vector3.MoveTowards(
                        col.transform.position, transform.position, 15f * Time.deltaTime);
                }
            }

            // 2D fallback
            var colliders2D = Physics2D.OverlapCircleAll(transform.position, range);
            foreach (var col in colliders2D)
            {
                if (col.CompareTag("Coin"))
                {
                    col.transform.position = Vector3.MoveTowards(
                        col.transform.position, transform.position, 15f * Time.deltaTime);
                }
            }
        }

        void UpdateDistance(float dt)
        {
            distanceTraveled += currentSpeed * dt;
            int meters = Mathf.FloorToInt(distanceTraveled);
            if (meters >= _lastReportedDistance + 10)
            {
                _lastReportedDistance = meters;
                Bill.Events.Fire(new DistanceChangedEvent { Meters = meters });
            }
        }

        // ─── Damage ──────────────────────────────

        public void TakeDamage(int amount, bool ignoreShield = false)
        {
            if (!alive || _invincible || (_speedBoostActive && !ignoreShield)) return;

            if (_shieldActive && !ignoreShield)
            {
                _shieldActive = false;
                Bill.Events.Fire(new ItemExpiredEvent { ItemKey = "item_shield" });
                Bill.Audio.Play("sfx_shield_break");
                return;
            }

            currentHP -= amount;
            Bill.Audio.Play("sfx_hurt");
            Bill.Events.Fire(new PlayerHurtEvent { RemainingHP = currentHP });

            if (currentHP <= 0)
            {
                Die();
            }
            else
            {
                // Invincibility frames
                _invincible = true;
                _invincibleTimer = 0.5f;
                SetAnimState(PlayerAnimState.Hurt);
                Bill.Timer.Delay(0.3f, () =>
                {
                    if (alive) SetAnimState(_grounded ? PlayerAnimState.Run : PlayerAnimState.JumpDown);
                });
            }
        }

        public void InstantKill()
        {
            if (!alive) return;
            currentHP = 0;
            Die();
        }

        void Die()
        {
            alive = false;
            SetAnimState(PlayerAnimState.Death);
            Bill.Audio.Play("sfx_death");

            // Death tumble tween
            if (modelRoot)
            {
                BillTween.RotateZ(modelRoot, -90f, 0.4f).SetEase(EaseType.InQuad);
                BillTween.MoveY(transform, -0.5f, 0.3f).SetDelay(0.2f);
            }

            Bill.Timer.Delay(0.8f, () =>
            {
                Bill.Events.Fire(new PlayerDiedEvent
                {
                    Distance = Mathf.FloorToInt(distanceTraveled),
                    Coins = coinsCollected
                });
            });
        }

        // ─── Collision ───────────────────────────

        void OnTriggerEnter(Collider other)
        {
            HandleTrigger(other.gameObject, other.tag);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger(other.gameObject, other.tag);
        }

        void HandleTrigger(GameObject obj, string tag)
        {
            if (!alive) return;

            switch (tag)
            {
                case "Obstacle":
                    var obs = obj.GetComponent<RunnerObstacle>();
                    if (obs != null && obs.instantKill) InstantKill();
                    else TakeDamage(1);
                    break;

                case "Spike":
                    InstantKill();
                    break;

                case "Coin":
                    CollectCoin(obj);
                    break;

                case "PowerUp":
                    CollectPowerUp(obj);
                    break;
            }
        }

        void CollectCoin(GameObject coinGO)
        {
            var coin = coinGO.GetComponent<RunnerCollectible>();
            int value = coin != null ? coin.value : 1;
            if (_coinDoublerActive) value *= 2;

            coinsCollected += value;
            Bill.Events.Fire(new CoinCollectedEvent { Value = value, Position = coinGO.transform.position });
            Bill.Audio.Play("sfx_coin");

            // Collect tween then return to pool
            BillTween.MoveY(coinGO.transform, coinGO.transform.position.y + 1f, 0.2f);
            BillTween.Scale(coinGO.transform, 0f, 0.2f)
                .OnComplete(() => Bill.Pool.Return(coinGO));
        }

        void CollectPowerUp(GameObject itemGO)
        {
            var item = itemGO.GetComponent<RunnerPowerUp>();
            if (item == null) return;

            Bill.Audio.Play("sfx_item");
            Bill.Events.Fire(new ItemPickedUpEvent { ItemKey = item.itemKey, Duration = item.duration });

            // Pickup tween
            BillTween.Scale(itemGO.transform, 1.5f, 0.1f)
                .OnComplete(() =>
                {
                    BillTween.Scale(itemGO.transform, 0f, 0.1f)
                        .OnComplete(() => Bill.Pool.Return(itemGO));
                });

            // Start power-up timer
            if (item.duration > 0)
            {
                Bill.Timer.Delay(item.duration, () =>
                {
                    Bill.Events.Fire(new ItemExpiredEvent { ItemKey = item.itemKey });
                });
            }
        }

        // ─── Power-up Events ─────────────────────

        void OnItemPickup(ItemPickedUpEvent e)
        {
            switch (e.ItemKey)
            {
                case "item_magnet": _magnetActive = true; break;
                case "item_shield": _shieldActive = true; break;
                case "item_speed": _speedBoostActive = true; break;
                case "item_2x": _coinDoublerActive = true; break;
                case "item_tiny":
                    _tinyModeActive = true;
                    if (modelRoot) BillTween.Scale(modelRoot, 0.5f, 0.2f);
                    SetColliderSliding(); // Smaller hitbox
                    break;
            }
        }

        void OnItemExpired(ItemExpiredEvent e)
        {
            switch (e.ItemKey)
            {
                case "item_magnet": _magnetActive = false; break;
                case "item_shield": _shieldActive = false; break;
                case "item_speed": _speedBoostActive = false; break;
                case "item_2x": _coinDoublerActive = false; break;
                case "item_tiny":
                    _tinyModeActive = false;
                    if (modelRoot) BillTween.Scale(modelRoot, 1f, 0.2f);
                    if (!_sliding) SetColliderStanding();
                    break;
            }
        }

        void OnGameStart(RunnerStartEvent _)
        {
            alive = true;
            SetAnimState(PlayerAnimState.Run);
        }

        // ─── Collider management ─────────────────

        void SetColliderStanding()
        {
            if (_collider3D) { _collider3D.center = standingColliderCenter; _collider3D.size = standingColliderSize; }
            if (_collider2D) { _collider2D.offset = standingColliderCenter; _collider2D.size = standingColliderSize; }
        }

        void SetColliderSliding()
        {
            if (_collider3D) { _collider3D.center = slidingColliderCenter; _collider3D.size = slidingColliderSize; }
            if (_collider2D) { _collider2D.offset = slidingColliderCenter; _collider2D.size = slidingColliderSize; }
        }

        // ─── Animation ───────────────────────────

        void SetAnimState(PlayerAnimState state)
        {
            _animState = state;
            if (animator == null) return;

            animator.SetBool(AHash_Grounded, _grounded);
            animator.SetFloat(AHash_Speed, currentSpeed / maxSpeed);

            switch (state)
            {
                case PlayerAnimState.Run: animator.SetTrigger(AHash_Run); break;
                case PlayerAnimState.JumpUp:
                case PlayerAnimState.DoubleJump: animator.SetTrigger(AHash_Jump); break;
                case PlayerAnimState.Slide: animator.SetTrigger(AHash_Slide); break;
                case PlayerAnimState.Hurt: animator.SetTrigger(AHash_Hurt); break;
                case PlayerAnimState.Death: animator.SetTrigger(AHash_Death); break;
            }
        }
    }
}
