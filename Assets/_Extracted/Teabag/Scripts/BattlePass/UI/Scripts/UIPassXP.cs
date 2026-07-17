using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Teabag.BattlePass.UI
{
    public class UIPassXP : MonoBehaviour
    {
        public Slider slider;
        public TMP_Text text;

        private void Awake()
        {
            
        }

        void Update()
        {
            if (BattlePassManager.BattlePass == null)
            {
                slider.value = 0;
                text.text = "";
                return;
            }

            int targetTier = BattlePassManager.CurrentTier + 1;
            if (targetTier >= BattlePassManager.BattlePass.Tiers.Length)
            {
                slider.value = 1;
                text.text = "MAX";
                return;
            }

            int lastTierXP = BattlePassManager.BattlePass.Tiers[BattlePassManager.CurrentTier].RequiredXP;
            int requiredXP = BattlePassManager.BattlePass.Tiers[targetTier].RequiredXP - lastTierXP;
            int currentXP = BattlePassManager.Xp - lastTierXP;

            slider.value = currentXP / (float)requiredXP;
            text.text = $"{currentXP}/{requiredXP}";
        }
    }
}
