using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "ThrowingConfig", menuName = "Configuration/ThrowingConfig")]
    public class ThrowingConfig : ScriptableObject, IConfig
    {
        public int ThrowPresets = 1;
        public float HorizontalForce = 0;
        public float VerticalForce = 0;
        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}