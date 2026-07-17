using System.Collections.Generic;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// ScriptableObject asset that holds every quest definition in a single place.
    /// Create via Assets ▸ Create ▸ Teabag ▸ Quest Catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestCatalog", menuName = "Teabag/Quest Catalog")]
    public class QuestCatalog : ScriptableObject
    {
        [SerializeField]
        private List<QuestDefinition> _quests = new List<QuestDefinition>();

        /// <summary>
        /// All quest definitions contained in this catalog.
        /// </summary>
        public IReadOnlyList<QuestDefinition> Quests => _quests;

        /// <summary>
        /// Finds a quest definition by its unique id.
        /// </summary>
        public QuestDefinition GetById(string questId)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Id == questId)
                    return _quests[i];
            }
            return null;
        }

        private void Reset()
        {
            if (_quests == null || _quests.Count == 0)
            {
                _quests = new List<QuestDefinition>
                {
                    new QuestDefinition
                    {
                        Id = "daily_login",
                        Name = "Daily Login",
                        Description = "Log in to the game",
                        Type = QuestType.Daily,
                        RequiredAmount = 1,
                        RewardAmount = 50
                    },
                    new QuestDefinition
                    {
                        Id = "play_one_game",
                        Name = "Play One Game",
                        Description = "Play one game in any mode",
                        Type = QuestType.Daily,
                        RequiredAmount = 1,
                        RewardAmount = 100
                    },
                    new QuestDefinition
                    {
                        Id = "win_public_game",
                        Name = "Win One Public Game",
                        Description = "Win a public multiplayer game",
                        Type = QuestType.Daily,
                        RequiredAmount = 1,
                        RewardAmount = 200
                    }
                };
            }
        }

    }
}
