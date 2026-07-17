using Teabag.BattlePass.Models;
using Teabag.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teabag.BattlePass.UI
{
    public class UITier : MonoBehaviour
    {
        public UIBattlePass UIBattlePass;
        public UIReward reward;
        public Transform parent;

        public string passName;

        BattlePassTier[] tiers
        {
            get
            {
                if (BattlePassManager.BattlePass == null)
                    return new BattlePassTier[0];

                return BattlePassManager.BattlePass.Tiers;
            }
        }

        private void Awake()
        {
            UIBattlePass.onPageChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            UIBattlePass.onPageChanged -= Refresh;
        }

        public void Refresh()
        {
            foreach (UIReward d in parent.GetComponentsInChildren<UIReward>())
                Destroy(d.gameObject);

            if (tiers.Length < 1)
                return;

            int start = UIBattlePass.pageSize * UIBattlePass.page;
            if (start > 0 && start < UIBattlePass.pageSize)
                start = start - 1; // Adjust for zero-based index
            for (int i = start; i < tiers.Length && i < UIBattlePass.pageSize * (UIBattlePass.page + 1); i++)
            {
                GameLogger.Info("Reward for " + i);

                //GameLogger.Info(tiers[i]);

                BattlePassReward re = null;
                foreach (BattlePassReward r in tiers[i].Rewards)
                {
                    if (r.RequiredPass == passName)
                    {
                        re = r;
                        break;
                    }
                }

                if (re == null)
                {
                    GameLogger.Error("Failed to find a reward for " + tiers[i] + " (" + passName + ")");
                    continue;
                }

                UIReward spawn = Instantiate(reward, parent);
                spawn.SetReward(re, i);
            }
        }
    }
}