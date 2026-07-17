using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/***
 * 
 * XRInputDevice detects the XR input devices and update the transform with actual position/rotation of the device if shouldSynchDevicePosition boolean is set to true
 * 
 ***/

namespace Fusion.XR.Shared.Rig
{
    public class XRInputDevice : MonoBehaviour
    {
        [Header("Detected input device")]
        [SerializeField] bool isDeviceFound = false;
        public InputDevice device;

        [Header("Synchronisation")]
        public bool shouldSynchDevicePosition = true;

        [Header("Positioning timing")]
        public bool updateOnAfterInputSystemUpdate = true;

        public enum DetectionMode
        {
            Device,
            Pointer
        }
        [Header("Detection mode")]
        public DetectionMode detectionMode = DetectionMode.Device;

        protected virtual InputDeviceCharacteristics DesiredCharacteristics => InputDeviceCharacteristics.TrackedDevice;
        protected bool isUsingOculusPlugin = false;
        const string OcculusDeviceName = "oculus display"; // const string OpenXRPluginDeviceName = "OpenXR Display";

#if ENABLE_INPUT_SYSTEM
        protected void OnEnable()
        {
            UnityEngine.InputSystem.InputSystem.onAfterUpdate += OnAfterInputSystemUpdate;

        }

        protected void OnDisable()
        {
            UnityEngine.InputSystem.InputSystem.onAfterUpdate -= OnAfterInputSystemUpdate;
        }
#endif

        public void OnAfterInputSystemUpdate()
        {
            if (updateOnAfterInputSystemUpdate)
            {
                UpdatePosition();
            }
        }

        public virtual void DetectDevice()
        {
            if (isDeviceFound) return;
            foreach (var d in DeviceLookup())
            {
                device = d;
                isDeviceFound = true;
                isUsingOculusPlugin = XRSettings.loadedDeviceName == OcculusDeviceName;
                break;
            }
        }
        public virtual List<InputDevice> DeviceLookup()
        {
            InputDeviceCharacteristics desiredCharacteristics = DesiredCharacteristics;
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, devices);
            return devices;
        }

        public Vector3 GetAdaptedPosition()
        {
            DetectDevice();
            var positionFeature = detectionMode == DetectionMode.Device ? CommonUsages.devicePosition : new InputFeatureUsage<Vector3>("PointerPosition");
            if (isDeviceFound && device.TryGetFeatureValue(positionFeature, out var position))
            {
                return AdaptPosition(position);
            }
            return Vector3.zero;
        }

        public Quaternion GetAdaptedRotation()
        {
            DetectDevice();
            var rotationFeature = detectionMode == DetectionMode.Device ? CommonUsages.deviceRotation : new InputFeatureUsage<Quaternion>("PointerRotation");
            if (isDeviceFound && device.TryGetFeatureValue(rotationFeature, out var rotation))
            {
                return AdaptRotation(rotation);
            }
            return Quaternion.identity;
        }

        public void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            transform.localPosition = position;
            transform.localRotation = rotation;
        }

        protected virtual void Update()
        {
            UpdatePosition();
        }

        protected void UpdatePosition()
        {
            if (shouldSynchDevicePosition)
            {
                var position = GetAdaptedPosition();
                var rotation = GetAdaptedRotation();
                UpdateTransform(position, rotation);
            }
        }

        protected virtual Vector3 AdaptPosition(Vector3 pos)
        {
            return pos;
        }

        protected virtual Quaternion AdaptRotation(Quaternion rot)
        {
            return rot;
        }
    }
}
