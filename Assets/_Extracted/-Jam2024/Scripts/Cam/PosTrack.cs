using UnityEngine;

public class PosTrack : MonoBehaviour
{
    public Transform target; // The target to follow
    public Vector3 positionOffset; // Offset from the target's local position
    public Vector3 rotationOffset; // Rotation offset to apply
    public float smoothScale = 1.0f; // Scale for the smooth movement

    private void LateUpdate()
    {
        if (target != null)
        {
            // Calculate the desired position based on the target's position and the offset
            Vector3 desiredPosition = target.position + target.TransformDirection(positionOffset);
            // Calculate the time-based smooth speed
            float positionSmoothSpeed = Time.deltaTime * smoothScale;
            // Smoothly interpolate the position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed);

            // Calculate the desired rotation based on the target's rotation and the offset
            Quaternion desiredRotation = target.rotation * Quaternion.Euler(rotationOffset);
            // Smoothly interpolate the rotation
            float rotationSmoothSpeed = Time.deltaTime * smoothScale;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed);
        }
    }
}