using UnityEngine;

namespace GorillaLocomotion
{
    public class Player : MonoBehaviour
    {
        private const float GRAVITY = 9.8f;
        private static Player _instance;

        public static Player Instance { get { return _instance; } }

        [Header("References")]
        [SerializeField] private SphereCollider _headCollider;
        [SerializeField] private CapsuleCollider _bodyCollider;

        [SerializeField] private Transform _leftHandTransform;
        [SerializeField] private Transform _rightHandTransform;

        [Header("Speed")]
        [SerializeField] private float _velocityLimit = 0.4f;
        [SerializeField] private float _maxJumpSpeed = 6.5f;
        [SerializeField] private float _jumpMultiplier = 1.1f;
        [SerializeField] private int _velocityHistorySize = 8;
        
        [Header("Config")]
        [SerializeField] private float _maxArmLength = 1.5f;
        [SerializeField] private float _unStickDistance = 1f;
        [SerializeField] private float _minimumRaycastDistance = 0.05f;
        [SerializeField] private float _defaultSlideFactor = 0.03f;
        [SerializeField] private float _defaultPrecision = 0.995f;
        
        [SerializeField] private Vector3 _rightHandOffset = Vector3.zero;
        [SerializeField] private Vector3 _leftHandOffset = Vector3.zero;

        [SerializeField] private LayerMask _locomotionEnabledLayers = 0; //layer "default"

        [SerializeField] private bool _disableMovement = false;
        
        private Rigidbody _playerRigidBody;
        
        private bool _wasLeftHandTouching;
        private bool _wasRightHandTouching;

        private Vector3 _lastLeftHandPosition;
        private Vector3 _lastRightHandPosition;
        private Vector3 _lastHeadPosition;

        private Vector3[] _velocityHistory;
        private int _velocityIndex;
        private Vector3 _currentVelocity;
        private Vector3 _denormalizedVelocityAverage;
        private Vector3 _lastPosition;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            InitializeValues();
        }

        public void InitializeValues()
        {
            _playerRigidBody = GetComponent<Rigidbody>();
            _velocityHistory = new Vector3[_velocityHistorySize];
            _lastHeadPosition = _headCollider.transform.position;
            _velocityIndex = 0;
            _lastPosition = transform.position;
        }

        private void Update()
        {
            _bodyCollider.transform.eulerAngles = new Vector3(0, _headCollider.transform.eulerAngles.y, 0);

            bool leftHandColliding = IsHandColliding(out var firstIterationLeftHand, leftHand: true);
            bool rightHandColliding = IsHandColliding(out var firstIterationRightHand, leftHand: false);
            
            var rigidBodyMovement = GetRigidbodyMovement(leftHandColliding, firstIterationLeftHand, rightHandColliding, firstIterationRightHand);
            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position += rigidBodyMovement;
            }

            _lastHeadPosition = _headCollider.transform.position;

            _lastLeftHandPosition = FinalHandPosition(_lastLeftHandPosition, ref leftHandColliding, rightHandColliding, leftHand: true);
            _lastRightHandPosition = FinalHandPosition(_lastRightHandPosition, ref rightHandColliding, leftHandColliding, leftHand: false);

            StoreVelocities();

            ClampVelocity(leftHandColliding, rightHandColliding);

            _lastLeftHandPosition = UnstickHand(_lastLeftHandPosition, ref leftHandColliding, leftHand: true);
            _lastRightHandPosition = UnstickHand(_lastRightHandPosition, ref rightHandColliding, leftHand: false);

            _wasLeftHandTouching = leftHandColliding;
            _wasRightHandTouching = rightHandColliding;
        }

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private Vector3 CurrentLeftHandPosition()
        {
            return (PositionWithOffset(_leftHandTransform, _leftHandOffset) - _headCollider.transform.position).magnitude < _maxArmLength
                ? PositionWithOffset(_leftHandTransform, _leftHandOffset)
                : _headCollider.transform.position + (PositionWithOffset(_leftHandTransform, _leftHandOffset) - _headCollider.transform.position).normalized * _maxArmLength;
        }

        private Vector3 CurrentRightHandPosition()
        {
            return (PositionWithOffset(_rightHandTransform, _rightHandOffset) - _headCollider.transform.position).magnitude < _maxArmLength
                ? PositionWithOffset(_rightHandTransform, _rightHandOffset)
                : _headCollider.transform.position + (PositionWithOffset(_rightHandTransform, _rightHandOffset) - _headCollider.transform.position).normalized * _maxArmLength;
        }

        private bool IsHandColliding(out Vector3 firstIteration, bool leftHand)
        {
            firstIteration = Vector3.zero;
            var gravityOffset = 2f * GRAVITY * Time.deltaTime * Time.deltaTime * Vector3.down;
            var currentHandPos = leftHand ? CurrentLeftHandPosition() : CurrentRightHandPosition();
            var lastHandPos = leftHand ? _lastLeftHandPosition : _lastRightHandPosition;

            Vector3 distanceTraveled = currentHandPos - lastHandPos + gravityOffset;

            if (IterativeCollisionSphereCast(lastHandPos, _minimumRaycastDistance, distanceTraveled, _defaultPrecision, out var finalPosition, true))
            {
                //this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
                firstIteration = IsHandTouching(leftHand) ?
                    lastHandPos - currentHandPos :
                    finalPosition - currentHandPos;

                _playerRigidBody.linearVelocity = Vector3.zero;

                return true;
            }
            return false;
        }

        private Vector3 GetRigidbodyMovement(bool leftHandColliding, Vector3 firstIterationLeftHand, bool rightHandColliding, Vector3 firstIterationRightHand)
        {
            //average or add
            Vector3 rigidBodyMovement;
            if ((leftHandColliding || _wasLeftHandTouching) && (rightHandColliding || _wasRightHandTouching))
            {
                //this lets you grab stuff with both hands at the same time
                rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
            }
            else
            {
                rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
            }

            //check valid head movement
            if (IterativeCollisionSphereCast(
                _lastHeadPosition,
                _headCollider.radius,
                _headCollider.transform.position + rigidBodyMovement - _lastHeadPosition,
                _defaultPrecision,
                out var finalHeadPosition,
                false))
            {
                rigidBodyMovement = finalHeadPosition - _lastHeadPosition;
                //last check to make sure the head won't phase through geometry
                if (Physics.Raycast(
                    _lastHeadPosition,
                    _headCollider.transform.position - _lastHeadPosition + rigidBodyMovement,
                    out _,
                    (_headCollider.transform.position - _lastHeadPosition + rigidBodyMovement).magnitude + _headCollider.radius * _defaultPrecision * 0.999f,
                    _locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = _lastHeadPosition - _headCollider.transform.position;
                }
            }

            return rigidBodyMovement;
        }

        private Vector3 FinalHandPosition(Vector3 lastHandPos, ref bool thisHandColliding, in bool otherHandColliding, bool leftHand)
        {
            //do final hand position
            var currentHandPos = leftHand ? CurrentLeftHandPosition() : CurrentRightHandPosition();
            bool thisHandTouching = IsHandTouching(leftHand);
            bool otherHandTouching = IsHandTouching(!leftHand);
            var distanceTraveled = currentHandPos - lastHandPos;

            bool singleHandColliding = !((thisHandColliding || thisHandTouching) && (otherHandColliding || otherHandTouching));

            if (IterativeCollisionSphereCast(lastHandPos, _minimumRaycastDistance, distanceTraveled, _defaultPrecision, out var finalPosition, singleHandColliding))
            {
                thisHandColliding = true;
                return finalPosition;
            }
            else
            {
                return currentHandPos;
            }
        }

        private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
        {
            //first spherecast from the starting position to the final position
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out var hitInfo))
            {
                //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

                //take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
                Vector3 firstPosition = endPosition;
                Surface gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                float slipPercentage = gorillaSurface != null ? gorillaSurface.slipPercentage : (!singleHand ? _defaultSlideFactor : 0.001f);
                Vector3 movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
                if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out _))
                {
                    //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface
                else if (CollisionsSphereCast(
                    movementToProjectedAboveCollisionPlane + firstPosition,
                    sphereRadius,
                    startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition),
                    precision * precision * precision,
                    out endPosition,
                    out _))
                {
                    //if we hit, then return the spot we hit
                    return true;
                }
                else
                {
                    //this shouldn't really happe, since this means that the sliding motion got you around some corner or something and let you get to your final point. back off because something strange happened, so just don't do the slide
                    endPosition = firstPosition;
                    return true;
                }
            }
            //as kind of a sanity check, try a smaller spherecast. this accounts for times when the original spherecast was already touching a surface so it didn't trigger correctly
            else if (CollisionsSphereCast(
                startPosition, 
                sphereRadius * precision * 0.66f, 
                movementVector.normalized * (movementVector.magnitude + (sphereRadius * precision * 0.34f)), 
                precision * 0.66f, out _/*endPosition*/, out _))
            {
                endPosition = startPosition;
                return true;
            }
            else
            {
                endPosition = Vector3.zero;
                return false;
            }
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
            //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial spherecase
            if (Physics.SphereCast(
                startPosition,
                sphereRadius * precision,
                movementVector,
                out hitInfo,
                movementVector.magnitude + sphereRadius * (1 - precision),
                _locomotionEnabledLayers.value))
            {
                //if we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                //check a spherecase from the original position to the intended final position
                if (Physics.SphereCast(
                    startPosition,
                    sphereRadius * precision * precision,
                    finalPosition - startPosition,
                    out var innerHit,
                    (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision),
                    _locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                //bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
                else if (Physics.Raycast(
                    startPosition,
                    finalPosition - startPosition,
                    out innerHit,
                    (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f,
                    _locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }
                return true;
            }
            //anti-clipping through geometry check
            else if (Physics.Raycast(
                startPosition,
                movementVector,
                out hitInfo,
                movementVector.magnitude + sphereRadius * precision * 0.999f,
                _locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                return true;
            }
            else
            {
                finalPosition = Vector3.zero;
                return false;
            }
        }

        private void StoreVelocities()
        {
            _velocityIndex = (_velocityIndex + 1) % _velocityHistorySize;
            Vector3 oldestVelocity = _velocityHistory[_velocityIndex];
            _currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
            _denormalizedVelocityAverage += (_currentVelocity - oldestVelocity) / (float)_velocityHistorySize;
            _velocityHistory[_velocityIndex] = _currentVelocity;
            _lastPosition = transform.position;
        }

        private void ClampVelocity(bool leftHandColliding, bool rightHandColliding)
        {
            if ((rightHandColliding || leftHandColliding) && !_disableMovement)
            {
                if (_denormalizedVelocityAverage.magnitude > _velocityLimit)
                {
                    _playerRigidBody.linearVelocity =_denormalizedVelocityAverage.magnitude * _jumpMultiplier > _maxJumpSpeed
                        ? _denormalizedVelocityAverage.normalized * _maxJumpSpeed
                        : _jumpMultiplier * _denormalizedVelocityAverage;
                }
            }
        }

        private Vector3 UnstickHand(Vector3 lastHandPos, ref bool leftHandColliding, bool leftHand)
        {
            var currentHandPos = leftHand ? CurrentLeftHandPosition() : CurrentRightHandPosition();

            //check to see if hand is stuck and we should unstick it
            bool sphereCastLeft = !Physics.SphereCast(
                _headCollider.transform.position,
                _minimumRaycastDistance * _defaultPrecision,
                currentHandPos - _headCollider.transform.position,
                out _,
                (currentHandPos - _headCollider.transform.position).magnitude - _minimumRaycastDistance,
                _locomotionEnabledLayers.value);

            if (leftHandColliding && (currentHandPos - lastHandPos).magnitude > _unStickDistance && sphereCastLeft)
            {
                leftHandColliding = false;
                return currentHandPos;
            }
            return lastHandPos;
        }

        public bool IsHandTouching(bool leftHand)
        {
            return leftHand ? _wasLeftHandTouching : _wasRightHandTouching;
        }

        public void Turn(float degrees)
        {
            transform.RotateAround(_headCollider.transform.position, transform.up, degrees);
            _denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * _denormalizedVelocityAverage;
            for (int i = 0; i < _velocityHistory.Length; i++)
            {
                _velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * _velocityHistory[i];
            }
        }


    }
}