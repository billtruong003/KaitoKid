using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "FlashlightConfig", menuName = "Configuration/FlashlightConfig")]
    public class FlashlightConfig : ScriptableObject, IConfig
    {
        public bool IsFlashlightEnabled = false;
        public float FlashlightIntensity = 1f;
        public float FlashlightRange = 10f;
        public Action ConfigUpdated { get; set; }
        
        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}
