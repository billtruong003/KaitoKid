using Cysharp.Threading.Tasks;
using Stratton.Core;
using UnityEngine;

namespace Shmackle.User
{
    public struct UserInfoResult
    {
        public bool IsValid;
        public UserInfo UserInfo;
    }
    public interface IUserInfoProvider
    {
        public UniTask<InitializationResult> Init();
        public UserInfo GetUserInfo();
        public UniTask<UserInfoResult> LoadUserInfo();
        public UniTask<bool> UpdateUserName(string newPlayerName);
    }
}