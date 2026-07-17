using System;
using UnityEngine;

public class BoneRotator : MonoBehaviour
{
    public Transform wrist;
    public Transform forearm;

    [Range(0f, 1f)]
    public float twistWeight = 0.5f; // How much of the wrist twist to apply to the forearm

    private Quaternion initialForearmLocalRotation;
    private Quaternion initialWristLocalRotation;
    
    void Start()
    {
        if (forearm != null) initialForearmLocalRotation = forearm.localRotation;
        if (wrist != null) initialWristLocalRotation = wrist.localRotation;
    }

    void LateUpdate()
    {
        if (wrist == null || forearm == null) return;

        // Calculate delta from initial wrist rotation
        Quaternion wristDelta = Quaternion.Inverse(initialWristLocalRotation) * wrist.localRotation;

        // Extract the twist (around wrist's local axis, e.g., Z)
        wristDelta.ToAngleAxis(out float angle, out Vector3 axis);
        axis = wrist.InverseTransformDirection(axis); // local space

        // Keep only the twist around local Z (adjust axis if your arm's twist is different)
        float twistAngle = Vector3.Dot(axis, Vector3.forward) * angle;

        // Apply a portion of the twist to the forearm
        Quaternion twistRotation = Quaternion.AngleAxis(twistAngle * twistWeight, forearm.forward);
        forearm.localRotation = initialForearmLocalRotation * twistRotation;
    }
}
