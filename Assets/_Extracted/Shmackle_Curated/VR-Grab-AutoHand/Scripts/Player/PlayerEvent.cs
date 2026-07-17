using Shmackle.Minigames.PropHunter;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerEvent : MonoBehaviour
{
    public void OnTogglePlayerProp(bool isProp)
    {
        var networkRig = ShmackleGameManager.Instance?.playerNetworkRig;
        if (networkRig == null)
        {
            Debug.LogError($"networkRig is null!");
            return;
        }
        
        networkRig.RPC_EnablePropController(isProp);
    }
}