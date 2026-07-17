using UnityEngine;

namespace Teabag.Core
{
    [CreateAssetMenu(fileName = "New Ammo", menuName = "Data Base/Perk/Ammo")]
    public class AmmoPerkDataObject : BasePerkDataObject
    {
        [SerializeField] private string weaponID;
        [SerializeField, Range(0, byte.MaxValue)] private byte magazinePercent;

        public string WeaponID => weaponID;
        public byte MagazineBonus => magazinePercent;
    }
}
