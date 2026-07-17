using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class FixedCameraHeight : MonoBehaviour
{
    public float fixedHeight = 1.75f;  // Set your desired fixed height (in meters)

    private XROrigin xrOrigin;

    void Start()
    {
        xrOrigin = GetComponent<XROrigin>();
    }

    void Update()
    {
        if (xrOrigin && xrOrigin.Camera)
        {
            // Lock the camera Y position to the fixed height
            Vector3 cameraPosition = xrOrigin.Camera.transform.localPosition;
            cameraPosition.y = fixedHeight;
            xrOrigin.Camera.transform.localPosition = cameraPosition;
        }
    }
}
