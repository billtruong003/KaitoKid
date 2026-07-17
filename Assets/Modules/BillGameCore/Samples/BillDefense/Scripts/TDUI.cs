using UnityEngine;
using UnityEngine.UI;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    /// <summary>Main HUD: gold, lives, wave info, build timer.</summary>
    public class TDHUD : MonoBehaviour
    {
        [Header("Top Bar")]
        public Text goldText;
        public Text livesText;
        public Text waveText;
        public Text killsText;

        [Header("Build Phase")]
        public GameObject buildPhaseGroup;
        public Text buildTimerText;
        public Image buildTimerFill;
        public Button sendWaveButton;
        public Text hintText;

        [Header("Wave Active")]
        public GameObject waveActiveGroup;
        public Text waveBannerText;

        [Header("Wave Complete")]
        public GameObject waveCompleteGroup;
        public Text waveCompleteBonusText;

        [Header("Victory")]
        public GameObject victoryGroup;
        public Text victoryStarsText;
        public Text victoryKillsText;

        private TDGameManager _manager;

        public void Init(TDGameManager manager)
        {
            _manager = manager;
            if (sendWaveButton) sendWaveButton.onClick.AddListener(() => _manager.SendWave());
            HideAll();
        }

        void HideAll()
        {
            if (buildPhaseGroup) buildPhaseGroup.SetActive(false);
            if (waveActiveGroup) waveActiveGroup.SetActive(false);
            if (waveCompleteGroup) waveCompleteGroup.SetActive(false);
            if (victoryGroup) victoryGroup.SetActive(false);
        }

        public void UpdateGold(int gold)
        {
            if (goldText) goldText.text = $"Gold: {gold}";
        }

        public void UpdateLives(int lives, int max)
        {
            if (livesText) livesText.text = $"Lives: {lives}/{max}";
            if (lives <= 3 && livesText)
                livesText.color = Color.red;
        }

        public void UpdateWave(int current, int total)
        {
            if (waveText) waveText.text = $"Wave: {current}/{total}";
        }

        public void UpdateKills(int kills)
        {
            if (killsText) killsText.text = $"Kills: {kills}";
        }

        public void ShowBuildPhase(int nextWave, int totalWaves)
        {
            HideAll();
            if (buildPhaseGroup) buildPhaseGroup.SetActive(true);
            UpdateWave(nextWave, totalWaves);
        }

        public void UpdateBuildTimer(float remaining, float total)
        {
            if (buildTimerText) buildTimerText.text = $"{Mathf.CeilToInt(remaining)}s";
            if (buildTimerFill) buildTimerFill.fillAmount = remaining / total;
        }

        public void ShowHint(string hint)
        {
            if (hintText)
            {
                hintText.gameObject.SetActive(true);
                hintText.text = hint;
                Bill.Timer.Delay(5f, () => { if (hintText) hintText.gameObject.SetActive(false); });
            }
        }

        public void ShowWaveActive(int wave, int totalWaves)
        {
            HideAll();
            if (waveActiveGroup) waveActiveGroup.SetActive(true);
            UpdateWave(wave, totalWaves);

            if (waveBannerText)
            {
                waveBannerText.text = $"WAVE {wave}";
                waveBannerText.transform.localScale = Vector3.zero;
                BillTween.Scale(waveBannerText.transform, 1f, 0.3f).SetEase(EaseType.OutElastic);
                Bill.Timer.Delay(2f, () =>
                {
                    if (waveBannerText) BillTween.Scale(waveBannerText.transform, 0f, 0.2f);
                });
            }
        }

        public void ShowWaveComplete(int wave, int bonus)
        {
            if (waveCompleteGroup) waveCompleteGroup.SetActive(true);
            if (waveCompleteBonusText)
            {
                waveCompleteBonusText.text = $"Wave {wave} Clear! +{bonus}g";
                BillTween.Scale(waveCompleteBonusText.transform, 0f, 0f);
                BillTween.Scale(waveCompleteBonusText.transform, 1f, 0.3f).SetEase(EaseType.OutBack);
            }
            Bill.Timer.Delay(2.5f, () => { if (waveCompleteGroup) waveCompleteGroup.SetActive(false); });
        }

        public void ShowVictory(int stars, int kills)
        {
            HideAll();
            if (victoryGroup) victoryGroup.SetActive(true);
            string starStr = new string('★', stars) + new string('☆', 3 - stars);
            if (victoryStarsText) victoryStarsText.text = starStr;
            if (victoryKillsText) victoryKillsText.text = $"Enemies defeated: {kills}";
        }
    }

    /// <summary>Tower selection panel and tower info popup.</summary>
    public class TDTowerPanel : MonoBehaviour
    {
        [Header("Tower Buttons")]
        public Button[] towerButtons; // One per tower type (6 buttons)
        public Text[] towerCostLabels;

        [Header("Selected Tower Info")]
        public GameObject infoPanel;
        public Text infoName, infoLevel, infoStats, infoBonusText;
        public Button upgradeButton, sellButton, targetButton;
        public Text upgradeCostText, sellPriceText, targetModeText;

        private TDGameManager _manager;
        private TDGrid _grid;
        private TowerType? _selectedBuildType;
        private TDTower _selectedTower;
        private Vector2Int _lastHoverTile;

        public void Init(TDGameManager manager, TDGrid grid)
        {
            _manager = manager;
            _grid = grid;

            // Wire tower buttons
            for (int i = 0; i < towerButtons.Length && i < TDDatabase.Towers.Length; i++)
            {
                int idx = i; // Capture
                var def = TDDatabase.Towers[i];
                if (towerButtons[i]) towerButtons[i].onClick.AddListener(() => SelectBuildType((TowerType)idx));
                if (towerCostLabels != null && i < towerCostLabels.Length && towerCostLabels[i] != null)
                    towerCostLabels[i].text = $"{def.BaseCost}g";
            }

            if (upgradeButton) upgradeButton.onClick.AddListener(OnUpgrade);
            if (sellButton) sellButton.onClick.AddListener(OnSell);
            if (targetButton) targetButton.onClick.AddListener(OnCycleTarget);

            HideInfo();
        }

        void Update()
        {
            // Handle mouse/touch for tower placement and selection
            if (_selectedBuildType.HasValue)
            {
                HandleBuildMode();
            }
            else
            {
                HandleSelection();
            }

            // Right click / Escape to cancel
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBuild();
                HideInfo();
            }
        }

        void SelectBuildType(TowerType type)
        {
            _selectedBuildType = type;
            _selectedTower = null;
            HideInfo();
        }

        void HandleBuildMode()
        {
            if (!_selectedBuildType.HasValue) return;

            if (_grid.ScreenToTile(Input.mousePosition, out Vector2Int tile))
            {
                // Highlight
                if (tile != _lastHoverTile)
                {
                    _grid.ResetTileHighlight(_lastHoverTile);
                    _lastHoverTile = tile;
                    _grid.HighlightTile(tile, _grid.IsBuildable(tile));
                }

                // Click to place
                if (Input.GetMouseButtonDown(0))
                {
                    if (_manager.TryPlaceTower(_selectedBuildType.Value, tile))
                    {
                        _grid.ResetTileHighlight(tile);
                        // Don't clear selection — allow placing multiple
                    }
                }
            }
        }

        void HandleSelection()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // Raycast for existing tower
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var tower = hit.collider.GetComponentInParent<TDTower>();
                if (tower != null)
                {
                    SelectTower(tower);
                    return;
                }
            }

            // Clicked empty — check for tile with tower
            if (_grid.ScreenToTile(Input.mousePosition, out Vector2Int tile))
            {
                var tower = _grid.GetTowerAt(tile);
                if (tower != null)
                {
                    SelectTower(tower);
                    return;
                }
            }

            HideInfo();
        }

        void SelectTower(TDTower tower)
        {
            _selectedTower = tower;
            _selectedBuildType = null;
            tower.ShowRange(true);
            ShowInfo(tower);
        }

        void ShowInfo(TDTower tower)
        {
            if (infoPanel) infoPanel.SetActive(true);
            if (infoName) infoName.text = tower.DisplayName;
            if (infoLevel) infoLevel.text = tower.LevelString;

            var def = TDDatabase.GetTower(tower.towerType);
            var ld = def.levels[tower.level];
            string stats = $"DMG: {ld.damage}  SPD: {ld.attackSpeed}s  RNG: {ld.range}";
            if (infoStats) infoStats.text = stats;

            if (infoBonusText) infoBonusText.text = tower.BonusDesc;

            if (upgradeButton)
            {
                upgradeButton.gameObject.SetActive(tower.CanUpgrade);
                if (upgradeCostText) upgradeCostText.text = tower.CanUpgrade ? $"Upgrade: {tower.UpgradeCost}g" : "";
            }
            if (sellPriceText) sellPriceText.text = $"Sell: {tower.SellPrice}g";
            if (targetModeText) targetModeText.text = tower.targetMode.ToString();
        }

        void HideInfo()
        {
            if (infoPanel) infoPanel.SetActive(false);
            if (_selectedTower) _selectedTower.ShowRange(false);
            _selectedTower = null;
        }

        void CancelBuild()
        {
            if (_selectedBuildType.HasValue)
            {
                _grid.ResetTileHighlight(_lastHoverTile);
                _selectedBuildType = null;
            }
        }

        void OnUpgrade()
        {
            if (_selectedTower != null && _manager.TryUpgradeTower(_selectedTower))
                ShowInfo(_selectedTower);
        }

        void OnSell()
        {
            if (_selectedTower != null)
            {
                _manager.SellTower(_selectedTower);
                HideInfo();
            }
        }

        void OnCycleTarget()
        {
            if (_selectedTower != null)
            {
                _selectedTower.CycleTargetMode();
                if (targetModeText) targetModeText.text = _selectedTower.targetMode.ToString();
            }
        }
    }

    /// <summary>Game Over panel.</summary>
    public class TDGameOverPanel : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public Text waveReachedText, killCountText;
        public Button retryButton, menuButton;

        private TDGameManager _manager;

        public void Init(TDGameManager manager)
        {
            _manager = manager;
            if (retryButton) retryButton.onClick.AddListener(() => _manager.OnRetry());
            if (menuButton) menuButton.onClick.AddListener(() => _manager.OnMenu());
            Hide();
        }

        public void Show(int waveReached, int kills)
        {
            gameObject.SetActive(true);
            if (waveReachedText) waveReachedText.text = $"Reached Wave {waveReached}";
            if (killCountText) killCountText.text = $"Enemies Defeated: {kills}";
            if (canvasGroup) { canvasGroup.alpha = 0; BillTween.Fade(canvasGroup, 1f, 0.4f); }
        }

        public void Hide() { gameObject.SetActive(false); }
    }
}
