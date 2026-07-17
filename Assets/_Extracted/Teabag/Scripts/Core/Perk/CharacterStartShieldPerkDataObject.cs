using UnityEngine;

namespace Teabag.Core
{
    [CreateAssetMenu(fileName = "New Character Shield", menuName = "Data Base/Perk/Unlock Shield")]
    public class CharacterStartShieldPerkDataObject : BasePerkDataObject
    {
        [SerializeField] private byte _shieldStartPercent;
        public byte ShieldStartPercent => _shieldStartPercent;
    }
}
