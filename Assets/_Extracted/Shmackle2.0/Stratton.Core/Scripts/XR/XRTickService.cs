using MessagePipe;
using Stratton.Core;
using System.Collections.Generic;
using Unity.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace Stratton.XR
{
    /// <summary>
    /// Sets the display refresh rate on the current XR device, and the FixedUpdate rate to match refresh rate of the display.
    /// </summary>
    /// <remarks>
    /// Only works on Meta Quest and when using the Oculus XR Plugin.
    /// </remarks>
    public class XRTickService : AppServiceBase
    {
        const float MINIMUM_PHYSICS_TICK_RATE = 30f;

        #region Fields
        /// <summary>
        /// When true, <see cref="_overridePhysicsTickRate"/> will be used instead of <see cref="_currentRefreshRate"/> to set <see cref="Time.fixedDeltaTime"/>.
        /// </summary>
        [SerializeField, Tooltip("When true, " + nameof(_overridePhysicsTickRate) + " will be used to set " + nameof(Time.fixedDeltaTime))]
        private bool _manualOverride;

        [SerializeField, EnableIf(nameof(IsTickRateOverridden)), Min(MINIMUM_PHYSICS_TICK_RATE)]
        [Tooltip("Tick Rate in Hz")]
        [InfoBox("This separates Time.fixedDeltaTime from the display refresh rate, which might cause visible stuttering. Use with caution.", EInfoBoxType.Warning)]
        private float _overridePhysicsTickRate = 90f;

        [InfoBox("When running in Editor, display refresh rate is always controlled by the desktops Link software.")]
        [SerializeField, Tooltip("To avoid stuttering, we must have a stable framerate of this same value. Targeting 120 fps on VR is tough. ")]
        #if UNITY_EDITOR
        [NaughtyAttributes.DisableIf(nameof(IsInPlaymode))]
        #endif
        private DisplayRefreshRate _targetDisplayRefreshRate = DisplayRefreshRate._90Hz;

        public enum DisplayRefreshRate { _72Hz = 72, _80Hz = 80, _90Hz = 90, _120Hz = 120 } //Rates supported by Meta Quest 2. 

        private readonly List<XRDisplaySubsystem> _displaySubsystems = new List<XRDisplaySubsystem>();

        private float _currentRefreshRate = 0;
        private NativeArray<float> _availableRefreshRates = new NativeArray<float>();

        /// <summary>
        /// Prevents spamming the warning.
        /// </summary>
        private bool _warnedAboutMultipleSubsystems;

        #endregion

        #region Unity Methods

#if !UNITY_EDITOR   // The editor manages the FPS
        private void Awake()
        {
            SetDisplayRefreshRate();
        }
#endif

        private void Update()
        {
            CheckCurrentRefreshRate();
            SetFixedDeltaTime(_manualOverride ? _overridePhysicsTickRate : _currentRefreshRate);
        }
        #endregion

        #region Public Methods
        public float GetCurrentDeviceRefereshRate() => _currentRefreshRate;

        /// <summary>
        /// This separates Time.fixedDeltaTime from the display refresh rate, which might cause visible stuttering. Use with caution.
        /// </summary>
        /// <param name="newTickRate">Tick Rate in Hz, <see cref="Time.fixedDeltaTime"/> is set to 1 / <paramref name="newTickRate"/>.</param>
        public void OverridePhysicsTickRate(float newTickRate)
        {
            if (newTickRate < MINIMUM_PHYSICS_TICK_RATE)
            {
                Log.Error(BaseLogChannel.Core, $"{nameof(newTickRate)} must be higher or equal to {MINIMUM_PHYSICS_TICK_RATE}!");
                return;
            }
            _manualOverride = true;
            _overridePhysicsTickRate = newTickRate;
        }

        public void DisableOverridePhysicsTickRate()
        {
            _manualOverride = false;
        }

        public bool TrySetRefreshRate(DisplayRefreshRate displayRefreshRate)
        {
            return RequestRefreshRateIfSupported(GetXRDisplaySubsystem(), (float)displayRefreshRate);   //TODO: cache displaySubsystem?
        }

        public override void InstallMessageBrokers(BuiltinContainerBuilder builtinContainerBuilder)
        {
        }

        #endregion

        #region Private Methods
#if UNITY_EDITOR
        private bool IsInPlaymode() => UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#endif
        private bool IsTickRateOverridden() => _manualOverride;

        private void SetDisplayRefreshRate()
        {
            var displaySubsystem = GetXRDisplaySubsystem();
            RequestRefreshRateIfSupported(displaySubsystem, (float)_targetDisplayRefreshRate);
        }

        private XRDisplaySubsystem GetXRDisplaySubsystem()
        {
            var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
            if (activeLoader == null)
            {
#if UNITY_EDITOR
                Log.Warning(BaseLogChannel.Core, $"No XR loader found (probably Play Mode without Headset connected");
#else
                Log.Error(BaseLogChannel.Core, $"No XR loader found");
#endif
                enabled = false;
                return null;
            }

            return activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
        }

        private bool TryRequestDisplayRefreshRate(XRDisplaySubsystem displaySubsystem, float refreshRate)
        {
            if (!displaySubsystem.TryRequestDisplayRefreshRate(refreshRate))
            {
                Log.Error(BaseLogChannel.Core, $"Failed to change the display refresh rate");
                return false;
            }

            Log.Message(BaseLogChannel.Core, $"Set the display refresh rate to {refreshRate} Hz.");
            _currentRefreshRate = refreshRate;
            return true;
        }

        private bool RequestRefreshRateIfSupported(XRDisplaySubsystem displaySubsystem, float refreshRate)
        {
            if (!displaySubsystem.TryGetSupportedDisplayRefreshRates(Allocator.Temp, out _availableRefreshRates))   //TODO: cache these
            {
                Log.Error(BaseLogChannel.Core, "Failed to get the available display refresh rates on this device");
                return false;
            }

            var targetFPSIsAvailable = false;
            float selectedDisplayRefreshRate = refreshRate;
            foreach (var rate in _availableRefreshRates)
            {
                if (Mathf.Approximately(rate, (float)_targetDisplayRefreshRate))
                {
                    targetFPSIsAvailable = true;
                    selectedDisplayRefreshRate = rate;
                    break;
                }
            }

            if (!targetFPSIsAvailable)
            {
                Log.Error(BaseLogChannel.Core, $"The requested display refresh rate ({selectedDisplayRefreshRate}) is not available on the device");
                return false;
            }

            return TryRequestDisplayRefreshRate(displaySubsystem, selectedDisplayRefreshRate);
        }

        private void CheckCurrentRefreshRate()
        {
            SubsystemManager.GetSubsystems(_displaySubsystems); //TODO: replace with GetActiveLoader? Or is there any benefit to the extra checks?
            var refreshRateDetected = false;

            foreach (var displaySubsystem in _displaySubsystems)
            {
                if (!displaySubsystem.running) return;

                if (displaySubsystem.TryGetDisplayRefreshRate(out var refreshRate) && refreshRate > 0f)
                {
                    if (refreshRateDetected) // Have we already found a device with a valid refresh rate?
                    {
                        if (!_warnedAboutMultipleSubsystems)
                        {
                            _warnedAboutMultipleSubsystems = true;
                            Log.Warning(BaseLogChannel.Core, $"Found more than 1 {nameof(XRDisplaySubsystem)}, detected refresh rate ");
                        }
                        break;
                    }
                    refreshRateDetected = true;
                    _currentRefreshRate = refreshRate;
                }
            }

#if !UNITY_EDITOR
            if (!refreshRateDetected)
                Log.Error(BaseLogChannel.Core, $"No Display Refresh Rate detected!", this);
#endif
        }

        /// <summary>
        /// Sets FixedUpdate rate to match the <paramref name="tickRate"/>.
        /// Setting <see cref="Time.fixedDeltaTime"/> to match the current displays refresh rate (<see cref="_currentRefreshRate"/>) is the simplest and most reliable way to ensure smoothness and responsiveness in an XR app.
        /// </summary>
        /// <param name="tickRate"> <see cref="Time.fixedDeltaTime"/> is set to 1 / <paramref name="tickRate"/>. </param>

        private void SetFixedDeltaTime(float tickRate)
        {
            if (tickRate == 0) return;
            var newFixedDeltaTime = 1f / tickRate;
            if (Mathf.Approximately(newFixedDeltaTime, Time.fixedDeltaTime)) return;    //NOTE (KC): if we find replication is badly affected by differences in fixedDeltaTime, we can limit the changes to a ceratain range, like 0.5 Hz. 
            Time.fixedDeltaTime = newFixedDeltaTime;
        }
        #endregion
    }
}