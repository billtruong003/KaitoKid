using Fusion;
using MessagePipe;
using Shmackle.Gameplay;
using Shmackle.Utilities;
using Stratton.Core;
using Stratton.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shmackle.UI
{
    public class UIPlayerScoreList : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private UIPlayerScore _playerScorePrefab;
        [SerializeField]
        private Transform _scoreContainer;
        
        #endregion
        
        #region Private Fields
        
        private IDisposable _eventsBagDisposable;
        private NetworkingSystem _networkingSystem;
        private readonly Dictionary<ShmacklePlayerState, UIPlayerScore> _playerScoreUIs = new();
        private readonly List<ShmacklePlayerState> _playersList = new();
        private bool _isScoreDirty = false;
        private SimpleObjectPool<UIPlayerScore> _playerScoreUIObjectPool;
        
        #endregion
        
        #region Private Methods

        private void Awake()
        {
            _playerScoreUIObjectPool = new SimpleObjectPool<UIPlayerScore>(_playerScorePrefab, 10, _scoreContainer);
            
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            
            var bag = DisposableBag.CreateBuilder();

            GlobalMessagePipe.GetSubscriber<PlayerStateRegisteredEvent>().Subscribe(e => OnPlayerJoined(e.PlayerState as ShmacklePlayerState)).AddTo(bag);
            
            _eventsBagDisposable = bag.Build();
        }

        private void OnDestroy()
        {
            _eventsBagDisposable?.Dispose();
            for (int i = 0; i < _playersList.Count; ++i)
            {
                PlayerState playerState = _playersList[i];
                if (playerState)
                {
                    playerState.ScoreChanged -= OnScoreChanged;
                }
            }
        }
        
        private void Start()
        {
            GameplaySystem gameplaySystem = GameSystemsManager.Instance.Get<GameplaySystem>();
            if (_networkingSystem.Runner)
            {
                // If this object is spawned later than the actual registration of the player states.
                foreach (var playerRef in _networkingSystem.Runner.ActivePlayers)
                {
                    OnPlayerJoined(gameplaySystem.GetPlayerState<ShmacklePlayerState>(playerRef));
                }
            }
        }

        private void OnPlayerJoined(ShmacklePlayerState playerState)
        {
            if (playerState == null || _playerScoreUIs.ContainsKey(playerState))
            {
                return;
            }
            _playersList.Add(playerState);
            UIPlayerScore uiPlayerScore = _playerScoreUIObjectPool.Get();
            uiPlayerScore.InitializePlayerState(playerState);
            _playerScoreUIs.Add(playerState, uiPlayerScore);
            playerState.ScoreChanged -= OnScoreChanged;
            playerState.ScoreChanged += OnScoreChanged;
        }

        private void OnScoreChanged(int score)
        {
            _isScoreDirty = true;
        }

        private void LateUpdate()
        {
            if (_isScoreDirty)
            {
                _isScoreDirty = false;
                _playersList.Sort((a, b) => b.Score.CompareTo(a.Score));
                for (int i = 0; i < _playersList.Count; i++)
                {
                    _playerScoreUIs[_playersList[i]].transform.SetSiblingIndex(i);
                }
            }
        }

        #endregion
    }
}