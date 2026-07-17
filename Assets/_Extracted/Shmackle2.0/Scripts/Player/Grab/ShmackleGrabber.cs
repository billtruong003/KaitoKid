using System.Collections.Generic;
using Fusion.XR.Shared.Core;
using Player.Config;
using UnityEngine;

namespace Shmackle.Player.Grab
{
    /// <summary>
    /// Handles grabbing logic for the player's hand, managing hover detection,
    /// grab/release actions, and interaction with ShmackleGrabbable objects.
    /// </summary>
    public class ShmackleGrabber : MonoBehaviour, IGrabber, IHapticConsumer
    {
        public IGrabbingProvider RigPart { get; set; }

        public bool IsGrabbing => RigPart != null && RigPart.IsGrabbing;

        public ShmackleGrabbable GrabbedObject { get; private set; }

        /// <summary>
        /// Gets the NetworkGrabber component for network synchronization.
        /// Caches the result to avoid repeated component lookups.
        /// </summary>
        public ShmackleNetworkGrabber NetworkGrabber
        {
            get
            {
                if (_networkGrabber == null)
                {
                    _networkGrabber = RigPart.LocalUserNetworkRigPart?.gameObject.GetComponentInChildren<ShmackleNetworkGrabber>(true);
                }

                if (_networkGrabber == null)
                {
                    Debug.LogError($"No NetworkGrabber for {RigPart}");
                }

                return _networkGrabber;
            }
        }

        // List of grabbable objects currently in trigger range
        public List<ShmackleGrabbable> HoveredGrabbables = new List<ShmackleGrabbable>();

        // Throw configuration values
        public float VerticalForce => _verticalForce;
        public float HorizontalForce => _horizontalForce;
        public float ThrowPower => _throwPower;

        [SerializeField] private ThrowingConfig _throwingConfig;

        // Throw physics parameters
        private float _verticalForce;
        private float _horizontalForce;

        private float _throwPower;

        // Previous values so we know which slider the user actually moved
        private float _previousThrowPreset;
        private float _previousVerticalForceSlider;
        private float _previousHorizontalForceSlider;

        private ShmackleNetworkGrabber _networkGrabber;
        private Collider _lastCheckedCollider;
        private ShmackleGrabbable _lastCheckColliderGrabbable;

        // Prevents immediate re-grabbing of the just-released object
        private ShmackleGrabbable _lastUngrabbedObject;

        private Dictionary<Collider, ShmackleGrabbable> _hoveredGrabbableByColliders = new Dictionary<Collider, ShmackleGrabbable>();

        /// <summary>
        /// Attempts to grab the specified object and triggers haptic feedback.
        /// </summary>
        public void TryGrab(ShmackleGrabbable grabbable)
        {
            if (grabbable == null) return;

            grabbable.TryGrab(this);
            GrabbedObject = grabbable;

            // Trigger haptic feedback if available
            if (RigPart.RelatedLocalHardwareRigPart() is IHapticFeedbackProviderRigPart hardwareRigPart)
            {
                HapticFeedback(hardwareRigPart);
            }
        }

        /// <summary>
        /// Releases the currently grabbed object and triggers haptic feedback.
        /// Prevents immediate re-grab by tracking the last ungrabbed object.
        /// </summary>
        public void TryRelease()
        {
            if (GrabbedObject == null) return;

            // Track to prevent immediate re-grab on same frame
            _lastUngrabbedObject = GrabbedObject;
            GrabbedObject.TryRelease();
            GrabbedObject = null;

            // Trigger haptic feedback if available
            if (RigPart.RelatedLocalHardwareRigPart() is IHapticFeedbackProviderRigPart hardwareRigPart)
            {
                HapticFeedback(hardwareRigPart);
            }
        }

        /// <summary>
        /// Provides haptic feedback through the controller.
        /// </summary>
        public void HapticFeedback(IHapticFeedbackProviderRigPart hapticFeedbackProviderRigPart) { hapticFeedbackProviderRigPart.SendHapticImpulse(0.5f, 0.1f); }

        private void Awake()
        {
            RigPart = GetComponentInParent<IGrabbingProvider>();
            if (RigPart == null) Debug.LogError("Grabber should be placed next to an IGrabbingProviderHardwareRigPart");

            if (GetComponentInParent<Rigidbody>() == null)
                Debug.LogError("A rigid body (and a trigger collider) is required for the Grabber to be sure to trigger the OnTrigger callback");

            ConfigUpdate();
        }

        private void OnEnable()
        {
            if (!_throwingConfig)
                return;

            _throwingConfig.ConfigUpdated += ConfigUpdate;
        }

        private void OnDisable()
        {
            if (!_throwingConfig)
                return;

            _throwingConfig.ConfigUpdated -= ConfigUpdate;
        }

        /// <summary>
        /// Updates locomotion configuration values
        /// </summary>
        private void ConfigUpdate()
        {
            if (!_throwingConfig)
                return;

            // Read current config values
            int throwPresets = _throwingConfig.ThrowPresets;
            float sliderVertical = _throwingConfig.VerticalForce;
            float sliderHorizontal = _throwingConfig.HorizontalForce;

            // Detect slider overrides
            bool presetChanged = !Mathf.Approximately(throwPresets, _previousThrowPreset);
            bool verticalChanged = !Mathf.Approximately(sliderVertical, _previousVerticalForceSlider);
            bool horizontalChanged = !Mathf.Approximately(sliderHorizontal, _previousHorizontalForceSlider);

            if (presetChanged)
            {
                switch (throwPresets)
                {
                    case 1: // No throw / Default preset
                        _verticalForce = 0f;
                        _horizontalForce = 0f;
                        _throwPower = 1f;
                        break;

                    case 2: // Light throw preset - gentle arc
                        _verticalForce = 0.13f; // Minimal upward force for slight arc
                        _horizontalForce = 0.3f; // Low forward force
                        _throwPower = 1f;
                        break;

                    case 3: // Strong throw preset - powerful trajectory
                        _verticalForce = 0.13f; // Same vertical as light (arc shape)
                        _horizontalForce = 1f; // Maximum forward force for distance
                        _throwPower = 1f;
                        break;
                }
            }
            
            // Override only if that slider was moved
            if (verticalChanged)
                _verticalForce = sliderVertical;

            if (horizontalChanged)
                _horizontalForce = sliderHorizontal;

            // Save values for the next update
            _previousThrowPreset = throwPresets;
            _previousVerticalForceSlider = sliderVertical;
            _previousHorizontalForceSlider = sliderHorizontal;
        }


        private void Update()
        {
            // Release only when the hand opens
            if (!IsGrabbing)
            {
                _lastUngrabbedObject = null;

                if (GrabbedObject != null)
                    TryRelease();
            }

            CheckHovered();
        }

        /// <summary>
        /// Removes destroyed objects from hover collections.
        /// Uses reusable list to avoid GC allocation from dictionary.Keys enumeration.
        /// </summary>
        private void CheckHovered()
        {
            foreach (var key in _hoveredGrabbableByColliders.Keys)
            {
                if (key == null)
                {
                    _hoveredGrabbableByColliders.Remove(key);
                    break;
                }
            }

            HoveredGrabbables.RemoveAll(grabbable => grabbable == null);
        }

        /// <summary>
        /// Handles trigger stay events to detect grabbable objects and initiate grabs.
        /// Uses caching to minimize expensive GetComponentInParent calls.
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (!enabled)
                return;

            // Don't hover or grab new objects while holding something
            if (GrabbedObject != null)
                return;

            // Use cached lookup to avoid repeated GetComponentInParent calls (expensive)
            ShmackleGrabbable grabbable;

            if (_lastCheckedCollider == other)
            {
                grabbable = _lastCheckColliderGrabbable;
            }
            else
            {
                grabbable = other.GetComponentInParent<ShmackleGrabbable>();
            }

            _lastCheckedCollider = other;
            _lastCheckColliderGrabbable = grabbable;
            if (grabbable != null)
            {
                bool isRegrabbing = _lastUngrabbedObject == grabbable;

                if (IsGrabbing && !isRegrabbing)
                {
                    bool wasHovered = HoveredGrabbables.Contains(grabbable);
                    if (wasHovered || grabbable.AllowedCloseHandGrabing)
                    {
                        TryGrab(grabbable);
                    }
                }
                else
                {
                    if (!HoveredGrabbables.Contains(grabbable))
                    {
                        _hoveredGrabbableByColliders[other] = grabbable;
                        HoveredGrabbables.Add(grabbable);
                    }
                }
            }
        }

        /// <summary>
        /// Handles trigger exit events to remove objects from hover tracking.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (_hoveredGrabbableByColliders.ContainsKey(other))
            {
                if (HoveredGrabbables.Contains(_hoveredGrabbableByColliders[other]))
                {
                    HoveredGrabbables.Remove(_hoveredGrabbableByColliders[other]);
                }

                _hoveredGrabbableByColliders.Remove(other);
            }
        }
    }
}