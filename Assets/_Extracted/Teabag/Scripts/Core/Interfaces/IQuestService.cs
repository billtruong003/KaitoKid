using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;

namespace Teabag.Core
{
    /// <summary>
    /// Abstraction for quest management. Implementations can be backed by PlayFab,
    /// local storage, or a placeholder for development.
    /// </summary>
    public interface IQuestService : IService
    {
        /// <summary>Fired when a quest transitions from incomplete to completed.</summary>
        event Action<QuestSnapshot> OnQuestCompleted;

        /// <summary>Fired whenever a quest's progress value changes.</summary>
        event Action<QuestSnapshot> OnQuestProgressUpdated;

        /// <summary>
        /// Returns the current daily quests with their progress.
        /// </summary>
        UniTask<IReadOnlyList<QuestSnapshot>> GetDailyQuestsAsync();

        /// <summary>
        /// Reports progress towards a quest objective.
        /// </summary>
        /// <param name="questId">The unique id of the quest to update.</param>
        /// <param name="amount">How much progress to add (default 1).</param>
        UniTask ReportProgressAsync(string questId, int amount = 1);

        /// <summary>
        /// Claims the reward for a completed quest.
        /// </summary>
        /// <returns>True if the reward was successfully claimed.</returns>
        UniTask<bool> ClaimRewardAsync(string questId);

        /// <summary>
        /// Returns the remaining time until the daily quests reset.
        /// </summary>
        TimeSpan GetTimeUntilReset();
        
        /// <summary>
        /// Attempts to dequeue a pending quest completion snapshot for notification.
        /// Returns null if no notifications are pending.
        /// </summary>
        QuestSnapshot? DequeueCompletionNotification();

        /// <summary>
        /// Debug method to manually enqueue a completion notification in the queue.
        /// </summary>
        void DebugEnqueueCompletion(string questName);
    }
}
