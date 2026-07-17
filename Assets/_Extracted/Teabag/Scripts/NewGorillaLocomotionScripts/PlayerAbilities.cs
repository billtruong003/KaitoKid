using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GorillaLocomotion
{
    [Flags]
    public enum PlayerAbility
    {
        None = 0,
        Movement = 1 << 0,
        Turning = 1 << 1,
        Parachute = 1 << 2,
        Swimming = 1 << 3,
        All = ~0
    }

    public class PlayerAbilities : MonoBehaviour
    {
        [Header("Starting State")]
        [FormerlySerializedAs("currentAbilities")]
        [SerializeField] private PlayerAbility baseAbilities = PlayerAbility.All;

        private PlayerAbility _currentAbilities;
        private readonly Dictionary<PlayerAbility, HashSet<object>> _blockers = new Dictionary<PlayerAbility, HashSet<object>>();

        [Header("Cached Components")]
        [SerializeField] private Player movementPlayer;

        [SerializeField] private GorillaTurning turningScript; // Using base class to avoid assembly definition reference issues
        [SerializeField] private ParachuteManager parachuteScript;
        [SerializeField] private Swimming swimmingScript;

        private void Awake()
        {
            // Cache components dynamically
            if (movementPlayer == null){
                movementPlayer = GetComponentInChildren<Player>();
            }

            if (turningScript == null){
                turningScript = GetComponentInChildren<GorillaTurning>();
            }

            if (parachuteScript == null){
                parachuteScript = GetComponentInChildren<ParachuteManager>();
            }

            if (swimmingScript == null){
                swimmingScript = GetComponentInChildren<Swimming>();
            }

            // Initialize dictionary for abilities
            foreach (PlayerAbility ability in Enum.GetValues(typeof(PlayerAbility)))
            {
                if (ability == PlayerAbility.None || ability == PlayerAbility.All) continue;
                _blockers[ability] = new HashSet<object>();
            }

            // Initialize correct state
            ComputeAbilities();
        }

        public void SetAbilities(PlayerAbility abilities, bool state, object requester = null)
        {
            if (requester == null) requester = "Default";

            bool changed = false;

            foreach (PlayerAbility ability in Enum.GetValues(typeof(PlayerAbility)))
            {
                if (ability == PlayerAbility.None || ability == PlayerAbility.All) continue;

                if ((abilities & ability) == ability)
                {
                    if (state)
                    {
                        // True means ENABLE the ability -> remove block
                        if (_blockers[ability].Remove(requester))
                        {
                            changed = true;
                        }
                    }
                    else
                    {
                        // False means DISABLE the ability -> add block
                        if (_blockers[ability].Add(requester))
                        {
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                ComputeAbilities();
            }
        }

        private void ComputeAbilities()
        {
            _currentAbilities = baseAbilities;

            foreach (var kvp in _blockers)
            {
                if (kvp.Value.Count > 0)
                {
                    _currentAbilities &= ~kvp.Key;
                }
            }

            ApplyAbilities();
        }

        private void ApplyAbilities()
        {
            // Apply Movement
            if (movementPlayer != null)
            {
                bool hasMovement = (_currentAbilities & PlayerAbility.Movement) != 0;
                // movementPlayer.enabled = hasMovement; // Instead of disabling the script
                movementPlayer.disableMovement = !hasMovement;
                movementPlayer.lockIsKinematic = !hasMovement;
            }

            // Apply Turning
            if (turningScript != null)
            {
                bool hasTurning = (_currentAbilities & PlayerAbility.Turning) != 0;
                turningScript.enabled = hasTurning;
            }

            // Apply Parachute
            if (parachuteScript != null)
            {
                bool hasParachute = (_currentAbilities & PlayerAbility.Parachute) != 0;
                parachuteScript.enabled = hasParachute;
            }

            // Apply Swimming
            if (swimmingScript != null)
            {
                bool hasSwimming = (_currentAbilities & PlayerAbility.Swimming) != 0;
                swimmingScript.enabled = hasSwimming;
            }
        }
    }
}
