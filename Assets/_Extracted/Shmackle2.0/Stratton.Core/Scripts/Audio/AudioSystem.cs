using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratton.Assets;
using Stratton.Audio.Factories;
using Stratton.Core;
using Stratton.Effects;
using MessagePipe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Stratton.Audio
{
    public class AudioSystem : ObjectEmittingSystem<AudioEmitter>
    {
        #region Serialized Fields

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private AssetReferenceT<AudioLibrary> _audioLibraryAssetReference;
        [SerializeField] private List<AudioListenerBase> _listeners;

        #endregion

        #region Injected Fields

        private AssetsSystem _assetsSystem;
        private AudioEmitterFactory _audioEmitterFactory = new();
        private AudioPoolFactory _audioPoolFactory = new();

        #endregion

        #region Fields

        private AsyncOperationHandle _audioLibraryHandle;
        private AudioPool _audioPool;

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builder)
        {
        }

        public override async UniTask<InitializationResult> Init()
        {
            var result = await base.Init();

            foreach (var listener in _listeners)
            {
                listener.Init();
            }

            return result;
        }

        public override UniTask<DeinitializationResult> DeInit()
        {
            foreach (var listener in _listeners)
            {
                listener?.DeInit();
            }
            UnloadAudioLibrary();
            return base.DeInit();
        }

        public virtual async UniTask LoadAudioLibrary()
        {
            var result = await _assetsSystem.LoadAsset<AudioLibrary>(_audioLibraryAssetReference);
            _audioLibraryHandle = result.OperationHandle;
            _audioLibrary = result.Asset;
            CreatePool();
        }

        public virtual void UnloadAudioLibrary()
        {
            if (_audioLibraryHandle.IsValid())
            {
                _assetsSystem.ReleaseAsset(_audioLibraryHandle);
            }
        }

        public override AudioEmitter Play(string effectKey, Transform mainCameraTransform = null)
        {
            if (_audioLibrary == null)
            {
                if (_assetsSystem.IsAllAddressablesCached)
                {
                    Log.Error(BaseLogChannel.Assets, "Audio Library is not loaded yet!");
                }
                return null;
            }

            if (_audioLibrary.TryGetAudioData(effectKey, out var data))
            {
                var audioEmitter = _audioPool.GetAudioEmitter();
                audioEmitter.SetMainCameraTransform(mainCameraTransform);
                audioEmitter.ObjectEmitterData = data;
                audioEmitter.Play();
                return audioEmitter;
            }
            Log.Error(BaseLogChannel.Assets, $"AudioData - {effectKey} not present in AudioLibrary");
            return null;
        }

        public override AudioEmitter Play(string effectKey, Vector3 position, Transform mainCameraTransform = null)
        {
            var audioEmitter = Play(effectKey, mainCameraTransform);
            if (audioEmitter != null)
            {
                audioEmitter.SetPosition(position);
            }
            return audioEmitter;
        }

        public override AudioEmitter Play(string effectKey, Transform parent, Transform mainCameraTransform = null)
        {
            var audioEmitter = Play(effectKey, mainCameraTransform);
            if (audioEmitter != null)
            {
                audioEmitter.SetParent(parent);
            }
            return audioEmitter;
        }

        public override void Stop(AudioEmitter emitter)
        {
            emitter.Stop();
        }

        public override void Stop(AudioEmitter emitter, float fadeOutTime)
        {
            StartCoroutine(FadeOutAudioEmitter(emitter, fadeOutTime));
        }

        public void Resume(AudioEmitter audioEmitter)
        {
            if (audioEmitter != null)
            {
                audioEmitter.Resume();
            }
        }

        /// <summary>
        /// Mutting all groups by mutting Master group
        /// </summary>
        public void MuteAll()
        {
            SetVolumeToAllGroups(-80f);
        }

        public void MuteSpecificGroup(string groupName)
        {
            SetVolumeToGroup(groupName, -80f);
        }

        /// <summary>
        /// Setting volume to group;
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="volume">Must be between -80(dB) and 20(dB)</param>
        public void SetVolumeToGroup(string groupName, float volume)
        {
            if (_mixer != null)
            {
                _mixer.SetFloat(groupName, volume);
            }
            else
            {
                Log.Error(BaseLogChannel.Audio, "There is no reference set for Audio Mixer in Audio system");
            }
        }

        public void SetVolumeToAllGroups(float volume)
        {
            SetVolumeToGroup("Master", volume);
        }

        public AudioMixerGroup GetMixerGroup(string groupName)
        {
            if (_mixer != null)
            {
                return _mixer.FindMatchingGroups(groupName)[0];
            }

            return null;
        }

        #endregion

        #region Private Methods

        protected virtual void Update()
        {
            if (_audioPool != null)
            {
                _audioPool.OnUpdate();
            }
        }

        protected override void CreatePool()
        {
            if (_audioLibrary == null)
            {
                return;
            }
            _audioPool = _audioPoolFactory.Create(_audioEmitterFactory, _audioLibrary.PoolSize, transform);
            _audioPool.PrefillPool();
        }

        private IEnumerator FadeOutAudioEmitter(AudioEmitter audioEmitter, float fadeOutTime)
        {
            float timer = 0f;
            float volume = 0f;
            float startVolume = audioEmitter.AudioSource.volume;
            while (timer < fadeOutTime)
            {
                volume = Mathf.Lerp(startVolume, 0f, timer / fadeOutTime);
                audioEmitter.AudioSource.volume = volume;
                timer += UnityEngine.Time.deltaTime;
                yield return null;
            }
            audioEmitter.Stop();
        }

        #endregion
    }
}