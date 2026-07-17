using UnityEngine;

namespace Mythfall.Player
{
    /// <summary>
    /// Sits on the same GameObject as the SphereCollider trigger child of a melee player.
    /// Forwards OnTriggerEnter to the parent's MeleeCombat (which lives on the root player GO).
    /// Using a relay keeps MeleeCombat colocated with PlayerBase while letting the collider
    /// be a child for visualization + transform offsetting.
    /// </summary>
    public class HitboxRelay : MonoBehaviour
    {
        [SerializeField] MeleeCombat target;

        public void Bind(MeleeCombat combat) => target = combat;

        void OnTriggerEnter(Collider other)
        {
            if (target != null) target.OnHitboxTriggerEnter(other);
        }
    }
}
