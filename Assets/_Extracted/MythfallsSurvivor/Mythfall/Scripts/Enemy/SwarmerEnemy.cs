using UnityEngine;
using BillGameCore;
using Mythfall.Player;

namespace Mythfall.Enemy
{
    /// <summary>
    /// Basic chase-and-melee enemy. Walks toward player; once inside attackRange,
    /// triggers Attack animation. Damage application is animation-event driven via
    /// DynamicAnimationEventHub (event "OnAttackHit" → SwarmerEnemy.OnAttackHit).
    ///
    /// Timer fallback covers placeholder Animator without animation events so combat
    /// loop is testable on capsule prefab before real anim is wired.
    ///
    /// State machine: Idle → Chase → Attack → Chase loop. Death handled by EnemyBase.
    /// </summary>
    public class SwarmerEnemy : EnemyBase
    {
        [Header("Refs")]
        [SerializeField] Animator animator;

        [Header("Animation event fallback")]
        [SerializeField] bool useTimerFallback = true;
        [SerializeField] float fallbackHitDelay = 0.3f;

        float attackCooldownTimer;
        bool attackPending;

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
            attackCooldownTimer = 0f;
            attackPending = false;
        }

        protected override void TickState(float dt)
        {
            if (attackCooldownTimer > 0f) attackCooldownTimer -= dt;

            switch (CurrentState)
            {
                case EnemyAIState.Idle:
                    TransitionTo(EnemyAIState.Chase);
                    break;

                case EnemyAIState.Chase:
                    TickChase(dt);
                    break;

                case EnemyAIState.Attack:
                    TickAttack(dt);
                    break;
            }
        }

        void TickChase(float dt)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            float range = data.attackRange;

            if (dist <= range)
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
            // Face player while attacking
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized);

            if (animator != null) animator.SetFloat(SpeedHash, 0f, 0.1f, dt);

            // Wait until current swing finishes and cooldown elapses before re-deciding.
            if (attackPending || attackCooldownTimer > 0f) return;

            // Cooldown done — re-evaluate range. If player walked out, chase again
            // instead of swinging at empty air.
            if (toPlayer.magnitude > data.attackRange + 0.2f)
            {
                TransitionTo(EnemyAIState.Chase);
                return;
            }

            // Still in range → swing again.
            FireAttack();
        }

        void FireAttack()
        {
            attackPending = true;
            attackCooldownTimer = data.attackCooldown > 0f ? data.attackCooldown : 1.2f;
            if (animator != null) animator.SetTrigger(AttackHash);

            if (useTimerFallback)
                Bill.Timer.Delay(fallbackHitDelay, OnAttackHit);
        }

        // ----- Animation event handler (UnityEvent compatible) -----

        public void OnAttackHit()
        {
            if (!attackPending) return;
            attackPending = false;

            if (!IsAlive || playerTransform == null) return;

            // Only apply damage if player is still close enough — they may have dodged.
            // Either way, fall back to Chase so the next tick re-decides (attack/chase).
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= data.attackRange + 0.5f)
            {
                var hp = playerTransform.GetComponent<PlayerHealth>();
                hp?.TakeDamage(ScaledAttackPower);
            }

            TransitionTo(EnemyAIState.Chase);
        }

        protected override void OnDeath()
        {
            if (animator != null) animator.SetTrigger(DeathHash);
            attackPending = false;
        }
    }
}
