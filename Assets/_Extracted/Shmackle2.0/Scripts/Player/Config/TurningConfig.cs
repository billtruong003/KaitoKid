using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "TurningConfig", menuName = "Configuration/TurningConfig")]
    public class TurningConfig : ScriptableObject, IConfig
    {
        public bool IsSmoothTurningEnabled = false;
        public bool IsSnapTurningEnabled = true;
        public int TurningPresets = 2; // NOTE (Russ): This is temporary and can be removed later.
        public int SlowTurningSpeed = 6;
        public int MediumTurningSpeed = 10;
        public int FastTurningSpeed = 15;
        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}