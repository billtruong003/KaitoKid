namespace Teabag.BattlePass.Models
{
    public class BattlePassTier
    {
        public int RequiredXP { get; private set; }
        public BattlePassReward[] Rewards { get; private set; }

        public BattlePassTier(int requiredXp, params BattlePassReward[] rewards)
        {
            RequiredXP = requiredXp;
            Rewards = rewards;
        }
    }
}
