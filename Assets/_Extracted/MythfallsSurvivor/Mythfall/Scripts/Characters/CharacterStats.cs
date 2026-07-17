using System;
using UnityEngine;

namespace Mythfall.Characters
{
    /// <summary>
    /// Stat axis identifiers — used as keys into RuntimeCharacterStats.
    /// Order is irrelevant; do not reorder for serialization safety though.
    /// </summary>
    public enum StatType
    {
        MaxHP,
        AttackPower,
        Defense,
        MoveSpeed,
        AttackRange,
        AttackInterval,
        CritRate,        // percent (0-100)
        CritDamage,      // percent (100 = no bonus, 200 = +100%)
        CooldownReduction, // percent (0-100)
        Lifesteal,       // percent (0-100)
        AoeRadius,       // meters
    }

    public enum CombatRole
    {
        Melee,
        Ranged,
    }

    /// <summary>
    /// Base stat block authored on CharacterDataSO. Cloned into RuntimeCharacterStats
    /// at run start so per-run upgrades don't bleed across runs.
    /// </summary>
    [Serializable]
    public class CharacterBaseStats
    {
        public float maxHP = 100f;
        public float attackPower = 10f;
        public float defense = 5f;
        public float moveSpeed = 5f;
        public float attackRange = 1.8f;
        public float attackInterval = 0.8f;
        public float critRate = 5f;
        public float critDamage = 150f;
        public float cooldownReduction = 0f;
        public float lifesteal = 0f;
        public float aoeRadius = 0f;

        public float Get(StatType type) => type switch
        {
            StatType.MaxHP => maxHP,
            StatType.AttackPower => attackPower,
            StatType.Defense => defense,
            StatType.MoveSpeed => moveSpeed,
            StatType.AttackRange => attackRange,
            StatType.AttackInterval => attackInterval,
            StatType.CritRate => critRate,
            StatType.CritDamage => critDamage,
            StatType.CooldownReduction => cooldownReduction,
            StatType.Lifesteal => lifesteal,
            StatType.AoeRadius => aoeRadius,
            _ => 0f,
        };
    }
}
