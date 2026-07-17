using System;
using Stratton.Configuration;
using UnityEngine;

namespace Player.Config
{
    [CreateAssetMenu(fileName = "MouthConfig", menuName = "Configuration/MouthConfig")]
    public class MouthConfig : ScriptableObject, IConfig
    {
        public float MouthSpeechSensitivity = 5f; // How much the mouth opens when the character speaks.
        public float MouthOpenCloseSpeed = 14f; // How fast the mouth opens and closes when reacting to speech.
    
        public Action ConfigUpdated { get; set; }

        private void OnValidate()
        {
            ConfigUpdated?.Invoke();
        }
    }
}