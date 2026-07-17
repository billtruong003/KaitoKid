using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Teabag.Core
{
    /// <summary>
    /// Manages audio playback for sound effects and music within the application.
    /// </summary>
    /// <remarks>
    /// This class provides functionalities to play, stop, and crossfade audio clips
    /// with extensive configuration options, allowing for spatial audio, looping,
    /// and priority-based playback management. It interacts with Unity's audio system
    /// through AudioSource components.
    /// </remarks>
    public class AudioManagerInstance : MonoBehaviour, IAudioManagerInstance
    {
        // ── Inspector fields ──────────────────────────────────────────────────────

        [Header("Pool Settings")]
        [SerializeField, Tooltip("Initial number of AudioSources pre-allocated in the pool.")]
        private int _initialPoolSize = 16;

        [SerializeField, Tooltip("Maximum number of sounds that may play simultaneously.")]
        private int _maxConcurrentSounds = 32;

        [Header("Music")]
        [SerializeField, Tooltip("Default time (seconds) for music fade in/out.")]
        private float _defaultMusicFadeTime = 1f;


        // ── Private state ─────────────────────────────────────────────────────────

        private readonly Queue<AudioSource> _pool = new Queue<AudioSource>();
        private readonly List<ActiveSound> _activeSounds = new List<ActiveSound>();
        private AudioSource _musicSource;
        private Coroutine _musicFadeCoroutine;
        private Transform _poolRoot;

        private Dictionary<AudioGroup, AudioMixerGroup> _mixerGroupsCache = new ();

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            // FIX: Ensure it's a Root GameObject for DontDestroyOnLoad to work
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            // Fill mixer groups cache
            var audioSettings = AudioSettingsAsset.InstanceAsset.Settings;
            _mixerGroupsCache.Add(AudioGroup.Music, audioSettings.MusicMixerGroup);
            _mixerGroupsCache.Add(AudioGroup.Sfx, audioSettings.SfxMixerGroup);
            _mixerGroupsCache.Add(AudioGroup.Voice, audioSettings.VoiceMixerGroup);
            _mixerGroupsCache.Add(AudioGroup.Ambient, audioSettings.AmbientMixerGroup);
            _mixerGroupsCache.Add(AudioGroup.UI, audioSettings.UiMixerGroup);

            // Initialize Pool Root
            _poolRoot = new GameObject("Pool").transform;
            _poolRoot.SetParent(transform);

            // Pre-warm pool
            for (int i = 0; i < _initialPoolSize; i++)
                _pool.Enqueue(CreateSource());

            // Dedicated music source
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.outputAudioMixerGroup = _mixerGroupsCache[AudioGroup.Music];
        }

        private void Update()
        {
            // Centralized management: Return finished sources to the pool
            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                var s = _activeSounds[i];
                // Only automatically reclaim if NOT looping and finished playing
                if (!s.Source.loop && !s.Source.isPlaying)
                {
                    ReturnSource(s.Source);
                    _activeSounds.RemoveAt(i);
                }
            }
        }

        // ── Core Play (IAudioManager) ─────────────────────────────────────────────

        public void Play(AudioClip clip, float volume, Vector3 position,
            float spatialBlend, float pitch, float rollOff,
            bool loop, AudioGroup group, AudioPriority priority)
        {
            if (clip == null) return;

            AudioSource src = AcquireSource(priority);
            if (src == null) return;

            // Configure Transform & AudioSource
            src.transform.position = position;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.loop = loop;
            src.spatialBlend = spatialBlend;
            src.minDistance = rollOff;
            src.maxDistance = rollOff * 500f;
            src.dopplerLevel = 0f;
            src.spread = 0f;
            src.rolloffMode = spatialBlend > 0f ? AudioRolloffMode.Logarithmic : AudioRolloffMode.Linear;

            src.outputAudioMixerGroup = _mixerGroupsCache[group];
            src.Play();

            _activeSounds.Add(new ActiveSound(clip.name, src, priority));

            // NOTE: Removed ReturnAfter Coroutine as Update() above handles reclamation based on clip.length
        }

        // ── Music (IAudioManager) ─────────────────────────────────────────────────

        public void PlayMusic(AudioClip clip, float fadeTime)
        {
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeTime));
        }

        public void StopMusic(float fadeTime)
        {
            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
            _musicFadeCoroutine = StartCoroutine(FadeOutMusic(fadeTime));
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
        {
            if (_musicSource.isPlaying && fadeTime > 0f)
            {
                float startVol = _musicSource.volume;
                for (float t = 0; t < fadeTime; t += Time.deltaTime)
                {
                    _musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                    yield return null;
                }
            }

            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.volume = 0f;
            _musicSource.Play();

            if (fadeTime > 0f)
            {
                for (float t = 0; t < fadeTime; t += Time.deltaTime)
                {
                    _musicSource.volume = Mathf.Lerp(0f, 1f, t / fadeTime);
                    yield return null;
                }
            }
            _musicSource.volume = 1f;
        }

        private IEnumerator FadeOutMusic(float fadeTime)
        {
            if (!_musicSource.isPlaying) yield break;
            float startVol = _musicSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _musicSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.volume = 1f;
        }

        // ── Stop Methods (IAudioManager) ──────────────────────────────────────────

        public void Stop(string clipName)
        {
            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                var s = _activeSounds[i];
                if (s.ClipName == clipName)
                {
                    ReturnSource(s.Source);
                    _activeSounds.RemoveAt(i);
                }
            }
        }

        public void StopAll()
        {
            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                ReturnSource(_activeSounds[i].Source);
            }
            _activeSounds.Clear();
        }

        // ── Internal Helpers ──────────────────────────────────────────────────────

        private AudioSource AcquireSource(AudioPriority requestedPriority)
        {
            if (_activeSounds.Count < _maxConcurrentSounds)
                return GetSource();

            int lowestIdx = -1;
            float lowestPriority = float.MaxValue;

            for (int i = 0; i < _activeSounds.Count; i++)
            {
                float p = (float)_activeSounds[i].Priority;
                if (p < lowestPriority)
                {
                    lowestPriority = p;
                    lowestIdx = i;
                }
            }

            if (lowestIdx >= 0 && (float)requestedPriority > lowestPriority)
            {
                var stolen = _activeSounds[lowestIdx];
                _activeSounds.RemoveAt(lowestIdx);
                stolen.Source.Stop();
                return stolen.Source;
            }

            return null;
        }

        private AudioSource GetSource()
        {
            if (_pool.Count > 0)
            {
                var src = _pool.Dequeue();
                src.gameObject.SetActive(true);
                return src;
            }
            return CreateSource();
        }

        private void ReturnSource(AudioSource src)
        {
            src.Stop();
            src.clip = null;
            src.loop = false;
            src.outputAudioMixerGroup = null;
            src.gameObject.SetActive(false);
            _pool.Enqueue(src);
        }

        private AudioSource CreateSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(_poolRoot);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            go.SetActive(false);
            return src;
        }



        // ── Inner data type ───────────────────────────────────────────────────────

        private readonly struct ActiveSound
        {
            public readonly string ClipName;
            public readonly AudioSource Source;
            public readonly AudioPriority Priority;

            public ActiveSound(string name, AudioSource src, AudioPriority priority)
            {
                ClipName = name;
                Source = src;
                Priority = priority;
            }
        }
    }
}
