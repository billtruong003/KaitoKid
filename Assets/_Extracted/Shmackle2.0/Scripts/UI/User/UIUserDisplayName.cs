using Shmackle.Gameplay;
using Shmackle.User;
using Stratton.Core;
using TMPro;
using UnityEngine;

namespace Shmackle.UI
{
    public class UIUserDisplayName : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private TMP_Text _userNameText;

        #endregion
        
        #region Private Fields
        
        private PlayerState _playerState;
        
        #endregion

        #region Private Methods

        private void Awake()
        {
            if (_userNameText == null)
            {
                _userNameText = GetComponentInChildren<TMP_Text>();
            }
        }

        private void OnDestroy()
        {
            if (_playerState)
            {
                _playerState.UserInfoUpdated -= OnUserInfoUpdated;
            }
        }

        private void OnUserInfoUpdated(UserInfo userInfo)
        {
            _userNameText.SetText(userInfo.DisplayName);
        }

        #endregion
        
        #region Public Methods
        
        public void InitializePlayerState(ShmacklePlayerState playerState)
        {
            if (!playerState)
            {
                return;
            }
            
            if (_playerState)
            {
                _playerState.UserInfoUpdated -= OnUserInfoUpdated;
            }

            _playerState = playerState;

            OnUserInfoUpdated(_playerState.NetworkUserInfo.ToUserInfo());
            _playerState.UserInfoUpdated += OnUserInfoUpdated;
        }
        
        #endregion
    }
}