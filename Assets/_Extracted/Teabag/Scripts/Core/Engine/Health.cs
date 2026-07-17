using Fusion;
using System;
using UnityEngine;

namespace Teabag.Core
{
    public class Health : RoyaleNetworkBehaviour
    {
        [NonSerialized] public bool ready = false;
        public Action<byte, byte> onHealthChanged;
        public Action onDeath;
        [SerializeField] protected byte baseHealth = 100;
        [SerializeField] protected byte baseShield = 50;
        [Networked] public byte MaxHealth { set; get; } = 100;
        [Networked] public byte MaxShield { set; get; } = 0;

        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        public byte CurrentShieldAmount { get; set; }

        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        public byte CurrentHealthAmount { get; set; }

        public byte TotalHealth => (byte)(CurrentHealthAmount + (CurrentHealthAmount == byte.MinValue ? 0 : CurrentShieldAmount));

        public bool isDead
        {
            get
            {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD // disable death for local player if God Mode is enabled (debug only, this doesn't appear in production)
                if (Object && Object.StateAuthority == Runner.LocalPlayer && GameServices.GodModeEnabled)
                {
                    CurrentHealthAmount = byte.MaxValue;
                    return false;
                }
    #endif
            
                if (!Object || !Object.IsValid)
                    return false;
                return TotalHealth == byte.MinValue;
            }
        }

        public override void SpawnedRoyale()
        {
            base.SpawnedRoyale();
            MaxHealth = baseHealth;
            MaxShield = baseShield;
            CurrentHealthAmount = MaxHealth;
            CurrentShieldAmount = MaxShield;
            OnHealthChanged();
        }

        public virtual void OnHealthChanged()
        {
            if (ready)
            {
                onHealthChanged?.Invoke(CurrentHealthAmount, CurrentShieldAmount);
            }

            ready = true;
        }

        public void OnHit(byte damage, float bulletSpeed, RaycastHit hit, Vector3 source, PlayerRef? killer = null)
        {
            Damage(damage);
        }

        public void Damage(byte amount, PlayerRef? killer = null)
        {
            // If killer is not specified, default to the local player who initiated the damage
            PlayerRef explicitKiller = killer ?? Runner.LocalPlayer;
            RPCDamage(amount, explicitKiller, 0);
        }

        public void Damage(byte amount, byte hitType, PlayerRef? killer = null)
        {
            PlayerRef explicitKiller = killer ?? Runner.LocalPlayer;
            RPCDamage(amount, explicitKiller, hitType);
        }
    
        // shield + health
        // e.g. 50 + 100 -> 150
        //
        // max shield is 50
        // max health is 100
        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        protected virtual void RPCDamage(byte damageAmount, PlayerRef explicitKiller, byte hitType)
        {
            // early exit if no damage to apply or if already dead
            if (damageAmount == 0 || isDead) return;

    #if UNITY_EDITOR || DEVELOPMENT_BUILD // disable damage for local player if God Mode is enabled (debug only, this doesn't appear in production)
            if (Object && Object.StateAuthority == Runner.LocalPlayer && GameServices.GodModeEnabled)
            {
                CurrentHealthAmount = byte.MaxValue;
                return;
            }
    #endif

            // apply shield damage
            int shieldDamage = Math.Min(damageAmount, CurrentShieldAmount);
            CurrentShieldAmount -= (byte)shieldDamage;

            // apply remaining damage to health
            int remainingDamage = damageAmount - shieldDamage;
            int newHealth = Math.Max(CurrentHealthAmount - remainingDamage, 0);
            CurrentHealthAmount = (byte)newHealth;

            if (TotalHealth == 0 && explicitKiller != PlayerRef.None)
                RPCReportDeath(explicitKiller);
        }

        public void Heal(byte amount, bool isShield) => RPCHeal(amount, isShield);

        [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
        private void RPCHeal(byte healAmount, bool isShield)
        {
            if (healAmount == 0 || isDead || (!isShield && CurrentHealthAmount >= MaxHealth) || (isShield && CurrentShieldAmount >= MaxShield && MaxShield > 0))
                return;

            if (!isShield)
            {
                int newHealthValue = CurrentHealthAmount + healAmount;
                CurrentHealthAmount = (byte)Math.Min(newHealthValue, MaxHealth);
            }
            else
            {
                int newShieldValue = CurrentShieldAmount + healAmount;
                CurrentShieldAmount = (byte)Math.Min(newShieldValue, MaxShield);
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPCReportDeath(PlayerRef killer)
        {
            OnDeathReported(killer);
            onDeath?.Invoke();
        }

        public virtual void OnDeathReported(PlayerRef killer)
        {

        }

        public bool CanHeal(bool isShield = false)
        {
            if (isDead || MaxHealth <= 0)
                return false;

            if (!isShield)
                return CurrentHealthAmount < MaxHealth;

            return CurrentShieldAmount < MaxShield;
        }
    }
}
