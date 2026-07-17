#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline.UI
{
    /// <summary>
    /// Main gameplay HUD showing HP/MP bars, level/EXP, skill cooldowns,
    /// and a target info panel. Listens to player_stats OnUpdate for the
    /// local player and uses BillTween for smooth bar transitions.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector - Health / Mana
        // -------------------------------------------------------

        [Header("HP Bar")]
        [SerializeField] private Slider hpBar;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image hpFillImage;

        [Header("MP Bar")]
        [SerializeField] private Slider mpBar;
        [SerializeField] private TMP_Text mpText;
        [SerializeField] private Image mpFillImage;

        [Header("Level / EXP")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider expBar;
        [SerializeField] private TMP_Text expText;

        // -------------------------------------------------------
        // Inspector - Skills
        // -------------------------------------------------------

        [Header("Skill Slots")]
        [SerializeField] private Image[] skillIcons = new Image[NetworkConfig.SKILL_SLOT_COUNT];
        [SerializeField] private Image[] skillCooldownOverlays = new Image[NetworkConfig.SKILL_SLOT_COUNT];
        [SerializeField] private TMP_Text[] skillCooldownTexts = new TMP_Text[NetworkConfig.SKILL_SLOT_COUNT];
        [SerializeField] private TMP_Text[] skillKeyLabels = new TMP_Text[NetworkConfig.SKILL_SLOT_COUNT];

        // -------------------------------------------------------
        // Inspector - Target
        // -------------------------------------------------------

        [Header("Target Info")]
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private TMP_Text targetNameText;
        [SerializeField] private Slider targetHpBar;
        [SerializeField] private TMP_Text targetHpText;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private int _displayedHp, _displayedMaxHp;
        private int _displayedMp, _displayedMaxMp;
        private int _displayedLevel;
        private int _displayedExp, _displayedMaxExp;

        private Tween _hpTween;
        private Tween _mpTween;
        private Tween _expTween;

        // Skill cooldown state
        private readonly float[] _skillCooldownTimers = new float[NetworkConfig.SKILL_SLOT_COUNT];
        private readonly float[] _skillMaxCooldowns = new float[NetworkConfig.SKILL_SLOT_COUNT];

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Start()
        {
            // Initialize skill key labels
            string[] keyLabels = { "1", "2", "3", "4" };
            for (int i = 0; i < NetworkConfig.SKILL_SLOT_COUNT; i++)
            {
                if (i < skillKeyLabels.Length && skillKeyLabels[i] != null)
                {
                    skillKeyLabels[i].text = keyLabels[i];
                }
            }

            // Hide target panel initially
            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
            }

            // Subscribe to events
            if (Bill.IsReady)
            {
                Bill.Events.Subscribe<PlayerStatsUpdatedEvent>(OnPlayerStatsUpdated);
                Bill.Events.Subscribe<TargetChangedEvent>(OnTargetChanged);
                Bill.Events.Subscribe<SkillUsedEvent>(OnSkillUsed);
            }

            // Initial display from cached stats
            RefreshFromCachedStats();
        }

        private void OnDestroy()
        {
            KillTweens();

            if (Bill.IsReady)
            {
                Bill.Events.Unsubscribe<PlayerStatsUpdatedEvent>(OnPlayerStatsUpdated);
                Bill.Events.Unsubscribe<TargetChangedEvent>(OnTargetChanged);
                Bill.Events.Unsubscribe<SkillUsedEvent>(OnSkillUsed);
            }
        }

        private void Update()
        {
            UpdateSkillCooldowns();
        }

        // -------------------------------------------------------
        // Stats Update
        // -------------------------------------------------------

        private void OnPlayerStatsUpdated(PlayerStatsUpdatedEvent evt)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.LocalPlayerStats == null) return;

            // Only update if it is our local player
            if (evt.Identity != gm.LocalIdentity.ToString()) return;

            UpdateStats(gm.LocalPlayerStats);
        }

        private void RefreshFromCachedStats()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.LocalPlayerStats == null) return;

            UpdateStats(gm.LocalPlayerStats);
        }

        private void UpdateStats(PlayerStats stats)
        {
            // HP
            int newHp = stats.CurrentHp;
            int newMaxHp = stats.MaxHp;

            if (newHp != _displayedHp || newMaxHp != _displayedMaxHp)
            {
                AnimateBar(hpBar, hpText, ref _hpTween, _displayedHp, newHp, newMaxHp, "HP");
                _displayedHp = newHp;
                _displayedMaxHp = newMaxHp;
            }

            // MP
            int newMp = stats.CurrentMp;
            int newMaxMp = stats.MaxMp;

            if (newMp != _displayedMp || newMaxMp != _displayedMaxMp)
            {
                AnimateBar(mpBar, mpText, ref _mpTween, _displayedMp, newMp, newMaxMp, "MP");
                _displayedMp = newMp;
                _displayedMaxMp = newMaxMp;
            }

            // Level
            if (stats.Level != _displayedLevel)
            {
                _displayedLevel = stats.Level;
                if (levelText != null)
                {
                    levelText.text = $"Lv. {_displayedLevel}";

                    // Level-up pulse animation
                    BillTween.Scale(levelText.transform, 1.3f, 0.15f)
                        ?.SetEase(EaseType.OutBack)
                        .SetTarget(levelText)
                        .OnComplete(() =>
                        {
                            BillTween.Scale(levelText.transform, 1f, 0.1f)
                                ?.SetTarget(levelText);
                        });
                }
            }

            // EXP (only total Exp is available; estimate max exp as level * 100 for display)
            int newExp = stats.Exp;
            int newMaxExp = (int)(50.0 * Mathf.Pow(stats.Level, 1.5f));
            if (newMaxExp <= 0) newMaxExp = 100;

            if (newExp != _displayedExp || newMaxExp != _displayedMaxExp)
            {
                AnimateBar(expBar, expText, ref _expTween, _displayedExp, newExp, newMaxExp, "EXP");
                _displayedExp = newExp;
                _displayedMaxExp = newMaxExp;
            }
        }

        // -------------------------------------------------------
        // Bar Animation
        // -------------------------------------------------------

        private void AnimateBar(Slider bar, TMP_Text text, ref Tween tween, int fromValue, int toValue, int maxValue, string label)
        {
            if (bar == null) return;

            float fromRatio = maxValue > 0 ? (float)fromValue / maxValue : 0f;
            float toRatio = maxValue > 0 ? (float)toValue / maxValue : 0f;

            // Kill previous tween
            if (tween != null) BillTween.Kill(tween);

            tween = BillTween.Float(fromRatio, toRatio, 0.4f, value =>
            {
                bar.value = value;

                if (text != null)
                {
                    int interpolatedValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, value > 0 ? value / toRatio : 0f));
                    text.text = $"{interpolatedValue}/{maxValue}";
                }
            })?.SetEase(EaseType.OutQuad)
              .SetTarget(bar)
              .OnComplete(() =>
              {
                  // Ensure final value is exact
                  bar.value = toRatio;
                  if (text != null) text.text = $"{toValue}/{maxValue}";
              });

            // Flash red on HP loss
            if (label == "HP" && toValue < fromValue && hpFillImage != null)
            {
                Color originalColor = hpFillImage.color;
                hpFillImage.color = Color.white;
                BillTween.Float(1f, 0f, 0.3f, t =>
                {
                    hpFillImage.color = Color.Lerp(originalColor, Color.white, t);
                })?.SetTarget(hpFillImage);
            }
        }

        // -------------------------------------------------------
        // Skill Cooldowns
        // -------------------------------------------------------

        private void OnSkillUsed(SkillUsedEvent evt)
        {
            int i = evt.SlotIndex;
            if (i < 0 || i >= NetworkConfig.SKILL_SLOT_COUNT) return;

            _skillCooldownTimers[i] = evt.CooldownDuration;
            _skillMaxCooldowns[i] = evt.CooldownDuration;
        }

        private void UpdateSkillCooldowns()
        {
            for (int i = 0; i < NetworkConfig.SKILL_SLOT_COUNT; i++)
            {
                if (_skillCooldownTimers[i] > 0f)
                {
                    _skillCooldownTimers[i] -= Time.deltaTime;
                    if (_skillCooldownTimers[i] < 0f) _skillCooldownTimers[i] = 0f;

                    // Update overlay fill
                    if (i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null)
                    {
                        float ratio = _skillMaxCooldowns[i] > 0 ? _skillCooldownTimers[i] / _skillMaxCooldowns[i] : 0f;
                        skillCooldownOverlays[i].fillAmount = ratio;
                        skillCooldownOverlays[i].gameObject.SetActive(ratio > 0f);
                    }

                    // Update cooldown text
                    if (i < skillCooldownTexts.Length && skillCooldownTexts[i] != null)
                    {
                        if (_skillCooldownTimers[i] > 0f)
                        {
                            skillCooldownTexts[i].text = _skillCooldownTimers[i].ToString("F1");
                            skillCooldownTexts[i].gameObject.SetActive(true);
                        }
                        else
                        {
                            skillCooldownTexts[i].gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    // Ready state
                    if (i < skillCooldownOverlays.Length && skillCooldownOverlays[i] != null)
                    {
                        skillCooldownOverlays[i].gameObject.SetActive(false);
                    }
                    if (i < skillCooldownTexts.Length && skillCooldownTexts[i] != null)
                    {
                        skillCooldownTexts[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Update skill icons from the SkillController data.
        /// Called once after the player spawns.
        /// </summary>
        public void RefreshSkillIcons(SkillController skillController)
        {
            if (skillController == null) return;

            for (int i = 0; i < NetworkConfig.SKILL_SLOT_COUNT; i++)
            {
                var slot = skillController.GetSkillSlot(i);
                if (slot != null && i < skillIcons.Length && skillIcons[i] != null)
                {
                    skillIcons[i].sprite = slot.icon;
                    skillIcons[i].color = slot.icon != null ? Color.white : new Color(1, 1, 1, 0.2f);
                }
            }
        }

        // -------------------------------------------------------
        // Target Panel
        // -------------------------------------------------------

        private void OnTargetChanged(TargetChangedEvent evt)
        {
            if (evt.Target == null)
            {
                // Hide target panel
                if (targetPanel != null)
                {
                    targetPanel.SetActive(false);
                }
                return;
            }

            // Show target panel
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
            }

            if (targetNameText != null)
            {
                targetNameText.text = evt.TargetName;
            }

            if (targetHpBar != null)
            {
                float ratio = evt.MaxHp > 0 ? (float)evt.CurrentHp / evt.MaxHp : 0f;
                targetHpBar.value = ratio;
            }

            if (targetHpText != null)
            {
                targetHpText.text = $"{evt.CurrentHp}/{evt.MaxHp}";
            }
        }

        // -------------------------------------------------------
        // Cleanup
        // -------------------------------------------------------

        private void KillTweens()
        {
            if (_hpTween != null) BillTween.Kill(_hpTween);
            if (_mpTween != null) BillTween.Kill(_mpTween);
            if (_expTween != null) BillTween.Kill(_expTween);
        }
    }
}

#endif // STDB_BINDINGS
