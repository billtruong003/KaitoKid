using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;
using Teabag.UI;

namespace Teabag.BattlePass.UI
{
    public class UIBattlePass : MonoBehaviour
    {
        private int m_LastXp = 0; // cached last XP value
        
        public List<GorillaButton> buttons = new List<GorillaButton>();
        public GameObject battlePass;
        public GameObject loading;

        [Header("Page")]
        public int page;
        public int pageSize = 5;
        public Action onPageChanged;

        public void NextPage()
        {
            int maxPage = Mathf.CeilToInt(BattlePassManager.BattlePass.Tiers.Length / (float)pageSize) - 1;
            GameLogger.Info("Max page: " + maxPage);
            GameLogger.Info("Before page: " + page);
            page = Mathf.Clamp(page + 1, 0, maxPage);
            GameLogger.Info("After page: " + page);
            onPageChanged?.Invoke();
        }

        public void PreviousPage()
        {
            page--;
            if (page < 0)
                page = 0;
            onPageChanged?.Invoke();
        }

        private void Awake() => BattlePassManager.OnPassUpdated += Refresh;
        private void OnDestroy() => BattlePassManager.OnPassUpdated -= Refresh;
        
        public void Refresh()
        {
            if (m_LastXp != BattlePassManager.Xp)
            {
                page = Mathf.FloorToInt(BattlePassManager.CurrentTier / (float)pageSize);
                m_LastXp = BattlePassManager.Xp;
            }
            onPageChanged?.Invoke();
            UpdateButtons(false);
        }

        public void UpdateButtons(bool disableAll)
        {
            if (disableAll)
            {
                foreach (GorillaButton button in buttons)
                    button.interactable = false;
                return;
            }

            buttons[0].interactable = page > 0; // Previous
            buttons[1].interactable = page < Mathf.CeilToInt(BattlePassManager.BattlePass.Tiers.Length / (float)pageSize) - 1; // Next
        }

        private void Update()
        {
            bool l = (GameServices.IsComputerLoading?.Invoke() ?? true) || BattlePassManager.BattlePass == null;
            loading.SetActive(l);
            battlePass.SetActive(!l);
            UpdateButtons(l);
        }
    }
}
