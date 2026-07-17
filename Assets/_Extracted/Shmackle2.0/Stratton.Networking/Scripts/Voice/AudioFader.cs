using System.Collections;
using UnityEngine;

namespace Stratton.Networking.Voice
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioFader : MonoBehaviour
    {
        [SerializeField]
        private float _defaultDelayFadeIn = 0.5f;
        [SerializeField]
        private float _defaultDelayFadeOut = 0f;
        [SerializeField]
        private float _defaultFadeTime = 0.5f;

        private Coroutine _fadeCoroutine;
        private AudioSource _audioSource;
        private AudioSourceOverrideVolume _overrideVolume;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _overrideVolume = GetComponent<AudioSourceOverrideVolume>();
        }

        public void StopFade()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        public void FadeIn()
        {
            StopFade();
            _fadeCoroutine = StartCoroutine(Fade(1, _defaultDelayFadeIn));
        }

        public void FadeOut()
        {
            StopFade();
            _fadeCoroutine = StartCoroutine(Fade(0, _defaultDelayFadeOut));
        }

        private IEnumerator Fade(float targetVolume, float delay = 0)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            float startVolume = _overrideVolume != null ? _overrideVolume.Volume : _audioSource.volume;
            float currentTime = 0;
            while(currentTime < _defaultFadeTime)
            {
                currentTime += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / _defaultFadeTime);
                yield return null;
            }
            _audioSource.volume = targetVolume;
            _fadeCoroutine = null;
        }

        private void OnDisable()
        {
            StopFade();
            if (_audioSource)
            {
                _audioSource.volume = 1;
            }
        }
    }
}