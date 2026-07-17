using System;
using UnityEngine;
using BillGameCore;
using Mythfall.Events;

namespace Mythfall.Player
{
    /// <summary>
    /// HP container with brief invincibility frames (iFrame) on hit. Owned by PlayerBase.
    /// Fires <see cref="PlayerDamagedEvent"/> on damage and OnDeath C# event when HP hits 0.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsAlive => CurrentHP > 0f;

        public event Action<float> OnDamaged;
        public event Action<float> OnHealed;
        public event Action OnDeath;

        const float HitInvincibilityDuration = 0.3f;

        float invincibleTimer;

        public void Initialize(float maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = maxHP;
            IsInvincible = false;
            invincibleTimer = 0f;
        }

        void Update()
        {
            if (invincibleTimer > 0f)
            {
                invincibleTimer -= Time.deltaTime;
                if (invincibleTimer <= 0f) IsInvincible = false;
            }
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || IsInvincible || amount <= 0f) return;

            CurrentHP = Mathf.Max(0f, CurrentHP - amount);
            OnDamaged?.Invoke(amount);
            Bill.Events.Fire(new PlayerDamagedEvent
            {
                damage = amount,
                currentHP = CurrentHP,
                maxHP = MaxHP,
            });

            if (CurrentHP <= 0f)
            {
                OnDeath?.Invoke();
                return;
            }

            SetInvincible(HitInvincibilityDuration);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHealed?.Invoke(amount);
        }

        public void SetInvincible(float duration)
        {
            IsInvincible = true;
            invincibleTimer = Mathf.Max(invincibleTimer, duration);
        }
    }
}
