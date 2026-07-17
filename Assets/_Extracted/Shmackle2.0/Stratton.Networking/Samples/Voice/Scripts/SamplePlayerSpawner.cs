#define SHARED
using Fusion;
using UnityEngine;

namespace Stratton.Networking.Voice.Sample
{
#if SHARED
    public class SamplePlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
#else
public class SamplePlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
#endif
    {
        [SerializeField] private NetworkPrefabRef _playerPrefab;
#if !SHARED
    private Dictionary<PlayerRef, NetworkObject> _players = default;
#endif

        public void PlayerJoined(PlayerRef player)
        {
#if SHARED
            if (player == Runner.LocalPlayer)
            {
                Runner.Spawn(_playerPrefab, new Vector3(Random.Range(0, 5), 0, Random.Range(0, 5)), Quaternion.identity, player);
            }
#else
        if(HasStateAuthority)
        {
            NetworkObject playerObject = Runner.Spawn(_playerPrefab, Vector3.up, Quaternion.identity, player);
            _players.Add(player, playerObject);
        }
#endif
        }

        public void PlayerLeft(PlayerRef player)
        {
#if SHARED
#else
        if (HasStateAuthority)
        {
            if(_players.TryGetValue(player, out NetworkObject playerObject))
            {
                _players.Remove(player);
                Runner.Despawn(playerObject);
            }
        }
#endif
        }
    }
}