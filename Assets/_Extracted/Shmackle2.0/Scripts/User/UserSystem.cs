using Cysharp.Threading.Tasks;
using Fusion.Photon.Realtime;
using MessagePipe;
using Stratton.Core;
using Stratton.Networking;
using UnityEngine;

namespace Shmackle.User
{
    public class UserSystem : GameSystemBase, IAuthenticationValue
    {
        #region Private Fields

        private IUserInfoProvider _userInfoProvider;
        private IPublisher<UserInfoUpdatedEvent> _userInfoUpdatedEventPublisher;
        private UserInfoUpdatedEvent _userInfoUpdatedEvent;

        #endregion

        #region Properties

        public UserInfo LocalUserInfo => _userInfoProvider.GetUserInfo();

        #endregion

        #region Public Methods

        public override void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder)
        {
            builtinContainerBuilder.AddMessageBroker<UserInfoUpdatedEvent>();
        }

        public override async UniTask<InitializationResult> Init()
        {
            _userInfoUpdatedEvent = new UserInfoUpdatedEvent();
            _userInfoProvider = new LocalUserInfoProvider();
            _userInfoUpdatedEventPublisher = GlobalMessagePipe.GetPublisher<UserInfoUpdatedEvent>();
            await _userInfoProvider.Init();
            UserInfoResult userInfoResult = await LoadUserInfo();
            if (!userInfoResult.IsValid)
            {
                return new InitializationResult(InitializationErrorCode.Logic, "Failed to load user info");
            }
            return await base.Init();
        }

        public async UniTask<UserInfoResult> LoadUserInfo()
        {
            return await _userInfoProvider.LoadUserInfo();
        }

        public async UniTask<bool> UpdateUserName(string newUserName)
        {
            bool isSuccessful = await _userInfoProvider.UpdateUserName(newUserName);
            _userInfoUpdatedEvent.UserInfoResult.IsValid = isSuccessful;
            _userInfoUpdatedEvent.UserInfoResult.UserInfo = LocalUserInfo;
            _userInfoUpdatedEventPublisher.Publish(_userInfoUpdatedEvent);
            return isSuccessful;
        }

        #endregion
        
        #region IAUthenticationValue
        
        public AuthenticationValues GetValues()
        {
            string userId = LocalUserInfo.Id; 
#if UNITY_EDITOR
            // For editor testing, add the process ID to differentiate ID even on multiple parallel sync instances.
            userId += $"_editor:{System.Diagnostics.Process.GetCurrentProcess().Id.ToString()}";      
#endif
            return new AuthenticationValues(userId);
        }
        
        #endregion
    }
}