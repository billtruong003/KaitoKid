using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerFallingPath : MonoBehaviour
{
    public Transform startPoint;
    public Transform destinationPoint;
    public float duration;
    
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerBody"))
        {
            ShmacklePlayerController player = other.GetComponentInParent<ShmacklePlayerController>();
            if (player && player.enabled)
            {
                player.playerRigidbody.isKinematic = true;
                //player.transform.position = startPoint.position;
                player.transform.DOMove(destinationPoint.position , duration).SetEase(Ease.Linear).OnComplete(() =>
                {
                    player.playerRigidbody.isKinematic = false;
                });
            }
        }
    }
}
