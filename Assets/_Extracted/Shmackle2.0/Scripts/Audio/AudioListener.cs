using System;
using AYellowpaper.SerializedCollections;
using Stratton.Audio;
using Stratton.Core;
using MessagePipe;
using Shmackle.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shmackle.Audio
{
    public class AudioListener : AudioListenerBase
    {
        #region Serialized Fields

        [SerializeField] private float _minSurfaceTouchedTimeTreshold = 0.25f;
        [SerializeField] private SerializedDictionary<SurfaceType, string[]> _footstepAudioKeysPerSurfaceType = new();
        // [SerializeField] private SerializedDictionary<string[]> _footstepAudioKeysPerSurfaceType = new();
        [SerializeField] private string[] _buttSmackAudioKeys;
        [SerializeField] private string[] _kissAudioKeys;

        #endregion

        #region Fields

        private ISubscriber<GameStateChangedEvent> _gameStateChangedEventSubscriber;

        private IDisposable _eventsBagDisposable;

        private PlayerLocomotion _currentPlayerLocomotion;
        private float _lastSurfaceTouchedTime;
        private ISubscriber<ButtSmackNetworkEventRelay> _playerButtSmackEventSubscriber;
        private ISubscriber<KissNetworkEventRelay> _playerKissEventSubscriber;

        #endregion

        #region Public Methods

        public override void Init()
        {
            base.Init();

            _gameStateChangedEventSubscriber = GlobalMessagePipe.GetSubscriber<GameStateChangedEvent>();
            _playerButtSmackEventSubscriber = GlobalMessagePipe.GetSubscriber<ButtSmackNetworkEventRelay>();
            _playerKissEventSubscriber = GlobalMessagePipe.GetSubscriber<KissNetworkEventRelay>();

            var bag = DisposableBag.CreateBuilder();

            _playerButtSmackEventSubscriber.Subscribe(e => OnButtSmackEvent(e.ContactPosition)).AddTo(bag);
            _playerKissEventSubscriber.Subscribe(e => OnKissEvent(e.ContactPosition)).AddTo(bag);
            _gameStateChangedEventSubscriber.Subscribe(e => OnGameStateChanged(e.From, e.To)).AddTo(bag);

            _eventsBagDisposable = bag.Build();
        }

        public override void DeInit()
        {
            _eventsBagDisposable?.Dispose();
            base.DeInit();
        }

        private void OnGameStateChanged(GameStateType from, GameStateType to)
        {
            if (to == BaseGameStateType.Gameplay)
            {
                _currentPlayerLocomotion = FindAnyObjectByType<PlayerLocomotion>();
                _currentPlayerLocomotion.SurfaceTouched += OnSurfaceTouched;
            }
            else
            {
                if (_currentPlayerLocomotion)
                {
                    _currentPlayerLocomotion.SurfaceTouched -= OnSurfaceTouched;
                }
            }
        }

        private void OnButtSmackEvent(Vector3 contactPosition)
        {
            var audioKey = Random.Range(0, _buttSmackAudioKeys.Length);
            _audioSystem.Play(_buttSmackAudioKeys[audioKey], contactPosition);
        }

        private void OnKissEvent(Vector3 contactPosition)
        {
            var audioKey = Random.Range(0, _kissAudioKeys.Length);
            _audioSystem.Play(_kissAudioKeys[audioKey], contactPosition);
        }

        private void OnSurfaceTouched(SurfaceType surfaceType, Vector3 position)
        {
            // TODO: This check is not enough as the locomotion detects a new hit too often anyway because of how it works (i.e. when you try to unstick your hands)
            if (Time.time - _lastSurfaceTouchedTime < _minSurfaceTouchedTimeTreshold)
            {
                return;
            }
            if (_footstepAudioKeysPerSurfaceType.ContainsKey(surfaceType) && _footstepAudioKeysPerSurfaceType[surfaceType].Length > 0)
            {
                var audioKeysArray = _footstepAudioKeysPerSurfaceType[surfaceType];
                var audioKey = Random.Range(0, audioKeysArray.Length);
                _audioSystem.Play(audioKeysArray[audioKey], position);
            }
            _lastSurfaceTouchedTime = Time.time;
        }

        #endregion
    }
}