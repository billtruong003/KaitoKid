using UnityEngine;
using BillGameCore;
using ModularTopDown.Locomotion;
using Mythfall.Characters;
using Mythfall.Events;
using Mythfall.Input;
using Mythfall.States;

namespace Mythfall.Player
{
    /// <summary>
    /// Abstract base for both melee + ranged players. Wires CharacterLocomotion (with
    /// <c>ExternalRotationControl=true</c> per CLAUDE.md Rule 4), drives the Animator's
    /// "Speed" parameter, owns RuntimeCharacterStats, and routes death to DefeatState.
    ///
    /// Concrete subclasses (MeleePlayer / RangedPlayer — Day 2) supply the Combat property.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CharacterLocomotion))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(TargetSelector))]
    [RequireComponent(typeof(PlayerFacing))]
    public abstract class PlayerBase : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] protected CharacterDataSO characterData;

        [Header("Refs")]
        [SerializeField] protected Transform muzzlePoint;
        [SerializeField] protected Animator animator;

        protected CharacterLocomotion locomotion;
        protected PlayerFacing facing;
        protected TargetSelector targetSelector;
        protected PlayerHealth health;
        protected RuntimeCharacterStats stats;

        public CharacterDataSO Data => characterData;
        public RuntimeCharacterStats Stats => stats;
        public PlayerHealth Health => health;
        public Transform MuzzlePoint => muzzlePoint;
        public Animator Animator => animator;
        public TargetSelector TargetSelector => targetSelector;
        public PlayerFacing Facing => facing;
        public abstract PlayerCombatBase Combat { get; }

        static readonly int AnimSpeedHash = Animator.StringToHash("Speed");

        protected virtual void Awake()
        {
            locomotion = GetComponent<CharacterLocomotion>();
            facing = GetComponent<PlayerFacing>();
            targetSelector = GetComponent<TargetSelector>();
            health = GetComponent<PlayerHealth>();

            if (animator == null) animator = GetComponentInChildren<Animator>();

            // CRITICAL — CLAUDE.md Rule 4: locomotion does NOT auto-rotate; PlayerFacing handles facing.
            locomotion.ExternalRotationControl = true;
            locomotion.ConfigureJumps(allowDoubleJump: false);
        }

        protected virtual void Start()
        {
            if (characterData == null)
            {
                Debug.LogError($"[{name}] CharacterDataSO not assigned — disabling.", this);
                enabled = false;
                return;
            }

            stats = new RuntimeCharacterStats(characterData.baseStats);
            health.Initialize(stats.GetFinal(StatType.MaxHP));
            health.OnDeath += HandleDeath;
        }

        protected virtual void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        protected virtual void Update()
        {
            if (!health.IsAlive) return;

            var input = MobileInputManager.MoveVector;
            bool grounded = locomotion.IsGrounded();

            if (grounded) locomotion.HandleGroundedMovement(input, MobileInputManager.IsRunning);
            else locomotion.HandleAirborneMovement(input);

            UpdateAnimatorSpeed();
        }

        void UpdateAnimatorSpeed()
        {
            if (animator == null) return;
            float runSpeed = locomotion.RunSpeed > 0.01f ? locomotion.RunSpeed : 1f;
            float normalized = locomotion.HorizontalVelocity.magnitude / runSpeed;
            animator.SetFloat(AnimSpeedHash, normalized, 0.1f, Time.deltaTime);
        }

        protected virtual void HandleDeath()
        {
            if (animator != null) animator.SetTrigger("Death");

            Bill.Events.Fire(new PlayerDiedEvent
            {
                player = this,
                position = transform.position,
            });
            Bill.State.GoTo<DefeatState>();
        }
    }
}
