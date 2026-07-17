using UnityEngine;
using GameSystem.Audio;
using GameSystem.Environment;

namespace GameSystem.Player
{
    [System.Serializable]
    public class PlayerFootstep
    {
        [SerializeField] private SurfaceAudioProfile profile;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private LayerMask groundMask;

        public void Initialize()
        {
            audioSource.spatialBlend = 1f;
        }

        public void ProcessFootstep(float phase, bool triggerCondition, Vector3 origin)
        {
            if (triggerCondition)
            {
                PlayFootstepSound(origin);
            }
        }

        private void PlayFootstepSound(Vector3 origin)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, groundMask))
            {
                SurfaceMaterial material = hit.collider.GetComponent<SurfaceMaterial>();
                SurfaceType type = material != null ? material.SurfaceType : SurfaceType.Concrete;
                AudioClip clip = profile.GetRandomClip(type);

                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }
    }
}