using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Current quest service implementation.
    /// Provides three hardcoded daily quests with local progress tracking that resets every 24 hours.
    /// Replace with a PlayFabQuestService implementation when the backend is ready.
    /// </summary>
    public class QuestService : IQuestService
    {
        public event Action<QuestSnapshot> OnQuestCompleted;
        public event Action<QuestSnapshot> OnQuestProgressUpdated;



        private readonly List<QuestDefinition> _definitions;
        private readonly Dictionary<string, QuestProgress> _progressMap = new Dictionary<string, QuestProgress>();
        private readonly Queue<QuestSnapshot> _completionQueue = new Queue<QuestSnapshot>();
        private DateTime _lastResetDate;

        /// <summary>
        /// Creates the service and loads definitions from Resources.
        /// </summary>
        public QuestService()
        {
            var catalog = Resources.Load<QuestCatalog>("QuestCatalog");
            if (catalog != null && catalog.Quests != null && catalog.Quests.Count > 0)
            {
                _definitions = new List<QuestDefinition>(catalog.Quests);
                GameLogger.Info($"[QuestService] Loaded {_definitions.Count} quests from Resources/Quests/QuestCatalog.asset");
            }
            else
            {
                GameLogger.Error("[QuestService] Failed to load QuestCatalog from 'Resources/Quests/QuestCatalog'! Quests will be empty.");
                _definitions = new List<QuestDefinition>();
            }
        }

        public void Initialize()
        {
            _lastResetDate = DateTime.UtcNow.Date;
            InitializeProgress();
            
            GameLogger.Info("[QuestService] Initialized.");
        }

        public void Dispose()
        {
            GameLogger.Info("[QuestService] Disposed.");
        }


        public UniTask<IReadOnlyList<QuestSnapshot>> GetDailyQuestsAsync()
        {
            CheckReset();

            var snapshots = new List<QuestSnapshot>(_definitions.Count);
            foreach (var def in _definitions)
            {
                if (def.Type != QuestType.Daily) continue;
                snapshots.Add(CreateSnapshot(def));
            }

            return UniTask.FromResult<IReadOnlyList<QuestSnapshot>>(snapshots);
        }

        public UniTask ReportProgressAsync(string questId, int amount = 1)
        {
            CheckReset();

            if (!_progressMap.TryGetValue(questId, out var progress))
            {
                GameLogger.Warning($"[QuestService] Unknown quest id: {questId}");
                return UniTask.CompletedTask;
            }

            var definition = GetDefinition(questId);
            if (definition == null) return UniTask.CompletedTask;

            if (progress.IsCompleted || progress.IsClaimed)
            {
                GameLogger.Info($"[QuestService] Progress ignored for '{questId}'. Reason: {(progress.IsClaimed ? "Already Claimed" : "Already Completed")}");
                return UniTask.CompletedTask;
            }

            GameLogger.Info($"[QuestService] Reporting progress for '{definition.Name}' ({questId}): +{amount}");

            bool wasCompleted = progress.CurrentAmount >= definition.RequiredAmount;

            progress.CurrentAmount = Mathf.Min(progress.CurrentAmount + amount, definition.RequiredAmount);
            
            GameLogger.Info($"[QuestService] Updated progress for '{definition.Name}': {progress.CurrentAmount}/{definition.RequiredAmount}");

            bool isNowCompleted = progress.CurrentAmount >= definition.RequiredAmount;
            if (isNowCompleted && !wasCompleted)
            {
                progress.IsCompleted = true;
                GameLogger.Info($"[QuestService] Quest Completed: '{definition.Name}'!");
                // Auto-claim reward on completion as requested
                _ = ClaimRewardAsync(questId);
            }

            var snapshot = CreateSnapshot(definition);
            OnQuestProgressUpdated?.Invoke(snapshot);

            if (!wasCompleted && isNowCompleted)
            {
                _completionQueue.Enqueue(snapshot);
                OnQuestCompleted?.Invoke(snapshot);
            }

            return UniTask.CompletedTask;
        }



        public UniTask<bool> ClaimRewardAsync(string questId)
        {
            CheckReset();

            if (!_progressMap.TryGetValue(questId, out var progress))
                return UniTask.FromResult(false);

            var definition = GetDefinition(questId);
            if (definition == null || !progress.IsCompleted || progress.IsClaimed)
                return UniTask.FromResult(false);

            progress.IsClaimed = true;

            GameLogger.Info($"[QuestService] Claimed reward for '{definition.Name}': {definition.RewardAmount}");

            return UniTask.FromResult(true);
        }

        public TimeSpan GetTimeUntilReset()
        {
            DateTime nextReset = DateTime.UtcNow.Date.AddDays(1);
            TimeSpan remaining = nextReset - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public QuestSnapshot? DequeueCompletionNotification()
        {
            return _completionQueue.Count > 0 ? _completionQueue.Dequeue() : (QuestSnapshot?)null;
        }

        public void DebugEnqueueCompletion(string questName)
        {
            // Create a dummy snapshot for testing notifications
            var snapshot = new QuestSnapshot(questName);
            _completionQueue.Enqueue(snapshot);
        }

        // ------------------------------------------------------------------
        // Private helpers
        // ------------------------------------------------------------------

        private void CheckReset()
        {
            if (DateTime.UtcNow.Date <= _lastResetDate) return;

            _lastResetDate = DateTime.UtcNow.Date;
            InitializeProgress();
            GameLogger.Info("[QuestService] Daily quests have been reset.");
        }

        private void InitializeProgress()
        {
            _progressMap.Clear();
            foreach (var def in _definitions)
            {
                _progressMap[def.Id] = new QuestProgress(def.Id);
            }
        }

        private QuestDefinition GetDefinition(string questId)
        {
            foreach (var def in _definitions)
            {
                if (def.Id == questId) return def;
            }
            return null;
        }

        private QuestSnapshot CreateSnapshot(QuestDefinition definition)
        {
            var progress = _progressMap[definition.Id];
            return new QuestSnapshot(definition, progress);
        }

    }
}
