using Teabag.Authentication;
using Teabag.BattlePass.Models;
using Teabag.Core;
using UnityEngine;
using Teabag.Player;

namespace Teabag.BattlePass.UI
{
    public class UIRewardDisplay : MonoBehaviour
    {
        public GameObject currency;
        public CosmeticPreview cosmetic;

        public string ShowReward(BattlePassReward reward)
        {
            if (reward == null)
            {
                return "";
            }

            if (string.IsNullOrEmpty(reward.Reward))
            {
                return "";
            }

            currency.SetActive(reward.IsCurrency);
            cosmetic.gameObject.SetActive(!reward.IsCurrency);

            if (reward.IsCurrency)
            {
                return reward.Reward + " B$";
            }
            else
            {
                Cosmetic? c = AuthenticationUtils.GetCosmetic(reward.Reward);
                if (c.HasValue)
                {
                    cosmetic.Set(c.Value);
                    return c.Value.cosmetic;
                }

                cosmetic.gameObject.SetActive(false);
            }

            return "";
        }
    }
}
