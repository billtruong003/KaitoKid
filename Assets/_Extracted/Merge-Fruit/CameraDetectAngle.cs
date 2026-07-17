using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDetectAngle : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject wall1;
    [SerializeField] private GameObject wall2;
    float cameraWidth;
    float cameraHeight;

    void Start()
    {
        cameraWidth = mainCamera.orthographicSize * 2 * mainCamera.aspect;
        cameraHeight = mainCamera.orthographicSize * 2;

        if (wall1 == null || wall2 == null)
            return;

        float halfWidth = cameraWidth / 2;
        wall1.transform.position = new Vector3(-halfWidth - 1, 0, 0);
        wall2.transform.position = new Vector3(halfWidth + 1, 0, 0);
    }
}
