namespace Teabag.BattlePass.Models
{
    public class BattlePassReward
    {
        public BattlePassReward(string pass, string reward)
        {
            RequiredPass = pass;
            Reward = reward;
        }

        public string RequiredPass { get; set; }
        public string Reward { get; set; }
        public bool IsCurrency => int.TryParse(Reward, out int result);
    }
}
