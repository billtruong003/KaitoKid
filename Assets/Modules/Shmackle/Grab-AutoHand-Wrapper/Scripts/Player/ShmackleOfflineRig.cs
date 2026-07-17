using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShmackleOfflineRig : MonoBehaviour
{
    [SerializeField]private ShmacklePlayerController playerController;
    public GameObject characterIK;
    public GameObject LeftControllerOffline;
    public GameObject RightControllerOffline;

    private void Update()
    {
        transform.position = playerController.transform.position;
        transform.rotation = playerController.transform.rotation;
        
        characterIK.transform.position = playerController.BodyTarget.transform.position;
        characterIK.transform.rotation = playerController.BodyTarget.transform.rotation;
        
        LeftControllerOffline.transform.position = playerController.LeftController.transform.position;
        LeftControllerOffline.transform.rotation = playerController.LeftController.transform.rotation;
        
        RightControllerOffline.transform.position = playerController.RightController.transform.position;
        RightControllerOffline.transform.rotation = playerController.RightController.transform.rotation;
    }
}
