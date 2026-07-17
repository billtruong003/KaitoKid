using UnityEngine;

namespace Shmackle.SoundMaterial
{
    [CreateAssetMenu(fileName = "MaterialSoundDatabase", menuName = "Shmackle/Material Sound Database", order = 1)]
    public class MaterialSoundDatabase : ScriptableObject
    {
        public MaterialSoundMapping[] materialSoundMappings;

        [System.Serializable]
        public class MaterialSoundMapping
        {
            public MaterialType materialType;
            public string VFXStepID;
            public AudioClip[] soundClips;
        }
    }
}