using System.Collections;
using UnityEngine;

namespace LayerLab.ArtMaker
{
    /// <summary>
    /// 카메라 이동 및 줌 제어
    /// Camera movement and zoom control
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraControl : MonoBehaviour
    {
        public static CameraControl Instance { get; private set; }
        [SerializeField] private Transform target; // 추적 대상 / Follow target
        [SerializeField] private Vector2 offset; // 카메라 오프셋 / Camera offset
        [SerializeField] private float followSpeed = 5f; // 추적 속도 / Follow speed
        [SerializeField] private float zoomSpeed = 2f; // 줌 속도 / Zoom speed
        [SerializeField] private float minZoom = 3f; // 최소 줌 / Minimum zoom
        [SerializeField] private float maxZoom = 10f; // 최대 줌 / Maximum zoom
        [SerializeField] private float smoothDampTime = 0.15f; // 부드러운 이동 감쇠 시간 / Smooth damp time

        public Camera Cam { get; private set; } // 카메라 컴포넌트 / Camera component
        public float targetZoom; // 목표 줌 레벨 / Target zoom level

        private Vector2 _minBounds; // 카메라 이동 최소 경계 / Camera movement minimum bounds
        private Vector2 _maxBounds; // 카메라 이동 최대 경계 / Camera movement maximum bounds
        private Vector3 _currentVelocity = Vector3.zero;

        [SerializeField] private Transform boundLeft; // 왼쪽 경계 / Left boundary
        [SerializeField] private Transform boundRight; // 오른쪽 경계 / Right boundary
        [SerializeField] private Transform boundTop; // 위쪽 경계 / Top boundary
        [SerializeField] private Transform boundBottom; // 아래쪽 경계 / Bottom boundary

        private void Awake()
        {
            Instance = this;
        }
        
        private void Update()
        {
            HandleZoom();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            MoveCamera();
        }
        
        /// <summary>
        /// 초기화
        /// Initialize
        /// </summary>
        public void Init()
        {
            Cam = GetComponent<Camera>();
            
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            targetZoom = Cam.orthographicSize;
            SetBounds();
            
            var desiredPosition = target.position + (Vector3)offset;
            desiredPosition.z = transform.position.z;

            transform.position = desiredPosition;
        }

        /// <summary>
        /// 카메라 움직임 처리
        /// Handle camera movement
        /// </summary>
        private void MoveCamera()
        {
            var desiredPosition = target.position + (Vector3)offset;
            desiredPosition.z = transform.position.z;

            var vertExtent = Cam.orthographicSize;
            var horzExtent = vertExtent * Cam.aspect; 

            var minX = _minBounds.x + horzExtent;
            var maxX = _maxBounds.x - horzExtent;

            if (minX > maxX)
            {
                var centerX = (_minBounds.x + _maxBounds.x) * 0.5f;
                desiredPosition.x = centerX;
                
                _currentVelocity.x = 0;
            }
            else
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            }

            var minY = _minBounds.y + vertExtent;
            var maxY = _maxBounds.y - vertExtent;

            if (minY > maxY)
            {
                var centerY = (_minBounds.y + _maxBounds.y) * 0.5f;
                desiredPosition.y = centerY;
                _currentVelocity.y = 0;
            }
            else
            {
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            var smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, smoothDampTime);
            transform.position = smoothedPosition;
        }

        /// <summary>
        /// 줌 처리
        /// Handle zoom functionality
        /// </summary>
        private void HandleZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                var previousZoom = targetZoom;
                
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                
                Cam.orthographicSize = targetZoom;
                
                if (Mathf.Abs(previousZoom - targetZoom) > 0.01f)
                {
                    var currentPos = transform.position;
                    var verticalExtent = Cam.orthographicSize;
                    var horizontalExtent = verticalExtent * Cam.aspect;
                    
                    var minX = _minBounds.x + horizontalExtent;
                    var maxX = _maxBounds.x - horizontalExtent;
                    var minY = _minBounds.y + verticalExtent;
                    var maxY = _maxBounds.y - verticalExtent;
                    
                    if (minX > maxX)
                    {
                        var centerX = (_minBounds.x + _maxBounds.x) * 0.5f;
                        currentPos.x = centerX;
                    }
                    else
                    {
                        currentPos.x = Mathf.Clamp(currentPos.x, minX, maxX);
                    }
                    
                    if (minY > maxY)
                    {
                        var centerY = (_minBounds.y + _maxBounds.y) * 0.5f;
                        currentPos.y = centerY;
                    }
                    else
                    {
                        currentPos.y = Mathf.Clamp(currentPos.y, minY, maxY);
                    }
                    
                    transform.position = currentPos;
                    _currentVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 경계 오브젝트로부터 카메라 경계 설정
        /// Set camera boundaries from boundary objects
        /// </summary>
        private void SetBounds()
        {
            if (boundLeft && boundRight && boundTop && boundBottom)
            {
                _minBounds = new Vector2(boundLeft.position.x, boundBottom.position.y);
                _maxBounds = new Vector2(boundRight.position.x, boundTop.position.y);
            }
            else
            {
                Debug.LogWarning("카메라 바운드 오브젝트가 설정되지 않았습니다. Camera bounds object is not set.");
            }
        }

        /// <summary>
        /// 선택 시 카메라 경계 기즈모 표시
        /// Draw camera boundary gizmos when selected
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_minBounds == Vector2.zero && _maxBounds == Vector2.zero)
                return;
                
            Gizmos.color = Color.green;

            var min = new Vector3(_minBounds.x, _minBounds.y, 0);
            var max = new Vector3(_maxBounds.x, _maxBounds.y, 0);

            var bottomLeft = new Vector3(min.x, min.y, 0);
            var bottomRight = new Vector3(max.x, min.y, 0);
            var topRight = new Vector3(max.x, max.y, 0);
            var topLeft = new Vector3(min.x, max.y, 0);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}