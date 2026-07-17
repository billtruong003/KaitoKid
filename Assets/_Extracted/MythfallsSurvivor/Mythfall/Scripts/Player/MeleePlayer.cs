using UnityEngine;

namespace Mythfall.Player
{
    /// <summary>
    /// Concrete melee character (Kai default). Owns the MeleeCombat component reference.
    /// Animation events on the player's Animator route through DynamicAnimationEventHub
    /// (root) to MeleeCombat.OnHitboxEnable / OnHitboxDisable via UnityEvent wiring.
    /// </summary>
    public class MeleePlayer : PlayerBase
    {
        [SerializeField] MeleeCombat meleeCombat;

        public override PlayerCombatBase Combat => meleeCombat;

        protected override void Awake()
        {
            base.Awake();
            if (meleeCombat == null) meleeCombat = GetComponent<MeleeCombat>();
        }

        protected override void Start()
        {
            base.Start();
            if (meleeCombat != null) meleeCombat.Initialize(this);
        }
    }
}
