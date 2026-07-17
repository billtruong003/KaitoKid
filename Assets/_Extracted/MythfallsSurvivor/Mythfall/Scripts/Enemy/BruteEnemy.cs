using UnityEngine;
using BillGameCore;
using Mythfall.Player;
using Mythfall.Polish;

namespace Mythfall.Enemy
{
    /// <summary>
    /// Slow, high-HP tank. Telegraphs a big slam attack with a clear wind-up window
    /// (designed so player can dodge if they react in time). On hit: AoE damage +
    /// knockback on player.
    ///
    /// State flow: Idle → Chase → Attack (telegraph → hit → recovery) → Chase.
    ///
    /// Tunables driven by <see cref="EnemyDataSO"/>:
    ///   - moveSpeed (designed ~2)
    ///   - maxHP (designed ~80)
    ///   - attackPower (designed ~15)
    ///   - attackRange (engage distance ~2.0)
    ///   - attackCooldown (full rotation ~2.5s)
    /// </summary>
    public class BruteEnemy : EnemyBase
    {
        [Header("Refs")]
        [SerializeField] Animator animator;

        [Header("Attack tuning")]
        [Tooltip("Telegraph (wind-up) duration before slam connects.")]
        [SerializeField] float telegraphDuration = 0.8f;

        [Tooltip("Active hit window after telegraph.")]
        [SerializeField] float hitWindow = 0.2f;

        [Tooltip("Recovery after hit window before returning to Chase.")]
        [SerializeField] float recoveryDuration = 0.5f;

        [Tooltip("AoE radius of the slam.")]
        [SerializeField] float slamRadius = 2f;

        [Tooltip("Knockback impulse applied to player on slam hit.")]
        [SerializeField] float knockbackForce = 10f;

        bool hitApplied;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int AttackHash = Animator.StringToHash("Attack");
        static readonly int DeathHash = Animator.StringToHash("Death");

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            hitApplied = false;
        }

        protected override void TickState(float dt)
        {
            switch (CurrentState)
            {
                case EnemyAIState.Idle:
                    TransitionTo(EnemyAIState.Chase);
                    break;
                case EnemyAIState.Chase: TickChase(dt); break;
                case EnemyAIState.Attack: TickAttack(dt); break;
            }
        }

        protected override void OnStateEnter(EnemyAIState state)
        {
            if (state == EnemyAIState.Attack)
            {
                hitApplied = false;
                if (animator != null) animator.SetTrigger(AttackHash);
            }
        }

        void TickChase(float dt)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist <= data.attackRange)
            {
                TransitionTo(EnemyAIState.Attack);
                return;
            }

            Vector3 dir = toPlayer.normalized;
            transform.position += dir * data.moveSpeed * dt;
            transform.rotation = Quaternion.LookRotation(dir);
            if (animator != null) animator.SetFloat(SpeedHash, 1f, 0.1f, dt);
        }

        void TickAttack(float dt)
        {
            if (animator != null) animator.SetFloat(SpeedHash, 0f, 0.1f, dt);

            // Face player during wind-up so slam tracks last-moment dodges.
            if (stateTimer < telegraphDuration)
            {
                Vector3 toPlayer = playerTransform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
                return;
            }

            // Hit window: deal damage once when first entering the window.
            if (!hitApplied && stateTimer >= telegraphDuration && stateTimer < telegraphDuration + hitWindow)
            {
                ApplySlam();
                hitApplied = true;
                return;
            }

            // Recovery → back to Chase.
            if (stateTimer >= telegraphDuration + hitWindow + recoveryDuration)
            {
                TransitionTo(EnemyAIState.Chase);
            }
        }

        void ApplySlam()
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist > slamRadius) return; // player dodged out of AoE

            var hp = playerTransform.GetComponent<PlayerHealth>();
            hp?.TakeDamage(ScaledAttackPower);

            var kb = playerTransform.GetComponent<KnockbackReceiver>();
            if (kb != null)
            {
                Vector3 knockDir = (playerTransform.position - transform.position);
                knockDir.y = 0f;
                if (knockDir.sqrMagnitude < 0.01f) knockDir = transform.forward;
                kb.ApplyKnockback(knockDir.normalized * knockbackForce);
            }
        }

        protected override void OnDeath()
        {
            if (animator != null) animator.SetTrigger(DeathHash);
        }
    }
}
