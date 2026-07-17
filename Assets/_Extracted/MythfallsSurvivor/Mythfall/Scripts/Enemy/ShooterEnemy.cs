using UnityEngine;
using BillGameCore;

namespace Mythfall.Enemy
{
    /// <summary>
    /// Ranged kite enemy. Maintains a comfort distance band from the player,
    /// back-pedaling if too close, advancing if too far, and firing a projectile
    /// on a fixed interval when in range. Low HP — designed to be a priority target.
    ///
    /// State flow: Idle → Chase (only). Firing is a sub-behavior of Chase, not a
    /// separate state, so movement and shooting interleave naturally.
    ///
    /// Tunables driven by <see cref="EnemyDataSO"/>:
    ///   - moveSpeed (designed ~3)
    ///   - maxHP (designed ~40)
    ///   - attackPower (projectile damage ~8)
    ///   - attackRange (max engage distance — see <see cref="maxEngageDistance"/> override)
    ///   - attackCooldown (fire interval, designed ~2s)
    /// </summary>
    public class ShooterEnemy : EnemyBase
    {
        [Header("Refs")]
        [SerializeField] Animator animator;
        [SerializeField] Transform muzzle;

        [Header("Projectile")]
        [SerializeField] string projectilePoolKey = "Enemy_Projectile";
        [SerializeField] float projectileSpeed = 12f;

        [Header("Kite tuning")]
        [Tooltip("Back-pedal if player closer than this.")]
        [SerializeField] float minEngageDistance = 5f;

        [Tooltip("Advance if player further than this.")]
        [SerializeField] float maxEngageDistance = 10f;

        float fireTimer;

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
            fireTimer = data != null ? data.attackCooldown : 2f;
        }

        protected override void TickState(float dt)
        {
            switch (CurrentState)
            {
                case EnemyAIState.Idle:
                    TransitionTo(EnemyAIState.Chase);
                    break;
                case EnemyAIState.Chase:
                    TickKite(dt);
                    break;
            }
        }

        void TickKite(float dt)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 dir = dist > 0.001f ? toPlayer / dist : Vector3.forward;

            // Always face the player so projectile aim and visual orientation match.
            transform.rotation = Quaternion.LookRotation(dir);

            // Maintain band: back-pedal if too close, advance if too far.
            float speed = data.moveSpeed;
            if (dist < minEngageDistance)
            {
                transform.position -= dir * speed * dt;
                if (animator != null) animator.SetFloat(SpeedHash, 1f, 0.1f, dt);
            }
            else if (dist > maxEngageDistance)
            {
                transform.position += dir * speed * dt;
                if (animator != null) animator.SetFloat(SpeedHash, 1f, 0.1f, dt);
            }
            else if (animator != null)
            {
                animator.SetFloat(SpeedHash, 0f, 0.1f, dt);
            }

            // Fire when cooldown expires AND we have line of sight in the engage band.
            fireTimer -= dt;
            if (fireTimer <= 0f && dist <= maxEngageDistance)
            {
                FireProjectile(dir);
                fireTimer = data.attackCooldown > 0f ? data.attackCooldown : 2f;
            }
        }

        void FireProjectile(Vector3 aimDir)
        {
            if (animator != null) animator.SetTrigger(AttackHash);

            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.2f;
            var rot = Quaternion.LookRotation(aimDir);
            var go = Bill.Pool.Spawn(projectilePoolKey, spawnPos, rot);
            if (go == null) return;

            var proj = go.GetComponent<EnemyProjectile>();
            if (proj == null)
            {
                Debug.LogWarning($"[{name}] '{projectilePoolKey}' pool returned GO without EnemyProjectile component.", this);
                return;
            }

            proj.Setup(aimDir, projectileSpeed, ScaledAttackPower);
        }

        protected override void OnDeath()
        {
            if (animator != null) animator.SetTrigger(DeathHash);
        }
    }
}
