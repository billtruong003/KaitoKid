using System;

namespace Teabag.BattlePass.Models
{
    public class BattlePass
    {
        public string Name { get; set; }
        public string StatisticName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public BattlePassTier[] Tiers { get; set; }
    }
}
