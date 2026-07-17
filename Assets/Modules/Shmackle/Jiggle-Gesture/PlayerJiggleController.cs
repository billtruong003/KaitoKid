using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EZhex1991.EZSoftBone;
using Fusion;
using Shmackle.Utils.CoroutinesTimer;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class PlayerJiggleController : MonoBehaviour
{
    #region Settings Structures

    [System.Serializable]
    private struct NodDetectionSettings
    {
        [Header("Angular Velocity")]
        [Tooltip("Ngưỡng vận tốc góc (độ/giây) để kích hoạt hiệu ứng. Người chơi phải gật đầu đủ nhanh để vượt qua ngưỡng này.")]
        [Range(20f, 300f)] public float AngularVelocityThreshold;

        [Tooltip("Thời gian chờ tối thiểu (giây) giữa hai lần phát âm thanh để tránh spam.")]
        [Range(0.05f, 1f)] public float MinTimeBetweenSounds;

        [Header("Look Down Requirement")]
        [Tooltip("Nếu được bật, người chơi phải nhìn xuống vượt qua một ngưỡng góc nhất định để việc phát hiện gật đầu được kích hoạt.")]
        public bool RequireLookingDown;

        [Tooltip("Góc nhìn xuống tối thiểu (tính từ phương ngang) để kích hoạt bộ phát hiện. 0 là nhìn thẳng, 90 là nhìn thẳng xuống đất.")]
        [Range(10f, 90f)] public float LookDownThresholdAngle;

        [Header("Movement Filter")]
        [Tooltip("Tốc độ di chuyển ngang tối đa (m/s) mà việc phát hiện gật đầu vẫn hoạt động. Nếu người chơi di chuyển nhanh hơn, việc phát hiện sẽ tạm dừng để tránh lỗi.")]
        [Range(0.1f, 5f)] public float MovementSpeedFilter;
    }

    [System.Serializable]
    private struct AudioSettings
    {
        [Tooltip("Danh sách các âm thanh 'bách bách' sẽ được phát ngẫu nhiên khi có chuyển động gật đầu mạnh.")]
        public List<AudioClip> NodSounds;
    }

    [System.Serializable]
    private struct VROptimizationSettings
    {
        public float DisableDistance;
    }

    [System.Serializable]
    private struct ImpulseSettings
    {
        [Tooltip("Khoảng cách mà bone điều khiển sẽ di chuyển khi có xung lực.")]
        public float Strength;
        [Tooltip("Thời gian (giây) để bone di chuyển ra xa.")]
        public float OutDuration;
        [Tooltip("Thời gian (giây) để bone quay trở lại vị trí ban đầu.")]
        public float ReturnDuration;
    }

    #endregion

    #region Inspector Fields

    [Header("Component References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerJiggleNetworkRelay networkRelay;

    [Header("Magica Cloth Control")]
    [SerializeField] private EZSoftBone[] targetCloth;
    // [SerializeField] private Transform[] jiggleDriverBone;

    [Header("Impulse Physics")]
    [SerializeField] private ImpulseSettings reactionImpulse = new ImpulseSettings { Strength = 0.02f, OutDuration = 0.08f, ReturnDuration = 0.25f };

    [Header("Nod Detection")]
    [SerializeField] private NodDetectionSettings nodDetection = new NodDetectionSettings { AngularVelocityThreshold = 100f, MinTimeBetweenSounds = 0.25f, RequireLookingDown = true, LookDownThresholdAngle = 30f, MovementSpeedFilter = 1.0f };
    [SerializeField] private AudioSettings audioSettings;

    [Header("VR Optimization (LOD)")]
    [SerializeField] private bool enableDistanceLOD = true;
    [SerializeField] private VROptimizationSettings vrOptimization = new VROptimizationSettings { DisableDistance = 20f };

    #endregion

    #region Private State
    private AudioSource _audioSource;
    private NetworkObject _networkObject;
    private Quaternion _lastRotation;
    private float _lastPitchVelocity = 0f;
    private Vector3 _lastHorizontalPosition;
    private float _lastSoundPlayTime = -1f;
    private float _lookDownDotThreshold;
    private Transform _localPlayerCamera;
    private Vector3 _initialDriverBoneLocalPos;
    private Coroutine _activeImpulseCoroutine;
    private WaitForSeconds _lodWait;
    private WaitForSeconds _detectionWait;
    private readonly float _detectionDeltaTime = 0.05f;
    #endregion

#if UNITY_EDITOR
    #region Debug
    private enum DebugState { Detecting, Cooldown, MovingTooFast, NotLookingDown }
    private DebugState _debug_currentState = DebugState.Detecting;
    private float _debug_currentPitchVelocityAbs = 0f;
    private float _debug_lastNodTimestamp = -10f;
    private readonly float _debug_flashDuration = 0.3f;
    #endregion
#endif

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _networkObject = GetComponentInParent<NetworkObject>();
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (targetCloth == null 
            // || jiggleDriverBone == null
            || cameraTransform == null)
        {
            this.enabled = false;
            return;
        }

        // _initialDriverBoneLocalPos = jiggleDriverBone.localPosition;
        _lookDownDotThreshold = Mathf.Sin(nodDetection.LookDownThresholdAngle * Mathf.Deg2Rad);
        _lodWait = CoroutineTimeUtils.GetWaitForSeconds(1f);
        _detectionWait = CoroutineTimeUtils.GetWaitForSeconds(_detectionDeltaTime);

        InitializeCoroutine();
    }

    private void InitializeCoroutine()
    {
        if (_networkObject.HasInputAuthority)
        {
            _lastRotation = cameraTransform.rotation;
            _lastHorizontalPosition = GetHorizontalPosition(transform.position);
            StartCoroutine(LocalPlayerNodDetectionRoutine());

            for (int i = 0; i < targetCloth.Length; i++)
            {
                targetCloth[i].enabled = false;
            }
        }
        else
        {
            InitializeRemotePlayerLOD();
            
            for (int i = 0; i < targetCloth.Length; i++)
            {
                targetCloth[i].enabled = true;
            }
        }
    }

    private void OnEnable()
    {
        InitializeCoroutine();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void InitializeRemotePlayerLOD()
    {
        if (enableDistanceLOD && Camera.main != null)
        {
            _localPlayerCamera = Camera.main.transform;
            StartCoroutine(UpdateRemotePlayerLODRoutine());
        }
    }

    private IEnumerator UpdateRemotePlayerLODRoutine()
    {
        while (true)
        {
            if (_localPlayerCamera != null)
            {
                float distanceSqr = (transform.position - _localPlayerCamera.position).sqrMagnitude;
                UpdateLODState(distanceSqr);
            }
            yield return _lodWait;
        }
    }

    private void UpdateLODState(float distanceSqr)
    {
        // bool shouldBeEnabled = distanceSqr <= vrOptimization.DisableDistance * vrOptimization.DisableDistance;
        // if (targetCloth.enabled != shouldBeEnabled)
        // {
        //     targetCloth.enabled = shouldBeEnabled;
        // }
    }

    private IEnumerator LocalPlayerNodDetectionRoutine()
    {
        while (true)
        {
            UpdateNodDetection();
            yield return _detectionWait;
        }
    }

    private void ResetDetectionState()
    {
        _lastPitchVelocity = 0;
        _lastRotation = cameraTransform.rotation;
    }

    private void UpdateNodDetection()
    {
        if (nodDetection.RequireLookingDown)
        {
            float lookDownFactor = -cameraTransform.forward.y; // 1.0 là nhìn thẳng xuống, 0 là ngang, -1.0 là thẳng lên
            if (lookDownFactor < _lookDownDotThreshold)
            {
                ResetDetectionState();
#if UNITY_EDITOR
                _debug_currentState = DebugState.NotLookingDown;
#endif
                return;
            }
        }

        // Điều kiện 2: Không được di chuyển quá nhanh
        Vector3 currentHorizontalPos = GetHorizontalPosition(transform.position);
        float horizontalSpeed = Vector3.Distance(currentHorizontalPos, _lastHorizontalPosition) / _detectionDeltaTime;
        _lastHorizontalPosition = currentHorizontalPos;

        if (horizontalSpeed > nodDetection.MovementSpeedFilter)
        {
            ResetDetectionState();
#if UNITY_EDITOR
            _debug_currentState = DebugState.MovingTooFast;
#endif
            return;
        }

        // Tính toán vận tốc góc (chỉ khi các điều kiện trên được thỏa mãn)
        Quaternion currentRotation = cameraTransform.rotation;
        (currentRotation * Quaternion.Inverse(_lastRotation)).ToAngleAxis(out float angle, out Vector3 axis);

        float pitchVelocity = Vector3.Dot(axis * angle * Mathf.Deg2Rad, cameraTransform.right) / _detectionDeltaTime;

#if UNITY_EDITOR
        _debug_currentPitchVelocityAbs = Mathf.Abs(pitchVelocity * Mathf.Rad2Deg);
#endif

        // Phát hiện đỉnh chuyển động
        bool hasChangedDirection = Mathf.Sign(pitchVelocity) != Mathf.Sign(_lastPitchVelocity) && _lastPitchVelocity != 0;
        bool hasSufficientMagnitude = Mathf.Abs(_lastPitchVelocity * Mathf.Rad2Deg) > nodDetection.AngularVelocityThreshold;
        bool isCooledDown = Time.time >= _lastSoundPlayTime + nodDetection.MinTimeBetweenSounds;

#if UNITY_EDITOR
        _debug_currentState = isCooledDown ? DebugState.Detecting : DebugState.Cooldown;
#endif

        if (hasChangedDirection && hasSufficientMagnitude && isCooledDown)
        {
            _lastSoundPlayTime = Time.time;
            TriggerNodEffect();
#if UNITY_EDITOR
            _debug_lastNodTimestamp = Time.time;
#endif
        }

        _lastRotation = currentRotation;
        _lastPitchVelocity = pitchVelocity;
    }

    private Vector3 GetHorizontalPosition(Vector3 position)
    {
        return new Vector3(position.x, 0, position.z);
    }

    private void TriggerNodEffect()
    {
        if (audioSettings.NodSounds == null || audioSettings.NodSounds.Count == 0) return;

        byte soundVariant = (byte)Random.Range(0, audioSettings.NodSounds.Count);
        networkRelay.Rpc_PlayNodEffect(soundVariant);
    }

    public void ExecuteNodEffect(byte soundVariant)
    {
        bool isSoundValid = audioSettings.NodSounds != null && audioSettings.NodSounds.Count > soundVariant;
        if (isSoundValid && _audioSource != null)
        {
            _audioSource.PlayOneShot(audioSettings.NodSounds[soundVariant]);
        }
        
        Debug.Log("Play Nod Effect");
        //Vector3 impulseDirection = (Random.onUnitSphere * 0.7f + Vector3.down * 0.3f).normalized;
        //ApplyImpulse(impulseDirection);
    }

    public void ApplyExternalImpact(Vector3 worldSpaceDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(worldSpaceDirection.normalized);
        ApplyImpulse(localDirection);
    }

    private void ApplyImpulse(Vector3 localDirection)
    {
        if (!this.enabled) return;
        // if (_activeImpulseCoroutine != null) StopCoroutine(_activeImpulseCoroutine);
        // _activeImpulseCoroutine = StartCoroutine(ExecuteJiggleImpulseRoutine(localDirection));
    }

    // private IEnumerator ExecuteJiggleImpulseRoutine(Vector3 localDirection)
    // {
    //     Vector3 startPos = _initialDriverBoneLocalPos;
    //     Vector3 peakPos = startPos + localDirection * reactionImpulse.Strength;
    //     float elapsedTime = 0f;
    //     while (elapsedTime < reactionImpulse.OutDuration)
    //     {
    //         jiggleDriverBone.localPosition = Vector3.Lerp(startPos, peakPos, elapsedTime / reactionImpulse.OutDuration);
    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }
    //     elapsedTime = 0f;
    //     while (elapsedTime < reactionImpulse.ReturnDuration)
    //     {
    //         jiggleDriverBone.localPosition = Vector3.Lerp(peakPos, startPos, elapsedTime / reactionImpulse.ReturnDuration);
    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }
    //     jiggleDriverBone.localPosition = startPos;
    //     _activeImpulseCoroutine = null;
    // }

    public void StopAllSounds()
    {
        if (_audioSource != null) _audioSource.Stop();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || cameraTransform == null || !_networkObject.HasInputAuthority) return;

        Vector3 gizmoPosition = cameraTransform.position + cameraTransform.right * 0.3f + cameraTransform.forward * 0.5f;
        float barTotalHeight = 0.2f;
        float barWidth = 0.02f;

        string statusText;
        Color statusColor;
        switch (_debug_currentState)
        {
            case DebugState.MovingTooFast:
                statusText = "STATUS: MOVING TOO FAST (Paused)";
                statusColor = Color.yellow;
                break;
            case DebugState.Cooldown:
                statusText = "STATUS: COOLDOWN";
                statusColor = Color.cyan;
                break;
            case DebugState.NotLookingDown:
                statusText = "STATUS: NOT LOOKING DOWN (Paused)";
                statusColor = new Color(1, 0.5f, 0); // Orange
                break;
            default:
                statusText = "STATUS: DETECTING";
                statusColor = Color.green;
                break;
        }
        Handles.color = statusColor;
        Handles.Label(gizmoPosition + Vector3.up * 0.05f, statusText);
        Handles.Label(gizmoPosition - Vector3.up * (barTotalHeight + 0.02f), $"Pitch Vel: {_debug_currentPitchVelocityAbs:F1} / {nodDetection.AngularVelocityThreshold}");

        Gizmos.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        Gizmos.DrawCube(gizmoPosition, new Vector3(barWidth, barTotalHeight, barWidth));

        float fillRatio = Mathf.Clamp01(_debug_currentPitchVelocityAbs / nodDetection.AngularVelocityThreshold);
        float fillHeight = barTotalHeight * fillRatio;
        Gizmos.color = Color.Lerp(Color.green, Color.red, fillRatio);
        Gizmos.DrawCube(gizmoPosition - Vector3.up * (barTotalHeight - fillHeight) * 0.5f, new Vector3(barWidth, fillHeight, barWidth));

        float thresholdY = -barTotalHeight / 2f + barTotalHeight;
        Handles.color = Color.white;
        Vector3 lineStart = gizmoPosition + new Vector3(-barWidth * 2, thresholdY, 0);
        Vector3 lineEnd = gizmoPosition + new Vector3(barWidth * 2, thresholdY, 0);
        //Handles.DrawLine(lineStart, lineEnd); // This might not be needed as the top of the bar is the threshold

        if (Time.time - _debug_lastNodTimestamp < _debug_flashDuration)
        {
            float flashAlpha = 1.0f - ((Time.time - _debug_lastNodTimestamp) / _debug_flashDuration);
            Gizmos.color = new Color(1, 0, 0, flashAlpha);
            Gizmos.DrawSphere(cameraTransform.position + cameraTransform.forward * 0.3f, 0.05f);
        }
    }
#endif
}