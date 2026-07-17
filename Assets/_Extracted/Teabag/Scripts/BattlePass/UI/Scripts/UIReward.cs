using Teabag.BattlePass.Models;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Teabag.BattlePass.UI
{
    public class UIReward : MonoBehaviour
    {
        BattlePassReward _reward;

        public GameObject lockIcon;
        public GameObject rim;
        public UIRewardDisplay rewardDisplay;

        [Header("Text")]
        public GameObject textBar;
        public TMP_Text rewardText;

        public void SetReward(BattlePassReward reward, int tier)
        {
            _reward = reward;
            rewardText.text = _reward.Reward;

            string text = rewardDisplay.ShowReward(_reward);
            rewardText.text = text;

            lockIcon.SetActive(!BattlePassManager.HasPass(_reward.RequiredPass));
            rim.SetActive(BattlePassManager.CurrentTier >= tier && BattlePassManager.HasPass(_reward.RequiredPass));

            if (!string.IsNullOrEmpty(text))
            {
                textBar.SetActive(true);
                rewardDisplay.gameObject.SetActive(true);
            }
            else
            {
                textBar.SetActive(false);
                rewardDisplay.gameObject.SetActive(false);
            }
        }
    }
}
