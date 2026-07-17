using UnityEngine;

namespace Stratton.Effects
{
    [System.Serializable]
    public class ObjectEmitterData
    {
        /// <summary>
        /// Only for editor list drawer - dont use!
        /// </summary>
        [HideInInspector]
        public string Name;
        public bool IsGlobal;
        
        // Note (Russ): This value currently does nothing for audio. The audio system relies entirely on its rolloff curve,
        // so this distance is only used by the VFX system.
        [Tooltip("This describe max distance of the emitter in which it affects player.")]
        public float MaxDistanceToCamera;
        
        [Tooltip("Priority (0-1) multiplied by distance to calculate priority based on significance")]
        public float Priority = 1f;
    }
}