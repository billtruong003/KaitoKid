using Teabag.Networking;
using Squido.JungleXRKit.Core;
using UnityEngine;

public class BunnyHole : MonoBehaviour
{
    public int requiredBunnyCount = 3;
    public Bunny bunny;
    public Transform spawnPoint;
    public INetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = ServiceLocator.Get<INetworkManager>();
            }
            return _networkManager;
        }
    }
    private  INetworkManager _networkManager;
    float lastSpawn;

    private void FixedUpdate()
    {
        if (Bunny.Bunnies.Count < requiredBunnyCount)
        {
            if (Time.time - lastSpawn < 3)
                return;

            NetworkManager.Runner.SpawnAsync(bunny, spawnPoint.position);
        }

        lastSpawn = Time.time;
    }
}
