using System.Collections;
using UnityEngine;

namespace Teabag.Player
{
    public class HandTap : MonoBehaviour
    {
        public new Renderer renderer;
        public AudioSource audioSource;
        private static readonly WaitForSeconds _waitFadeDuration = new WaitForSeconds(2f);

        public void Tap(HandTapType type)
        {
            if (type.texture != null)
                renderer.material.mainTexture = type.texture;
            else
                renderer.enabled = false;

            var advancedClip = type.clips[Random.Range(0, type.clips.Count)];
            audioSource.clip = advancedClip.Clip;
            audioSource.volume = advancedClip.Volume;
            audioSource.spatialBlend = advancedClip.SpatialBlend;
            audioSource.pitch = Random.Range(0.8f, 1.2f);
            audioSource.Play();

            StartCoroutine(Fade());
        }


        IEnumerator Fade()
        {
            yield return _waitFadeDuration;
            while (renderer.material.color.a > 0)
            {
                Color colour = renderer.material.color;
                colour.a -= Time.deltaTime;
                renderer.material.color = colour;
                yield return null;
            }

            Destroy(gameObject);
        }

        void OnDisable()
        {
            Destroy(gameObject);
        }
    }
}
