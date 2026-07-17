using Fusion;

namespace Shmackle.User
{ 
    [System.Serializable]
    public struct UserInfo
    {
        public string Id { get; set; }
        public bool IsNewPlayer { get; set; }
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Same user info data but supported by Fusion's replication
    /// </summary>
    [System.Serializable]
    public struct NetworkUserInfo : INetworkStruct
    {
        public NetworkString<_64> Id;
        public NetworkBool IsNewPlayer;
        public NetworkString<_16> DisplayName;

        public void Copy(NetworkUserInfo other)
        {
            Id = other.Id;
            IsNewPlayer = other.IsNewPlayer;
            DisplayName = other.DisplayName;
        }
        
        public void UpdateFromUserInfo(UserInfo userInfo)
        {
            Id = userInfo.Id;
            IsNewPlayer = userInfo.IsNewPlayer;
            DisplayName = userInfo.DisplayName;
        }

        public UserInfo ToUserInfo()
        {
            UserInfo userInfo = new UserInfo()
            {
                IsNewPlayer = IsNewPlayer,
                Id = Id.Value,
                DisplayName = DisplayName.Value
            };
            return userInfo;
        }
    }
}