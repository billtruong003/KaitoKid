using Teabag.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Teabag.UI.Quest
{
    /// <summary>
    /// UI element representing a single quest row on the quest board.
    /// Displays name, progress bar (via image scale), reward, and completion state.
    /// Quests are automatically claimed upon completion.
    /// </summary>
    public class QuestEntryUI : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _rewardText;

        [Header("Progress Bar")]
        [Tooltip("The fill image for the progress bar. Scale X will be modified (0 to 1).")]
        [SerializeField] private Image _progressFill;

        [Header("States")]
        [Tooltip("Active when the quest reward has been claimed.")]
        [SerializeField] private GameObject _claimedIndicator;

        private IQuestService _questService;
        private string _questId;

        /// <summary>The quest id this entry is currently displaying.</summary>
        public string QuestId => _questId;

        /// <summary>
        /// Initializes the entry with quest data and a service reference.
        /// </summary>
        public void Setup(QuestSnapshot snapshot, IQuestService questService)
        {
            _questService = questService;
            _questId = snapshot.Id;

            UpdateProgress(snapshot);
        }

        /// <summary>
        /// Updates the visual state of this entry to match the given snapshot.
        /// </summary>
        public void UpdateProgress(QuestSnapshot snapshot)
        {
            if (_nameText != null)
                _nameText.text = snapshot.Name;

            if (_progressText != null)
                _progressText.text = $"{snapshot.CurrentAmount}/{snapshot.RequiredAmount}";

            if (_rewardText != null)
                _rewardText.text = snapshot.RewardAmount.ToString();

            if (_progressFill != null)
            {
                float progress = (float)snapshot.CurrentAmount / snapshot.RequiredAmount;
                Vector3 scale = _progressFill.transform.localScale;
                scale.x = Mathf.Clamp01(progress);
                _progressFill.transform.localScale = scale;
            }

            if (_claimedIndicator != null)
            {
                _claimedIndicator.SetActive(snapshot.IsClaimed);
            }
        }
    }
}

