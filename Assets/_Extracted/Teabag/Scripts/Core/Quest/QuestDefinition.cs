using System;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Serializable data class describing a single quest's static configuration.
    /// Instances live inside a <see cref="QuestCatalog"/> ScriptableObject.
    /// </summary>
    [Serializable]
    public class QuestDefinition
    {
        [Tooltip("Unique identifier used to track progress (e.g. \"daily_login\").")]
        public string Id;

        [Tooltip("Display name shown on the quest board.")]
        public string Name;

        [Tooltip("Short description of what the player needs to do.")]
        public string Description;

        public QuestType Type = QuestType.Daily;

        [Tooltip("How many times the objective must be completed (e.g. 1 game).")]
        [Min(1)]
        public int RequiredAmount = 1;

        [Tooltip("Currency / XP reward granted on claim.")]
        [Min(0)]
        public int RewardAmount;
    }
}
