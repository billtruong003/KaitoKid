using MessagePipe;
using Shmackle.User;
using Stratton.Core;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.Utilities
{
    public class RequireUserInitialization : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Events")]
        [SerializeField, Tooltip("Called on require of the first time user setup (player name)")]
        private UnityEvent _requireInitialUserInfoSetup;
        [SerializeField, Tooltip("Called if user info is valid. Either OnEnable or when the info is updated")]
        private UnityEvent _userInfoConfirmed;

        #endregion

        #region Properties

        #endregion

        #region Private Fields

        private UserSystem _userSystem;
        private IDisposable _userInfoUpdatedEventSubscription;
        private bool _isNewPlayer = false;

        #endregion

        #region Private Methods

        private void Awake()
        {
            _userSystem = GameSystemsManager.Instance.Get<UserSystem>();
            _userInfoUpdatedEventSubscription = GlobalMessagePipe.GetSubscriber<UserInfoUpdatedEvent>().Subscribe(e => OnUserInfoUpdated(e.UserInfoResult));
        }

        private void OnDestroy()
        {
            _userInfoUpdatedEventSubscription?.Dispose();
        }

        private void OnEnable()
        {
            _isNewPlayer = _userSystem.LocalUserInfo.IsNewPlayer;
            if (_isNewPlayer)
            {
                _requireInitialUserInfoSetup.Invoke();
            }
            else
            {
                _userInfoConfirmed.Invoke();
            }
        }

        private void OnUserInfoUpdated(UserInfoResult userInfoResult)
        {
            if (!userInfoResult.IsValid)
            {
                return;
            }
            if (_isNewPlayer)
            {
                if (!userInfoResult.UserInfo.IsNewPlayer)
                {
                    _isNewPlayer = false;
                    _userInfoConfirmed.Invoke();
                    _userInfoUpdatedEventSubscription?.Dispose();
                }
            }
        }

        #endregion

    }
}
