using UnityEngine;
using BillGameCore;
using Mythfall.Enemy;
using Mythfall.Events;
using Mythfall.Player;

namespace Mythfall.Gameplay
{
    /// <summary>
    /// Pool-managed projectile. Setup() must be called immediately after Bill.Pool.Spawn.
    /// Returns to pool on lifetime expiry or when pierce budget reaches zero.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] float lifetime = 5f;

        Vector3 velocity;
        float damage;
        bool isCrit;
        int pierceLeft;
        PlayerBase owner;
        float aliveTimer;
        bool armed;

        public void Setup(Vector3 direction, float speed, float dmg, bool crit, PlayerBase ownerRef, int pierceCount)
        {
            velocity = direction.normalized * speed;
            damage = dmg;
            isCrit = crit;
            pierceLeft = pierceCount;
            owner = ownerRef;
            aliveTimer = 0f;
            armed = true;
            transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.0001f ? direction : Vector3.forward);
        }

        void OnDisable()
        {
            armed = false;
        }

        void Update()
        {
            if (!armed) return;
            transform.position += velocity * Time.deltaTime;
            aliveTimer += Time.deltaTime;
            if (aliveTimer >= lifetime) Despawn();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!armed) return;
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy == null || !enemy.IsAlive) return;

            enemy.TakeDamage(damage, owner);
            Bill.Events.Fire(new EnemyHitEvent
            {
                attacker = owner,
                victim = enemy,
                damage = damage,
                isCrit = isCrit,
                hitPoint = transform.position,
            });

            if (pierceLeft <= 0) Despawn();
            else pierceLeft--;
        }

        void Despawn()
        {
            armed = false;
            if (Bill.Pool != null) Bill.Pool.Return(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
