using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Shmackle
{
    public class Player3 : MonoBehaviour
    {
        public static Player3 Instance { get; private set; }

        [Header("Player References")]
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;
        public Transform leftController;
        public Transform rightController;
        public CapsuleCollider bodyCollider;
        public SphereCollider headCollider;

        [Header("Physics Settings")]
        public LayerMask groundLayers;
        public float maxArmLength = 1.5f;
        public float unstickDistance = 0.75f;
        public float maxJumpSpeed = 7f;
        public float jumpMultiplier = 2f;
        public bool disableMovement = false;

        [Header("Haptics and Audio")]
        public AudioSource leftHandTapAudio;
        public AudioSource rightHandTapAudio;
        public AudioSource leftHandSlideAudio;
        public AudioSource rightHandSlideAudio;
        public float hapticStrength = 0.5f;
        public float hapticDuration = 0.1f;
        public float slideHapticStrength = 0.3f;

        private Rigidbody _rigidbody;
        private Vector3 _lastHeadPosition, _lastLeftHandPosition, _lastRightHandPosition;
        private bool _isLeftHandTouching, _isRightHandTouching;
        private bool _isLeftHandSliding, _isRightHandSliding;
        private Vector3 _velocityAverage;
        private float _realTimeLast, _deltaTime;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeValues();
        }

        private void InitializeValues()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _lastHeadPosition = head.position;
            _lastLeftHandPosition = leftHand.position;
            _lastRightHandPosition = rightHand.position;
            _realTimeLast = Time.realtimeSinceStartup;
        }

        private void FixedUpdate()
        {
            HandleTeleportation();
        }

        private void LateUpdate()
        {
            CalculateDeltaTime();
            if (!disableMovement)
            {
                ApplyPhysics();
                UpdateHandPositions();
                AdjustBodyCollider();
                HandleAudioAndHaptics();
            }
        }

        private void HandleTeleportation()
        {
            if (Vector3.Distance(head.position, _lastHeadPosition) > _rigidbody.linearVelocity.magnitude * _deltaTime)
            {
                transform.position += _lastHeadPosition - head.position;
            }
        }

        private void CalculateDeltaTime()
        {
            float currentTime = Time.realtimeSinceStartup;
            _deltaTime = currentTime - _realTimeLast;
            _realTimeLast = currentTime;
            if (_deltaTime > 0.1f) _deltaTime = 0.05f; // Cap delta time to avoid large jumps
        }

        private void ApplyPhysics()
        {
            if (_isLeftHandTouching || _isRightHandTouching)
            {
                // Simulate gravity
                transform.position += Vector3.down * 4.9f * _deltaTime * _deltaTime;
            }

            if (_isLeftHandSliding || _isRightHandSliding)
            {
                // Apply sliding effect
                Vector3 slideVelocity = _velocityAverage.normalized * Mathf.Min(_velocityAverage.magnitude, maxJumpSpeed);
                _rigidbody.linearVelocity = slideVelocity;
            }
        }

        private void UpdateHandPositions()
        {
            _lastLeftHandPosition = UpdateHandPosition(leftController, _lastLeftHandPosition, _isLeftHandTouching);
            _lastRightHandPosition = UpdateHandPosition(rightController, _lastRightHandPosition, _isRightHandTouching);
        }

        private Vector3 UpdateHandPosition(Transform handController, Vector3 lastHandPosition, bool isHandTouching)
        {
            Vector3 handPosition = handController.position;
            float handDistance = Vector3.Distance(head.position, handPosition);

            if (handDistance > maxArmLength)
            {
                handPosition = head.position + (handPosition - head.position).normalized * maxArmLength;
            }

            if (isHandTouching && Vector3.Distance(handPosition, lastHandPosition) > unstickDistance)
            {
                lastHandPosition = handPosition;
            }

            return handPosition;
        }

        private void AdjustBodyCollider()
        {
            // Adjust collider position and size based on head and ground detection
            if (Physics.Raycast(head.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers))
            {
                bodyCollider.height = hit.distance;
            }

            bodyCollider.center = new Vector3(0, -bodyCollider.height / 2, 0);
            bodyCollider.transform.position = head.position + Vector3.down * bodyCollider.height / 2;
            bodyCollider.transform.eulerAngles = new Vector3(0, head.eulerAngles.y, 0);
        }

        private void HandleAudioAndHaptics()
        {
            if (_isLeftHandTouching)
            {
                leftHandTapAudio.Play();
                StartHapticFeedback(true, hapticStrength, hapticDuration);
            }
            else if (_isLeftHandSliding)
            {
                leftHandSlideAudio.Play();
                StartHapticFeedback(true, slideHapticStrength, Time.fixedDeltaTime);
            }
            else
            {
                leftHandSlideAudio.Stop();
            }

            if (_isRightHandTouching)
            {
                rightHandTapAudio.Play();
                StartHapticFeedback(false, hapticStrength, hapticDuration);
            }
            else if (_isRightHandSliding)
            {
                rightHandSlideAudio.Play();
                StartHapticFeedback(false, slideHapticStrength, Time.fixedDeltaTime);
            }
            else
            {
                rightHandSlideAudio.Stop();
            }
        }

        private void StartHapticFeedback(bool isLeftHand, float amplitude, float duration)
        {
            StartCoroutine(HapticPulse(isLeftHand, amplitude, duration));
        }

        private IEnumerator HapticPulse(bool isLeftHand, float amplitude, float duration)
        {
            InputDevice handDevice = isLeftHand ? InputDevices.GetDeviceAtXRNode(XRNode.LeftHand) : InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            float startTime = Time.unscaledTime;

            while (Time.unscaledTime < startTime + duration)
            {
                handDevice.SendHapticImpulse(0U, amplitude, 0.05f);
                yield return new WaitForSeconds(0.045f);
            }
        }

        public void TeleportPlayer(Vector3 targetPosition)
        {
            transform.position = targetPosition;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _lastLeftHandPosition = leftHand.position;
            _lastRightHandPosition = rightHand.position;
            _lastHeadPosition = head.position;
        }


    }
}
