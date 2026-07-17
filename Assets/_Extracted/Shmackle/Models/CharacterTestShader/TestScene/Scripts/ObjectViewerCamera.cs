// Filename: ObjectViewerController.cs
using UnityEngine;

/// <summary>
/// Controls a camera to view a target object.
/// Allows rotating the target with the mouse and zooming the camera with the scroll wheel.
/// </summary>
[AddComponentMenu("Camera/Object Viewer Controller")]
public class ObjectViewerController : MonoBehaviour
{
    [Header("Core Targets")]
    [Tooltip("Đối tượng chính sẽ được xoay khi người dùng tương tác.")]
    [SerializeField] private Transform targetObject;

    [Tooltip("Điểm mà camera sẽ luôn luôn nhìn vào. Thường là trung tâm của targetObject.")]
    [SerializeField] private Transform lookAtTarget;

    [Header("Rotation Settings")]
    [Tooltip("Tốc độ xoay đối tượng. Giá trị càng lớn, đối tượng xoay càng nhanh.")]
    [Range(50f, 500f)]
    [SerializeField] private float rotationSpeed = 150f;

    [Header("Zoom Settings")]
    [Tooltip("Tốc độ zoom của camera. Giá trị càng lớn, zoom càng nhạy.")]
    [SerializeField] private float zoomSpeed = 20f;
    [Tooltip("Khoảng cách gần nhất camera có thể tiếp cận đối tượng.")]
    [SerializeField] private float minZoomDistance = 2f;
    [Tooltip("Khoảng cách xa nhất camera có thể rời xa đối tượng.")]
    [SerializeField] private float maxZoomDistance = 15f;

    [Header("Initial Position")]
    [Tooltip("Khoảng cách ban đầu từ camera đến đối tượng.")]
    [SerializeField] private float initialDistance = 10f;
    [Tooltip("Góc nhìn ban đầu của camera theo trục Y (từ trên xuống).")]
    [Range(0f, 90f)]
    [SerializeField] private float initialPitchAngle = 20f;

    private float _currentDistance;
    private bool _isRotating;
    private Vector3 _previousMousePosition;
    private Vector3 _cameraOffset;

    private void Start()
    {
        InitializeCameraState();
    }

    private void Update()
    {
        if (!AreTargetsValid()) return;

        HandleRotationInput();
        HandleZoomInput();
    }

    private void LateUpdate()
    {
        if (!AreTargetsValid()) return;

        UpdateCameraPositionAndRotation();
    }

    private void InitializeCameraState()
    {
        if (!AreTargetsValid())
        {
            enabled = false; // Tự vô hiệu hóa script nếu thiếu target
            return;
        }

        _currentDistance = initialDistance;

        Quaternion initialRotation = Quaternion.Euler(initialPitchAngle, 0f, 0f);
        _cameraOffset = initialRotation * Vector3.forward;

        transform.position = lookAtTarget.position - _cameraOffset * _currentDistance;
        transform.LookAt(lookAtTarget);
    }

    private void HandleRotationInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isRotating = true;
            _previousMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isRotating = false;
        }

        if (_isRotating)
        {
            PerformRotation();
        }
    }

    private void PerformRotation()
    {
        Vector3 mouseDelta = Input.mousePosition - _previousMousePosition;
        float rotationAmount = -mouseDelta.x * rotationSpeed * Time.deltaTime;

        targetObject.Rotate(Vector3.up, rotationAmount, Space.World);

        _previousMousePosition = Input.mousePosition;
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            _currentDistance -= scrollInput * zoomSpeed;
            _currentDistance = Mathf.Clamp(_currentDistance, minZoomDistance, maxZoomDistance);
        }
    }

    private void UpdateCameraPositionAndRotation()
    {
        // Vector này giữ cho camera luôn ở vị trí tương đối so với target, ngay cả khi target di chuyển
        Vector3 desiredPosition = lookAtTarget.position - transform.forward * _currentDistance;
        transform.position = desiredPosition;

        // Đảm bảo camera luôn nhìn vào mục tiêu sau mọi thay đổi
        transform.LookAt(lookAtTarget);
    }

    private bool AreTargetsValid()
    {
        if (targetObject == null || lookAtTarget == null)
        {
            Debug.LogError("Vui lòng gán cả 'Target Object' và 'Look At Target' trong Inspector.");
            return false;
        }
        return true;
    }
}