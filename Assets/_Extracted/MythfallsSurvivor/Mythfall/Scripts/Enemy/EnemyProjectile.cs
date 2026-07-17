using UnityEngine;
using BillGameCore;
using Mythfall.Player;

namespace Mythfall.Enemy
{
    /// <summary>
    /// Pooled projectile fired by <see cref="ShooterEnemy"/>. Travels in a straight
    /// line, damages the player on trigger, returns to pool on hit or lifetime expiry.
    ///
    /// Mirrors <see cref="Mythfall.Gameplay.Projectile"/> but targets PlayerHealth
    /// instead of EnemyBase. Kept as a separate class so layer-collision matrix
    /// (Projectile vs Player vs Enemy) can be tuned independently per direction.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] float lifetime = 4f;

        Vector3 velocity;
        float damage;
        float aliveTimer;

        public void Setup(Vector3 dir, float speed, float dmg)
        {
            velocity = dir * speed;
            damage = dmg;
            aliveTimer = 0f;
        }

        void OnEnable()
        {
            aliveTimer = 0f;
        }

        void Update()
        {
            transform.position += velocity * Time.deltaTime;
            aliveTimer += Time.deltaTime;
            if (aliveTimer > lifetime) Return();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            var hp = other.GetComponent<PlayerHealth>();
            hp?.TakeDamage(damage);

            Return();
        }

        void Return()
        {
            if (Bill.Pool != null) Bill.Pool.Return(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
