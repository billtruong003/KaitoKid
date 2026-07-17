using System.Collections.Generic;
using UnityEngine;
using BillGameCore;
using Mythfall.Characters;
using Mythfall.Enemy;
using Mythfall.Events;

namespace Mythfall.Player
{
    /// <summary>
    /// Auto-attacks the nearest target inside AttackRange every AttackInterval seconds.
    /// Hitbox enable/disable is animation-event driven via DynamicAnimationEventHub:
    ///   Animation event "OnHitboxEnable"  → MeleeCombat.OnHitboxEnable
    ///   Animation event "OnHitboxDisable" → MeleeCombat.OnHitboxDisable
    /// User wires UnityEvent in DynamicAnimationEventHub Inspector.
    ///
    /// Timer fallback: if Animator placeholder has no clip events, Bill.Timer.Delay
    /// fires the same callbacks at fixed offsets so combat still works for testing.
    /// Set <see cref="useTimerFallback"/> = false on real prefabs once anim events exist.
    /// </summary>
    public class MeleeCombat : PlayerCombatBase
    {
        [Header("Hitbox")]
        [SerializeField] SphereCollider hitbox;
        [SerializeField, Range(30f, 360f)] float hitboxArcAngle = 120f;

        [Header("Animation event fallback (placeholder Animator)")]
        [SerializeField] bool useTimerFallback = true;
        [SerializeField] float fallbackEnableDelay = 0.15f;
        [SerializeField] float fallbackDisableDelay = 0.4f;

        readonly HashSet<EnemyBase> hitThisSwing = new(8);

        void Awake()
        {
            if (hitbox != null) hitbox.enabled = false;
        }

        protected override bool CanAttack()
        {
            if (attackTimer > 0f || owner == null) return false;
            var ts = owner.TargetSelector;
            if (ts == null || ts.CurrentTarget == null) return false;
            float dist = Vector3.Distance(owner.transform.position, ts.CurrentTarget.position);
            return dist <= owner.Stats.GetFinal(StatType.AttackRange);
        }

        protected override void Execute()
        {
            if (owner.Animator != null) owner.Animator.SetTrigger("Attack_1");
            attackTimer = owner.Stats.GetFinal(StatType.AttackInterval);

            if (useTimerFallback)
            {
                Bill.Timer.Delay(fallbackEnableDelay, OnHitboxEnable);
                Bill.Timer.Delay(fallbackDisableDelay, OnHitboxDisable);
            }
        }

        // ----- Animation event handlers (UnityEvent compatible — no params) -----

        public void OnHitboxEnable()
        {
            if (hitbox == null || hitbox.enabled) return;
            hitbox.enabled = true;
            hitThisSwing.Clear();
        }

        public void OnHitboxDisable()
        {
            if (hitbox != null) hitbox.enabled = false;
        }

        // Called by HitboxRelay (child GameObject hosting the SphereCollider trigger)
        public void OnHitboxTriggerEnter(Collider other)
        {
            if (hitbox == null || !hitbox.enabled) return;

            var enemy = other.GetComponent<EnemyBase>();
            if (enemy == null || !enemy.IsAlive || hitThisSwing.Contains(enemy)) return;

            // Arc check — only damage targets within forward cone
            Vector3 toEnemy = enemy.transform.position - owner.transform.position;
            toEnemy.y = 0f;
            Vector3 fwd = owner.transform.forward;
            fwd.y = 0f;
            if (toEnemy.sqrMagnitude < 0.0001f) return;

            float angle = Vector3.Angle(fwd.normalized, toEnemy.normalized);
            if (angle > hitboxArcAngle * 0.5f) return;

            float damage = CalculateDamage(out bool isCrit);
            enemy.TakeDamage(damage, owner);
            hitThisSwing.Add(enemy);

            Bill.Events.Fire(new EnemyHitEvent
            {
                attacker = owner,
                victim = enemy,
                damage = damage,
                isCrit = isCrit,
                hitPoint = enemy.transform.position,
            });
        }
    }
}
