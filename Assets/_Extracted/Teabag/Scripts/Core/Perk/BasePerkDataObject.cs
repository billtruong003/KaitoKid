using UnityEngine;

namespace Teabag.Core
{
    public abstract class BasePerkDataObject : ScriptableObject
    {
        [SerializeField] protected string id;
        [SerializeField] protected string nameDisplay;
        [SerializeField] protected int cost;
        [SerializeField] protected string descript;
        [SerializeField] protected Sprite icon;

        public string ID => id;
        public string NameDisplay => nameDisplay;
        public int Cost => Mathf.Abs(cost);
        public string Descript => descript;
        public Sprite Icon => icon;

    }

    public enum PerkStatus
    {
        Locked,
        Not_Enough_Cash,
        Unlockable,
        Owned
    }
}
