using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "PlayerLocomotionConfig", menuName = "Configuration/PlayerLocomotionConfig")]
    public class PlayerLocomotionConfig : ScriptableObject, IConfig
    {
        [Header("Speed")]
        public float VelocityLimit = 0.4f;
        public float MaxJumpSpeed = 6.5f;
        public float JumpMultiplier = 1.5f;
        public int VelocityHistorySize = 8;

        [Header("Config")]
        public float Mass = 0.4f;
        public float HeightOffset = -0.1f;
        public bool IsDoubleGrabEnabled = true;
        public bool BodyOffsetEnabled = false;
        public float MaxArmLength = 1.5f;
        public float UnStickDistance = 1f;
        public float MinimumRaycastDistance = 0.035f;
        public float DefaultSlideFactor = 0.01f;
        public float DefaultPrecision = 0.9995f;

        public Vector3 RightHandOffset = Vector3.zero;
        public Vector3 LeftHandOffset = Vector3.zero;
        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}