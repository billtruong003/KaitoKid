using UnityEngine;
using BillGameCore;
using Mythfall.Events;
using Mythfall.Player;

namespace Mythfall.Enemy
{
    public enum EnemyAIState { Idle, Chase, Attack, Stunned, Dying }

    /// <summary>
    /// Common enemy contract: HP, damage intake, death routing, pool-aware spawn/despawn,
    /// AI state machine driver.
    ///
    /// State machine model:
    ///   - Base owns the state field + transition plumbing + the Update() driver.
    ///   - Subclasses implement <see cref="TickState"/> with per-state behavior, and
    ///     optionally override <see cref="OnStateEnter"/> / <see cref="OnStateExit"/>
    ///     for transition hooks (e.g. trigger telegraph anim on Attack enter).
    ///
    /// Initial state is <see cref="EnemyAIState.Idle"/>; subclasses typically
    /// transition to Chase as soon as <see cref="ResolvePlayerTransform"/> succeeds.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyDataSO data;

        protected float currentHP;
        protected float maxHP;
        protected Transform playerTransform;

        public bool IsAlive { get; protected set; }
        public EnemyDataSO Data => data;
        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;
        public EnemyAIState CurrentState => _currentState;

        const string PlayerTag = "Player";
        const float DespawnDelay = 1f;

        EnemyAIState _currentState = EnemyAIState.Idle;
        protected float stateTimer;

        // Stat overrides (applied by EliteModifier or boss spawn).
        // 1.0 = baseline; mutate before OnSpawn to take effect.
        protected float hpMultiplier = 1f;
        protected float damageMultiplier = 1f;

        protected virtual void Start()
        {
            // Scene-placed instance (e.g. dropped into GameplayScene via editor) — pool spawner
            // would have called OnSpawn already, but for editor-only test layouts we need to
            // self-init so chase/attack logic activates.
            if (!IsAlive) OnSpawn();
        }

        public virtual void OnSpawn()
        {
            if (data == null)
            {
                Debug.LogError($"[{name}] EnemyDataSO not assigned.", this);
                return;
            }

            maxHP = data.maxHP * hpMultiplier;
            currentHP = maxHP;
            IsAlive = true;
            gameObject.SetActive(true);

            // Player may not be in scene yet — GameplaySpawner is async on Bill ready
            // and may run after WaveSpawner spawns this enemy. Subclasses lazy-fetch
            // via ResolvePlayerTransform() in their Update tick.
            playerTransform = null;

            // Reset state machine
            _currentState = EnemyAIState.Idle;
            stateTimer = 0f;
        }

        /// <summary>
        /// Lazy-fetch player transform when subclasses first need it. Cached on
        /// success — subsequent calls are O(1). Called from Update tick to handle
        /// the GameplaySpawner-vs-WaveSpawner ordering race.
        /// </summary>
        protected Transform ResolvePlayerTransform()
        {
            if (playerTransform != null) return playerTransform;
            var p = GameObject.FindGameObjectWithTag(PlayerTag);
            if (p != null) playerTransform = p.transform;
            return playerTransform;
        }

        // ----- State machine -----

        protected void Update()
        {
            if (!IsAlive) return;
            if (ResolvePlayerTransform() == null) return;

            stateTimer += Time.deltaTime;
            TickState(Time.deltaTime);
        }

        /// <summary>Per-state behavior tick. Implementation responsible for driving
        /// transitions via <see cref="TransitionTo"/> when conditions are met.</summary>
        protected abstract void TickState(float dt);

        /// <summary>Transition to a new state. Idempotent on same-state (no hook fires).</summary>
        public virtual void TransitionTo(EnemyAIState newState)
        {
            if (_currentState == newState) return;
            var prev = _currentState;
            OnStateExit(prev);
            _currentState = newState;
            stateTimer = 0f;
            OnStateEnter(newState);
        }

        /// <summary>Hook for entering a new state. Default no-op. Subclasses override
        /// to play telegraph anim, fire event, set timers, etc.</summary>
        protected virtual void OnStateEnter(EnemyAIState state) { }

        /// <summary>Hook for leaving the current state. Default no-op.</summary>
        protected virtual void OnStateExit(EnemyAIState state) { }

        // ----- Elite/boss stat overrides -----

        /// <summary>Apply stat multipliers BEFORE <see cref="OnSpawn"/> so HP scaling
        /// takes effect. Called by EliteModifier in its Awake.</summary>
        public void SetStatMultipliers(float hpMult, float dmgMult)
        {
            hpMultiplier = Mathf.Max(0.01f, hpMult);
            damageMultiplier = Mathf.Max(0.01f, dmgMult);
        }

        /// <summary>Damage value to apply to player on hit, factoring elite multiplier.</summary>
        protected float ScaledAttackPower
            => (data != null ? data.attackPower : 5f) * damageMultiplier;

        // ----- Damage + death -----

        public virtual void TakeDamage(float amount, PlayerBase attacker)
        {
            if (!IsAlive || amount <= 0f) return;

            currentHP -= amount;
            if (currentHP <= 0f) Die(attacker);
        }

        protected virtual void Die(PlayerBase killer)
        {
            IsAlive = false;
            TransitionTo(EnemyAIState.Dying);
            OnDeath();

            Bill.Events.Fire(new EnemyKilledEvent
            {
                enemy = this,
                killer = killer,
                position = transform.position,
            });

            // Brief delay so death anim can play before pool returns the GO
            Bill.Timer.Delay(DespawnDelay, ReturnToPool);
        }

        protected virtual void OnDeath() { }

        void ReturnToPool()
        {
            if (Bill.Pool != null) Bill.Pool.Return(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
