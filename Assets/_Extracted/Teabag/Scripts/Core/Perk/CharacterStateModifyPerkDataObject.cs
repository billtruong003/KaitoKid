using UnityEngine;

namespace Teabag.Core
{
    [CreateAssetMenu(fileName = "New Character State", menuName = "Data Base/Perk/State Modify")]
    public class CharacterStateModifyPerkDataObject : BasePerkDataObject
    {
        [SerializeField] private CharacterState state;
        [SerializeField, Range(0, byte.MaxValue)] private byte percentBonus;

        public CharacterState State => state;
        public byte PercentBonus => percentBonus;
    }

    public enum CharacterState
    {
        Health,
        Shield,
        Damage,
        Crit_Damage,
    }
}
