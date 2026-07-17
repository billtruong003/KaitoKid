using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public enum MagnetType
{
    Pull, Push
}

public enum ForceType
{
    CloseStrong, FarStrong, Linear
}

//:D inspired by Synthetic Selection
public class MagnetHover : MonoBehaviour
{
    [FoldoutGroup("Magnet Settings")]
    public bool Activated = true;
    [FoldoutGroup("Magnet Settings")]
    public MagnetType Type;
    [FoldoutGroup("Magnet Settings")]
    public ForceType ForceType;
    [FoldoutGroup("Magnet Settings")]
    public float KeepDistance = 2;
    [FoldoutGroup("Magnet Settings")]
    public float MagnetForce = 10;
    [FoldoutGroup("Magnet Settings")]
    public LayerMask surface;
    [FoldoutGroup("Magnet Settings")]
    public bool CenterMode;
    [FoldoutGroup("Magnet Settings")]
    public bool useNormal = true;

    [FoldoutGroup("Debug and Setup")]
    public bool GizmosDraw = true;
    [FoldoutGroup("Debug and Setup")]
    public bool Hitted;
    [FoldoutGroup("Debug and Setup")]
    [ReadOnly] public float currentDistance;
    [FoldoutGroup("Debug and Setup")]
    public Rigidbody rb;

    private Vector3 hoverSurface;
    private float lastHitDis;
    
    private void FixedUpdate()
    {
        SurfaceHover(-transform.up);
    }

    void SurfaceHover(Vector3 normalSurface)
    {
        if(rb == null) return;

        Vector3 HoverNormal = Vector3.zero;
        Vector3 ForceApply = Vector3.zero;
        RaycastHit hit;
        hoverSurface = normalSurface;
        float DistancePercentage = 0;
        if (Physics.Raycast(transform.position, hoverSurface, out hit, KeepDistance, surface))
        {
            //Debug
            currentDistance = Vector3.Distance(transform.position,hit.point);

            switch (ForceType)
            {
                case ForceType.FarStrong:
                    DistancePercentage = (hit.distance / KeepDistance); // far strong
                    break;
                case ForceType.CloseStrong:
                    DistancePercentage = 1 - (hit.distance / KeepDistance); //close stronger
                    break;
                case ForceType.Linear:
                    DistancePercentage = 1; //Linear
                    break;
            }
            
            if(useNormal) HoverNormal = hit.normal;
            else HoverNormal = hoverSurface;
        }
        else
        {
            //Debug
            currentDistance = Vector3.Distance(transform.position,hit.point);

            switch (ForceType)
            {
                case ForceType.FarStrong:
                    DistancePercentage = 1; // far strong
                    break;
                case ForceType.CloseStrong:
                    DistancePercentage = 0; //close stronger
                    break;
                case ForceType.Linear:
                    DistancePercentage = 1; //Linear
                    break;
            }
            
            if(useNormal) HoverNormal = hit.normal;
            else HoverNormal = hoverSurface;
        }

        if (Activated)
        {
            switch (Type)
            {
                case MagnetType.Pull:
                    ForceApply = -HoverNormal * (MagnetForce * DistancePercentage);
                    break;
                case MagnetType.Push:
                    ForceApply = HoverNormal * (MagnetForce * DistancePercentage);
                    break;
            }

            ForceApply *= Time.fixedDeltaTime;
        
            if(CenterMode)
                rb.AddForceAtPosition(ForceApply, rb.centerOfMass);
            else
                rb.AddForceAtPosition(ForceApply, transform.position);
        }
    }
    void OnDrawGizmos()
    {
        if(!GizmosDraw) return;
        
        RaycastHit hit;
        Hitted = (Physics.Raycast(transform.position, hoverSurface, out hit, KeepDistance, surface));

        if (!Hitted)
        {
            switch (Type)
            {
                case MagnetType.Pull:
                    Gizmos.color = Color.cyan; // Set color to light blue if hit
                    break;
                case MagnetType.Push:
                    Gizmos.color = Color.green; // Set color to light blue if hit
                    break;
            }
            Gizmos.DrawLine(transform.position, transform.position - (transform.up * KeepDistance)); // Draw a line from the sphere to the hit point
        }
        else
        {
            Gizmos.color = Color.red; // Set color to red if no hit
            Gizmos.DrawLine(transform.position, transform.position - (transform.up * KeepDistance)); // Draw a line from the sphere to the hit point
        }
    }
}
