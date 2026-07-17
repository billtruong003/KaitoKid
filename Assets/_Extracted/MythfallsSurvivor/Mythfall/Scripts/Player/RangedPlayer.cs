using UnityEngine;

namespace Mythfall.Player
{
    /// <summary>
    /// Concrete ranged character (Lyra default). RangedCombat spawns Projectile prefab
    /// from MuzzlePoint on animation event "OnArrowRelease".
    /// </summary>
    public class RangedPlayer : PlayerBase
    {
        [SerializeField] RangedCombat rangedCombat;

        public override PlayerCombatBase Combat => rangedCombat;

        protected override void Awake()
        {
            base.Awake();
            if (rangedCombat == null) rangedCombat = GetComponent<RangedCombat>();
        }

        protected override void Start()
        {
            base.Start();
            if (rangedCombat != null) rangedCombat.Initialize(this);
        }
    }
}
