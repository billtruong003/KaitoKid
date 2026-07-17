using Fusion;
using MessagePipe;
using System;
using Shmackle.User;
using Stratton.Core;
using UnityEngine;

namespace Shmackle.Gameplay
{
    /// <summary>
    /// Represents networked player data
    /// Due to shared mode, this is owned by the State Authority, so no direct player data manipulation can be done.
    /// Only manipulate values on state authority.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerState : NetworkBehaviour
    {
        #region Public Fields
        
        public event Action<UserInfo> UserInfoUpdated;
        public event Action<PlayerState> OwnerUpdated;
        public event Action<int> ScoreChanged;
        
        #endregion
        
        #region Private Fields
        
        private IDisposable _userInfoUpdatedSubscription;
        
        #endregion
        
        #region Properties

        [Networked, OnChangedRender(nameof(OnOwnerUpdated))]
        public PlayerRef Owner { get; set; }
        [Networked]
        public PlayerRef LastValidOwner { get; private set; }
        /// <summary>
        /// Has a valid owner
        /// </summary>
        public bool IsValid { get; private set; }
        
        [Networked, OnChangedRender(nameof(OnUserInfoUpdated))]
        public ref NetworkUserInfo NetworkUserInfo => ref MakeRef<NetworkUserInfo>();
        public bool IsLocalPlayer => Owner == Runner.LocalPlayer;
        
        [Networked, OnChangedRender(nameof(OnScoreChanged))]
        public int Score { get; private set; }

        #endregion

        #region Public Methods

        public override void Spawned()
        {
            base.Spawned();
            if (IsLocalPlayer)
            {
                UpdateUserInfo(GameSystemsManager.Instance.Get<UserSystem>().LocalUserInfo);
                _userInfoUpdatedSubscription = GlobalMessagePipe.GetSubscriber<UserInfoUpdatedEvent>().Subscribe(e => OnLocalUserInfoUpdated(e.UserInfoResult));
            }
            OnOwnerUpdated();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _userInfoUpdatedSubscription?.Dispose();
            base.Despawned(runner, hasState);
        }
        
        public void AddScore(int addScore)
        {
            Score += addScore;
        }

        public void SetScore(int score)
        {
            Score = score;
        }

        #endregion

        #region Protected Methods

        protected virtual void OnOwnerUpdated()
        {
            IsValid = Runner.IsPlayerValid(Owner);
            if (IsValid)
            {
                LastValidOwner = Owner;
                bool isLocal = Runner.LocalPlayer == Owner;
#if UNITY_EDITOR
                name = isLocal ? $"(Local) PlayerState[{Owner.PlayerId}]" : $"PlayerState[{Owner.PlayerId}]";
#endif
                GlobalMessagePipe.GetPublisher<PlayerStateRegisteredEvent>().Publish(new () { PlayerState = this });
            }
            else
            {
#if UNITY_EDITOR
                name = $"InvactivePlayerState[{LastValidOwner.PlayerId}]";
#endif
                GlobalMessagePipe.GetPublisher<PlayerStateUnregisteredEvent>().Publish(new () { PlayerState = this });
            }
            OwnerUpdated?.Invoke(this);
        }

        protected virtual void OnScoreChanged()
        {
            ScoreChanged?.Invoke(Score);
        }
        
        #endregion
        
        #region Private Methods

        private void OnUserInfoUpdated()
        {
            UserInfoUpdated?.Invoke(NetworkUserInfo.ToUserInfo());
        }

        private void OnLocalUserInfoUpdated(UserInfoResult userInfoResult)
        {
            if (userInfoResult.IsValid)
            {
                UpdateUserInfo(userInfoResult.UserInfo);
            }
        }

        private void UpdateUserInfo(UserInfo userInfo)
        {
            if (!IsLocalPlayer)
            {
                return;
            }
            NetworkUserInfo networkUserInfo = new();
            networkUserInfo.UpdateFromUserInfo(userInfo);
            // Only networked version can be passed on RPCs
            RpcUpdateUserInfo(networkUserInfo);
        }

        #endregion
        
        #region RPC Methods

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RpcUpdateUserInfo(NetworkUserInfo networkUserInfo)
        {
            NetworkUserInfo.Copy(networkUserInfo);
        }
        
        #endregion
    }
}