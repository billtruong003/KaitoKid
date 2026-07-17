using NaughtyAttributes;
using UnityEngine;

namespace Shmackle.Player.Hands
{
    public enum FingerMode
    {
        Controller,
        Grabbing,
        SurfaceDetection
    }

    /// <summary>
    /// Summary:
    /// <list type="bullet">
    /// <item>
    /// <description>Maps a normalized rotation value (0..1) to an angle range on a selected axis and positions a target Transform along a circular arc.</description>
    /// </item>
    /// <item>
    /// <description>Provides runtime visualization (circle, constraint arc, min/max/current markers, and axis indicator) to aid authoring and debugging.</description>
    /// </item>
    /// <item>
    /// <description>Exposes API to set rotation directly or smoothly over time.</description>
    /// </item>
    /// </list>
    ///
    /// Usage:
    /// <list type="bullet">
    /// <item>
    /// <description>Add this component to a GameObject, assign a target Transform, choose the RotationAxis, Min/Max angles, and Radius.</description>
    /// </item>
    /// <item>
    /// <description>Drive the target using SetRotationValue or SetSmoothRotationValue to move it along the constrained arc.</description>
    /// </item>
    /// </list>
    /// </summary>
    public class FingerTargetRotationConstraints : MonoBehaviour
    {
        public float CurrentAngle => _currentAngle;

        public bool UseManualAngleControl
        {
            get => _useManualAngleControl;
            set => _useManualAngleControl = value;
        }

        [SerializeField, Range(0, 1)] private float _rotation;

        [SerializeField, ReadOnly] private float _currentAngle;

        [SerializeField] private Transform _target;

        [SerializeField] private RotationAxis _rotationAxis = RotationAxis.Z;

        [Header("Controller Rotation (Manual Input 0-1)")] [SerializeField, Range(-180f, 180f)]
        private float _controllerMinAngle = 0f;

        [SerializeField, Range(-180f, 180f)] private float _controllerMaxAngle = 90f;

        [SerializeField] private Color _controllerGizmoColor = Color.cyan;

        [Header("Grabbing Mode (Collision Detection)")] [SerializeField, Range(-180f, 180f)]
        private float _grabbingMinAngle = 0f;

        [SerializeField, Range(-180f, 180f)] private float _grabbingMaxAngle = 120f;

        [SerializeField] private Color _grabbingGizmoColor = Color.yellow;

        [Header("Surface Detection Mode")] [SerializeField, Range(-180f, 180f)]
        private float _surfaceMinAngle = 0f;

        [SerializeField, Range(-180f, 180f)] private float _surfaceMaxAngle = 90f;

        [SerializeField] private Color _surfaceGizmoColor = Color.green;

        [Header("Collision Detection Settings")] [SerializeField]
        private LayerMask _grabbableLayer;

        [SerializeField] private LayerMask _surfaceLayer;

        [SerializeField, Min(0.01f)] private float _collisionCheckDistance = 0.5f;

        [SerializeField, Range(1, 32)] private int _collisionScanSteps = 16;

        [Header("General Settings")] [SerializeField, Min(0.01f)]
        private float _radius = 1f;

        [SerializeField] private bool _visualizeGizmo = true;

        [SerializeField] private Color _gizmoColor = Color.cyan;

        [Header("Manual Control")]
        [SerializeField, Tooltip("When enabled, UpdateTargetPosition is skipped and you must use SetCurrentAngle to control the finger")]
        private bool _useManualAngleControl = false;

        [Header("Mode")]
        [SerializeField] private FingerMode _mode = FingerMode.Controller;

        // Smoothing variables
        private Vector3 _currentVelocity;

        [Header("Smoothing Settings")] [SerializeField, Range(0.01f, 0.5f)]
        private float _positionSmoothTime = 0.03f;

        private enum RotationAxis
        {
            X,
            Y,
            Z
        }

        private void Awake()
        {
            // Initialize target position to prevent initial snap
            if (_target)
            {
                _currentAngle = Mathf.Lerp(_controllerMinAngle, _controllerMaxAngle, _rotation);
                Vector3 localPosition = GetPositionOnCircle(_currentAngle);
                _target.position = transform.position + transform.TransformDirection(localPosition);
            }
        }

        private void Update()
        {
            if (_target && !_useManualAngleControl)
            {
                UpdateTargetPosition();
            }
        }

        /// <summary>
        /// Sets the rotation value. Only accepts values between 0 and 1.
        /// </summary>
        public void SetRotationValue(float value)
        {
            _rotation = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Directly sets the current angle and applies smooth dampening.
        /// This bypasses the normal UpdateTargetPosition logic.
        /// </summary>
        /// <param name="angle">The angle in degrees to set</param>
        /// <param name="enableManualControl">If true, automatically enables manual control mode to prevent UpdateTargetPosition from running</param>
        public void SetCurrentAngle(float angle)
        {
            _currentAngle = angle;
            ApplySmoothTargetPosition(_currentAngle);
        }

        private void UpdateTargetPosition()
        {
            float minAngle, maxAngle;

            // Determine which angle range to use based on mode
            switch (_mode)
            {
                case FingerMode.Grabbing:
                    minAngle = _grabbingMinAngle;
                    maxAngle = _grabbingMaxAngle;

                    // Detect grabbable collision and adjust rotation
                    if (DetectCollision(minAngle, maxAngle, _grabbableLayer, out float collisionAngle))
                    {
                        maxAngle = collisionAngle;
                    }

                    // Set to max regardless of whether a collision is detected or not. This is so that the fingers
                    // will bend by default.
                    _rotation = 1.0f; // Set to max since we hit the object
                    break;

                case FingerMode.SurfaceDetection:
                    minAngle = _surfaceMinAngle;
                    maxAngle = _surfaceMaxAngle;

                    // Detect surface collision and adjust rotation
                    if (DetectCollision(minAngle, maxAngle, _surfaceLayer, out float surfaceCollisionAngle))
                    {
                        maxAngle = surfaceCollisionAngle;
                        _rotation = 1.0f; // Set to max since we hit the surface
                    }
                    else
                    {
                        // When no collider detected, set to 0 state.
                        // This stretches the fingers when it misses a collider while touching.
                        _rotation = 0.0f;
                    }
                    break;

                case FingerMode.Controller:
                default:
                    // Default controller mode
                    minAngle = _controllerMinAngle;
                    maxAngle = _controllerMaxAngle;
                    break;
            }

            // Clamp the rotation value and convert to angle
            _currentAngle = Mathf.Lerp(minAngle, maxAngle, _rotation);

            // Apply smooth dampening to target position
            ApplySmoothTargetPosition(_currentAngle);
        }

        /// <summary>
        /// Applies smooth dampening to move the target to the specified angle
        /// </summary>
        private void ApplySmoothTargetPosition(float angle)
        {
            // Calculate position on circle based on selected axis
            Vector3 localPosition = GetPositionOnCircle(angle);
            Vector3 targetPosition = transform.position + transform.TransformDirection(localPosition);

            // Apply smooth damping to prevent jitter/stuttering
            if (_target)
            {
                // If in controller mode, do not smoothen the finger bends.
                if(_mode == FingerMode.Controller)
                    _target.position = targetPosition;
                else // Otherwise, apply smooth dampening on surface detection and grabbing finger bends.
                    _target.position = Vector3.SmoothDamp(_target.position, targetPosition, ref _currentVelocity,
                        _positionSmoothTime);
            }
        }

        private Vector3 GetPositionOnCircle(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;

            switch (_rotationAxis)
            {
                case RotationAxis.X:
                    // Rotate around X axis (YZ plane)
                    return new Vector3(0, Mathf.Sin(radians) * _radius, Mathf.Cos(radians) * _radius);

                case RotationAxis.Y:
                    // Rotate around Y axis (XZ plane)
                    return new Vector3(Mathf.Sin(radians) * _radius, 0, Mathf.Cos(radians) * _radius);

                case RotationAxis.Z:
                default:
                    // Rotate around Z axis (XY plane)
                    return new Vector3(Mathf.Cos(radians) * _radius, Mathf.Sin(radians) * _radius, 0);
            }
        }

        /// <summary>
        /// Scans for collision along the arc and returns the angle where collision occurs
        /// </summary>
        private bool DetectCollision(float minAngle, float maxAngle, LayerMask layerMask, out float collisionAngle)
        {
            collisionAngle = 0f;
            if (layerMask == 0) return false;

            float angleStep = (maxAngle - minAngle) / _collisionScanSteps;

            for (int i = 0; i <= _collisionScanSteps; i++)
            {
                float testAngle = minAngle + (angleStep * i);
                Vector3 localPosition = GetPositionOnCircle(testAngle);
                Vector3 worldPosition = transform.position + transform.TransformDirection(localPosition);
                Vector3 direction = (worldPosition - transform.position).normalized;

                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, _collisionCheckDistance,
                        layerMask))
                {
                    // Calculate the actual angle based on hit point
                    Vector3 hitLocalPos = transform.InverseTransformPoint(hit.point);
                    float hitAngle = CalculateAngleFromLocalPosition(hitLocalPos);
                    collisionAngle = Mathf.Clamp(hitAngle, minAngle, maxAngle);
                    return true;
                }
            }

            return false;
        }

        private float CalculateAngleFromLocalPosition(Vector3 localPos)
        {
            switch (_rotationAxis)
            {
                case RotationAxis.X:
                    return Mathf.Atan2(localPos.y, localPos.z) * Mathf.Rad2Deg;
                case RotationAxis.Y:
                    return Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg;
                case RotationAxis.Z:
                default:
                    return Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg;
            }
        }

        /// <summary>
        /// Sets the finger mode (Controller, Grabbing, or SurfaceDetection)
        /// </summary>
        public void SetMode(FingerMode mode)
        {
            _mode = mode;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_visualizeGizmo) return;

            // Draw the full circle
            DrawCircle(transform.position, transform.rotation, _radius, _gizmoColor, 64);

            // Draw controller mode arc (cyan by default)
            DrawArc(transform.position, transform.rotation, _radius, _controllerMinAngle, _controllerMaxAngle,
                _controllerGizmoColor, 32);
            DrawAngleIndicators(_controllerMinAngle, _controllerMaxAngle, _controllerGizmoColor, 0.05f);

            // Draw grabbing mode arc (yellow by default)
            DrawArc(transform.position, transform.rotation, _radius * 0.95f, _grabbingMinAngle, _grabbingMaxAngle,
                _grabbingGizmoColor, 32);
            DrawAngleIndicators(_grabbingMinAngle, _grabbingMaxAngle, _grabbingGizmoColor, 0.06f);

            // Draw surface detection arc (green by default)
            DrawArc(transform.position, transform.rotation, _radius * 0.9f, _surfaceMinAngle, _surfaceMaxAngle,
                _surfaceGizmoColor, 32);
            DrawAngleIndicators(_surfaceMinAngle, _surfaceMaxAngle, _surfaceGizmoColor, 0.07f);

            // Highlight active mode
            Color activeColor;
            float activeMin;
            float activeMax;

            switch (_mode)
            {
                case FingerMode.Grabbing:
                    activeColor = _grabbingGizmoColor;
                    activeMin = _grabbingMinAngle;
                    activeMax = _grabbingMaxAngle;
                    break;
                case FingerMode.SurfaceDetection:
                    activeColor = _surfaceGizmoColor;
                    activeMin = _surfaceMinAngle;
                    activeMax = _surfaceMaxAngle;
                    break;
                case FingerMode.Controller:
                default:
                    activeColor = _controllerGizmoColor;
                    activeMin = _controllerMinAngle;
                    activeMax = _controllerMaxAngle;
                    break;
            }

            // Draw thicker active arc
            Gizmos.color = activeColor;
            DrawArc(transform.position, transform.rotation, _radius * 1.05f, activeMin, activeMax, activeColor, 32);

            // Draw current target position
            if (_target)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(_target.position, _radius * 0.08f);
                Gizmos.DrawLine(transform.position, _target.position);
            }

            // Draw axis indicator
            Gizmos.color = Color.magenta;
            Vector3 axisDirection = GetAxisDirection();
            Gizmos.DrawRay(transform.position, transform.TransformDirection(axisDirection) * _radius * 0.5f);

            // Draw collision detection rays if in grabbing or surface detection mode
            if (_mode == FingerMode.Grabbing && _grabbableLayer != 0)
            {
                DrawCollisionRays(_grabbingMinAngle, _grabbingMaxAngle, _grabbableLayer, _grabbingGizmoColor);
            }
            else if (_mode == FingerMode.SurfaceDetection && _surfaceLayer != 0)
            {
                DrawCollisionRays(_surfaceMinAngle, _surfaceMaxAngle, _surfaceLayer, _surfaceGizmoColor);
            }
        }

        private void DrawAngleIndicators(float minAngle, float maxAngle, Color color, float sphereSize)
        {
            // Draw min angle indicator
            Vector3 minPos = transform.position + transform.TransformDirection(GetPositionOnCircle(minAngle));
            Gizmos.color = Color.Lerp(color, Color.red, 0.5f);
            Gizmos.DrawLine(transform.position, minPos);
            Gizmos.DrawWireSphere(minPos, _radius * sphereSize);

            // Draw max angle indicator
            Vector3 maxPos = transform.position + transform.TransformDirection(GetPositionOnCircle(maxAngle));
            Gizmos.color = Color.Lerp(color, Color.green, 0.5f);
            Gizmos.DrawLine(transform.position, maxPos);
            Gizmos.DrawWireSphere(maxPos, _radius * sphereSize);
        }

        private void DrawCollisionRays(float minAngle, float maxAngle, LayerMask layerMask, Color color)
        {
            float angleStep = (maxAngle - minAngle) / _collisionScanSteps;
            Gizmos.color = Color.Lerp(color, Color.red, 0.3f);

            for (int i = 0; i <= _collisionScanSteps; i++)
            {
                float testAngle = minAngle + (angleStep * i);
                Vector3 localPosition = GetPositionOnCircle(testAngle);
                Vector3 worldPosition = transform.position + transform.TransformDirection(localPosition);
                Vector3 direction = (worldPosition - transform.position).normalized;

                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, _collisionCheckDistance,
                        layerMask))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, hit.point);
                    Gizmos.DrawWireSphere(hit.point, _radius * 0.04f);
                }
                else
                {
                    Gizmos.color = Color.Lerp(color, Color.white, 0.7f);
                    Gizmos.DrawRay(transform.position, direction * _collisionCheckDistance);
                }
            }
        }

        private Vector3 GetAxisDirection()
        {
            switch (_rotationAxis)
            {
                case RotationAxis.X: return Vector3.right;
                case RotationAxis.Y: return Vector3.up;
                case RotationAxis.Z: return Vector3.forward;
                default: return Vector3.forward;
            }
        }

        private void DrawCircle(Vector3 center, Quaternion rotation, float radius, Color color, int segments)
        {
            Gizmos.color = color;
            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector3 pos1 = center + rotation * GetPositionOnCircle(angle1);
                Vector3 pos2 = center + rotation * GetPositionOnCircle(angle2);

                Gizmos.DrawLine(pos1, pos2);
            }
        }

        private void DrawArc(Vector3 center, Quaternion rotation, float radius, float startAngle, float endAngle,
            Color color, int segments)
        {
            Gizmos.color = color;
            float angleRange = endAngle - startAngle;
            float angleStep = angleRange / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = startAngle + i * angleStep;
                float angle2 = startAngle + (i + 1) * angleStep;

                Vector3 pos1 = center + rotation * GetPositionOnCircle(angle1);
                Vector3 pos2 = center + rotation * GetPositionOnCircle(angle2);

                Gizmos.DrawLine(pos1, pos2);
            }
        }
    }
}