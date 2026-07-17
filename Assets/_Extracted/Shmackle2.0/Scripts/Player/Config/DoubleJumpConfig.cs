using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "DoubleJumpConfig", menuName = "Configuration/DoubleJumpConfig")]
    public class DoubleJumpConfig : ScriptableObject, IConfig
    {
        public bool MidAirRequirement = true;
        public bool CanGenerateLineSegment = false;
        public float HandAlignmentThreshold = 0.4f;
        public float DoubleJumpVelocityMultiplier = 6.15f;
        public float HandSpeedThreshold = 0.2f;
        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}