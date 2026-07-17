#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using System;
using UnityEngine;
using BillGameCore;

namespace SpumOnline
{
    /// <summary>
    /// Manages skill data, cooldown tracking, and visual feedback for the local player's
    /// 4 skill slots. Works with LocalPlayerController for input and the HUD for UI display.
    /// </summary>
    public class SkillController : MonoBehaviour
    {
        // -------------------------------------------------------
        // Skill Definition
        // -------------------------------------------------------

        [Serializable]
        public class SkillSlot
        {
            public string skillName = "Unnamed Skill";
            public float cooldownDuration = 5f;
            public int mpCost = 10;
            public Sprite icon;
            [Tooltip("Pool key for skill VFX prefab.")]
            public string vfxPoolKey;

            [HideInInspector] public float currentCooldown;
            [HideInInspector] public bool isReady = true;
        }

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Skills")]
        [SerializeField] private SkillSlot[] skills = new SkillSlot[NetworkConfig.SKILL_SLOT_COUNT];

        [Header("VFX")]
        [Tooltip("Transform where skill VFX will be spawned (player center).")]
        [SerializeField] private Transform vfxSpawnPoint;

        // -------------------------------------------------------
        // References
        // -------------------------------------------------------

        private LocalPlayerController _localPlayer;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            _localPlayer = GetComponent<LocalPlayerController>();

            // Ensure we have exactly the right number of skill slots
            if (skills == null || skills.Length != NetworkConfig.SKILL_SLOT_COUNT)
            {
                skills = new SkillSlot[NetworkConfig.SKILL_SLOT_COUNT];
                for (int i = 0; i < skills.Length; i++)
                {
                    skills[i] = new SkillSlot();
                }
            }
        }

        private void Start()
        {
            if (vfxSpawnPoint == null)
            {
                vfxSpawnPoint = transform;
            }

            // Register VFX pools for any configured skills
            RegisterSkillVFXPools();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        // -------------------------------------------------------
        // Pool Registration
        // -------------------------------------------------------

        private void RegisterSkillVFXPools()
        {
            if (!Bill.IsReady || Bill.Pool == null) return;

            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] == null) continue;
                if (string.IsNullOrEmpty(skills[i].vfxPoolKey)) continue;

                // VFX prefabs are expected in Resources/Pools/{key}
                // Bill.Pool auto-loads from Resources/Pools/ if not pre-registered
            }
        }

        // -------------------------------------------------------
        // Skill Use
        // -------------------------------------------------------

        private void TryUseSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skills.Length) return;
            var skill = skills[slotIndex];
            if (skill == null) return;

            // Check cooldown
            if (!skill.isReady)
            {
                Debug.Log($"[SkillController] {skill.skillName} is on cooldown ({skill.currentCooldown:F1}s).");
                return;
            }

            // Check MP
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected) return;

            if (gm.LocalPlayerStats != null && gm.LocalPlayerStats.CurrentMp < skill.mpCost)
            {
                Debug.Log($"[SkillController] Not enough MP for {skill.skillName}. Need {skill.mpCost}, have {gm.LocalPlayerStats.CurrentMp}.");
                return;
            }

            // Send to server with target position (aim at current target or forward)
            float targetX = transform.position.x + (_localPlayer != null && _localPlayer.FacingRight ? 1f : -1f);
            float targetY = transform.position.y;
            if (_localPlayer != null && _localPlayer.CurrentTarget != null)
            {
                targetX = _localPlayer.CurrentTarget.transform.position.x;
                targetY = _localPlayer.CurrentTarget.transform.position.y;
            }
            gm.Connection.Reducers.UseSkill(slotIndex, targetX, targetY);

            // Optimistic: start cooldown immediately
            StartCooldown(slotIndex);

            // Spawn VFX
            SpawnSkillVFX(slotIndex);

            // Play attack animation
            var visualSync = GetComponent<CharacterVisualSync>();
            if (visualSync != null)
            {
                visualSync.SetAnimState(AnimState.Attack);
            }

            // Notify HUD
            if (_localPlayer != null)
            {
                _localPlayer.SetSkillCooldown(slotIndex, skill.cooldownDuration);
            }

            Debug.Log($"[SkillController] Used skill: {skill.skillName} (slot {slotIndex}).");
        }

        private void StartCooldown(int slotIndex)
        {
            var skill = skills[slotIndex];
            skill.currentCooldown = skill.cooldownDuration;
            skill.isReady = false;
        }

        // -------------------------------------------------------
        // Cooldown Update
        // -------------------------------------------------------

        private void UpdateCooldowns()
        {
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] == null) continue;
                if (skills[i].isReady) continue;

                skills[i].currentCooldown -= Time.deltaTime;
                if (skills[i].currentCooldown <= 0f)
                {
                    skills[i].currentCooldown = 0f;
                    skills[i].isReady = true;
                }
            }
        }

        // -------------------------------------------------------
        // VFX
        // -------------------------------------------------------

        private void SpawnSkillVFX(int slotIndex)
        {
            if (!Bill.IsReady || Bill.Pool == null) return;

            var skill = skills[slotIndex];
            if (string.IsNullOrEmpty(skill.vfxPoolKey)) return;

            Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;

            GameObject vfxObj = Bill.Pool.Spawn(skill.vfxPoolKey, spawnPos, Quaternion.identity);
            if (vfxObj != null)
            {
                // Auto-return after 1 second
                Bill.Pool.Return(vfxObj, 1f);
            }
        }

        // -------------------------------------------------------
        // Public API
        // -------------------------------------------------------

        /// <summary>Get the skill slot data for display in the HUD.</summary>
        public SkillSlot GetSkillSlot(int index)
        {
            if (index < 0 || index >= skills.Length) return null;
            return skills[index];
        }

        /// <summary>Get the cooldown progress (0 = ready, 1 = just used).</summary>
        public float GetCooldownNormalized(int index)
        {
            if (index < 0 || index >= skills.Length) return 0f;
            var skill = skills[index];
            if (skill == null || skill.cooldownDuration <= 0f) return 0f;
            return Mathf.Clamp01(skill.currentCooldown / skill.cooldownDuration);
        }

        /// <summary>Get the remaining cooldown in seconds.</summary>
        public float GetCooldownRemaining(int index)
        {
            if (index < 0 || index >= skills.Length) return 0f;
            return skills[index]?.currentCooldown ?? 0f;
        }

        /// <summary>Check if a skill slot is ready to use.</summary>
        public bool IsSkillReady(int index)
        {
            if (index < 0 || index >= skills.Length) return false;
            return skills[index]?.isReady ?? false;
        }

        /// <summary>
        /// Configure a skill slot at runtime (e.g., from server data).
        /// </summary>
        public void ConfigureSkill(int index, string name, float cooldown, int mpCost, Sprite icon, string vfxKey = null)
        {
            if (index < 0 || index >= skills.Length) return;
            var skill = skills[index];
            skill.skillName = name;
            skill.cooldownDuration = cooldown;
            skill.mpCost = mpCost;
            skill.icon = icon;
            skill.vfxPoolKey = vfxKey;
        }
    }
}

#endif // STDB_BINDINGS
