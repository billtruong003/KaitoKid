#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System;
using UnityEngine;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline
{
    /// <summary>
    /// Controller for the local player character.
    /// Handles WASD input, client-side prediction, server reconciliation,
    /// click-targeting for combat, skill hotkeys, and camera follow.
    /// </summary>
    [RequireComponent(typeof(CharacterVisualSync))]
    public class LocalPlayerController : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float reconcileSpeed = 14f;

        [Header("Camera")]
        [SerializeField] private float cameraFollowSpeed = 8f;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private LayerMask targetLayerMask;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private CharacterVisualSync _visualSync;
        private Camera _mainCamera;

        // Movement throttle
        private float _moveSendTimer;
        private Vector2 _lastSentPosition;
        private bool _lastSentFacing;

        // Server reconciliation
        private Vector2 _serverPosition;
        private bool _hasServerPosition;

        // Client-side predicted position
        private Vector2 _predictedPosition;

        // Target
        private GameObject _currentTarget;

        // Skill cooldowns (4 slots)
        private readonly float[] _skillCooldowns = new float[NetworkConfig.SKILL_SLOT_COUNT];
        private readonly float[] _skillMaxCooldowns = new float[NetworkConfig.SKILL_SLOT_COUNT];

        // Animation state tracking
        private bool _isMoving;
        private bool _facingRight = true;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            _visualSync = GetComponent<CharacterVisualSync>();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            _predictedPosition = (Vector2)transform.position;
            _serverPosition = _predictedPosition;
            _hasServerPosition = true;
            _lastSentPosition = _predictedPosition;

            // Initialize skill cooldown durations (will be populated from server data)
            for (int i = 0; i < NetworkConfig.SKILL_SLOT_COUNT; i++)
            {
                _skillCooldowns[i] = 0f;
                _skillMaxCooldowns[i] = 0f;
            }

            // Fire spawned event
            if (Bill.IsReady)
            {
                Bill.Events.Fire(new PlayerSpawnedEvent { PlayerObject = gameObject });
            }
        }

        private void Update()
        {
            HandleMovementInput();
            HandleTargeting();
            HandleSkillInput();
            UpdateCooldowns();
            ReconcileWithServer();
            UpdateCamera();
        }

        // -------------------------------------------------------
        // Movement
        // -------------------------------------------------------

        private void HandleMovementInput()
        {
            float h = 0f, v = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

            Vector2 input = new Vector2(h, v);
            bool isMovingNow = input.sqrMagnitude > 0.01f;

            if (isMovingNow)
            {
                Vector2 direction = input.normalized;

                // Client prediction: move visual immediately
                Vector2 delta = direction * moveSpeed * Time.deltaTime;
                _predictedPosition += delta;
                transform.position = new Vector3(_predictedPosition.x, _predictedPosition.y, transform.position.z);

                // Update facing direction
                if (Mathf.Abs(direction.x) > 0.01f)
                {
                    _facingRight = direction.x > 0f;
                    _visualSync.SetFacing(_facingRight);
                }

                // Set move animation
                if (!_isMoving)
                {
                    _isMoving = true;
                    _visualSync.SetAnimState(AnimState.Move);
                }

                // Throttle network sends to the configured rate
                _moveSendTimer += Time.deltaTime;
                if (_moveSendTimer >= NetworkConfig.MovementSendInterval)
                {
                    _moveSendTimer = 0f;
                    SendMovementToServer(direction);
                }
            }
            else
            {
                if (_isMoving)
                {
                    _isMoving = false;
                    _visualSync.SetAnimState(AnimState.Idle);

                    // Send a final stop update
                    SendMovementToServer(Vector2.zero);
                }
            }
        }

        private void SendMovementToServer(Vector2 direction)
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected || gm.Connection == null) return;

            // Call the UpdateMovement reducer with direction and animation state
            int animState = direction.sqrMagnitude > 0.01f ? AnimState.Move : AnimState.Idle;
            gm.Connection.Reducers.UpdateMovement(
                direction.x,
                direction.y,
                animState,
                _facingRight
            );

            _lastSentPosition = _predictedPosition;
            _lastSentFacing = _facingRight;
        }

        // -------------------------------------------------------
        // Server Reconciliation
        // -------------------------------------------------------

        /// <summary>
        /// Called by PlayerSpawner when the server sends a position update for this player.
        /// </summary>
        public void OnServerPositionUpdate(float x, float y, bool facingRight, int animState)
        {
            _serverPosition = new Vector2(x, y);
            _hasServerPosition = true;
        }

        private void ReconcileWithServer()
        {
            if (!_hasServerPosition) return;

            // Smoothly reconcile predicted position toward server position
            float distance = Vector2.Distance(_predictedPosition, _serverPosition);

            if (distance > 0.01f)
            {
                // If the discrepancy is large (teleport/lag), snap immediately
                if (distance > 3f)
                {
                    _predictedPosition = _serverPosition;
                }
                else
                {
                    _predictedPosition = Vector2.Lerp(_predictedPosition, _serverPosition, reconcileSpeed * Time.deltaTime);
                }

                transform.position = new Vector3(_predictedPosition.x, _predictedPosition.y, transform.position.z);
            }
        }

        // -------------------------------------------------------
        // Targeting
        // -------------------------------------------------------

        private void HandleTargeting()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera == null) return;

                Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0f, targetLayerMask);

                if (hit.collider != null)
                {
                    GameObject hitObj = hit.collider.gameObject;
                    _currentTarget = hitObj;

                    // Determine if target is a player or mob and call appropriate reducer
                    var remotePc = hitObj.GetComponent<RemotePlayerController>();
                    var mob = hitObj.GetComponent<NPC.MobController>();

                    if (remotePc != null)
                    {
                        // Attack another player
                        float dist = Vector2.Distance(transform.position, hitObj.transform.position);
                        if (dist <= attackRange)
                        {
                            var gm = GameManager.Instance;
                            if (gm != null && gm.IsConnected)
                            {
                                gm.Connection.Reducers.AttackPlayer(remotePc.OwnerIdentity);
                                _visualSync.SetAnimState(AnimState.Attack);
                            }
                        }

                        // Fire target changed event with player info
                        if (Bill.IsReady)
                        {
                            Bill.Events.Fire(new TargetChangedEvent
                            {
                                Target = hitObj,
                                TargetName = remotePc.PlayerName,
                                CurrentHp = remotePc.CurrentHp,
                                MaxHp = remotePc.MaxHp
                            });
                        }
                    }
                    else if (mob != null)
                    {
                        // Attack a mob
                        float dist = Vector2.Distance(transform.position, hitObj.transform.position);
                        if (dist <= attackRange)
                        {
                            var gm = GameManager.Instance;
                            if (gm != null && gm.IsConnected)
                            {
                                gm.Connection.Reducers.AttackMob(mob.MobId);
                                _visualSync.SetAnimState(AnimState.Attack);
                            }
                        }

                        // Fire target changed event with mob info
                        if (Bill.IsReady)
                        {
                            Bill.Events.Fire(new TargetChangedEvent
                            {
                                Target = hitObj,
                                TargetName = mob.MobName,
                                CurrentHp = mob.CurrentHp,
                                MaxHp = mob.MaxHp
                            });
                        }
                    }
                }
                else
                {
                    // Clicked empty space -- clear target
                    _currentTarget = null;
                    if (Bill.IsReady)
                    {
                        Bill.Events.Fire(new TargetChangedEvent
                        {
                            Target = null,
                            TargetName = "",
                            CurrentHp = 0,
                            MaxHp = 0
                        });
                    }
                }
            }
        }

        // -------------------------------------------------------
        // Skills
        // -------------------------------------------------------

        private void HandleSkillInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryUseSkill(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryUseSkill(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryUseSkill(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryUseSkill(3);
        }

        private void TryUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= NetworkConfig.SKILL_SLOT_COUNT) return;

            // Check cooldown
            if (_skillCooldowns[slotIndex] > 0f)
            {
                Debug.Log($"[LocalPlayer] Skill {slotIndex} is on cooldown ({_skillCooldowns[slotIndex]:F1}s remaining).");
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            // Check MP from local stats
            if (gm.LocalPlayerStats != null)
            {
                // SkillController can provide the MP cost for validation.
                // For now, let the server validate MP cost.
            }

            // Call the server reducer with target position (aim at current target or forward)
            float targetX = transform.position.x + (_facingRight ? 1f : -1f);
            float targetY = transform.position.y;
            if (_currentTarget != null)
            {
                targetX = _currentTarget.transform.position.x;
                targetY = _currentTarget.transform.position.y;
            }
            gm.Connection.Reducers.UseSkill(slotIndex, targetX, targetY);

            // Optimistic: play attack animation immediately
            _visualSync.SetAnimState(AnimState.Attack);

            Debug.Log($"[LocalPlayer] Used skill in slot {slotIndex}.");
        }

        /// <summary>
        /// Set a skill's cooldown. Called externally by SkillController when the server confirms skill use.
        /// </summary>
        public void SetSkillCooldown(int slotIndex, float cooldownDuration)
        {
            if (slotIndex < 0 || slotIndex >= NetworkConfig.SKILL_SLOT_COUNT) return;
            _skillCooldowns[slotIndex] = cooldownDuration;
            _skillMaxCooldowns[slotIndex] = cooldownDuration;

            if (Bill.IsReady)
            {
                Bill.Events.Fire(new SkillUsedEvent
                {
                    SlotIndex = slotIndex,
                    CooldownDuration = cooldownDuration
                });
            }
        }

        /// <summary>
        /// Get the remaining cooldown for a skill slot (0 = ready).
        /// </summary>
        public float GetSkillCooldown(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= NetworkConfig.SKILL_SLOT_COUNT) return 0f;
            return _skillCooldowns[slotIndex];
        }

        /// <summary>
        /// Get the remaining cooldown as a normalized ratio (0..1, where 0 = ready).
        /// </summary>
        public float GetSkillCooldownNormalized(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= NetworkConfig.SKILL_SLOT_COUNT) return 0f;
            if (_skillMaxCooldowns[slotIndex] <= 0f) return 0f;
            return _skillCooldowns[slotIndex] / _skillMaxCooldowns[slotIndex];
        }

        private void UpdateCooldowns()
        {
            for (int i = 0; i < NetworkConfig.SKILL_SLOT_COUNT; i++)
            {
                if (_skillCooldowns[i] > 0f)
                {
                    _skillCooldowns[i] -= Time.deltaTime;
                    if (_skillCooldowns[i] < 0f) _skillCooldowns[i] = 0f;
                }
            }
        }

        // -------------------------------------------------------
        // Camera Follow
        // -------------------------------------------------------

        private void UpdateCamera()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector3 desiredPos = transform.position + cameraOffset;
            _mainCamera.transform.position = Vector3.Lerp(
                _mainCamera.transform.position,
                desiredPos,
                cameraFollowSpeed * Time.deltaTime
            );
        }

        // -------------------------------------------------------
        // Public Accessors
        // -------------------------------------------------------

        public GameObject CurrentTarget => _currentTarget;
        public bool FacingRight => _facingRight;
    }
}

#endif // STDB_BINDINGS
