using Fusion;
using MessagePipe;
using Shmackle.Gameplay;
using Shmackle.Utilities;
using Stratton.Core;
using Stratton.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shmackle.UI
{
    public class UIPlayerVoicesContainer : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private RectTransform _rootContainer;
        [SerializeField]
        private UIPlayerVoiceEntry _voiceEntryPrefab;

        #endregion

        #region Private Fields

        private IDisposable _eventsBagDisposable;
        private NetworkRunner _networkRunner;

        private Dictionary<PlayerRef, UIPlayerVoiceEntry> _voiceEntries = new Dictionary<PlayerRef, UIPlayerVoiceEntry>();

        private SimpleObjectPool<UIPlayerVoiceEntry> _playerVoiceEntryObjectPool;

        #endregion

        #region Private Methods

        private void Awake()
        {
            if (_rootContainer == null)
            {
                _rootContainer = GetComponent<RectTransform>();
            }
            foreach (Transform child in _rootContainer)
            {
                Destroy(child.gameObject);
            }

            var bag = DisposableBag.CreateBuilder();

            GlobalMessagePipe.GetSubscriber<PlayerStateRegisteredEvent>().Subscribe(e => OnPlayerJoined(e.PlayerState as ShmacklePlayerState)).AddTo(bag);
            GlobalMessagePipe.GetSubscriber<PlayerStateUnregisteredEvent>().Subscribe(e => OnPlayerLeft(e.PlayerState as ShmacklePlayerState)).AddTo(bag);

            _eventsBagDisposable = bag.Build();

            NetworkingSystem networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            if (networkingSystem == null)
            {
                Stratton.Core.Log.Error(BaseLogChannel.UI, "Networking system not yet initialized");
                return;
            }
            _networkRunner = networkingSystem.Runner.GetComponent<NetworkRunner>();
            if (_networkRunner == null)
            {
                Stratton.Core.Log.Error(BaseLogChannel.UI, "Network Runner not yet initialized");
            }
            
            _playerVoiceEntryObjectPool = new SimpleObjectPool<UIPlayerVoiceEntry>(_voiceEntryPrefab, 10, _rootContainer);
        }

        private void Start()
        {
            GameplaySystem gameplaySystem = GameSystemsManager.Instance.Get<GameplaySystem>();
            PlayerRef[] existingPlayers = _networkRunner.ActivePlayers.ToArray();
            for (int i = 0; i < existingPlayers.Length; i++)
            {
                OnPlayerJoined(gameplaySystem.GetPlayerState<ShmacklePlayerState>(existingPlayers[i]));
            }
        }

        private void OnDestroy()
        {
            _eventsBagDisposable?.Dispose();
        }

        private void OnPlayerJoined(ShmacklePlayerState newPlayerState)
        {
            if (newPlayerState.IsLocalPlayer)
            {
                return;
            }
            if (_voiceEntries.ContainsKey(newPlayerState.Owner))
            {
                return;
            }

            UIPlayerVoiceEntry voiceEntry = _playerVoiceEntryObjectPool.Get();
            voiceEntry.Initialize(_networkRunner, newPlayerState);
            _voiceEntries.Add(newPlayerState.Owner, voiceEntry);
        }

        private void OnPlayerLeft(ShmacklePlayerState leavingPlayerState)
        {
            if (!_voiceEntries.ContainsKey(leavingPlayerState.LastValidOwner))
            {
                return;
            }
            _playerVoiceEntryObjectPool.Release(_voiceEntries[leavingPlayerState.LastValidOwner]);
            _voiceEntries.Remove(leavingPlayerState.LastValidOwner);
        }

        #endregion
    }
}
