using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TPSFixed : MonoBehaviour
{
    public Transform target; // The player's transform

    [FoldoutGroup("Camera Settings")]
    public float mouseSensitivity = 2.0f;
    [FoldoutGroup("Camera Settings")]
    public bool Smooth = true;
    [FoldoutGroup("Camera Settings")]
    public float smooth = 5.0f;
    [FoldoutGroup("Camera Settings")]
    public float MinYAngle = 5.0f;
    [FoldoutGroup("Camera Settings")]
    public float MaxYAngle = 10.0f;
    [FoldoutGroup("Camera Settings")]
    public float ZoomMinYAngle = 5.0f;
    [FoldoutGroup("Camera Settings")]
    public float ZoomMaxYAngle = 10.0f;
    [FoldoutGroup("Camera Settings")]
    public LayerMask ignoreLayer;

    [FoldoutGroup("Angle and Distance Offset")]
    public Transform CamPos;
    [FoldoutGroup("Angle and Distance Offset")]
    public float distance = 4.0f; // max Distance from the player
    [FoldoutGroup("Angle and Distance Offset")]
    public float zoomDistance = 10.0f; // max Distance from the player
    
    [FoldoutGroup("Debug")]
    public float currentX, currentY;
    float defaultMinYAngle, defaultMaxYAngle, defaultDistance;

    public static TPSFixed Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }

        defaultMinYAngle = MinYAngle;
        defaultMaxYAngle = MaxYAngle;
        defaultDistance = distance;
    }
    void Update()
    {
        CameraView();
    }
    
    
    void CameraView()
    {
        if (target != null)
        {
            currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
            currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentY = Mathf.Clamp(currentY, MinYAngle, MaxYAngle);
            
            //Calculate pos and rot
            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0); //orbit test
            //Vector3 direction = rotation * -Vector3.forward; // orbit test
            //Vector3 desiredPosition = target.position + direction * distance; // orbit test
            
            Vector3 direction = CamPos.position - target.position;
            Vector3 desiredPosition = target.position + direction.normalized * distance;
            Quaternion desiredRotation = CamPos.rotation * Quaternion.Euler(currentY, 0, 0);

            // Check collision
            RaycastHit hit;
            float sphereRadius = 0.1f; // Radius of the sphere cast
            Vector3 sphereCastDirection = desiredPosition - target.position; // Direction from target to desired position

            // Check collider, hit? cool, pos is sphere center now
            if (Physics.SphereCast(target.position, sphereRadius, sphereCastDirection.normalized, out hit, sphereCastDirection.magnitude, ~ignoreLayer))
            {
                Debug.DrawRay(target.transform.position, sphereCastDirection, Color.white);
                Debug.Log("hit: " + hit.transform.name);

                // Set the desired position to the center of the sphere
                desiredPosition = hit.point + (sphereCastDirection.normalized * sphereRadius);
            }
            
            if (Smooth)
            {
                // Smoothly interpolate the position and rotation
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smooth);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * smooth);
            }
            else
            {
                transform.position = desiredPosition;
                transform.rotation = desiredRotation;
            }

            if (currentX > 0.00025 || currentX < -0.00025)
                currentX = Mathf.Lerp(currentX, 0, Time.deltaTime * 5f);
            else
                currentX = 0;
        }
    }

    public void ZoomDefault(float duration)
    {
        StartCoroutine(Zoom(defaultDistance, defaultMinYAngle, defaultMaxYAngle, duration));
    }
    public void ZoomOut(float duration)
    {
        StartCoroutine(Zoom(zoomDistance, ZoomMinYAngle, ZoomMaxYAngle, duration));
        StartCoroutine(ResetOffsetValue(0.15f));
    }
    IEnumerator Zoom(float targetDistance, float targetMinYAngle, float targetMaxYAngle, float duration)
    {
        float elapsed = 0f;
        Vector3 currentVelocity = Vector3.zero;
        float startDistance = distance;
        float startMinYAngle = MinYAngle;
        float startMaxYAngle = MaxYAngle;

        while (elapsed < duration)
        {
            distance = Mathf.SmoothDamp(distance, targetDistance, ref currentVelocity.x, duration);
            MinYAngle = Mathf.SmoothDamp(MinYAngle, targetMinYAngle, ref currentVelocity.y, duration);
            MaxYAngle = Mathf.SmoothDamp(MaxYAngle, targetMaxYAngle, ref currentVelocity.z, duration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        distance = targetDistance;
        MinYAngle = targetMinYAngle;
        MaxYAngle = targetMaxYAngle;
    }
    IEnumerator ResetOffsetValue(float duration)
    {
        float elapsed = 0f;
        float targetMaxYAngle = 0f;
        float currentVelocity = 0;
        while (elapsed < duration)
        {
            currentY = Mathf.SmoothDamp(currentY, targetMaxYAngle, ref currentVelocity, duration);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentY = 0;
    }

}