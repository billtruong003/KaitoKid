using DG.Tweening;
using Shmackle;
using UnityEngine;

public class TriggerTeleportPlayer : MonoBehaviour
{
    public string tagDetect = "PlayerBody";
    public Transform teleportPoint;
    public ShmacklePlayerController player;
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag(tagDetect))
        {
            player = other.GetComponentInParent<ShmacklePlayerController>();
            if (player)
            {
                player.physicsRig.isTeleporting = true;
                
                player.transform.position = teleportPoint.position;
                player.transform.rotation = teleportPoint.rotation;
                
                DOVirtual.DelayedCall(1, () =>
                {
                    player.physicsRig.isTeleporting = false;
                });
            }
        }
    }
}
