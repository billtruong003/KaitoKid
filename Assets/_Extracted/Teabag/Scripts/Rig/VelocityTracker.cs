using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teabag.Player
{
public class VelocityTracker : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 angularVelocity;
    Vector3 lastPosition;
    Quaternion lastRotation;

    private void Awake()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void Update()
    {
        // Velocity
        velocity = (transform.position - lastPosition) / Time.deltaTime;

        // Angular velocity
        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRotation);
        delta.ToAngleAxis(out float magnitude, out Vector3 axis);
        angularVelocity = magnitude * Mathf.Deg2Rad * axis / Time.deltaTime;

        // Store position and rotation for next frame
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
}
