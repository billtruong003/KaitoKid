using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    /// <summary>
    /// A ScriptableObject configuration container that defines runtime parameters for eye behavior.
    /// Centralizes settings for detection ranges, look sensitivity, damping, and audio reactivity thresholds.
    /// Supports hot-reloading values at runtime via the ConfigUpdated event.
    /// </summary>
    [CreateAssetMenu(fileName = "EyeTrackingConfig", menuName = "Configuration/EyeTrackingConfig")]
    public class EyeTrackingConfig : ScriptableObject, IConfig
    {
        [Header("Detection Settings")]
        public float TrackingDistance = 20f;
        [Range(0, 180)] public float ViewConeAngle = 100f;
        public Vector3 LookRotationOffset = Vector3.zero;

        [Header("Motion Settings")]
        public float Damping = 15f;
        public float LookSensitivity = 0.8f;
        public Vector2 LimitUv = new Vector2(0.4f, 0.4f);

        [Header("Audio Reactivity")]
        public float AudioSensitivity = 1.2f;
        public float MinIrisScale = 1.0f;
        public float MaxIrisScale = 1.6f;
        public float IrisSmoothSpeed = 15f;
        public float AudioSampleInterval = 0.05f;

        [Header("Calibration")]
        public bool InvertX = false;
        public bool InvertY = false;

        [Header("Optimization")]
        public float CullingDistance = 30f;

        [Header("Velocity-Based Eye Movement (Confused State)")]
        public float VelocityEyeSensitivity = 0.5f;
        public float VelocityEyeSmoothing = 0.1f;

        [Header("Idle Eye Movement (Confuse When Standing Still)")]
        public bool IdleEyeMovementEnabled = true;
        public float IdleEyeMovementRange = 0.3f;
        public float IdleEyeMovementInterval = 0.3f;

        [Header("Reset")]
        public bool ResetToDefault = false;

        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}