using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "BootyJiggleConfig", menuName = "Configuration/BootyJiggleConfig")]
    public class BootyJiggleConfig : ScriptableObject, IConfig
    {
        public float JiggleStretch = 0.5f;
        public float JiggleStiffness = 1f;
        public float JiggleIgnoreRootMotion = 0.31f;
        public float JiggleGravity = 0.27f;
        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}