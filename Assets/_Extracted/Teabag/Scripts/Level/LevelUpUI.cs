using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Teabag.Authentication;
using Teabag.Core;
using Teabag.Services;
using UnityEngine.ProBuilder.MeshOperations;
using Teabag.UI;

namespace Teabag.Progression
{
    public class LevelUpUI : MonoBehaviour
    {
        public static LevelUpUI instance;
        public Slider lightBlue;
        public Slider blue;
        public TextMeshProUGUI levelUpsText;
        public TextMeshProUGUI levelsText;
        public TextMeshProUGUI xpText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI nextRewardText;
        public FollowValue followValue;
        public AudioSource levelUpAudio;
        public AudioSource clickAudio;
        public AudioClip completedAudio;
        private static readonly WaitForSeconds _waitOneSecond = new WaitForSeconds(1f);
        private static readonly WaitForSeconds _waitTwoSeconds = new WaitForSeconds(2f);


        private void Awake()
        {
            xpText.text = "";
            levelUpsText.text = "";
            levelsText.text = "LEVEL\n1";
            nextRewardText.text = "LOADING\nLOADING";
            nameText.text = "LOADING";
            instance = this;
        }

        private void OnEnable()
        {
            Initialise(LevelManager.CurrentLevel, LevelManager.CurrentXp);
        }

        private void Update()
        {
            nameText.text = PlayerData.displayName;
        }

        public void Initialise(int level, int xp)
        {
            levelsText.text = "LEVEL\n" + level;
            if (LevelManager.LevelProgressionData == null)
                return;

            if (level < LevelManager.LevelProgressionData.LevelProgression.Count - 1)
            {
                lightBlue.maxValue = LevelManager.LevelProgressionData.LevelProgression[level + 1].RequiredXp;
                blue.maxValue = LevelManager.LevelProgressionData.LevelProgression[level + 1].RequiredXp;
                lightBlue.value = xp;
                blue.value = xp;
            }
            else
            {
                lightBlue.maxValue = 100;
                blue.maxValue = 100;
                lightBlue.value = 100;
                blue.value = 100;
            }
            SetNextRewardText(level);
        }

        public void SetNextRewardText(int currentLevel)
        {
            if (currentLevel < LevelManager.LevelProgressionData.LevelProgression.Count - 1)
            {
                nextRewardText.text = $"LEVEL {currentLevel + 1}\n{GetLevelData(currentLevel + 1).Reward} BANANA-BUCKS";
                followValue.clampT = 0.63f;
                if (GetLevelData(currentLevel + 1).Reward >= 1000)
                    followValue.clampT = 0.61f;
            }
            else
            {
                nextRewardText.text = $"LEVEL {LevelManager.LevelProgressionData.LevelProgression.Count - 1}\nMAX LEVEL";
            }
        }

        public void ShowXp(int levels, int xp, int reward)
        {
            StopAllCoroutines();
            Initialise(LevelManager.CurrentLevel, LevelManager.CurrentXp);
            StartCoroutine(ShowXpCoroutine(levels, xp, reward));
        }

        IEnumerator ShowXpCoroutine(int levels, int xp, int reward)
        {
            Debug.Log($"Showing XP. Level: {levels} XP: {xp}");
            int l = LevelManager.CurrentLevel;
            int originalXp = LevelManager.CurrentXp;
            int originalLevel = l;

            int finalXpTarget = 0;
            for (int i = originalLevel; i < levels; i++)
                finalXpTarget += XpForLevel(i);
            finalXpTarget += xp;

            yield return _waitTwoSeconds;
            while (l < levels)
            {
                if (l >= LevelManager.LevelProgressionData.LevelProgression.Count - 1)
                    break;
                yield return RunLevelCoroutine(l, originalLevel, originalXp, LevelManager.LevelProgressionData.LevelProgression[l + 1].RequiredXp, finalXpTarget);

                levelUpAudio.PlayOneShot(levelUpAudio.clip);
                levelUpsText.text += "LEVEL UP!\n";
                l++;
                levelsText.text = "LEVEL\n" + l;
                if (l < LevelManager.LevelProgressionData.LevelProgression.Count - 1)
                    blue.value = 0;
                SetNextRewardText(l);
            }


            yield return RunLevelCoroutine(l, originalLevel, originalXp, xp, finalXpTarget);
            yield return _waitOneSecond;

            int xpGotten = 0;
            for (int i = originalLevel; i < l; i++)
                xpGotten += XpForLevel(i);
            xpGotten += xp;
            xpGotten -= originalXp;

            while (blue.value < lightBlue.value)
            {
                blue.maxValue = lightBlue.maxValue;
                blue.value += Time.deltaTime * 20;
                xpText.text = $"+{Mathf.RoundToInt(Mathf.Lerp(xpGotten, 0, blue.value / lightBlue.value))}XP";
                yield return null;
            }

            if (l < LevelManager.LevelProgressionData.LevelProgression.Count - 1)
            {
                clickAudio.pitch = 1;
                clickAudio.PlayOneShot(completedAudio);
            }

            xpText.text = "";
            levelUpsText.text = "";
            levelsText.text = "LEVEL\n" + levels;
            if (reward > 0)
            {
                yield return _waitOneSecond;
                FindObjectOfType<MapBoard>().OpenScreen("ATM");
                AuthenticationUtils.currency += reward;
            }
        }

        IEnumerator RunLevelCoroutine(int l, int originalLevel, int originalXp, int xp, int finalTarget)
        {
            if (l >= LevelManager.LevelProgressionData.LevelProgression.Count - 1)
                yield break;

            float startAt = 0;
            if (l == originalLevel)
                startAt = originalXp;

            int levelsPassed = l - originalLevel;
            float t = startAt;

            int xpGotten = 0;
            for (int i = originalLevel; i < l; i++)
                xpGotten += XpForLevel(i + 1);

            int rounded = 0;
            int lastP = 0;

            while (t < xp + 0.001f)
            {
                // add to t
                float speed = (levelsPassed + lightBlue.value) / (levelsPassed + xp);
                speed *= -1;
                speed += 1.3f;
                t += Time.deltaTime * 30 * speed;

                lightBlue.maxValue = XpForLevel(l + 1);
                lightBlue.value = t;

                rounded = Mathf.RoundToInt(xpGotten + t);
                if (rounded != lastP)
                {
                    clickAudio.pitch = 1 + ((xpGotten + t) / finalTarget);
                    clickAudio.PlayOneShot(clickAudio.clip);
                    Debug.Log($"Gotten {xpGotten} T {t} Rounded {rounded} Original {originalXp} XP {rounded - originalXp}");
                    lastP = rounded;
                }

                // Display
                xpText.text = $"+{rounded - originalXp}XP";
                yield return null;
            }

            yield return null;
        }

        public int XpForLevel(int level) => GetLevelData(level).RequiredXp;
        public LevelProgressionEntry GetLevelData(int level) => LevelManager.LevelProgressionData.LevelProgression[level];
    }
}
