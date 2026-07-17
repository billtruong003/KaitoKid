using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Teabag.Core
{
    /// <summary>
    /// Provides audio-related functionality, including initialization, playback, volume control, and cleanup.
    /// This service allows managing audio clips and groups, as well as playing different types of audio
    /// with customizable options such as volume, spatial blending, looping, and crossfading.
    /// </summary>
    public class AudioService : IAudioService
    {
        private const string ParamMaster = "MasterVol";
        private const string ParamMusic = "MusicVol";
        private const string ParamSFX = "SFXVol";
        private const string ParamVoice = "VoiceVol";
        private const string ParamAmbient = "AmbientVol";
        private const string ParamUI = "UIVol";


        private IAudioSettings _audioSettings;
        private GameObject _audioManager;
        private IAudioManagerInstance _audioManagerInstance;
        private IDataPersistenceService _persistence;

        public AudioService(IDataPersistenceService persistence)
        {
            _persistence = persistence;
        }

        public void Initialize()
        {
            _audioSettings = AudioSettingsAsset.InstanceAsset.Settings;

            GenerateAudioManager();
            RestoreVolumes();
        }

        public void Dispose()
        {
            _audioSettings = null;
            Object.Destroy(_audioManager);
        }


        private void GenerateAudioManager()
        {
            _audioManager = Object.Instantiate(_audioSettings.AudioManagerPrefab);
            _audioManagerInstance = _audioManager.GetComponent<IAudioManagerInstance>();
        }

        private void RestoreVolumes()
        {
            foreach (AudioGroup g in Enum.GetValues(typeof(AudioGroup)))
            {
                float saved = _persistence.LoadData<float>("AudioVol_" + g, 1f);
                SetGroupVolume(g, saved);
            }
        }


        // ── Public API ────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public AudioMixerGroup GetMixerGroup(AudioGroup group) => _audioSettings.GetMixerGroup(group);

        /// <inheritdoc/>
        public void SetGroupVolume(AudioGroup group, float normalised)
        {
            var db = Mathf.Log10(Mathf.Clamp(normalised, 0.0001f, 1f)) * 20f;
            var param = ParamFor(group);
            if (_audioSettings.MasterMixer != null)
            {
                _audioSettings.MasterMixer.SetFloat(param, db);
            }

            _persistence.TrySaveData("AudioVol_" + group, normalised);
        }

        /// <inheritdoc/>
        public float GetGroupVolume(AudioGroup group) => _persistence.LoadData<float>("AudioVol_" + group, 1f);

        /// <inheritdoc/>
        public void Play(AudioClip clip)
            => _audioManagerInstance.Play(clip, 0.5f, Vector3.zero, 0f, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);

        /// <inheritdoc/>
        public void Play(AudioClip clip, float volume)
            => _audioManagerInstance.Play(clip, volume, Vector3.zero, 0f, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);

        /// <inheritdoc/>
        public void Play(AudioClip clip, Vector3 position)
            => _audioManagerInstance.Play(clip, 0.5f, position, 0f, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);

        /// <inheritdoc/>
        public void Play(AudioClip clip, float volume, Vector3 position)
            => _audioManagerInstance.Play(clip, volume, position, 0f, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);

        /// <inheritdoc/>
        public void Play(AudioClip clip, float volume, Vector3 position, float spatialBlend)
            => _audioManagerInstance.Play(clip, volume, position, spatialBlend, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);

        /// <inheritdoc/>
        public void Play(IReadOnlyList<AudioClip> clips, float volume, Vector3 position)
        {
            if (clips == null || clips.Count == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Count)];

            _audioManagerInstance.Play(clip, volume, position, 0f, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);
        }

        /// <inheritdoc/>
        public void Play(IReadOnlyList<AudioClip> clips, float volume, Vector3 position, float spatialBlend)
        {
            if (clips == null || clips.Count == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Count)];

            _audioManagerInstance.Play(clip, volume, position, spatialBlend, 1f, 1f, false, AudioGroup.Sfx, AudioPriority.Sfx);
        }


        /// <inheritdoc/>
        public void Play(AdvancedAudioClip clip)
        {
            if (clip == null) return;

            _audioManagerInstance.Play(clip.Clip, clip.Volume, Vector3.zero,
                clip.SpatialBlend, 1f, clip.RollOff, false,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void Play(AdvancedAudioClip clip, bool loop)
        {
            if (clip == null) return;

            _audioManagerInstance.Play(clip.Clip, clip.Volume, Vector3.zero,
                clip.SpatialBlend, 1f, clip.RollOff, loop,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void Play(AdvancedAudioClip clip, Vector3 position)
        {
            if (clip == null) return;

            _audioManagerInstance.Play(clip.Clip, clip.Volume, position,
                clip.SpatialBlend, 1f, clip.RollOff, false,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void Play(AdvancedAudioClip clip, Vector3 position, float pitch)
        {
            if (clip == null) return;

            _audioManagerInstance.Play(clip.Clip, clip.Volume, position,
                clip.SpatialBlend, pitch, clip.RollOff, false,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void Play(AdvancedAudioClip clip, float volume, float pitch, Vector3 position)
        {
            if (clip == null) return;

            _audioManagerInstance.Play(clip.Clip, volume, position,
                clip.SpatialBlend, pitch, clip.RollOff, false,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void Play(IReadOnlyList<AdvancedAudioClip> clips, Vector3 position)
        {
            if (clips == null || clips.Count == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Count)];

            _audioManagerInstance.Play(clip.Clip, clip.Volume, position,
                clip.SpatialBlend, 1f, clip.RollOff, false,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void Play(IReadOnlyList<AdvancedAudioClip> clips, Vector3 position, float pitch)
        {
            if (clips == null || clips.Count == 0) return;
            var clip = clips[UnityEngine.Random.Range(0, clips.Count)];

            _audioManagerInstance.Play(clip.Clip, clip.Volume, position,
                clip.SpatialBlend, pitch, clip.RollOff, false,
                clip.Group, clip.Priority);
        }

        /// <inheritdoc/>
        public void CrossfadeMusic(AudioClip clip, float fadeTime = 1f) => _audioManagerInstance.PlayMusic(clip, fadeTime);

        /// <inheritdoc/>
        public void Stop(string clipName) => _audioManagerInstance.Stop(clipName);

        /// <inheritdoc/>
        public void StopMusic(float fadeTime = 1f) => _audioManagerInstance.StopMusic(fadeTime);

        /// <inheritdoc/>
        public void StopAll()
        {
            if (_audioManagerInstance == null)
            {
                GameLogger.Warning("[AudioService] StopAll called after Dispose — _audioManagerInstance is null, skipping.");
                return;
            }
            _audioManagerInstance.StopAll();
        }



        private string ParamFor(AudioGroup group) => group switch
        {
            AudioGroup.Music => ParamMusic,
            AudioGroup.Sfx => ParamSFX,
            AudioGroup.Voice => ParamVoice,
            AudioGroup.Ambient => ParamAmbient,
            AudioGroup.UI => ParamUI,
            _ => ParamMaster
        };
    }
}
