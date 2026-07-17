using UnityEngine;
using BillGameCore;
using Mythfall.Localization;

namespace Mythfall.Enemy
{
    /// <summary>
    /// ScriptableObject describing an enemy archetype. Localization keys per CLAUDE.md Rule 8.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Mythfall/Enemy Data", order = 1)]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId;
        public string nameKey;
        public string descKey;

        [Header("Stats")]
        public float maxHP = 30f;
        public float attackPower = 5f;
        public float moveSpeed = 4f;
        public float attackRange = 0.8f;
        public float attackCooldown = 1.2f;

        [Header("Reward")]
        public float xpReward = 1f;

        [Header("Prefab")]
        public GameObject prefab;
        public string poolKey = "Enemy_Swarmer";

        public string GetDisplayName()
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            return loc != null ? loc.Get(nameKey) : enemyId;
        }
    }
}
