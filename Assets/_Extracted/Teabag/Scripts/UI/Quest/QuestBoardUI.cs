using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.UI.Quest
{
    /// <summary>
    /// Manages the quest board display. Fetches daily quests from <see cref="IQuestService"/>
    /// and manages a pool of <see cref="QuestEntryUI"/> instances.
    /// Throttles countdown updates to improve performance.
    /// </summary>
    public class QuestBoardUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Prefab for a single quest row. Must have a QuestEntryUI component.")]
        [SerializeField] private QuestEntryUI _questEntryPrefab;

        [Tooltip("Parent transform where quest entries are instantiated.")]
        [SerializeField] private Transform _questListParent;

        [Tooltip("Text showing time remaining until daily reset.")]
        [SerializeField] private TMP_Text _countdownText;

        private IQuestService _questService;
        private readonly List<QuestEntryUI> _activeEntries = new List<QuestEntryUI>();
        private readonly Stack<QuestEntryUI> _cachePool = new Stack<QuestEntryUI>();
        
        private bool _initialized;
        private float _countdownTimer;
        private const float CountdownUpdateInterval = 0.5f;

        private async void OnEnable()
        {
            _questService = ServiceLocator.Get<IQuestService>();
            if (_questService == null)
            {
                GameLogger.Warning("[QuestBoardUI] IQuestService not available yet.");
                return;
            }

            _questService.OnQuestProgressUpdated += HandleQuestProgressUpdated;
            await RefreshBoard();
            
            // Initial countdown update
            UpdateCountdown();
            _initialized = true;
        }

        private void OnDisable()
        {
            if (_questService != null)
                _questService.OnQuestProgressUpdated -= HandleQuestProgressUpdated;

            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized || _questService == null) return;

            _countdownTimer += Time.deltaTime;
            if (_countdownTimer >= CountdownUpdateInterval)
            {
                _countdownTimer = 0f;
                UpdateCountdown();
            }
        }

        /// <summary>
        /// Fetches the latest quest data and updates the board using cached entries.
        /// </summary>
        public async System.Threading.Tasks.Task RefreshBoard()
        {
            if (_questService == null) return;

            var snapshots = await _questService.GetDailyQuestsAsync();

            // Return current active entries to pool
            foreach (var entry in _activeEntries)
            {
                entry.gameObject.SetActive(false);
                _cachePool.Push(entry);
            }
            _activeEntries.Clear();

            // Activate / Create entries from pool
            foreach (var snapshot in snapshots)
            {
                QuestEntryUI entryUI;
                if (_cachePool.Count > 0)
                {
                    entryUI = _cachePool.Pop();
                    entryUI.gameObject.SetActive(true);
                }
                else
                {
                    if (_questEntryPrefab == null || _questListParent == null) continue;
                    entryUI = Instantiate(_questEntryPrefab, _questListParent);
                }

                entryUI.Setup(snapshot, _questService);
                _activeEntries.Add(entryUI);
            }
        }

        private void UpdateCountdown()
        {
            if (_countdownText == null || _questService == null) return;

            var remaining = _questService.GetTimeUntilReset();
            _countdownText.text = $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s";
        }


        private void HandleQuestProgressUpdated(QuestSnapshot snapshot)
        {
            // Update the specific active entry if it exists
            foreach (var entry in _activeEntries)
            {
                if (entry.QuestId == snapshot.Id)
                {
                    entry.UpdateProgress(snapshot);
                    return;
                }
            }
        }
    }
}

