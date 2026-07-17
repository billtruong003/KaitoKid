using UnityEngine;
using BillGameCore;
using Mythfall.Characters;
using Mythfall.Gameplay;

namespace Mythfall.Player
{
    /// <summary>
    /// Auto-attacks the nearest target inside AttackRange. Aim direction is captured at
    /// attack-start (fire-and-forget — projectile flies toward where target was, not where
    /// it is when arrow releases). Projectile spawn happens on animation event "OnArrowRelease".
    ///
    /// Timer fallback: if Animator placeholder has no clip events, Bill.Timer.Delay fires
    /// OnArrowRelease at <see cref="fallbackReleaseDelay"/> so combat works for testing.
    /// </summary>
    public class RangedCombat : PlayerCombatBase
    {
        [Header("Projectile")]
        [SerializeField] string projectilePoolKey = "Projectile_Arrow";
        [SerializeField] float projectileSpeed = 15f;
        [SerializeField, Min(0)] int pierceCount = 0;

        [Header("Animation event fallback")]
        [SerializeField] bool useTimerFallback = true;
        [SerializeField] float fallbackReleaseDelay = 0.5f;

        Vector3 cachedAimDirection;
        bool releasePending;

        protected override bool CanAttack()
        {
            if (attackTimer > 0f || releasePending || owner == null) return false;
            var ts = owner.TargetSelector;
            if (ts == null || ts.CurrentTarget == null) return false;
            float dist = Vector3.Distance(owner.transform.position, ts.CurrentTarget.position);
            return dist <= owner.Stats.GetFinal(StatType.AttackRange);
        }

        protected override void Execute()
        {
            if (owner.Animator != null) owner.Animator.SetTrigger("Attack_1");
            attackTimer = owner.Stats.GetFinal(StatType.AttackInterval);

            // Lock aim at attack start
            var ts = owner.TargetSelector;
            Vector3 source = owner.MuzzlePoint != null ? owner.MuzzlePoint.position : owner.transform.position;
            if (ts != null && ts.CurrentTarget != null)
            {
                cachedAimDirection = ts.CurrentTarget.position - source;
            }
            else
            {
                cachedAimDirection = owner.transform.forward;
            }
            cachedAimDirection.y = 0f;
            if (cachedAimDirection.sqrMagnitude < 0.0001f) cachedAimDirection = owner.transform.forward;
            cachedAimDirection.Normalize();

            releasePending = true;

            if (useTimerFallback)
                Bill.Timer.Delay(fallbackReleaseDelay, OnArrowRelease);
        }

        // ----- Animation event handler (UnityEvent compatible) -----

        public void OnArrowRelease()
        {
            if (!releasePending) return;
            releasePending = false;

            if (owner == null || owner.MuzzlePoint == null) return;

            var go = Bill.Pool?.Spawn(projectilePoolKey, owner.MuzzlePoint.position, Quaternion.LookRotation(cachedAimDirection));
            if (go == null)
            {
                Debug.LogWarning($"[RangedCombat] Pool '{projectilePoolKey}' missing — register it in GameBootstrap.");
                return;
            }

            var proj = go.GetComponent<Projectile>();
            if (proj == null)
            {
                Debug.LogWarning($"[RangedCombat] Spawned '{projectilePoolKey}' has no Projectile component.");
                return;
            }

            float dmg = CalculateDamage(out bool isCrit);
            proj.Setup(cachedAimDirection, projectileSpeed, dmg, isCrit, owner, pierceCount);
        }
    }
}
