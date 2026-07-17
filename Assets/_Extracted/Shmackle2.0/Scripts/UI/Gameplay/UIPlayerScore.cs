using System;
using DG.Tweening;
using Shmackle.Gameplay;
using TMPro;
using UnityEngine;

namespace Shmackle.UI
{
    public class UIPlayerScore : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField] 
        private UIUserDisplayName _userDisplayName;
        [SerializeField]
        private TMP_Text _scoreText;
        [SerializeField]
        private GameObject _localPlayerIndicator;
        [SerializeField]
        private CanvasGroup _canvasGroup;
        
        [Header("Tween")]
        private float _pulseScale = 1.1f;
        private float _pulseDuration = 0.1f;
        
        #endregion
        
        #region Private Fields
        
        private ShmacklePlayerState _playerState;
        
        #endregion
        
        #region Public Methods
        
        public void InitializePlayerState(ShmacklePlayerState playerState)
        {
            _playerState = playerState;
            _userDisplayName.InitializePlayerState(playerState);
            _localPlayerIndicator.SetActive(playerState.IsLocalPlayer);
            UnregisterEvents();
            _playerState.ScoreChanged += OnScoreChanged;
            _playerState.OwnerUpdated += OnOwnerUpdated;
            OnOwnerUpdated(playerState);
            OnScoreChanged(playerState.Score);
        }
        
        #endregion

        #region Private Methods

        private void UnregisterEvents()
        {
            if (_playerState)
            {
                _playerState.ScoreChanged -= OnScoreChanged;
                _playerState.OwnerUpdated -= OnOwnerUpdated;
            }
        }
        
        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void OnOwnerUpdated(PlayerState playerState)
        {
            _canvasGroup.alpha = playerState.IsValid ? 1 : 0.5f;
        }

        private void OnEnable()
        {
            _scoreText.transform.localScale = Vector3.one;
            _scoreText.transform.DOKill();
        }

        private void OnScoreChanged(int score)
        {
            _scoreText.SetText(score.ToString());
            _scoreText.transform.DOKill();

            _scoreText.transform.DOScale(_pulseScale, _pulseDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => 
                    _scoreText.transform.DOScale(1f, _pulseDuration).SetEase(Ease.InQuad)
                );
        }
        
        #endregion
    }
}