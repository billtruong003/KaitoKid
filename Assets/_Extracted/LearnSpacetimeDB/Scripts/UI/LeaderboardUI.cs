#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.UI
{
    /// <summary>
    /// Leaderboard UI panel toggled with the L key. Shows player rankings
    /// sorted by Kills, Deaths, or Level. Highlights the local player's row.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Panel")]
        [SerializeField] private GameObject leaderboardPanel;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Tabs")]
        [SerializeField] private Button killsTabButton;
        [SerializeField] private Button deathsTabButton;
        [SerializeField] private Button levelTabButton;

        [Header("Content")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform entryContainer;
        [SerializeField] private GameObject entryPrefab;

        [Header("Tab Highlight")]
        [SerializeField] private Color activeTabColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color inactiveTabColor = Color.white;

        [Header("Row Colors")]
        [SerializeField] private Color normalRowColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        [SerializeField] private Color localPlayerRowColor = new Color(0.2f, 0.3f, 0.5f, 0.9f);
        [SerializeField] private Color alternateRowColor = new Color(0.12f, 0.12f, 0.17f, 0.8f);

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private enum SortColumn { Kills, Deaths, Level }
        private SortColumn _currentSort = SortColumn.Kills;
        private bool _isOpen;
        private readonly List<GameObject> _entryObjects = new List<GameObject>();

        /// <summary>
        /// Intermediate data for leaderboard entries (combines PlayerStats + Player name).
        /// </summary>
        private struct LeaderboardEntry
        {
            public string Username;
            public int TotalKills;
            public int TotalDeaths;
            public int Level;
            public int Exp;
            public Identity Owner;
        }

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Start()
        {
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);

            if (killsTabButton != null) killsTabButton.onClick.AddListener(() => SetSort(SortColumn.Kills));
            if (deathsTabButton != null) deathsTabButton.onClick.AddListener(() => SetSort(SortColumn.Deaths));
            if (levelTabButton != null) levelTabButton.onClick.AddListener(() => SetSort(SortColumn.Level));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                ToggleLeaderboard();
            }
        }

        // -------------------------------------------------------
        // Toggle
        // -------------------------------------------------------

        public void ToggleLeaderboard()
        {
            _isOpen = !_isOpen;

            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(_isOpen);
            }

            if (_isOpen)
            {
                RefreshLeaderboard();
                AnimateOpen();
            }
        }

        private void AnimateOpen()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                BillTween.Fade(panelCanvasGroup, 1f, 0.25f)
                    ?.SetEase(EaseType.OutQuad);
            }
        }

        // -------------------------------------------------------
        // Sort Selection
        // -------------------------------------------------------

        private void SetSort(SortColumn column)
        {
            _currentSort = column;
            UpdateTabHighlights();
            RefreshLeaderboard();
        }

        private void UpdateTabHighlights()
        {
            SetTabColor(killsTabButton, _currentSort == SortColumn.Kills);
            SetTabColor(deathsTabButton, _currentSort == SortColumn.Deaths);
            SetTabColor(levelTabButton, _currentSort == SortColumn.Level);
        }

        private void SetTabColor(Button tab, bool isActive)
        {
            if (tab == null) return;
            var text = tab.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = isActive ? activeTabColor : inactiveTabColor;
            }
        }

        // -------------------------------------------------------
        // Refresh
        // -------------------------------------------------------

        private void RefreshLeaderboard()
        {
            // Clear existing entries
            foreach (var obj in _entryObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _entryObjects.Clear();

            var gm = GameManager.Instance;
            if (gm == null || gm.Connection == null) return;

            // Collect all player stats and resolve usernames from the Player table
            var allEntries = new List<LeaderboardEntry>();
            foreach (var stats in gm.Connection.Db.PlayerStats.Iter())
            {
                // Look up username from the Player table
                string username = "Unknown";
                var player = gm.Connection.Db.Player.Owner.Find(stats.Owner);
                if (player != null) username = player.Username;

                allEntries.Add(new LeaderboardEntry
                {
                    Username = username,
                    TotalKills = stats.TotalKills,
                    TotalDeaths = stats.TotalDeaths,
                    Level = stats.Level,
                    Exp = stats.Exp,
                    Owner = stats.Owner
                });
            }

            // Sort by selected column (descending)
            switch (_currentSort)
            {
                case SortColumn.Kills:
                    allEntries = allEntries.OrderByDescending(s => s.TotalKills).ThenBy(s => s.Username).ToList();
                    break;
                case SortColumn.Deaths:
                    allEntries = allEntries.OrderByDescending(s => s.TotalDeaths).ThenBy(s => s.Username).ToList();
                    break;
                case SortColumn.Level:
                    allEntries = allEntries.OrderByDescending(s => s.Level).ThenByDescending(s => s.Exp).ThenBy(s => s.Username).ToList();
                    break;
            }

            // Get local identity for highlighting
            Identity localIdentity = gm.LocalIdentity;

            // Create entries
            for (int i = 0; i < allEntries.Count; i++)
            {
                var entry = allEntries[i];
                bool isLocal = entry.Owner == localIdentity;

                CreateEntry(i + 1, entry, isLocal, i % 2 == 1);
            }

            // Update tab highlights
            UpdateTabHighlights();
        }

        private void CreateEntry(int rank, LeaderboardEntry entry, bool isLocalPlayer, bool isAlternateRow)
        {
            if (entryContainer == null) return;

            GameObject entryObj;

            if (entryPrefab != null)
            {
                entryObj = Instantiate(entryPrefab, entryContainer);
            }
            else
            {
                // Create a simple entry if no prefab
                entryObj = CreateDefaultEntry(entryContainer);
            }

            entryObj.name = $"Entry_{rank}_{entry.Username}";

            // Find text components (expected layout: Rank, Name, Kills, Deaths, Level)
            var texts = entryObj.GetComponentsInChildren<TMP_Text>();

            if (texts.Length >= 5)
            {
                texts[0].text = $"#{rank}";
                texts[1].text = entry.Username;
                texts[2].text = entry.TotalKills.ToString();
                texts[3].text = entry.TotalDeaths.ToString();
                texts[4].text = entry.Level.ToString();
            }
            else if (texts.Length >= 1)
            {
                // Fallback: single text
                string sortValue = _currentSort switch
                {
                    SortColumn.Kills => $"Kills: {entry.TotalKills}",
                    SortColumn.Deaths => $"Deaths: {entry.TotalDeaths}",
                    SortColumn.Level => $"Level: {entry.Level}",
                    _ => ""
                };
                texts[0].text = $"#{rank}  {entry.Username}  -  {sortValue}";
            }

            // Set row background color
            var bg = entryObj.GetComponent<Image>();
            if (bg == null) bg = entryObj.GetComponentInChildren<Image>();

            if (bg != null)
            {
                if (isLocalPlayer)
                {
                    bg.color = localPlayerRowColor;
                }
                else
                {
                    bg.color = isAlternateRow ? alternateRowColor : normalRowColor;
                }
            }

            // Bold the local player
            if (isLocalPlayer && texts.Length > 1)
            {
                foreach (var t in texts)
                {
                    t.fontStyle = FontStyles.Bold;
                }
            }

            _entryObjects.Add(entryObj);
        }

        private GameObject CreateDefaultEntry(Transform parent)
        {
            // Fallback entry when no prefab is assigned
            var go = new GameObject("LeaderboardEntry", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 32f);

            var img = go.GetComponent<Image>();
            img.color = normalRowColor;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Create text columns: Rank, Name, Kills, Deaths, Level
            string[] headers = { "Rank", "Name", "Kills", "Deaths", "Level" };
            for (int i = 0; i < headers.Length; i++)
            {
                var textGo = new GameObject(headers[i], typeof(RectTransform));
                textGo.transform.SetParent(go.transform, false);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 14;
                tmp.alignment = i == 1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.text = "";

                var textRt = textGo.GetComponent<RectTransform>();
                textRt.sizeDelta = new Vector2(i == 1 ? 150 : 80, 28);

                var le = textGo.AddComponent<LayoutElement>();
                le.preferredWidth = i == 1 ? 150 : 80;
                le.preferredHeight = 28;
            }

            return go;
        }

        public bool IsOpen => _isOpen;
    }
}

#endif // STDB_BINDINGS
