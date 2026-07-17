using Cysharp.Threading.Tasks;
using Fusion;
using MessagePipe;
using Stratton.Core;
using Stratton.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shmackle.Gameplay
{
    public enum MatchStatus : byte
    {
        NotStarted,
        InProgress,
        Ended
    }
    /// <summary>
    /// Handles game mode logic
    /// </summary>
    [DisallowMultipleComponent]
    public class GameModeBase : NetworkBehaviour, IStateAuthorityChanged
    {
        #region Properties

        [Header("Runtime Values")]
        
        // NetworkedDictionaries should be defined at the top of the class to fix loading crash (reflection/weaving bug?).
        [Networked, Capacity(32)]
        protected NetworkDictionary<PlayerRef, NetworkId> PlayerStateNetworkObjects { get; } = new();
        [Networked, Capacity(32)] // Networked only to keep it across host migration
        protected NetworkDictionary<NetworkString<_64>, NetworkId> InactivePlayerStateNetworkObjects { get; } = new();
        [Networked, Capacity(32)] // Networked only to keep it across host migration
        protected NetworkDictionary<PlayerRef, NetworkString<_64>> PlayerRefToPlayerId { get;  } = new();
        [Networked, OnChangedRender(nameof(OnMatchStatusChanged))] 
        public MatchStatus MatchStatus { get; protected set; } = MatchStatus.NotStarted;

        #endregion
        
        #region Serialized Fields

        [SerializeField]
        private GameModeSettings _settings;
        
        #endregion
        
        #region Public Fields

        public event Action<MatchStatus> MatchStatusChanged;
        
        #endregion

        #region Protected Fields
        
        protected NetworkingSystem _networkingSystem;

        #endregion
        
        #region Private Fields
        
        // local cached PlayerState type
        private readonly Dictionary<PlayerRef, PlayerState> _activePlayerStates = new();
        private readonly Dictionary<PlayerRef, NetworkObject> _activePlayerObjects = new();
        private readonly Dictionary<NetworkId, PlayerState> _playerStateNetworkObjects = new();
        
        #endregion
        
        #region Private Methods
        
        private PlayerState SpawnPlayerState(PlayerRef playerRef)
        {
            PlayerState playerState;
            if (InactivePlayerStateNetworkObjects.ContainsKey(PlayerRefToPlayerId[playerRef]))
            {
                string playerId = PlayerRefToPlayerId[playerRef].Value;
                playerState = GetPlayerStateFromNetworkId(InactivePlayerStateNetworkObjects[playerId]);
                InactivePlayerStateNetworkObjects.Remove(playerId);
                playerState.Owner = playerRef;
                Stratton.Core.Log.Message(BaseLogChannel.Gameplay, "Player reconnecting...");
            }
            else
            {
                playerState = _networkingSystem.Runner.Spawn(_settings.PlayerStatePrefab, 
                    inputAuthority: Runner.Topology != Topologies.Shared ? playerRef : null, // only give input authority on host mode
                    onBeforeSpawned: (runner, obj) =>
                    {
                        PlayerState newPlayerState = obj.GetComponent<PlayerState>();
                        newPlayerState.Owner = playerRef;
                    });
                
            }
            PlayerStateNetworkObjects.Add(playerRef, playerState.Object);
            return playerState;
        }

        private void DespawnPlayerState(PlayerRef playerRef)
        {
            if (PlayerStateNetworkObjects.ContainsKey(playerRef))
            {
                PlayerState leavingPlayerState = GetPlayerState<PlayerState>(playerRef);
                InactivePlayerStateNetworkObjects.Add(PlayerRefToPlayerId[playerRef], PlayerStateNetworkObjects[playerRef]);
                // Don't destroy any player state for reconnection.
                // Only assign invalid player to invalidate that player state
                leavingPlayerState.Owner = PlayerRef.Invalid;
                PlayerStateNetworkObjects.Remove(playerRef);
                _activePlayerStates.Remove(playerRef);
            }
        }
        
        #endregion

        #region Protected Methods

        protected virtual void Awake()
        {
            _networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
        } 

        protected virtual void OnMatchStatusChanged()
        {
            switch (MatchStatus)
            {
                case MatchStatus.NotStarted:
                    break;
                case MatchStatus.InProgress:
                    OnMatchStarted();
                    break;
                case MatchStatus.Ended:
                    OnMatchEnded();
                    break;
            }
            MatchStatusChanged?.Invoke(MatchStatus);
        }
        
        protected virtual void OnMatchStarted() { }
        protected virtual void OnMatchEnded() { }

        /// <summary>
        /// Called on StateAuthority when a player joins
        /// </summary>
        protected internal virtual void OnPlayerJoined(PlayerRef newPlayer)
        {
            PlayerRefToPlayerId.Add(newPlayer, Runner.GetPlayerUserId(newPlayer));
            SpawnPlayerState(newPlayer);
        }

        /// <summary>
        /// Called on StateAuthority when a player leaves
        /// </summary>
        protected internal virtual void OnPlayerLeft(PlayerRef leavingPlayer)
        {
            DespawnPlayerState(leavingPlayer);
            PlayerRefToPlayerId.Remove(leavingPlayer);
        }
        
        protected PlayerState GetPlayerStateFromNetworkId(NetworkId networkId)
        {
            if (_playerStateNetworkObjects.ContainsKey(networkId))
            {
                return _playerStateNetworkObjects[networkId];
            }

            if (Runner.TryFindObject(networkId, out NetworkObject playerStateObject))
            {
                PlayerState playerState = playerStateObject.GetComponent<PlayerState>();
                if (playerState)
                {
                    _playerStateNetworkObjects.Add(networkId, playerState);
                    return playerState;
                }
            }
            return null;
        }
        
        #endregion

        #region Public Methods

        public override void Spawned()
        {
            if (_settings.AutoStartMatch && HasStateAuthority)
            {
                StartMatch();
            }

            if (Runner.IsClient)
            {
                for (int i = 0; i < _settings.DefaultClientSpawnedPrefabs.Length; i++)
                {
                    Instantiate(_settings.DefaultClientSpawnedPrefabs[i]);
                }
            }
        }
        
        public void StateAuthorityChanged()
        {
            /*
                OnPlayerLeft is not properly called when the master client leaves.
                There is a chance that it is called on the upcoming master client, while it still has NO state authority.
                This is the best way to simulate the OnPlayerLeft that guarantees that the authority has already transferred before it gets called.
                This will check which players are no longer active.
                There should be no chance that OnPlayerLeft can be called more than once as we remove the players who already left on the PlayerStateNetworkObjects.
            */
            if (HasStateAuthority)
            {
                List<PlayerRef> justLeftPlayers = new();
                foreach (var playerStatePair in PlayerStateNetworkObjects)
                {
                    PlayerRef playerRef = playerStatePair.Key;
                    if (!Runner.IsPlayerValid(playerRef))
                    {
                        justLeftPlayers.Add(playerRef);
                    }
                }

                foreach (PlayerRef playerRef in justLeftPlayers)
                {
                    OnPlayerLeft(playerRef);
                }
            }
        }

        public virtual void StartMatch()
        {
            if (HasStateAuthority)
            {
                MatchStatus = MatchStatus.InProgress;
            }
        }

        public virtual void EndMatch()
        {
            if (HasStateAuthority)
            {
                MatchStatus = MatchStatus.Ended;
            }
        }
        
        /// <summary>
        /// Get a specific player-owned player state object.
        /// Can be called on clients and server.
        /// </summary>
        /// <param name="playerRef">Owning player</param>
        /// <returns>Owned Player State</returns>
        public T GetPlayerState<T>(PlayerRef playerRef) where T : PlayerState
        {
            if (!PlayerStateNetworkObjects.ContainsKey(playerRef))
            {
                return null;
            }
            if (!_activePlayerStates.ContainsKey(playerRef))
            {
                PlayerState playerState  = GetPlayerStateFromNetworkId(PlayerStateNetworkObjects[playerRef]);
                _activePlayerStates.Add(playerRef, playerState);
            }
            return _activePlayerStates[playerRef] as T;
        }

        /// <summary>
        /// Wait until the player-owned player state is available.
        /// Player state maybe unavailable on clients if it's not yet replicated, or invalid.
        /// Can be called on clients and server.
        /// </summary>
        /// <param name="playerRef">Owning player</param>
        /// <param name="timeOutSeconds">Cancels the request after the timeout</param>
        /// <returns>Owned Player State</returns>
        public async UniTask<T> WaitForPlayerState<T>(PlayerRef playerRef, float timeOutSeconds = 5) where T : PlayerState
        {
            float startTime = Time.time;
            T playerState = GetPlayerState<T>(playerRef);
			while (!playerState)
            {
                if (Time.time - startTime > timeOutSeconds)
                {
                    return null;
                }
                await UniTask.DelayFrame(1);
				playerState = GetPlayerState<T>(playerRef);
            }
            return playerState;
        }

        public T GetLocalPlayerState<T>() where T : PlayerState
        {
            return GetPlayerState<T>(Runner.LocalPlayer);
        }

        public async UniTask<T> WaitForLocalPlayerState<T>(float timeOutSeconds = 5) where T : PlayerState
        {
            return await WaitForPlayerState<T>(Runner.LocalPlayer, timeOutSeconds);
        }

        public NetworkObject GetPlayerObject(PlayerRef playerRef)
        {
            if (!_activePlayerObjects.ContainsKey(playerRef))
            {
                if (Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject))
                {
                    _activePlayerObjects.Add(playerRef, playerObject);
                }
            }

            if (_activePlayerObjects.ContainsKey(playerRef))
            {
                return _activePlayerObjects[playerRef];
            }

            return null;
        }

        public List<T> GetAllPlayerStates<T>(bool includeInactive = true) where T : PlayerState
        {
            List<T> playerStates = new();
            foreach (var playerStatePair in PlayerStateNetworkObjects)
            {
                playerStates.Add(GetPlayerState<T>(playerStatePair.Key));
            }

            if (includeInactive)
            {
                foreach (var playerStatePair in InactivePlayerStateNetworkObjects)
                {
                    T playerState = GetPlayerStateFromNetworkId(playerStatePair.Value) as T;
                    if (playerState)
                    {
                        playerStates.Add(playerState);
                    }
                }
            }

            return playerStates;
        }

        public T GetSettings<T>() where T : GameModeSettings
        {
            return _settings as T;
        }
        
        #endregion
    }
}