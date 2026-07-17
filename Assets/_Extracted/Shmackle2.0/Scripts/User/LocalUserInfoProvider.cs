using Cysharp.Threading.Tasks;
using Stratton.Core;
using Stratton.Save;

namespace Shmackle.User
{
    public class LocalUserInfoProvider : IUserInfoProvider
    {
        #region Private Fields

        private SaveSystem _saveSystem;
        private PersistentValue<UserInfo> _localUserInfoValue;

        #endregion

        #region IUserInfoProvider

        public async UniTask<InitializationResult> Init()
        {
            _saveSystem = GameSystemsManager.Instance.Get<SaveSystem>();
            if (_saveSystem == null)
            {
                return new InitializationResult(InitializationErrorCode.Logic, "Save system is not yet initialized");
            }
            UserInfo defaultUserInfo = new UserInfo
            {
                Id = System.Guid.NewGuid().ToString(),
                IsNewPlayer = true,
                DisplayName = ""
            };
            _localUserInfoValue = new PersistentValue<UserInfo>(_saveSystem, "UserInfo", SavePattern.OnDemand, defaultUserInfo);
            return InitializationResult.Success;

        }

        public UserInfo GetUserInfo()
        {
            return _localUserInfoValue.Value;
        }

        public async UniTask<UserInfoResult> LoadUserInfo()
        {
            return new UserInfoResult
            {
                IsValid = true,
                UserInfo = _localUserInfoValue.Value
            };
        }

        public async UniTask<bool> UpdateUserName(string newUserName)
        {
            if (newUserName.IsNullOrEmpty())
            {
                return false;
            }
            UserInfo modifiedUserInfo = _localUserInfoValue.Value;
            modifiedUserInfo.DisplayName = newUserName;
            modifiedUserInfo.IsNewPlayer = false; // no longer tagged as new player once name is updated (first play)
            _localUserInfoValue.Value = modifiedUserInfo;
            _localUserInfoValue.Save();
            return true;
        }

        #endregion
    }
}