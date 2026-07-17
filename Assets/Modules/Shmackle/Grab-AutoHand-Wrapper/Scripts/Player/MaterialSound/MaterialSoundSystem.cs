using UnityEngine;
using MyPooler;
using UnityEngine.VFX;
using System.Linq;
using Utilities.Timers;

namespace Shmackle.SoundMaterial
{
    public class MaterialSoundSystem : MonoBehaviour
    {
        [Header("Material Sound Database")]
        [SerializeField] public MaterialSoundDatabase soundDatabase;
        [SerializeField] private bool enableRandomPitch = true;
        [SerializeField] private float effectWaitTime = 0.5f;
        private int _randomIndex;
        private TimeGate _effectGate;
        private bool checkPass;

        private void Awake()
        {
            _effectGate = new TimeGate(effectWaitTime);
        }

        public void ProcessSurfaceContact(GameObject hitObject, AudioSource audioSource)
        {
            if (hitObject == null || audioSource == null || soundDatabase == null) return;

            MaterialSoundDatabase.MaterialSoundMapping mapping = FindBestMapping(hitObject);

            if (mapping != null && mapping.soundClips.Length > 0)
            {
                PlaySound(audioSource, mapping, enableRandomPitch);
                TriggerStepVFX(mapping.VFXStepID, audioSource.transform);
                return;
            }

            PlayDefaultAudioSourceSound(audioSource);
        }

        private MaterialSoundDatabase.MaterialSoundMapping FindBestMapping(GameObject hitObject)
        {
            MaterialIdentifier materialIdentifier = hitObject.GetComponentInParent<MaterialIdentifier>();
            MaterialType materialType = materialIdentifier != null ? materialIdentifier.materialType : MaterialType.Default;

            MaterialSoundDatabase.MaterialSoundMapping specificMapping = soundDatabase.materialSoundMappings
                .FirstOrDefault(m => m.materialType == materialType && m.soundClips.Length > 0);

            if (specificMapping != null)
            {
                return specificMapping;
            }

            if (materialType != MaterialType.Default)
            {
                return soundDatabase.materialSoundMappings
                    .FirstOrDefault(m => m.materialType == MaterialType.Default && m.soundClips.Length > 0);
            }

            return null;
        }

        private void PlaySound(AudioSource audioSource, MaterialSoundDatabase.MaterialSoundMapping mapping, bool useRandomPitch)
        {
            _randomIndex = Random.Range(0, mapping.soundClips.Length);
            audioSource.clip = mapping.soundClips[_randomIndex];

            if (useRandomPitch)
            {
                RandomizePitch(audioSource);
            }

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void TriggerStepVFX(string vfxStepID, Transform spawnPoint)
        {
            checkPass = _effectGate.TryPass();
            if (string.IsNullOrEmpty(vfxStepID) || spawnPoint == null || checkPass == false) return;

            GameObject vfxObject = ObjectPooler.Instance.GetFromPool(vfxStepID, spawnPoint.position, spawnPoint.rotation);
            if (vfxObject == null) return;

            VisualEffect vfxComponent = vfxObject.GetComponentInChildren<VisualEffect>();
            if (vfxComponent != null)
            {
                vfxComponent.Play();
            }
        }

        private void RandomizePitch(AudioSource audioSource)
        {
            audioSource.pitch = 1f + Random.Range(-0.05f, 0.05f);
        }

        private void PlayDefaultAudioSourceSound(AudioSource audioSource)
        {
            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.pitch = 1f;
                audioSource.Play();
            }
        }
    }
}