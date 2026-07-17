using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _Shmackle.Minigames;
using _Shmackle.Minigames.BloodJman;
using Autohand;
using Autohand.Demo;
using BKPureNature;
using DG.Tweening;
using Fusion;
using Fusion.Sockets;
using Micosmo.SensorToolkit;
using Photon.Voice.Unity;
using Shmackle.Analytics;
using Shmackle.Analytics.Heatmap;
using Shmackle.Data;
using Shmackle.PlayFab;
using Shmackle.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using LegacyTPD = UnityEngine.SpatialTracking.TrackedPoseDriver;
using InputSystemTPD = UnityEngine.InputSystem.XR.TrackedPoseDriver;
using UnityEngine.InputSystem;
#endif

public class ShmacklePlayerController : MonoBehaviour, INetworkRunnerCallbacks, IHitable
{
    private const float UPDATE_COMBAT_TIMER_INTERVAL = 1f;
    private const float GROUND_CHECK_DISTANCE_DEFAULT = 0.5f;

    [Fusion.ReadOnly] public NetworkRunner runner;

    public XROrigin xrOrigin;
    public MinimapController MinimapController;

    public event Action<DirectionNavigatorState> onTakeDamaged;
    public enum RunnerExpectations
    {
        Offline, // For offline usages
        PresetRunner,
        DetectRunner // should not be used in multipeer scenario
    }

    public RunnerExpectations runnerExpectations = RunnerExpectations.DetectRunner;
    bool searchingForRunner = false;

    public bool IsInCombatRoom
    {
        get { return _isInCombatRoom; }
    }

    public bool IsInSafeRoom
    {
        get
        {
            return _isInSafeRoom;
        }
    }

    public float CombatDifficultTimer
    {
        get
        {
            return _combatDifficultTimer;
        }
    }

    public int CombatRoomId
    {
        get
        {
            return _combatRoomId;
        }
    }

    public bool grabbedKey = false;
    public bool isSpawned;

    [FoldoutGroup("References")] public AudioListener audioListener;
    [FoldoutGroup("References")] public PlayerMobileDeviceController playerMobileController;
    [FoldoutGroup("References")] public PlayerMobile playerMobile;
    [FoldoutGroup("References")] public ShmackleCombatUI shmackleCombatUI;
    [FoldoutGroup("References")] public PlayerAbilities playerAbilities;
    [FoldoutGroup("References")] public PlayerHealthSimple playerHealth;
    [FoldoutGroup("References")] public ShmacklePlayerShooting playerShooting;
    [FoldoutGroup("References")] public Rigidbody playerRigidbody;
    [FoldoutGroup("References")] public PlayerInputListener playerInputListener;
    [FoldoutGroup("References")] public PlayerModuleRef playerModuleRef;
    [FoldoutGroup("References")] public ShmackleInputActionManager inputActionManager;
    [FoldoutGroup("References")] public ShmackleKissDetectController kissController;
    [FoldoutGroup("References")] public PropHuntShootingController propHuntShooting;

    [FoldoutGroup("Auto Hands")] public Hand autoHandLeft;
    [FoldoutGroup("Auto Hands")] public Hand autoHandRight;
    [HideInInspector] public Rigidbody autoHandLeftRigidbody;
    [HideInInspector] public Rigidbody autoHandRightRigidbody;


    [FoldoutGroup("Physics Hands")] public ShmackleRaycastLocomotion physicsRig;
    [FoldoutGroup("Physics Hands")] public ShmackleHandController physicsHandLeft;
    [FoldoutGroup("Physics Hands")] public ShmackleHandController physicsHandRight;


    [FoldoutGroup("Local Rig")] public Camera HeadCamera;
    [FoldoutGroup("Local Rig")] public GameObject HeadTarget;
    [FoldoutGroup("Local Rig")] public GameObject BodyTarget;
    [FoldoutGroup("Local Rig")] public GameObject LeftController;
    [FoldoutGroup("Local Rig")] public GameObject RightController;
    [FoldoutGroup("Local Rig")] public Transform leftHandPosition;
    [FoldoutGroup("Local Rig")] public Transform rightHandPosition;

    //[HorizontalLine]
    //[Header("Head Follow")]
    //public Transform headFollower;
    //private Vector3 lastHeadPosition;


    [FoldoutGroup("Body Collider")] public bool updateBodyCollider = true;
    [FoldoutGroup("Body Collider")] public CapsuleCollider bodyCollider;
    [FoldoutGroup("Body Collider")] public SphereCollider headCollider;
    [FoldoutGroup("Body Collider")] public CollisionEventListener headCollisionListener;
    [FoldoutGroup("Body Collider")] public bool isHeadCollision = false;
    [FoldoutGroup("Body Collider")] public Vector3 bodyOffset;
    [FoldoutGroup("Body Collider")] public LayerMask locomotionEnabledLayers;
    [FoldoutGroup("Body Collider")] public float bodyMinRadius = 0.2f;
    private RaycastHit[] bodyHitInfo = new RaycastHit[3];
    private float bodyMaxRadius, bodyLerp = 0.17f;
    private Vector3 bodyOffsetVector;
    private float bodyInitialRadius, bodyInitialHeight;
    private int overlapAttempts;
    private Collider[] overlapColliders = new Collider[10];
    private int bufferCount;

    // Body collider update optimizations
    [SerializeField] private int bodyUpdateEveryNFrames = 2; // reduce per-frame workload
    [SerializeField] private float bodyUpdatePositionThreshold = 0.01f; // meters
    [SerializeField] private float bodyUpdateYawThresholdDegrees = 0.5f; // degrees
    private int _bodyUpdateFrameCounter;
    private Vector3 _lastHeadWorldPosition;
    private float _lastHeadYawDegrees;


    [FoldoutGroup("Detect Ground")] public LayerMask groundLayer; // Set this to the layer your ground objects are on
    [FoldoutGroup("Detect Ground")] public bool isGrounded;
    [FoldoutGroup("Detect Ground")] public float gravityFactor = 11;

    [FoldoutGroup("Audio")] public AudioSource audioSourceAlwaysOn;

    [FoldoutGroup("UI References")] public GameObject notificationPopUp;
    [FoldoutGroup("UI References")] public TextMeshProUGUI notificationText;


    [FoldoutGroup("Fog")][SerializeField] private MeshRenderer fogRenderer;
    [FoldoutGroup("Fog")] public GameObject fogSphere;
    [FoldoutGroup("Fog")] public GameObject blackFadingEffect;
    [FoldoutGroup("Fog")] public GameObject spotLight;


    // [FoldoutGroup("Game Master Content")]public Grabbable shmackleMoneyGun;
    // [FoldoutGroup("Game Master Content")]public GameObject shmackleGunUI;

    [FoldoutGroup("Player Culling")] public bool isUsePlayerCulling;
    [FoldoutGroup("Player Culling")] public LOSSensor playerDetectionSensor;
    [FoldoutGroup("Player Culling")] public GameObject[] optimizeList;

    [FoldoutGroup("Combat")][SerializeField] private float farClipPlaneInCombat = 30;
    [FoldoutGroup("Combat")][SerializeField] private float farClipPlaneOutCombat = 200;
    [FoldoutGroup("Combat")][SerializeField] private float fogFadeDistance = 15.0f;
    [FoldoutGroup("Combat")][SerializeField] private float maxDistance = 5.0f;
    //[SerializeField] private Color32 defaulttFogColor = new Color32(137, 137, 137, 255);

    [FoldoutGroup("ScreenEffect")][SerializeField] public GameObject bloodJmanScreenEffect;
    [FoldoutGroup("VoiceChat")][SerializeField] private Recorder _recorder;
    [FoldoutGroup("FlyMode")][SerializeField] private RelativeFlyController _relativeFlyController;

    private Color32 _curFogColor;
    private bool _isInCombatRoom;
    private bool _isInSafeRoom; // Combat
    private bool _isCountingDificultyTime;
    private int _combatRoomId = 0;
    private Coroutine _fogFadingCoroutine;
    private Vector3 _combatStartPosition;
    private Tween _changeColorTween;
    private float _combatDifficultTimer;
    private float _timerUpdateCombat;
    private float _lastCheckedDistanceRatio;
    private Collider[] _allCollisionCollider;

    private float _currentFreezeTime;
    private float _defaultCameraFarClipPlaneDistance;
    public static event Action<bool> onJoinedOffShore;

    // private void Awake()
    // {
    //     //Application.targetFrameRate = -1;
    //     Application.targetFrameRate = 90;
    //     QualitySettings.vSyncCount = 0; // Disable VSync for manual framerate cap
    //     TrySetBestRefreshRate();
    // }

    public async Task<NetworkRunner> FindRunner()
    {
        while (searchingForRunner) await Task.Delay(10);
        searchingForRunner = true;
        if (runner == null && runnerExpectations != RunnerExpectations.Offline)
        {
            if (runnerExpectations == RunnerExpectations.PresetRunner ||
                NetworkProjectConfig.Global.PeerMode == NetworkProjectConfig.PeerModes.Multiple)
            {
                Debug.LogWarning("Runner has to be set in the inspector to forward the input");
            }
            else
            {
                // Try to detect the runner
                runner = FindObjectOfType<NetworkRunner>(true);
                var searchStart = Time.time;
                while (searchingForRunner && runner == null)
                {
                    if (NetworkRunner.Instances.Count > 0)
                    {
                        runner = NetworkRunner.Instances[0];
                    }

                    if (runner == null)
                    {
                        await System.Threading.Tasks.Task.Delay(10);
                    }
                }
            }
        }

        searchingForRunner = false;
        return runner;
    }

    // Start is called before the first frame update
    protected virtual async void Start()
    {
        //Application.targetFrameRate = TickRate.Resolve(NetworkProjectConfig.Global.Simulation.TickRateSelection).Server;
        //TrySetBestRefreshRate();
        Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate(90);
        await FindRunner();
        if (runner)
        {
            ShmackleGameManager.Instance.shmackleLocalPlayer = this;
            runner.AddCallbacks(this);

            // check enable heatmap tracker
            if (PlayFabServices.Instance.TitleDataConfig.HeatMap)
            {
                HeatmapTracker.instance?.SetTrackedObject(transform);
            }

            fogRenderer.material.SetFloat("_FadeDistance", ShmackleConnectionManager.Instance._connectTarget.FogDistanceDefault);
            playerShooting.SetActiveCombat(ShmackleConnectionManager.Instance._connectTarget.EnableCombatDefault);
        }
        
        _defaultCameraFarClipPlaneDistance = HeadCamera.farClipPlane;
        OnCombatRoomUpdate(false);
        grabbedKey = false;
#if UNITY_EDITOR
        AddDebugComponents();
#endif

        playerAbilities = GetComponent<PlayerAbilities>();
        playerHealth = GetComponentInChildren<PlayerHealthSimple>();

        if (shmackleCombatUI)
        {
            shmackleCombatUI.Init();
        }

        playerInputListener = GetComponent<PlayerInputListener>();


        bodyOffsetVector = new Vector3(0f, -bodyCollider.height / 2f, 0f);
        bodyInitialHeight = bodyCollider.height;
        bodyInitialRadius = bodyCollider.radius;

        //lastHeadPosition = headFollower.transform.position;

        playerRigidbody = GetComponent<Rigidbody>();



        autoHandRightRigidbody = autoHandRight.GetComponent<Rigidbody>();
        autoHandLeftRigidbody = autoHandLeft.GetComponent<Rigidbody>();


        playerHealth.Init(runnerExpectations == RunnerExpectations.Offline);

        if (runnerExpectations == RunnerExpectations.Offline)
        {
            playerModuleRef.shmackleNetworkRig.enabled = false;
            playerAbilities.enabled = false;
            playerMobileController.gameObject.SetActive(false);
            playerHealth.enabled = false;
            fogSphere.gameObject.SetActive(false);
            playerShooting.enabled = false;
        }
        else
        {

            Invoke(nameof(onPlayerSpawn), 1f);
        }

        if (isUsePlayerCulling)
        {
            playerDetectionSensor.OnDetected.AddListener(onPlayerDetection);
            playerDetectionSensor.OnLostDetection.AddListener(onLostPlayerDetection);
        }


        if (ShmackleConnectionManager.Instance.IsOffShore())
        {
            OnCombatRoomUpdate(true);
            onJoinedOffShore?.Invoke(true);
        }
        else
        {
            onJoinedOffShore?.Invoke(false);
        }

        if (ShmackleConnectionManager.Instance.IsBloodJmanMinigame())
        {
            playerAbilities.playerPunchColliderLeft.bloodJmanHand.Active(true);
            playerAbilities.playerPunchColliderRight.bloodJmanHand.Active(true);
        }
        else
        {
            playerAbilities.playerPunchColliderLeft.bloodJmanHand.Active(false);
            playerAbilities.playerPunchColliderRight.bloodJmanHand.Active(false);
        }

        Light playerLight = spotLight.GetComponent<Light>();
        playerLight.intensity = ShmackleConnectionManager.Instance._connectTarget.setSpotLightIntensity;
    }

    private void onPlayerDetection(GameObject arg0, Micosmo.SensorToolkit.Sensor arg1)
    {
        if (arg0 != bodyCollider)
        {
            ShmackleNetworkRig player = arg0.GetComponentInParent<ShmackleNetworkRig>();
            if (player && !player.IsLocalNetworkRig)
            {
                foreach (var obj in player.playerController.optimizeList)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
    
    private void onLostPlayerDetection(GameObject arg0, Micosmo.SensorToolkit.Sensor arg1)
    {
        // Check if destroyed or null
        if (arg0 == null || arg0.Equals(null))
            return;

        if (arg0 != bodyCollider)
        {
            // Try to safely get the player
            var player = arg0.GetComponentInParent<ShmackleNetworkRig>();
            if (player == null || player.Equals(null))
                return;

            // Make sure the player is not the local one and still valid
            if (!player.IsLocalNetworkRig && player.playerController != null)
            {
                foreach (var obj in player.playerController.optimizeList)
                {
                    if (obj != null && obj.Equals(null) == false)
                        obj.SetActive(false);
                }
            }
        }
    }



    void onPlayerSpawn()
    {
        isSpawned = true;
        CheckMasterClient();
    }


    private void OnDestroy()
    {
        if (searchingForRunner) Debug.LogError("Cancel searching for runner in HardwareRig");
        searchingForRunner = false;

        if (runner) runner.RemoveCallbacks(this);
    }

    void TrySetBestRefreshRate()
    {
        float fallbackRate = 90f;

        if (!Unity.XR.Oculus.Performance.TryGetDisplayRefreshRate(out float currentRate))
            return;

        float targetRate = fallbackRate;

        if (Unity.XR.Oculus.Performance.TryGetAvailableDisplayRefreshRates(out var availableRates) && availableRates.Length > 0)
        {
            targetRate = availableRates.Max();
        }

        if (currentRate >= targetRate || !Unity.XR.Oculus.Performance.TrySetDisplayRefreshRate(targetRate))
            return;

        Time.fixedDeltaTime = 1f / targetRate;
        Time.maximumDeltaTime = 1f / targetRate;
    }

    // Update is called once per frame
    void Update()
    {
        //handle update auto hand if this is offline player
        if (runnerExpectations == RunnerExpectations.Offline)
        {
            playerModuleRef.shmackleNetworkRig.transform.position = transform.position;
            playerModuleRef.shmackleNetworkRig.transform.rotation = transform.rotation;

            playerModuleRef.shmackleNetworkRig.leftControllerNetwork.transform.position =
                LeftController.transform.position;
            playerModuleRef.shmackleNetworkRig.leftControllerNetwork.transform.rotation =
                LeftController.transform.rotation;

            playerModuleRef.shmackleNetworkRig.rightControllerNetwork.transform.position =
                RightController.transform.position;
            playerModuleRef.shmackleNetworkRig.rightControllerNetwork.transform.rotation =
                RightController.transform.rotation;

            playerModuleRef.shmackleNetworkRig.characterIK.transform.position = BodyTarget.transform.position;
            playerModuleRef.shmackleNetworkRig.characterIK.transform.rotation = bodyCollider.transform.rotation;

            playerModuleRef.shmackleNetworkRig.headTarget.transform.position = HeadTarget.transform.position;
            playerModuleRef.shmackleNetworkRig.headTarget.transform.rotation = HeadTarget.transform.rotation;
        }

        OnCombatUpdate();

        isGrounded = CheckGrounded(0.5f);
    }

    private void LateUpdate()
    {
        if (playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            if (updateBodyCollider)
            {
                BodyCollider();
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerAbilities.isDoubleJump)
        {
            playerRigidbody.AddForce(new Vector3(0, -gravityFactor, 0), ForceMode.Acceleration);
        }
    }

    public bool CheckGrounded(float groundCheckDistance)
    {
        return Physics.Raycast(bodyCollider.transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    // Checking grounded with distance 
    public bool ManuallyCheckGrounded(float groundCheckDistance)
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    // private void OnCollisionEnter(Collision other)
    // {
    //     if (playerHealth.IsDead)
    //     {
    //         return;
    //     }
    //
    //     // Check if the collided object's layer is in the ground layer mask
    //     if (((1 << other.gameObject.layer) & groundLayer) != 0)
    //     {
    //         // Loop through all contact points to check collision direction
    //         foreach (ContactPoint contact in other.contacts)
    //         {
    //             float groundDistanceCheck = GROUND_CHECK_DISTANCE_DEFAULT;
    //             // If the contact normal points upwards, it's ground contact
    //             if (contact.normal.y > groundDistanceCheck) // Adjust the threshold if needed
    //             {
    //                 isGrounded = true;
    //                 // physicsRig.disableMovement = false;
    //                 // playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    //                 return;
    //             }
    //         }
    //     }
    // }
    //
    // private void OnCollisionExit(Collision other)
    // {
    //     // Check if the collided object's layer is in the target layer mask
    //     if (((1 << other.gameObject.layer) & groundLayer) != 0)
    //     {
    //         isGrounded = false;
    //     }
    // }

    public void checkHeadCollision(bool isCollision)
    {
        if (isCollision)
        {
            isHeadCollision = true;
        }
        else
        {
            isHeadCollision = false;
        }
    }


    //Preventing tall colliders from clipping through low ceilings.
    //Letting players crouch in real life and have their avatar's body update accordingly.
    private int _lastHitInfor = -1;
    private void BodyCollider()
    {
        // Frame skipping to reduce physics calls
        _bodyUpdateFrameCounter++;
        if ((bodyUpdateEveryNFrames > 1) && (_bodyUpdateFrameCounter % bodyUpdateEveryNFrames != 0))
        {
            return;
        }

        // Early-out if head moved/rotated insignificantly since last update
        Transform headTransform = headCollider.transform;
        Vector3 currentHeadPos = headTransform.position;
        float currentHeadYaw = headTransform.eulerAngles.y;
        if (_lastHeadWorldPosition != Vector3.zero)
        {
            float posDelta = (currentHeadPos - _lastHeadWorldPosition).sqrMagnitude;
            float yawDelta = Mathf.DeltaAngle(_lastHeadYawDegrees, currentHeadYaw);
            if (posDelta < (bodyUpdatePositionThreshold * bodyUpdatePositionThreshold) && Mathf.Abs(yawDelta) < bodyUpdateYawThresholdDegrees)
            {
                return;
            }
        }

        if (MaxSphereSizeForNoOverlap(bodyInitialRadius, PositionWithOffset(headCollider.transform, bodyOffset),
                out bodyMaxRadius))
        {
            int hitCount = Physics.SphereCastNonAlloc(
                PositionWithOffset(headCollider.transform, bodyOffset), // origin
                bodyMaxRadius,                                          // radius
                Vector3.down,                                           // direction
                bodyHitInfo,                                             // hit array
                bodyInitialHeight - bodyMaxRadius,                      // max distance
                locomotionEnabledLayers                                 // layer mask
            );

            int hitIndex = 0;
            for (int i = 0; i < hitCount; i++)
            {
                if (bodyHitInfo[i].transform.GetInstanceID() == _lastHitInfor)
                {
                    hitIndex = i;
                }
            }

            bodyCollider.radius = bodyMaxRadius;
            if (hitCount > 0)
            {
                _lastHitInfor = bodyHitInfo[hitIndex].transform.GetInstanceID();
                if (bodyMinRadius > bodyHitInfo[hitIndex].distance + bodyMaxRadius)
                {
                    bodyCollider.height = bodyMinRadius;

                }
                else
                {
                    bodyCollider.height = bodyHitInfo[hitIndex].distance + bodyMaxRadius;

                }
            }
            else
            {
                bodyCollider.height = bodyInitialHeight;
            }
        }

        bodyCollider.height = Mathf.Lerp(bodyCollider.height, bodyInitialHeight, bodyLerp);
        bodyCollider.radius = Mathf.Lerp(bodyCollider.radius, bodyInitialRadius, bodyLerp);

        bodyOffsetVector = Vector3.down * bodyCollider.height / 2f;
        bodyCollider.transform.position = PositionWithOffset(headCollider.transform, bodyOffset) + bodyOffsetVector;
        bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);
    }

    private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector) =>
        transformToModify.position + transformToModify.rotation * offsetVector;

    private bool MaxSphereSizeForNoOverlap(float testRadius, Vector3 checkPosition, out float overlapRadiusTest)
    {
        // Binary search for largest non-overlapping radius in [minRadius, maxRadius]
        float minRadius = testRadius * 0.75f;
        float maxRadius = testRadius;
        float bestRadius = minRadius;

        // Cap iterations to keep cost predictable
        for (int i = 0; i < 8; i++)
        {
            float mid = (minRadius + maxRadius) * 0.5f;
            bufferCount = Physics.OverlapSphereNonAlloc(checkPosition, mid, overlapColliders,
                locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore);

            if (bufferCount <= 0)
            {
                // no overlap, try bigger
                bestRadius = mid;
                minRadius = mid;
            }
            else
            {
                // overlap, try smaller
                maxRadius = mid;
            }
        }

        overlapRadiusTest = bestRadius;
        return bestRadius > 0.0f;
    }



    private void ClearColliderBuffer(ref Collider[] colliders) { }

    public void playRightHandHaptic(float amplitude, float duration)
    {
        //playerInputListener._rightHandDevice.SendHapticImpulse(0, amplitude, duration);
    }

    public void playLeftHandHaptic(float amplitude, float duration)
    {
        //playerInputListener._leftHandDevice.SendHapticImpulse(0, amplitude, duration);
    }


    private void CheckMasterClient()
    {
        var isMasterPlayer = RuntimeUserData.CacheUser.gameMaster;
    }

    public void EnableAllCollisionCollider(bool isEnabled)
    {
        if (_allCollisionCollider == null)
        {
            _allCollisionCollider = this.GetComponentsInChildren<Collider>().Where(x => x.isTrigger == false).ToArray();
        }

        foreach (var collider in _allCollisionCollider)
        {
            collider.enabled = isEnabled;
        }
    }

    public void EnableAutoHand(bool isEnable)
    {
        autoHandLeft.enabled = isEnable;
        autoHandRight.enabled = isEnable;

        EnableOpenXRHandControllerLink(isEnable);
    }
    
    public void EnableOpenXRHandControllerLink(bool isEnable)
    {
        autoHandLeft.GetComponent<OpenXRHandControllerLink>().enabled  = isEnable;
        autoHandRight.GetComponent<OpenXRHandControllerLink>().enabled = isEnable;
    }

    public void ForceReleaseGrab(bool isForceLeftHandRelease = true, bool isForceRightHandRelease = true)
    {
        if (isForceLeftHandRelease)
        {
            autoHandLeft.ForceReleaseGrab();
        }

        if (isForceRightHandRelease)
        {
            autoHandRight.ForceReleaseGrab();
        }
    }
    
    public void ForceStopClimbing(bool isForceLeftHandRelease = true, bool isForceRightHandRelease = true)
    {
        if (isForceLeftHandRelease)
        {
            physicsHandLeft.StopClimbing();
        }
        
        if (isForceRightHandRelease)
        {
            physicsHandRight.StopClimbing();
        }
    }

    #region  Shmackle Money Gun
    //call by shmackle button component in mobile
    // [ContextMenu("Equip Shmackle Gun")]
    // public void EquipShmackleGun()
    // {
    //     autoHandRight.ForceReleaseGrab();
    //
    //     DOVirtual.DelayedCall(0.0f, () =>
    //     {
    //         shmackleMoneyGun.gameObject.SetActive(true);
    //         shmackleMoneyGun.transform.position = autoHandRight.transform.position;
    //         autoHandRight.TryGrab(shmackleMoneyGun);
    //
    //         showShmackleGun(runner, playerModuleRef.shmackleNetworkRig.PlayerRef);
    //     });
    // }

    // [ContextMenu("Unequip Shmackle Gun")]
    // public void ReleaseShmackleGun()
    // {
    //     shmackleMoneyGun.ForceHandsRelease();
    //     shmackleMoneyGun.gameObject.SetActive(false);
    //
    //     playerModuleRef.shmackleNetworkRig.RPC_HideShmackleGun();
    // }
    //
    // //update for users that join after gun appear
    // void showShmackleGun(NetworkRunner runner, PlayerRef player)
    // {
    //     if (shmackleMoneyGun.gameObject.activeInHierarchy)
    //         playerModuleRef.shmackleNetworkRig.RPC_ShowShmackleGun();
    // }

    #endregion

    private void OnDrawGizmos()
    {
        if (bodyCollider != null)
        {
            // Set the Gizmo color
            Gizmos.color = Color.green;

            // Draw the capsule collider using the collider's properties
            DrawCapsuleCollider(bodyCollider);
        }
    }

    private void DrawCapsuleCollider(CapsuleCollider collider)
    {
        // Get the position and rotation
        Vector3 position = collider.transform.position + collider.center;
        Quaternion rotation = collider.transform.rotation;

        // Calculate the radius and height of the capsule
        float radius = collider.radius;
        float height = collider.height;

        // Draw the capsule
        Gizmos.DrawWireSphere(position + rotation * new Vector3(0, height / 2 - radius, 0), radius);
        Gizmos.DrawWireSphere(position + rotation * new Vector3(0, -height / 2 + radius, 0), radius);
        Gizmos.DrawLine(position + rotation * new Vector3(0, height / 2 - radius, 0),
            position + rotation * new Vector3(0, -height / 2 + radius, 0));
    }

    public void FreezePlayerMovement(bool isFreeze)
    {
        if (!playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
            return;

        playerRigidbody.isKinematic = isFreeze;

        physicsRig.disableMovement = isFreeze;

        autoHandLeft.enableMovement = !isFreeze;
        autoHandRight.enableMovement = !isFreeze;

        //physicsRig.isTeleporting = isFreeze;
        physicsRig.ToggleNoCollideMode(isFreeze);

        if (isFreeze)
        {
            playerRigidbody.interpolation = RigidbodyInterpolation.None;
        }
        else
        {
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    // public void ChangeVoiceChatState(bool isUseVoiceChat)
    // {
    //     if (!_recorder) return;
    //
    //     _recorder.TransmitEnabled = isUseVoiceChat;
    // }

    public void SetActiveAllColliderObjects(bool isActive)
    {
        for (int i = 0; i < playerModuleRef.CharacterColliders.Length; i++)
        {
            if (playerModuleRef.CharacterColliders[i].collider == null)
            {
                continue;
            }
            playerModuleRef.CharacterColliders[i].collider.enabled = isActive;
        }
    }
    
    public void ChangeFlyModeState(bool canFlyMode)
    {
        if (!_relativeFlyController) return;

        _relativeFlyController.ToggleFlyMode(canFlyMode);
    }

    public void FreezePlayer(bool freeze, bool isDisableKinematic = true)
    {
        if (!playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
            return;

        if (freeze)
        {
            // Debug.Log("Freeze ---- Free player: " + transform.position);
            physicsRig.ActiveTeleport(true, Vector3.zero);

            if (isDisableKinematic)
                playerRigidbody.isKinematic = true;

            playerMobileController.gameObject.SetActive(false);
            autoHandLeft.enableMovement = false;
            autoHandRight.enableMovement = false;



        }
        else
        {
            // Debug.Log("Release ---- Free player: " + transform.position);
            //physicsRig.ActiveTeleport(false, transform.position);
            playerRigidbody.isKinematic = false;

            playerMobileController.gameObject.SetActive(true);

            autoHandLeft.enableMovement = true;
            autoHandRight.enableMovement = true;



            Debug.Log("Release ---- Free player: " + transform.position);
        }

        physicsRig.ToggleNoCollideMode(freeze);
    }

    public void FreezeVirtualHand(bool freeze)
    {
        //playerModuleRef.shmackleNetworkRig.enabled = !freeze;
        playerModuleRef.shmackleNetworkRig.isFreezeHand = freeze;
        autoHandLeft.enableMovement = !freeze;
        autoHandRight.enableMovement = !freeze;

        autoHandLeft.body.isKinematic = freeze;
        autoHandRight.body.isKinematic = freeze;
    }

    [ContextMenu("Debug freeze player hand")]
    void DebugFreezeVirtualHand()
    {
        FreezeVirtualHand(true);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!enabled) return;

        if(ShmackleConnectionManager.Instance.IsProphunt())
        {
        
            propHuntShooting.OnInput(runner, input);
        }
        else
        {
            playerShooting.OnInput(runner, input);
        }
        


    }

    #region Callbacks

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }


    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        ;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    #endregion

    #region COMBAT
    private void OnCombatUpdate()
    {
        if (EnemySpawner.Instance == null)
        {
            return;
        }

        if (!_isInCombatRoom)
        {
            return;
        }

        if (!_isCountingDificultyTime)
        {
            return;
        }

        if (_timerUpdateCombat >= 0)
        {
            _timerUpdateCombat -= Time.deltaTime;
            return;
        }

        _combatDifficultTimer += UPDATE_COMBAT_TIMER_INTERVAL;
        _timerUpdateCombat = UPDATE_COMBAT_TIMER_INTERVAL;

        if (!playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            return;
        }


        shmackleCombatUI.UpdateDifficultyTimer(_combatDifficultTimer);
        UpdateDifficultyBar(EnemySpawner.Instance.GetDifficulty(_combatRoomId));
    }

    public void OnJoinedCombatRoom(int roomId, Color32 fogColor)
    {
        _combatRoomId = roomId;
        _curFogColor = fogColor;

        if (_changeColorTween != null)
        {
            _changeColorTween.Kill();
            _changeColorTween = null;
        }

        _changeColorTween = fogRenderer.material.DOColor(fogColor, 0.35f);
    }

    public void OnJoinedSafeRoom()
    {
        _isInSafeRoom = true;
    }

    public void OnLeftSafeRoom()
    {
        _isInSafeRoom = false;
    }

    public void OnCombatRoomUpdate(bool isIn, bool enableFog = true)
    {

        if (_isInCombatRoom == isIn)
        {
            return;
        }
        Debug.Log("OnCombatRoomUpdate: " + isIn);
        _isInCombatRoom = isIn;
        if (playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {

            playerShooting.ActiveWeaponsLaser(isIn);
            playerShooting.SetActiveCombat(isIn);

            if (!isSpawned)
            {
                return;
            }

            if (_fogFadingCoroutine != null)
            {
                StopCoroutine(_fogFadingCoroutine);
            }

            if (_isInCombatRoom)
            {
                if (enableFog)
                {
                    FogFadingIn();
                }
            }
            else
            {


                if (enableFog)
                {
                    FogFadingOut();

                    _combatDifficultTimer = 0;
                    UpdateDifficultyBar(0);
                    shmackleCombatUI.Reset();
                }
            }
        }
        else
        {
            if (!_isInCombatRoom)
            {
                _combatDifficultTimer = 0;
            }
        }

    }

    public void OnActiveDifficultyTimer()
    {
        _isCountingDificultyTime = true;
    }

    public void SetCameraFarClipPlane(float distance)
    {
        HeadCamera.farClipPlane = distance;
    }

    public void ResetCameraFarClipPlane()
    {
        HeadCamera.farClipPlane = _defaultCameraFarClipPlaneDistance;
    }

    public void FogFadingIn()
    {

        if (_fogFadingCoroutine != null)
        {
            StopCoroutine(_fogFadingCoroutine);
            _fogFadingCoroutine = null;
        }

        float blending = fogRenderer.material.GetFloat("_Blending");
        fogSphere.SetActive(true);
        fogRenderer.material.SetFloat("_FadeDistance", fogFadeDistance);
        _combatStartPosition = EnemySpawner.Instance.CombatStartPosition;
        _combatStartPosition.y = 0;

        if (playerHealth.IsDead)
        {
            return;
        }

        _fogFadingCoroutine = StartCoroutine(IEFogFading(blending, 0, 1.0f,
            () => { HeadCamera.farClipPlane = farClipPlaneInCombat; }));
    }

    public void FogFadingOut()
    {
        if (_fogFadingCoroutine != null)
        {
            StopCoroutine(_fogFadingCoroutine);
            _fogFadingCoroutine = null;
        }


        float blending = fogRenderer.material.GetFloat("_Blending");
        if (playerHealth.IsDead)
        {
            return;
        }
        HeadCamera.farClipPlane = farClipPlaneOutCombat;
        _fogFadingCoroutine =
            StartCoroutine(IEFogFading(blending, 1, 1.0f, () => { fogSphere.SetActive(false); }));
    }

    private IEnumerator IEFogFading(float _startValue, float _endValue, float duration, Action onComplete = null)
    {
        float blending = _startValue;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            blending = Mathf.Lerp(_startValue, _endValue, elapsedTime / duration);
            fogRenderer.material.SetFloat("_Blending", blending);
            yield return null;
        }

        blending = _endValue;
        fogRenderer.material.SetFloat("_Blending", blending);

        onComplete.Invoke();
    }

    public EntityType EntityType
    {
        get
        {
            return EntityType.Player;
        }
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    public void OnCollectItem(PowerupItemController powerupItemController)
    {
        if (!playerHealth.HasStateAuthority)
        {
            return;
        }

        playerHealth.PlayPowerupEffect();

        switch (powerupItemController.Type)
        {
            case PowerupType.AMMO:
                playerShooting.ChangeAmmo(powerupItemController.Value);
                break;
            case PowerupType.HEALTH:
                playerHealth.RestoreHealth(powerupItemController.Value);
                break;
            case PowerupType.WEAPON:
                WeaponItemController weaponItemController = (WeaponItemController)powerupItemController;
                GearEquipmentSlot slot = GearEquipmentSlot.None;



                switch (weaponItemController.WeaponData.pack)
                {

                    case GearPack.Drone:
                        playerShooting.TryEquipDrone(weaponItemController.WeaponData.runtime.dronePrefab);
                        break;
                    case GearPack.Consumable:
                        break;
                }


                GearSlotContainer slotContainer =
                    playerModuleRef.shmackleNetworkRig.gearManager.TryGetFreeSlot(weaponItemController.WeaponData.runtime.gearPart,
                        out slot);
                Debug.Log("On Collect Item: " + weaponItemController.WeaponData.pack + "- " + (slotContainer != null));
                if (slotContainer != null)
                {

                    var gearObject = playerModuleRef.shmackleNetworkRig.gearManager.GetGearObject(weaponItemController.WeaponData.id, weaponItemController.WeaponData.runtime.gearPrefab,
                        slotContainer.SlotTransform);

                    if (weaponItemController.WeaponData.pack == GearPack.Weapon)
                    {
                        playerShooting.TryEquipWeapon(gearObject as BasePlayerWeapon, slot);
                    }

                }
                break;
        }
    }

    public void OnTargetKilled(IHitable victim)
    {
        if (!_isInCombatRoom)
        {
            return;
        }

        playerHealth.OnTargetKilled(victim);
    }

    public bool IsPlayer()
    {
        return true;
    }

    public void TakeDamage(IHitable owner, int damage, Vector3 knockBackDirection, Vector3 contactPoint, Vector3 sourceDamagePosition,
        DamageType damageType, bool isLocalDamage, int projectileId, int numCollisionEvent)
    {
        if (playerHealth.IsDead)
            return;

        Transform _ownerTranform = HeadCamera.transform;
        Vector3 _direction = (sourceDamagePosition - _ownerTranform.position).normalized;
        float _x = Vector3.Dot(_ownerTranform.right, _direction);
        float _y = Vector3.Dot(_ownerTranform.up, _direction);
        float _z = Vector3.Dot(_ownerTranform.forward, _direction);
        DirectionNavigatorState _damageDirection = DamageDirectionCalculate(new Vector3(_x, _y, _z), 0.25f);
        
        // Debug.Log(">> damage direction: " + _damageDirection + " - [" + _x + ", " + _y + ", " + _z + "]");
        // Debug.Log("Player Took Damage: " + damage);

        // take damage
        playerHealth.TakeDamage(damage);
        // tracking
        if (playerHealth.IsDead && playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            if (owner is CombatEnemyAI ai) AnalyticsHelper.RecordDeathByEnemy((int)ai.EnemyType);
            else AnalyticsHelper.RecordDeathByExplode();
        }

        onTakeDamaged?.Invoke(_damageDirection);
    }

    private DirectionNavigatorState GetRandomDirection()
    {
        var allValues = Enum.GetValues(typeof(DirectionNavigatorState));
        var randomIndex = Random.Range(0, allValues.Length);

        return (DirectionNavigatorState)allValues.GetValue(randomIndex);
    }

    private DirectionNavigatorState DamageDirectionCalculate(Vector3 damageDirection, float defaultDirection = 0.25f)
    {
        DirectionNavigatorState _direction = DirectionNavigatorState.UNDEFINED;
        float _absX = Mathf.Abs(damageDirection.x);
        float _absY = Mathf.Abs(damageDirection.y);
        float _absZ = Mathf.Abs(damageDirection.z);

        float _absVerticalal = Mathf.Max(_absZ, _absY);
        float _verticalal = Mathf.Max(damageDirection.z, damageDirection.y);


        if (_absX > defaultDirection && _absVerticalal <= defaultDirection)
        {
            if (damageDirection.x > defaultDirection)
            {
                return DirectionNavigatorState.RIGHT;
            }

            if (damageDirection.x < defaultDirection)
            {
                return DirectionNavigatorState.LEFT;
            }
        }

        if (_absX <= defaultDirection && _absVerticalal > defaultDirection)
        {
            if (_verticalal > defaultDirection)
            {
                return DirectionNavigatorState.UP;
            }

            if (_verticalal < defaultDirection)
            {
                return DirectionNavigatorState.DOWN;
            }
        }

        if (_absX > defaultDirection && _absVerticalal > defaultDirection)
        {
            if (damageDirection.x > defaultDirection && (_verticalal > defaultDirection))
            {
                return DirectionNavigatorState.UP_RIGHT;
            }

            if (damageDirection.x > defaultDirection && (_verticalal < defaultDirection))
            {
                return DirectionNavigatorState.RIGHT_DOWN;
            }

            if (damageDirection.x < defaultDirection && (_verticalal > defaultDirection))
            {
                return DirectionNavigatorState.LEFT_UP;
            }

            if (damageDirection.x < defaultDirection && (_verticalal < defaultDirection))
            {
                return DirectionNavigatorState.DOWN_LEFT;
            }
        }
        return _direction;
    }

    public void ResetCombatDifficultyTime()
    {
        _combatDifficultTimer = 0;
    }

    private void UpdateDifficultyBar(float value)
    {

        // Difficulty from Distance - 1 + Difficulty from Time - 1 = 2
        shmackleCombatUI.UpdateDifficultBar(value, 1.5f);
    }

    #endregion

    

    #region EDITOR_DEBUG
#if UNITY_EDITOR
    private void AddDebugComponents()
    {
        if (runner || runnerExpectations == RunnerExpectations.Offline)
        {
            gameObject.AddComponent<BK_FreeCamera>();
            physicsRig.gameObject.SetActive(false);

            // Update for Xr Devices Simulator;

            LegacyTPD[] drivers = GetComponentsInChildren<LegacyTPD>();

            for (int i = 0; i < drivers.Length; i++)
            {
                drivers[i].enabled = false;

                var trackingType = drivers[i].trackingType;
                var updateType = drivers[i].updateType;
                var useRelative = drivers[i].UseRelativeTransform;

                var newTPD = drivers[i].gameObject.AddComponent<InputSystemTPD>();
                newTPD.trackingType = (InputSystemTPD.TrackingType)trackingType;
                newTPD.updateType = (InputSystemTPD.UpdateType.BeforeRender);
                newTPD.ignoreTrackingState = false; // Match based on use case

                switch (drivers[i].poseSource)
                {
                    case LegacyTPD.TrackedPose.LeftPose:
                        CreateTPD(newTPD, "<XRController>{LeftHand}/devicePosition",
                            "<XRController>{LeftHand}/deviceRotation", "<XRController>{LeftHand}/trackingState");
                        break;
                    case LegacyTPD.TrackedPose.RightPose:
                        CreateTPD(newTPD, "<XRController>{RightHand}/devicePosition",
                            "<XRController>{RightHand}/deviceRotation", "<XRController>{RightHand}/trackingState");
                        break;
                    case LegacyTPD.TrackedPose.Center:
                        drivers[i].enabled = true;
                        //  CreateTPD(newTPD, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");
                        break;

                }
            }
        }
    }

    private InputSystemTPD CreateTPD(InputSystemTPD tpd, string posPath, string rotPath, string statePath)
    {
        // Position
        var posAction = new InputAction("position", InputActionType.Value, posPath);
        posAction.Enable();
        tpd.positionInput = new InputActionProperty(posAction);

        // Rotation
        var rotAction = new InputAction("rotation", InputActionType.Value, rotPath);
        rotAction.Enable();
        tpd.rotationInput = new InputActionProperty(rotAction);

        // Tracking State (Optional)
        var stateAction = new InputAction("trackingState", InputActionType.Value, statePath);
        stateAction.Enable();
        tpd.trackingStateInput = new InputActionProperty(stateAction);

        return tpd;
    }
#endif
    #endregion
}

public enum DirectionNavigatorState
{
    UNDEFINED,
    UP,
    UP_RIGHT,
    RIGHT,
    RIGHT_DOWN,
    DOWN,
    DOWN_LEFT,
    LEFT,
    LEFT_UP
}