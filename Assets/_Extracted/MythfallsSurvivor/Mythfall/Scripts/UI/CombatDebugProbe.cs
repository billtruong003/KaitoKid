using UnityEngine;
using BillGameCore;
using Mythfall.Characters;
using Mythfall.Enemy;
using Mythfall.Player;

namespace Mythfall.DebugTools
{
    /// <summary>
    /// TEMP — drop on Kai/Lyra in scene to print combat state every second.
    /// Verifies whether the combat pipeline is alive even when animations
    /// aren't visible. Remove after Day 2 verification.
    /// </summary>
    [RequireComponent(typeof(PlayerBase))]
    public class CombatDebugProbe : MonoBehaviour
    {
        [SerializeField] float logInterval = 1f;

        PlayerBase player;
        TargetSelector selector;
        PlayerHealth health;
        float timer;
        int enemiesKilled;

        void Awake()
        {
            player = GetComponent<PlayerBase>();
            selector = GetComponent<TargetSelector>();
            health = GetComponent<PlayerHealth>();
        }

        void OnEnable()
        {
            Bill.Events?.Subscribe<Mythfall.Events.EnemyKilledEvent>(OnEnemyKilled);
            Bill.Events?.Subscribe<Mythfall.Events.EnemyHitEvent>(OnEnemyHit);
        }

        void OnDisable()
        {
            if (!Bill.IsReady) return;
            Bill.Events.Unsubscribe<Mythfall.Events.EnemyKilledEvent>(OnEnemyKilled);
            Bill.Events.Unsubscribe<Mythfall.Events.EnemyHitEvent>(OnEnemyHit);
        }

        void OnEnemyKilled(Mythfall.Events.EnemyKilledEvent e)
        {
            enemiesKilled++;
            Debug.Log($"[CombatProbe] KILLED enemy at {e.position} — total kills: {enemiesKilled}");
        }

        void OnEnemyHit(Mythfall.Events.EnemyHitEvent e)
        {
            Debug.Log($"[CombatProbe] HIT {e.victim.name} for {e.damage:F1}{(e.isCrit ? " CRIT" : "")}");
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < logInterval) return;
            timer = 0f;

            if (player == null || player.Stats == null)
            {
                Debug.Log("[CombatProbe] player or stats null — PlayerBase.Start probably disabled the player. Check characterData on Inspector.");
                return;
            }

            string state =
                $"[CombatProbe] HP={health.CurrentHP:F0}/{health.MaxHP:F0} " +
                $"alive={health.IsAlive} " +
                $"target={(selector.CurrentTarget != null ? selector.CurrentTarget.name : "<none>")} " +
                $"targetDist={(selector.CurrentTarget != null ? Vector3.Distance(transform.position, selector.CurrentTarget.position).ToString("F2") : "n/a")} " +
                $"attackRange={player.Stats.GetFinal(StatType.AttackRange):F2}";
            Debug.Log(state);
        }
    }
}
