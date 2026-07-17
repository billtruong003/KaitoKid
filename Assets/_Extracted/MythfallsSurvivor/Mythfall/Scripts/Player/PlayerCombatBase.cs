using UnityEngine;

namespace Mythfall.Player
{
    /// <summary>
    /// Sprint 1 Day 1 — abstract scaffold so PlayerBase compiles before MeleeCombat /
    /// RangedCombat exist. Day 2 fills in CanAttack/Execute logic + animation event hooks.
    /// </summary>
    public abstract class PlayerCombatBase : MonoBehaviour
    {
        protected PlayerBase owner;
        protected float attackTimer;

        public virtual void Initialize(PlayerBase ownerRef)
        {
            owner = ownerRef;
        }

        protected virtual void Update()
        {
            if (attackTimer > 0f) attackTimer -= Time.deltaTime;
            if (CanAttack()) Execute();
        }

        protected abstract bool CanAttack();
        protected abstract void Execute();

        protected float CalculateDamage(out bool isCrit)
        {
            isCrit = false;
            if (owner == null) return 0f;

            float atk = owner.Stats.GetFinal(Mythfall.Characters.StatType.AttackPower);
            float critRate = owner.Stats.GetFinal(Mythfall.Characters.StatType.CritRate);
            float critDmg = owner.Stats.GetFinal(Mythfall.Characters.StatType.CritDamage);
            isCrit = Random.Range(0f, 100f) < critRate;
            return isCrit ? atk * (critDmg / 100f) : atk;
        }
    }
}
