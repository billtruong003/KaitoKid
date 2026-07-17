using Fusion;
using MessagePipe;
using Shmackle.Gameplay;
using Stratton.Core;
using System;
using UnityEngine;

namespace Shmackle.UI
{
    /// <summary>
    /// Use with UIUserDisplayName if it is supposed to be owned by the player.
    /// </summary>
    [RequireComponent(typeof(UIUserDisplayName))]
    public class UINetworkUserDisplayName : NetworkBehaviour
    {
        #region Serialized Fields
        
        [SerializeField]
        private UIUserDisplayName _userDisplayName;
        
        #endregion
        
        #region Private Fields
        
        private IDisposable _eventsDisposable;
        
        #endregion
        
        #region Private Methods

        private void Awake()
        {
            if (_userDisplayName == null)
            {
                _userDisplayName = GetComponent<UIUserDisplayName>();
            }
        }

        private void OnDestroy()
        {
            _eventsDisposable?.Dispose();
        }

        #endregion
        
        #region Public Methods
        
        public override void Spawned()
        {
            base.Spawned();
            GameplaySystem gameplaySystem = GameSystemsManager.Instance.Get<GameplaySystem>();
            ShmacklePlayerState playerState = gameplaySystem.GetPlayerState<ShmacklePlayerState>(Object.InputAuthority);

            if (playerState == null)
            {
                var bag = DisposableBag.CreateBuilder();
                GlobalMessagePipe.GetSubscriber<PlayerStateRegisteredEvent>()
                    .Subscribe(e =>
                    {
                        if (e.PlayerState.Owner == Object.InputAuthority)
                        {
                            _userDisplayName.InitializePlayerState(e.PlayerState as ShmacklePlayerState);
                        }
                    }).AddTo(bag);
                _eventsDisposable = bag.Build();
            }
            else
            {
                _userDisplayName.InitializePlayerState(playerState);                
            }
        }
        
        #endregion
    }
}