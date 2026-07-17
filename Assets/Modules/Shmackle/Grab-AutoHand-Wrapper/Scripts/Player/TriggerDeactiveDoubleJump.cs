using DG.Tweening;
using Shmackle;
using UnityEngine;

public class TriggerDeactiveDoubleJump : MonoBehaviour
{
    public Tags tagDetect = Tags.PlayerBody;
    public bool isLocalOnly = true;
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag(tagDetect.ToString()))
        {
            var player = other.GetComponentInParent<ShmacklePlayerController>();
            if (player && (!isLocalOnly || player.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig))
            {
                player.playerAbilities.dontAllowDoubleJump = true;
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.CompareTag(tagDetect.ToString()))
        {
            var player = other.GetComponentInParent<ShmacklePlayerController>();
            if (player && (!isLocalOnly || player.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig))
            {
                player.playerAbilities.dontAllowDoubleJump = false;
            }
        }
    }
}