using Stratton.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.Extensions.PerformanceSettings;
using UnityEngine.XR.Hands;
using Unity.Profiling;
using System.Text;
using Fusion;
using Stratton.Networking;
using Log = Stratton.Core.Log;
using Fusion.Statistics;
using Shmackle.Logging;
using Shmackle.Player.DoubleJump;
using Stratton.CI;
using TMPro;

namespace Shmackle.Debugging
{
    /// <summary>
    /// IMPORTANT NOTES: 
    /// Due to several different XR APIs (and even in some cases within the same API), there are lots of ways of reading the same value or doing the same thing.
    /// Some things can only be done on certain APIs. You can only use 1 XR plug-in/provider at a time. 
    /// At the moment of writing (23-09-2025) we use the OpenXR plug-in, this is the only one that can be used. 
    /// This means that any api from "Unity.XR.Oculus" will not work. 
    /// This also means we can't access Meta specific features (OVRPlugin / OVRManager), such as reading the CPU and GPU Levels <see cref="CPU_GPULevels"/>. 
    /// </summary>
    public class DebugRuntimeInfo : MonoBehaviour
    {
        const int B_TO_MB = 1024 * 1024;

        [SerializeField]
        private DebugRuntimeData _debugRuntimeData;
        
        [SerializeField]
        private Canvas _dynamicCanvas;

        [SerializeField]
        private Canvas _staticCanvas;

        [SerializeField]
        private GameObject _dynamicCanvasPrefab;

        [SerializeField]
        private GameObject _staticCanvasPrefab;

        [SerializeField]
        private float _zOffset = 1.5f;

        private Camera _cam;
        private float _eyeAspectRatio;
        private TMP_Text _staticText;
        private TMP_Text _dynamicText;
        private StringBuilder _dynamicStringBuilder;
        private DoubleJumpController _doubleJumpController;

        private List<XRHandSubsystem> _handSubsystemBuffer;
        private XRHandSubsystem _handSubsystem;
        private XRDisplaySubsystem _displaySubsystem;

        private NetworkRunner _networkRunner;
        private FusionStatisticsManager _networkStatisticsManager;

        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _triangleRecorder;
        private ProfilerRecorder _verticesRecorder;
        private ProfilerRecorder _systemMemoryRecorder;
        private ProfilerRecorder _gcMemoryRecorder;
        private ProfilerRecorder _mainThreadTimeRecorder;
        private ProfilerRecorder _isGroundedRecorder;
        private ProfilerRecorder _isTwoGripDetectedRecorder;
        private ProfilerRecorder _velocityRecorder;

        private int _framesCount;
        private float _framesTime;
        private float _lastFps;

        private string _cpuLevel;
        private string _gpuLevel;

        private bool _wasDoubleJumpUsed = false;

        private void InitCanvases()
        {
            _cam = null;

            if (_dynamicCanvas == null)
                _dynamicCanvas = Instantiate(_dynamicCanvasPrefab, transform).GetComponent<Canvas>();

            if (_staticCanvas == null)
                _staticCanvas = Instantiate(_staticCanvasPrefab, transform).GetComponent<Canvas>();

            _staticText = _staticCanvas.GetComponentInChildren<TMP_Text>();
            _dynamicText = _dynamicCanvas.GetComponentInChildren<TMP_Text>();

            var sb = new StringBuilder(500);

            BuildInfo(sb);
            StaticDeviceInfo(sb);

            _staticText.text = sb.ToString();
        }

        private void Awake()
        {
            _handSubsystemBuffer = new List<XRHandSubsystem>();
            _dynamicStringBuilder = new StringBuilder(500);

            InitDisplay();
            InitCanvases();
        }

        public void EnableDebugInfo() { InitCanvases(); }

        public void DisableDebugInfo()
        {
            _dynamicCanvas.transform.SetParent(transform);
            _staticCanvas.transform.SetParent(transform);
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            XrPerformanceSettingsFeature.OnXrPerformanceChangeNotification += OnPerfChangeNotis;

            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _triangleRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");

            _systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            _gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            _mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        }

        private void OnDisable()
        {
            XrPerformanceSettingsFeature.OnXrPerformanceChangeNotification -= OnPerfChangeNotis;

            _drawCallsRecorder.Dispose();
            _triangleRecorder.Dispose();
            _verticesRecorder.Dispose();
            _systemMemoryRecorder.Dispose();
            _gcMemoryRecorder.Dispose();
            _mainThreadTimeRecorder.Dispose();
        }

        private void Start() { InitDisplay(); }

        private void InitDisplay()
        {
            var loader = GetActiveLoader();
            _displaySubsystem = loader != null ? loader.GetLoadedSubsystem<XRDisplaySubsystem>() : null;
        }

        private void InitNetworkStats()
        {
            if (_networkRunner != null) return;
            var networkSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            if (networkSystem == null) return;
            var runner = networkSystem.Runner;
            if (runner == null) return;
            var success = runner.TryGetFusionStatistics(out var statManager);
            if (success)
            {
                _networkRunner = runner;
                _networkStatisticsManager = statManager;
            }
        }

        private void BuildInfo(StringBuilder sb)
        {
            if (BuildSettings.Instance != null)
            {
                sb.AppendLine($"Version: {BuildSettings.Instance.ReleaseVersion}");
                //sb.AppendLine($"Stage: {BuildSettings.Instance.BuildStage}");
                //sb.AppendLine($"Branch name: {BuildSettings.Instance.RepoBranch}");
                //sb.AppendLine($"Revision: {BuildSettings.Instance.RepoRevision}");
            }
            else
            {
                sb.AppendLine("Version: " + Application.version);
            }
        }

        private void StaticDeviceInfo(StringBuilder sb)
        {
            sb.AppendLine($"OS: {SystemInfo.operatingSystem}");
            StartCoroutine(WaitForDisplaySubSys(() =>
            {
                sb.AppendLine($"Quest Device: {GetQuestModel()}");
                _staticText.text = sb.ToString();
            }));
        }

        private System.Collections.IEnumerator WaitForDisplaySubSys(System.Action runMethod, System.Action timeoutMethod = null)
        {
#if UNITY_EDITOR
            float timeout = 10f;
#else
            float timeout = 5f;
#endif
            float timer = 0f;
            while (timer < timeout)
            {
                if (_displaySubsystem != null && XRSettings.eyeTextureWidth != 0) break;
                InitDisplay();
                yield return null;
                timer += Time.unscaledDeltaTime;
            }

            if (timer >= timeout)
                timeoutMethod?.Invoke();
            else
                runMethod.Invoke();
        }

        private void Update()
        {
            InitNetworkStats();

            _dynamicStringBuilder.Clear();
            //PERF (KC): Seems to be faster to first cast a double to float, than to call double.ToString. 
            DynamicDeviceInfo();
            DynamicGameInfo();
            //HandTrackingStatus(); doesn't seem to be working right..
            DisplayJumpSpeeds();

            _dynamicText.SetText(_dynamicStringBuilder.ToString());
        }

        private void LateUpdate()
        {
            if (_cam != null) return;
            _cam = Camera.main;
            if (_cam == null) return;

            if (_doubleJumpController == null && _cam.transform.parent != null)
            {
                _doubleJumpController = _cam.transform.parent.GetComponent<DoubleJumpController>();
            }

            StartCoroutine(WaitForDisplaySubSys(
                runMethod: () =>
                {
                    float w = UnityEngine.XR.XRSettings.eyeTextureWidth;
                    float h = UnityEngine.XR.XRSettings.eyeTextureHeight;
                    float aspect = w / h;
                    SetupInfoCanvases(aspect);
                }
#if UNITY_EDITOR
                , timeoutMethod: () => SetupInfoCanvases(aspectRatio: 1)
#endif
            ));
        }

        private void SetupInfoCanvases(float aspectRatio)
        {
            var offset = aspectRatio * _zOffset * Vector3.forward;
            if (_dynamicCanvas != null)
            {
                _dynamicCanvas.transform.SetParent(_cam.transform);
                _dynamicCanvas.transform.SetLocalPositionAndRotation(offset, Quaternion.identity);
            }

            if (_staticCanvas != null)
            {
                _staticCanvas.transform.SetParent(_cam.transform);
                _staticCanvas.transform.SetLocalPositionAndRotation(offset, Quaternion.identity);
            }

            if (_dynamicCanvas == null && _staticCanvas == null)
                gameObject.SetActive(false);
        }

        private void DynamicGameInfo()
        {
            PerformanceInfo();

            //var index = QualitySettings.GetQualityLevel();
            //var name = QualitySettings.names[index];
            //_dynamicStringBuilder.AppendLine("Quality Setting: ");
            //_dynamicStringBuilder.Append(name);
            //_dynamicStringBuilder.AppendLine();

            var fixedUpdateRate = 1d / Time.fixedDeltaTime;
            fixedUpdateRate = System.Math.Round(fixedUpdateRate, 2);
            _dynamicStringBuilder.Append("FixedUpdate Rate: ");
            _dynamicStringBuilder.Append(fixedUpdateRate);
            _dynamicStringBuilder.AppendLine(" Hz");

            _dynamicStringBuilder.Append("FixedUpdate Delta: ");
            _dynamicStringBuilder.Append(Time.fixedDeltaTime);
            _dynamicStringBuilder.AppendLine("\n");

            NetworkInfo();
        }

        //TODO (KC): for better and more accurate performance values, see
        //Library\PackageCache\com.unity.render-pipelines.core@609b19816fd2\Runtime\Debugging\DebugDisplayStats.cs
        private void PerformanceInfo()
        {
            _framesCount++;
            _framesTime += Time.unscaledDeltaTime;
            if (_framesTime > 0.5f)
            {
                float fps = _framesCount / _framesTime;
                _lastFps = fps;
                _framesCount = 0;
                _framesTime = 0;
            }

            var roundedFPS = Mathf.Round(_lastFps);
            _dynamicStringBuilder.Append("FPS: ");
            _dynamicStringBuilder.Append(roundedFPS);
            _dynamicStringBuilder.AppendLine();

            var frameTimeRounded = GetRecorderFrameAverage(_mainThreadTimeRecorder) * (1e-6f);
            frameTimeRounded = System.Math.Round(frameTimeRounded, 1);
            //NOTE (KC): Not sure if this is 100% accurate for our platform. 
            _debugRuntimeData.FrameTime = (float)frameTimeRounded;
            _dynamicStringBuilder.Append("Frame Time: ");
            _dynamicStringBuilder.Append(frameTimeRounded >= 12f ? "<color=red>" : "<color=white>");
            _dynamicStringBuilder.Append((float)frameTimeRounded);
            _dynamicStringBuilder.AppendLine(" ms</color>");
            
            _dynamicStringBuilder.Append("DrawCalls: ");
            _dynamicStringBuilder.Append((int)_drawCallsRecorder.LastValue);
            _dynamicStringBuilder.AppendLine();

            var trisRounded = _triangleRecorder.LastValue * 0.001;
            trisRounded = System.Math.Round(trisRounded, 1);
            _dynamicStringBuilder.Append("Tris: ");
            _dynamicStringBuilder.Append((float)trisRounded);
            _dynamicStringBuilder.AppendLine("k");

            var vertsRounded = _verticesRecorder.LastValue * 0.001;
            vertsRounded = System.Math.Round(vertsRounded, 1);
            _dynamicStringBuilder.Append("Verts: ");
            _dynamicStringBuilder.Append((float)vertsRounded);
            _dynamicStringBuilder.AppendLine("k");

            var gcMem = _gcMemoryRecorder.LastValue / B_TO_MB;
            _dynamicStringBuilder.Append("GC Memory: ");
            _dynamicStringBuilder.Append((int)gcMem);
            _dynamicStringBuilder.AppendLine(" MB");

            var sysMem = _systemMemoryRecorder.LastValue / B_TO_MB;
            _dynamicStringBuilder.Append("System Memory: ");
            _dynamicStringBuilder.Append((int)sysMem);
            _dynamicStringBuilder.AppendLine(" MB\n");
        }

        private void DisplayJumpSpeeds()
        {
            if (_doubleJumpController == null)
                return;

            _dynamicStringBuilder.Append("Is Grounded: ");
            _dynamicStringBuilder.Append(_doubleJumpController.IsCurrentlyGrounded);
            _dynamicStringBuilder.AppendLine();
            _dynamicStringBuilder.AppendLine();
            _dynamicStringBuilder.Append("Velocity: ");
            _dynamicStringBuilder.Append(_doubleJumpController.HandVelocity);
            _dynamicStringBuilder.AppendLine();
            _dynamicStringBuilder.Append("Hand Alignment: ");
            _dynamicStringBuilder.Append(_doubleJumpController.HandAlignment);
            _dynamicStringBuilder.AppendLine();
            _dynamicStringBuilder.Append("Hand Speed: ");
            _dynamicStringBuilder.Append(_doubleJumpController.HandSpeed);
            _dynamicStringBuilder.AppendLine();
        }

        private void NetworkInfo()
        {
            if (_networkRunner == null || !_networkRunner.IsRunning) return;

            var rtt = _networkRunner.GetPlayerRtt(_networkRunner.LocalPlayer) * 1000;
            rtt = System.Math.Round(rtt);
            _dynamicStringBuilder.Append("Player RTT: ");
            _dynamicStringBuilder.Append((float)rtt);
            _dynamicStringBuilder.AppendLine(" ms");

            var region = _networkRunner.SessionInfo.Region;
            _dynamicStringBuilder.Append("Region: '");
            _dynamicStringBuilder.Append(region);
            _dynamicStringBuilder.AppendLine("'");

            var roomName = _networkRunner.SessionInfo.Name;
            _dynamicStringBuilder.Append("Room Name: '");
            _dynamicStringBuilder.Append(roomName);
            _dynamicStringBuilder.AppendLine("'");

            //NOTE (KC): Should calculate running average to avoid flickering values in frames between snapshots,
            //probably best to just open the built-in fusion statistics window instead. 
            //if (_networkStatisticsManager == null) return;
            //var snapshot = _networkStatisticsManager.CompleteSnapshot;

            //var snapshotRTT = snapshot.RoundTripTime;
            //_dynamicStringBuilder.AppendLine("RTT: {snapshotRTT * 1000} ms");

            //var inBandwidth = snapshot.InBandwidth;
            //if (inBandwidth == 0)
            //_dynamicStringBuilder.AppendLine("In Bandwidth: {inBandwidth} B");

            //var outBandwidth = snapshot.OutBandwidth;
            //if (outBandwidth == 0)
            //_dynamicStringBuilder.AppendLine("Out Bandwidth: {outBandwidth} B");
        }

        /// <summary>
        /// <see href= "https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html"> Source </see>
        /// </summary>
        /// <returns></returns>
        static private double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[samplesCount];
                recorder.CopyTo(samples, samplesCount);
                for (var i = 0; i < samplesCount; ++i)
                    r += samples[i].Value;
                r /= samplesCount;
            }

            return r;
        }

        private void DynamicDeviceInfo()
        {
            if (_displaySubsystem == null) return;

            CPU_GPULevels();

            var eyeWidth = XRSettings.eyeTextureWidth;
            var eyeHeight = XRSettings.eyeTextureHeight;
            _dynamicStringBuilder.Append("Per Eye Res: ");
            _dynamicStringBuilder.Append(eyeWidth);
            _dynamicStringBuilder.Append("x");
            _dynamicStringBuilder.Append(eyeHeight);
            _dynamicStringBuilder.AppendLine();

            var foveatedRenderingLevel = _displaySubsystem.foveatedRenderingLevel; // 1 = Full strength
            _dynamicStringBuilder.Append("Foveated Render Level: ");
            _dynamicStringBuilder.Append(foveatedRenderingLevel);
            _dynamicStringBuilder.AppendLine();

            //var successGPUFrameTime = UnityEngine.XR.Provider.XRStats.TryGetStat(displaySubsystem, "OpenVR.Display.GPUFrameTime", out var gpuFrameTime);

            _displaySubsystem.TryGetAppGPUTimeLastFrame(out float GPUAppTime);
            GPUAppTime = Mathf.Round(GPUAppTime);
            _dynamicStringBuilder.Append("GPU App Time: ");
            _dynamicStringBuilder.Append(GPUAppTime);
            _dynamicStringBuilder.AppendLine(" ms");

            _displaySubsystem.TryGetDisplayRefreshRate(out float displayRefreshRate);
            var refreshRate = System.Math.Round((double)displayRefreshRate, 2);
            _dynamicStringBuilder.Append("Refresh Rate: ");
            _dynamicStringBuilder.Append((float)refreshRate);
            _dynamicStringBuilder.AppendLine(" Hz");

            //Log.Message(BaseLogChannel.Core, $": {nameof(GPUAppTime)}: '{GPUAppTime}' ");
            //Log.Message(BaseLogChannel.Core, $": {nameof(displayRefreshRate)}: '{displayRefreshRate}' ");
            //Log.Message(BaseLogChannel.Core, $": {nameof(foveatedRenderingLevel)}: '{foveatedRenderingLevel}' ");

            //var GPUAppTime = Unity.XR.Oculus.Stats.AdaptivePerformance.GPUAppTime;   //used by (or it has the same name in) OVR Metrics tool to display framerate
            //var foveatedRenderingLevel = Unity.XR.Oculus.Utils.foveatedRenderingLevel;
            //var adaptivePerformanceScale = Unity.XR.Oculus.Stats.AdaptivePerformance.AdaptivePerformanceScale;
            //var isInPowerSavingMode = Unity.XR.Oculus.Stats.AdaptivePerformance.PowerSavingMode;

            //Log.Message(BaseLogChannel.Core, $": {nameof(GPUAppTime)}: '{GPUAppTime}' ");
            //Log.Message(BaseLogChannel.Core, $": {nameof(isInPowerSavingMode)}: '{isInPowerSavingMode}' ");
            //Log.Message(BaseLogChannel.Core, $": {nameof(foveatedRenderingLevel)}: '{foveatedRenderingLevel}' ");
            //Log.Message(BaseLogChannel.Core, $": {nameof(adaptivePerformanceScale)}: '{adaptivePerformanceScale}' ");
        }

        private void HandTrackingStatus()
        {
            if (_handSubsystem == null)
            {
                SubsystemManager.GetSubsystems(_handSubsystemBuffer);
                for (var i = 0; i < _handSubsystemBuffer.Count; ++i)
                {
                    var availableHandSubsystem = _handSubsystemBuffer[i];
                    if (availableHandSubsystem.running)
                    {
                        _handSubsystem = availableHandSubsystem;
                        break;
                    }
                }
            }

            bool leftTracked = false, rightTracked = false;
            if (_handSubsystem != null)
            {
                leftTracked = _handSubsystem.leftHand.isTracked;
                rightTracked = _handSubsystem.rightHand.isTracked;
            }

            _dynamicStringBuilder.Append("Is Hand Tracked - Left: ");
            _dynamicStringBuilder.Append(leftTracked);
            _dynamicStringBuilder.Append(", Right: ");
            _dynamicStringBuilder.Append(rightTracked);
            _dynamicStringBuilder.AppendLine();
        }


        /// <summary>
        /// Specific event subscriber to OpenXR's 
        /// <see cref="UnityEngine.XR.OpenXR.Features.Extensions.PerformanceSettings.XrPerformanceSettingsFeature.OnXrPerformanceChangeNotification"/>
        /// , used instead of CPU/GPU Levels. 
        /// </summary>
        /// <param name="changeNotification"></param>
        private void OnPerfChangeNotis(PerformanceChangeNotification changeNotification)
        {
            var newLevel = changeNotification.toLevel.ToString();
            switch (changeNotification.domain)
            {
                case PerformanceDomain.Cpu:
                    _cpuLevel = newLevel;
                    break;
                case PerformanceDomain.Gpu:
                    _gpuLevel = newLevel;
                    break;
            }
        }

        /// <summary>
        /// Unavaialble if using OpenXR as XR Provider, use <see cref="OnPerfChangeNotis"/> in OpenXR. 
        /// </summary>
        private void CPU_GPULevels()
        {
            _dynamicStringBuilder.Append("CPU Level: ");
            _dynamicStringBuilder.Append(_cpuLevel);
            _dynamicStringBuilder.AppendLine();

            _dynamicStringBuilder.Append("GPU Level: ");
            _dynamicStringBuilder.Append(_gpuLevel);
            _dynamicStringBuilder.AppendLine();

            //NOTE (KC): we can set performance hints, but cannot read them as levels in OpenXR. 
            // Set power savings hint for CPU and GPU
            //XrPerformanceSettingsFeature.SetPerformanceLevelHint(
            //    PerformanceDomain.Cpu,
            //    PerformanceLevelHint.PowerSavings);
            //XrPerformanceSettingsFeature.SetPerformanceLevelHint(
            //    PerformanceDomain.Gpu,
            //    PerformanceLevelHint.PowerSavings);

            //NOTE (KC): can subscribe to the following to 


            //NOTE (KC): Seems to be no way to get these values from OpenXR, as they are specific to Meta/Quest devices. 
            //TODO (KC): Try switching to the Meta Core SDK then use the api below. 

            //OVRPlugin.suggestedCpuPerfLevel;
            //OVRPlugin.suggestedGpuPerfLevel;
            //OVRPlugin.
            //OVRManager.

            //Unity.XR.Oculus.Performance.TrySetCPULevel()
            //Unity.XR.Oculus.Performance.TrySetGPULevel()
            //Unity.XR.Oculus.Stats.PerfMetrics.
            //Unity.XR.Oculus.Stats.AppMetrics.
            //Unity.XR.Oculus.Stats.AdaptivePerformance.
            //var gpuLevel = Unity.XR.Oculus.Stats.AdaptivePerformance.GPULevel;
            //var cpuLevel = Unity.XR.Oculus.Stats.AdaptivePerformance.CPULevel;


            //OpenXRRuntime.IsExtensionEnabled("XR_META_performance_metrics"); ?

            //Log.Message(BaseLogChannel.Core, $": {nameof(gpuLevel)}: '{gpuLevel}' ");
            //Log.Message(BaseLogChannel.Core, $": {nameof(cpuLevel)}: '{cpuLevel}' ");

            /* CPU & GPU LEVELS Explained

            As an app developer for Meta Quest headsets, you have the ability to change the clock speed of the headset�s CPU and GPU
            while running your app. Increasing the clock speed raises power consumption.
            These controls allow you to decide whether to emphasize long battery life (through low clock speeds) or enhanced features
            (through high clock speeds) in your application.

            To support simultaneous development on headsets with different chipsets, and to ensure applications can receive boosts
            without having to update their builds (such as the 7% GPU boost for GPU level 4 in v49 OS), we expose a system of CPU and GPU levels
            to select from, rather than giving you direct control over clock frequencies.

            Rather than directly setting a CPU or GPU level, you pick a ProcessorPerformanceLevel.
            The operating system will then keep your CPU and GPU levels at the lowest value within a certain range that preserves your application�s framerate.
            This allows your application to automatically adjust for longer battery life during less-intensive scenes.


            ProcessorPerformanceLevel	CPU level range	    GPU level range
                    PowerSavings        0 to 4              0 to 4
                    SustainedLow        2 to 4              1 to 4
                    SustainedHigh       4 to 6*             3 to 5
                    Boost**             4 to 6*             3 to 5

            *The CPU level range when CPU ProcessorPerformanceLevel is set to SustainedHigh depends on your headset generation and settings, as follows:
                Quest 3, Quest 3S                                               4 to 4
                Quest 2, Quest Pro, Quest 3 or 3S with CPU level trading        5 to 5      CPU level trading: https://developers.meta.com/horizon/documentation/unity/po-quest-boost#enabling-cpu-and-gpu-level-trading
                Quest 2 or Quest Pro with dual-core mode                        6 to 6      dual-core mode: https://developers.meta.com/horizon/documentation/unity/po-quest-boost#dual-core-mode

            **The Boost ProcessorPerformanceLevel exists for historical reasons, and behaves exactly the same as SustainedHigh.

            Note: Depending on your headset, some power levels may be inaccessible based on features and settings specified by your application, requiring modifications to your application to access. See your headset�s CPU/GPU level availability section for more details.
            Source: https://developers.meta.com/horizon/documentation/unity/os-cpu-gpu-levels/
             */
        }

        private string GetQuestModel()
        {
            //Interesting thread on this topic, using the "OpenXR" XR provider/plug-in: https://discussions.unity.com/t/openxr-is-it-no-longer-possible-to-get-descriptive-device-names/827343
            //tl;dr it can't be done using the OpenXR sdk alone. The reason is Unity wants to treat its users as babies who don't know any better. 
            //This user sums it up well: https://discussions.unity.com/t/openxr-is-it-no-longer-possible-to-get-descriptive-device-names/827343/67

#if UNITY_EDITOR
            return "Editor / Desktop";
#endif
#pragma warning disable CS0162 // Unreachable code detected

            if (Application.platform != RuntimePlatform.Android
                //|| SystemInfo.deviceModel != "Oculus Quest"
               )
            {
                return "INVALID";
            }

            var build = new AndroidJavaClass("android.os.Build");
            var deviceCode = build.GetStatic<string>("DEVICE");
            switch (deviceCode)
            {
                case "hollywood":
                    return "Quest 2";
                case "eureka":
                    return "Quest 3";
                case "panther":
                    return "Quest 3S";
                default:
                    Log.Warning(BaseLogChannel.Core, $"Unsupported device with codename '{deviceCode}'!");
                    return "INVALID DEVICE";
            }

            //OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();   //requires Meta Core SDK installed, this what we should do. 
            //OVRManager.systemHeadsetType  //requires Meta Core SDK installed

            //Log.Message(BaseLogChannel.Core, "SystemInfo.deviceName: " + SystemInfo.deviceName);   //NOTE (KC): User report that when running this on a standalone VR headset, this may sometimes just be "HMD" or some other generic value. The Android method is probably safer. 

            //var deviceModel = Unity.XR.Oculus.Utils.GetSystemHeadsetType(); //uses the legacy Occulus XR plugin/provider
#pragma warning restore CS0162 // Unreachable code detected
        }

        private XRLoader GetActiveLoader()
        {
            var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
            return activeLoader;
        }
    }
}