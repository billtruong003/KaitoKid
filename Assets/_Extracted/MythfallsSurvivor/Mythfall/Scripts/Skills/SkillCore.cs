using UnityEngine;
using BillGameCore;
using Mythfall.Localization;

namespace Mythfall.Skills
{
    /// <summary>
    /// Strategy Pattern entry point for all skills.
    /// CLAUDE.md Rule 6: each concrete skill = SO subclass + ISkillExecution implementation.
    /// Sprint 1 only needs the base scaffolding so CharacterDataSO compiles; Sprint 3 fills
    /// in concrete subclasses (BerserkerRushSO, OverchargeShotSO, etc.).
    /// </summary>
    public abstract class SkillDataSO : ScriptableObject
    {
        [Header("Identity (use localization keys, NOT raw text)")]
        public string skillId;
        public string nameKey;
        public string descKey;

        [Header("Visual")]
        public Sprite icon;

        [Header("Cooldown")]
        [Min(0f)] public float cooldown = 0f;

        public abstract ISkillExecution CreateExecution(SkillContext ctx);

        public string GetDisplayName()
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            return loc != null ? loc.Get(nameKey) : skillId;
        }

        public string GetDescription(params object[] args)
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            if (loc == null) return "";
            return (args != null && args.Length > 0)
                ? loc.GetFormatted(descKey, args)
                : loc.Get(descKey);
        }
    }

    /// <summary>
    /// Runtime execution lifecycle for a skill instance.
    /// Owner code calls CanExecute → Execute → Tick(dt) until IsFinished.
    /// </summary>
    public interface ISkillExecution
    {
        bool CanExecute();
        void Execute();
        void Tick(float dt);
        bool IsFinished { get; }
    }

    /// <summary>
    /// Context passed to ISkillExecution. Carries owner refs + transform pointers
    /// so executions don't need to GetComponent every frame.
    /// </summary>
    public struct SkillContext
    {
        public GameObject owner;
        public Transform ownerTransform;
        public Transform muzzlePoint;
        public Transform target;
    }
}
