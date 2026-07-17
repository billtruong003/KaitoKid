using System.Runtime.CompilerServices;
using Player.Config;
using Photon.Voice.Unity;
using Photon.Voice.Fusion;
using Shmackle.Audio;
using UnityEngine;
using Utilities.Timers;

namespace Shmackle.Player.EyeAnimated
{
    /// <summary>
    /// Controls procedural eye animation by manipulating shader UVs and material properties.
    /// Handles logic for tracking the best <see cref="EyeTarget"/> within a view cone 
    /// and modulates iris scale based on audio amplitude from Photon Voice/Fusion.
    /// Includes optimization features like distance culling and time-gated updates.
    /// </summary>
    public class EyeTrackingController : MonoBehaviour
    {

        [Header("References")]
        [SerializeField] private Renderer _eyeRenderer;
        [SerializeField] private Renderer _eyeRendererRight;
        [SerializeField] private EyeTrackingConfig _config;

        private MaterialPropertyBlock _propBlockLeft;
        private MaterialPropertyBlock _propBlockRight;
        private int _pupilOffsetId;
        private int _irisScaleId;
        private int _irisSizeId;

        [Header("Per-Eye Settings")]
        [SerializeField] private Vector2 _eyeOffsetLeft = Vector2.zero;
        [SerializeField] private Vector2 _eyeOffsetRight = Vector2.zero;

        private Vector2 _velocityEyeOffset = Vector2.zero;
        private Vector2 _targetVelocityEyeOffset = Vector2.zero;

        private Vector2 _idleEyeOffset = Vector2.zero;
        private Vector2 _targetIdleEyeOffset = Vector2.zero;
        private TimeGate _idleEyeGate;

        private Vector3 _lastFramePosition = Vector3.zero;

        private Vector2 _currentUv;
        private float _currentIrisScale;
        private float _targetIrisScale;

        private EyeTarget _currentTarget;
        private bool _isVisible = true;
        private Transform _mainCameraTransform;
        private Transform _cachedTransform;

        private VoiceNetworkObject _voiceNetworkObject;
        private bool _isLocal;
        private Recorder _localRecorder;
        private SpeakerAudioTap _remoteAudioTap;

        private TimeGate _audioGate;
        private TimeGate _searchGate;
        private bool _audioComponentsFound;

        private float _sqrTrackingDistance;
        private float _sqrCullingDistance;
        private float _cosHalfViewCone;

        private void Awake()
        {
            _cachedTransform = transform;
            if (_eyeRenderer == null) _eyeRenderer = GetComponent<Renderer>();

            _propBlockLeft = new MaterialPropertyBlock();
            if (_eyeRendererRight != null)
                _propBlockRight = new MaterialPropertyBlock();

            _pupilOffsetId = Shader.PropertyToID("_PupilOffset");
            _irisScaleId = Shader.PropertyToID("_IrisScale");
            _irisSizeId = Shader.PropertyToID("_IrisSize");

            if (Camera.main != null) _mainCameraTransform = Camera.main.transform;

            _lastFramePosition = _cachedTransform.position;

            _currentIrisScale = _config != null ? _config.MinIrisScale : 1.0f;
            _targetIrisScale = _currentIrisScale;

            InitializeTimers();
            RecalculateOptimizationParams();
        }

        private void Start()
        {
            FindAudioComponents();
        }

        private void OnEnable()
        {
            if (_config != null)
            {
                _config.ConfigUpdated += OnConfigUpdated;
                RecalculateOptimizationParams();
            }
        }

        private void OnDisable()
        {
            if (_config != null)
            {
                _config.ConfigUpdated -= OnConfigUpdated;
            }
        }

        private void LateUpdate()
        {
            if (!ShouldProcess()) return;

            ProcessLookLogic();
            ProcessVelocityLogic();
            ProcessIdleEyeLogic();
            ProcessAudioLogic();
            UpdateMaterial();

            _lastFramePosition = _cachedTransform.position;
        }

        private void OnConfigUpdated()
        {
            InitializeTimers();
            RecalculateOptimizationParams();
        }

        private void InitializeTimers()
        {
            if (_config == null) return;
            _audioGate = new TimeGate(_config.AudioSampleInterval);
            _searchGate = new TimeGate(1.0f);
            _idleEyeGate = new TimeGate(_config.IdleEyeMovementInterval);
        }

        private void RecalculateOptimizationParams()
        {
            if (_config == null) return;

            _sqrTrackingDistance = _config.TrackingDistance * _config.TrackingDistance;
            _sqrCullingDistance = _config.CullingDistance * _config.CullingDistance;
            _cosHalfViewCone = Mathf.Cos(_config.ViewConeAngle * 0.5f * Mathf.Deg2Rad);
        }

        private void OnBecameVisible() => _isVisible = true;
        private void OnBecameInvisible() => _isVisible = false;

        private bool ShouldProcess()
        {
            if (_config == null || !_isVisible) return false;

            if (_mainCameraTransform == null)
            {
                if (Camera.main != null) _mainCameraTransform = Camera.main.transform;
                return true;
            }

            return (_cachedTransform.position - _mainCameraTransform.position).sqrMagnitude < _sqrCullingDistance;
        }

        private void ProcessLookLogic()
        {
            if (_config.ResetToDefault)
            {
                _currentUv = Vector2.zero;
                return;
            }

            Quaternion virtualRotation = _cachedTransform.rotation * Quaternion.Euler(_config.LookRotationOffset);
            _currentTarget = GetBestTarget(virtualRotation);
            Vector2 targetUv = CalculateLocalUv(_currentTarget, virtualRotation);
            _currentUv = Vector2.Lerp(_currentUv, targetUv, Time.deltaTime * _config.Damping);
        }

        private void ProcessAudioLogic()
        {
            if (_audioGate != null && _audioGate.Throttle())
            {
                if (!_audioComponentsFound)
                {
                    if (_searchGate != null && _searchGate.Throttle()) FindAudioComponents();
                }
                else
                {
                    float amplitude = GetVoiceAmplitude();
                    float normalizedAmp = Mathf.Clamp01(amplitude * _config.AudioSensitivity);
                    _targetIrisScale = Mathf.Lerp(_config.MinIrisScale, _config.MaxIrisScale, normalizedAmp);
                }
            }

            _currentIrisScale = Mathf.Lerp(_currentIrisScale, _targetIrisScale, Time.deltaTime * _config.IrisSmoothSpeed);
        }

        private void ProcessVelocityLogic()
        {
            if (_config.ResetToDefault)
            {
                _velocityEyeOffset = Vector2.zero;
                _targetVelocityEyeOffset = Vector2.zero;
                return;
            }

            if (_currentTarget != null)
            {
                _velocityEyeOffset = Vector2.zero;
                _targetVelocityEyeOffset = Vector2.zero;
                return;
            }

            Vector3 positionDelta = _cachedTransform.position - _lastFramePosition;
            Vector3 velocity = positionDelta / Time.deltaTime;
            Vector3 rightDir = _mainCameraTransform != null ? _mainCameraTransform.right : _cachedTransform.right;

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;

            if (horizontalSpeed > 0.1f)
            {
                float rightComponent = Vector3.Dot(horizontalVelocity, rightDir);
                float targetXOffset = -rightComponent * _config.VelocityEyeSensitivity;
                _targetVelocityEyeOffset.x = Mathf.Clamp(targetXOffset, -_config.LimitUv.x, _config.LimitUv.x);
            }
            else
            {
                _targetVelocityEyeOffset.x = 0;
            }

            _velocityEyeOffset = Vector2.Lerp(_velocityEyeOffset, _targetVelocityEyeOffset,
                Time.deltaTime / (_config.VelocityEyeSmoothing > 0 ? _config.VelocityEyeSmoothing : 0.1f));
        }

        private void ProcessIdleEyeLogic()
        {
            if (_config.ResetToDefault || _currentTarget != null || !_config.IdleEyeMovementEnabled)
            {
                _idleEyeOffset = Vector2.zero;
                return;
            }

            Vector3 positionDelta = _cachedTransform.position - _lastFramePosition;
            Vector3 velocity = positionDelta / Time.deltaTime;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;

            if (horizontalSpeed > 0.1f) return;

            if (_idleEyeGate != null && _idleEyeGate.Throttle())
            {
                _targetIdleEyeOffset.x = Random.Range(-_config.IdleEyeMovementRange, _config.IdleEyeMovementRange);
                _targetIdleEyeOffset.x = Mathf.Clamp(_targetIdleEyeOffset.x, -_config.LimitUv.x, _config.LimitUv.x);
            }

            float smoothSpeed = 1.0f / (_config.VelocityEyeSmoothing > 0 ? _config.VelocityEyeSmoothing : 0.1f);
            _idleEyeOffset = Vector2.Lerp(_idleEyeOffset, _targetIdleEyeOffset, Time.deltaTime * smoothSpeed);
        }

        private void FindAudioComponents()
        {
            Transform root = _cachedTransform.root;

            if (_voiceNetworkObject == null)
                _voiceNetworkObject = root.GetComponentInChildren<VoiceNetworkObject>(true);

            if (_remoteAudioTap == null)
                _remoteAudioTap = root.GetComponentInChildren<SpeakerAudioTap>(true);

            if (_voiceNetworkObject != null)
            {
                _isLocal = _voiceNetworkObject.IsLocal;

                if (_isLocal)
                {
                    _localRecorder = _voiceNetworkObject.RecorderInUse;
                    _audioComponentsFound = _localRecorder != null;
                }
                else
                {
                    _audioComponentsFound = _remoteAudioTap != null;
                }
            }
        }

        private float GetVoiceAmplitude()
        {
            if (_voiceNetworkObject == null) return 0f;

            if (_isLocal)
            {
                if (_localRecorder == null) _localRecorder = _voiceNetworkObject.RecorderInUse;
                return _localRecorder != null && _localRecorder.LevelMeter != null
                    ? _localRecorder.LevelMeter.CurrentPeakAmp
                    : 0f;
            }

            return _remoteAudioTap != null ? _remoteAudioTap.CurrentAmplitude : 0f;
        }

        private void UpdateMaterial()
        {
            if (_eyeRenderer == null) return;

            Vector2 dynamicOffset = (_currentTarget == null) ? (_velocityEyeOffset + _idleEyeOffset) : Vector2.zero;
            Vector2 finalUvLeft = _currentUv + _eyeOffsetLeft + dynamicOffset;

            _eyeRenderer.GetPropertyBlock(_propBlockLeft);
            _propBlockLeft.SetVector(_pupilOffsetId, finalUvLeft);
            _propBlockLeft.SetFloat(_irisScaleId, _currentIrisScale);
            _propBlockLeft.SetFloat(_irisSizeId, _currentIrisScale);
            _eyeRenderer.SetPropertyBlock(_propBlockLeft);

            if (_eyeRendererRight != null)
            {
                Vector2 finalUvRight = _currentUv + _eyeOffsetRight + dynamicOffset;

                _eyeRendererRight.GetPropertyBlock(_propBlockRight);
                _propBlockRight.SetVector(_pupilOffsetId, finalUvRight);
                _propBlockRight.SetFloat(_irisScaleId, _currentIrisScale);
                _propBlockRight.SetFloat(_irisSizeId, _currentIrisScale);
                _eyeRendererRight.SetPropertyBlock(_propBlockRight);
            }
        }

        private EyeTarget GetBestTarget(Quaternion virtualRot)
        {
            EyeTarget best = null;
            float bestSqrDist = float.MaxValue;

            Vector3 eyePos = _cachedTransform.position;
            Vector3 virtualFwd = virtualRot * Vector3.forward;

            foreach (var t in EyeTarget.ActiveTargets)
            {
                if (t == null || t.transform.parent == _cachedTransform || t.gameObject == gameObject) continue;

                Vector3 dirToTarget = t.Position - eyePos;
                float dstSqr = dirToTarget.sqrMagnitude;

                if (dstSqr > _sqrTrackingDistance) continue;
                if (Vector3.Dot(virtualFwd, dirToTarget.normalized) < _cosHalfViewCone) continue;

                if (best == null)
                {
                    best = t;
                    bestSqrDist = dstSqr;
                    continue;
                }

                if (t.Priority > best.Priority)
                {
                    best = t;
                    bestSqrDist = dstSqr;
                }
                else if (t.Priority == best.Priority)
                {
                    if (dstSqr < bestSqrDist)
                    {
                        best = t;
                        bestSqrDist = dstSqr;
                    }
                }
            }
            return best;
        }

        private Vector2 CalculateLocalUv(EyeTarget target, Quaternion virtualRot)
        {
            if (target == null) return Vector2.zero;

            Vector3 dirToTarget = target.Position - _cachedTransform.position;
            Vector3 localDir = Quaternion.Inverse(virtualRot) * dirToTarget;

            if (localDir.z < 0.1f) return Vector2.zero;

            Vector2 projected = new Vector2(localDir.x, localDir.y);
            float magnitude = projected.magnitude;

            Vector2 uvOffset = (magnitude > 1e-5f)
                ? (projected / magnitude) * (Mathf.Atan2(magnitude, localDir.z) * _config.LookSensitivity)
                : Vector2.zero;

            if (_config.InvertX) uvOffset.x = -uvOffset.x;
            if (_config.InvertY) uvOffset.y = -uvOffset.y;

            return new Vector2(
                Mathf.Clamp(uvOffset.x, -_config.LimitUv.x, _config.LimitUv.x),
                Mathf.Clamp(uvOffset.y, -_config.LimitUv.y, _config.LimitUv.y)
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_config == null) return;

            Quaternion virtualRot = transform.rotation * Quaternion.Euler(_config.LookRotationOffset);
            Vector3 eyePos = transform.position;
            Vector3 forward = virtualRot * Vector3.forward;

            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.05f);
            UnityEditor.Handles.DrawWireDisc(eyePos, Vector3.up, _config.TrackingDistance);
            UnityEditor.Handles.DrawWireDisc(eyePos, Vector3.right, _config.TrackingDistance);

            float halfAngle = _config.ViewConeAngle * 0.5f;
            float coneHeight = _config.TrackingDistance * Mathf.Cos(halfAngle * Mathf.Deg2Rad);
            float coneRadius = _config.TrackingDistance * Mathf.Sin(halfAngle * Mathf.Deg2Rad);
            Vector3 coneBaseCenter = eyePos + (forward * coneHeight);

            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.15f);

            Vector3 startDirH = virtualRot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward;
            UnityEditor.Handles.DrawSolidArc(eyePos, virtualRot * Vector3.up, startDirH, _config.ViewConeAngle, _config.TrackingDistance);

            Vector3 startDirV = virtualRot * Quaternion.Euler(-halfAngle, 0, 0) * Vector3.forward;
            UnityEditor.Handles.DrawSolidArc(eyePos, virtualRot * Vector3.right, startDirV, _config.ViewConeAngle, _config.TrackingDistance);

            UnityEditor.Handles.color = new Color(1f, 0.3f, 0f, 0.5f);
            UnityEditor.Handles.DrawWireDisc(coneBaseCenter, forward, coneRadius);

            UnityEditor.Handles.DrawLine(eyePos, coneBaseCenter + (virtualRot * Vector3.up * coneRadius));
            UnityEditor.Handles.DrawLine(eyePos, coneBaseCenter - (virtualRot * Vector3.up * coneRadius));
            UnityEditor.Handles.DrawLine(eyePos, coneBaseCenter + (virtualRot * Vector3.right * coneRadius));
            UnityEditor.Handles.DrawLine(eyePos, coneBaseCenter - (virtualRot * Vector3.right * coneRadius));

            if (_currentTarget != null)
            {
                UnityEditor.Handles.color = Color.green;
                UnityEditor.Handles.DrawLine(eyePos, _currentTarget.Position, 4f);
                UnityEditor.Handles.DrawWireDisc(_currentTarget.Position, Camera.main ? Camera.main.transform.forward : Vector3.forward, 0.2f);
            }
        }
#endif
    }
}