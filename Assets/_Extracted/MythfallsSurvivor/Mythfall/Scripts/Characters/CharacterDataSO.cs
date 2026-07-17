using UnityEngine;
using BillGameCore;
using Mythfall.Localization;
using Mythfall.Skills;

namespace Mythfall.Characters
{
    /// <summary>
    /// ScriptableObject describing a playable character. Holds localization keys
    /// (NOT raw text — CLAUDE.md Rule 8), base stats, role, and skill slot references.
    /// Resolve display strings via GetDisplayName/GetTitle/GetLore at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "Mythfall/Character Data", order = 0)]
    public class CharacterDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string characterId;             // "kai", "lyra"

        [Header("Localization Keys (NOT raw text)")]
        public string nameKey;                 // "character.kai.name"
        public string titleKey;                // "character.kai.title"
        public string loreKey;                 // "character.kai.lore"

        [Header("Visual")]
        public Sprite portrait;
        public Sprite icon;

        [Header("Star Tier (display only in slice)")]
        [Range(1, 6)] public int starTier = 4;

        [Header("Type")]
        public CombatRole role;

        [Header("Stats")]
        public CharacterBaseStats baseStats = new();

        [Header("Skills (assigned in Sprint 3)")]
        public SkillDataSO autoAttackSkill;
        public SkillDataSO activeSkill;
        public SkillDataSO passiveSkill;

        [Header("Prefab")]
        public GameObject characterPrefab;

        // ----- Localization resolvers -----

        public string GetDisplayName()
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            return loc != null ? loc.Get(nameKey) : characterId;
        }

        public string GetTitle()
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            return loc != null ? loc.Get(titleKey) : "";
        }

        public string GetLore()
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            return loc != null ? loc.Get(loreKey) : "";
        }
    }
}
