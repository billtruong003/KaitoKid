using System.Collections;
using UnityEngine;

public class UnlimitPlayerVelocityArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerHealthSimple player = other.GetComponent<PlayerHealthSimple>();
        if (player && player.playerController)
        {
            Debug.Log("Player is unlimited velocity area");
            //player.playerController.isUnLimitVelocity = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealthSimple player = other.GetComponent<PlayerHealthSimple>();
        if (player && player.playerController)
        {
            StartCoroutine(delayLimitVolocity(player));
        }
    }


    IEnumerator delayLimitVolocity(PlayerHealthSimple  player)
    {
        yield return new WaitForSeconds(3);
        //player.playerController.isUnLimitVelocity = false;
    }
}
