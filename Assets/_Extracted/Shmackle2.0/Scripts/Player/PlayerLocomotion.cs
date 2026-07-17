using System;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Rig;
using Player.Config;
using UnityEngine;

public enum SurfaceType
{
    None,
    Default,
    Tile,
    Mud
}

[DefaultExecutionOrder(INetworkRig.EXECUTION_ORDER - 10)]
public class PlayerLocomotion : MonoBehaviour
{
    #region Fields

    public event Action<SurfaceType, Vector3> SurfaceTouched;

    private const float GRAVITY = 9.81f;

    [SerializeField] private PlayerLocomotionConfig _playerLocomotionConfig;

    [SerializeField] private SphereCollider _headCollider;
    [SerializeField] private CapsuleCollider _bodyCollider;
    [SerializeField] private Transform _leftHandTransform;
    [SerializeField] private Transform _rightHandTransform;
    [SerializeField] private Transform _leftHandFollower;
    [SerializeField] private Transform _rightHandFollower;
    [SerializeField] private XRControllerInputDevice _leftHandController;
    [SerializeField] private XRControllerInputDevice _rightHandController;

    private float _velocityLimit = 0.4f;
    private float _maxJumpSpeed = 6.5f;
    private float _jumpMultiplier = 1.1f;
    private int _velocityHistorySize = 8;
    private float _runtimeJumpMultiplier = 1.0f;

    private float _maxArmLength = 1.5f;
    private float _unStickDistance = 1f;
    private float _minimumRaycastDistance = 0.05f;
    private float _defaultSlideFactor = 0.03f;
    private float _defaultPrecision = 0.995f;

    private Vector3 _rightHandOffset = Vector3.zero;
    private Vector3 _leftHandOffset = Vector3.zero;

    [SerializeField] private LayerMask _locomotionEnabledLayers = 0; //layer "default"

    [SerializeField] private bool _disableMovement = false;
    [SerializeField] private bool _isDoubleGrabEnabled = true;
    [SerializeField] private bool _bodyOffsetEnabled = false;

    private Rigidbody _rigidbody;
    private float _defaultBodyHeight;
    private Vector3 _defaultBodyPosition; //Vector3(0,-0.24f,-0.06f)

    private bool _wasLeftHandTouching;
    private bool _wasRightHandTouching;
    private Vector3 _lastLeftHandPosition;
    private Vector3 _lastRightHandPosition;
    private Vector3 _lastHeadPosition;
    private Vector3 _lastHeadPositionForOffset;

    private Vector3[] _velocityHistory;
    private int _velocityIndex;
    private Vector3 _currentVelocity;
    private Vector3 _denormalizedVelocityAverage;
    private Vector3 _lastPosition;

    private bool _isGrounded = true;

    #endregion

    #region Properties

    public Rigidbody Rigidbody => _rigidbody;
    public float JumpMultiplier => _jumpMultiplier * _runtimeJumpMultiplier;
    public float MaxJumpSpeed => _maxJumpSpeed;
    public float VelocityLimit => _velocityLimit;
    public Vector3 AverageVelocity => _denormalizedVelocityAverage;

    public bool IsGrounded => _isGrounded;

    public Vector3 CurrentLeftHandPosition => (PositionWithOffset(_leftHandTransform, _leftHandOffset) - _headCollider.transform.position).magnitude < _maxArmLength
            ? PositionWithOffset(_leftHandTransform, _leftHandOffset)
            : _headCollider.transform.position + (PositionWithOffset(_leftHandTransform, _leftHandOffset) - _headCollider.transform.position).normalized * _maxArmLength;

    public Vector3 CurrentRightHandPosition => (PositionWithOffset(_rightHandTransform, _rightHandOffset) - _headCollider.transform.position).magnitude < _maxArmLength
            ? PositionWithOffset(_rightHandTransform, _rightHandOffset)
            : _headCollider.transform.position + (PositionWithOffset(_rightHandTransform, _rightHandOffset) - _headCollider.transform.position).normalized * _maxArmLength;

    // Anchor state (Modular Climbing)
    public Vector3? LeftHandAnchorPosition { get; set; }
    public Vector3? RightHandAnchorPosition { get; set; }
    #endregion

    private void OnEnable() { _playerLocomotionConfig.ConfigUpdated += OnConfigUpdated; }
    private void OnDisable() { _playerLocomotionConfig.ConfigUpdated -= OnConfigUpdated; }

    private void OnConfigUpdated()
    {
        _velocityLimit = _playerLocomotionConfig.VelocityLimit;
        _maxJumpSpeed = _playerLocomotionConfig.MaxJumpSpeed;
        _jumpMultiplier = _playerLocomotionConfig.JumpMultiplier;
        _velocityHistorySize = _playerLocomotionConfig.VelocityHistorySize;
        _velocityHistory = new Vector3[_velocityHistorySize];
        _velocityIndex = 0;
        _rigidbody.mass = _playerLocomotionConfig.Mass;
        _bodyCollider.height = _defaultBodyHeight + _playerLocomotionConfig.HeightOffset;
        var bodyColCenter = _bodyCollider.center;
        bodyColCenter.y = _playerLocomotionConfig.HeightOffset * -0.5f;
        _bodyCollider.center = bodyColCenter;
        _isDoubleGrabEnabled = _playerLocomotionConfig.IsDoubleGrabEnabled;
        _bodyOffsetEnabled = _playerLocomotionConfig.BodyOffsetEnabled;
        _maxArmLength = _playerLocomotionConfig.MaxArmLength;
        _unStickDistance = _playerLocomotionConfig.UnStickDistance;
        _minimumRaycastDistance = _playerLocomotionConfig.MinimumRaycastDistance;
        _defaultSlideFactor = _playerLocomotionConfig.DefaultSlideFactor;
        _defaultPrecision = _playerLocomotionConfig.DefaultPrecision;
        _rightHandOffset = _playerLocomotionConfig.RightHandOffset;
        _leftHandOffset = _playerLocomotionConfig.LeftHandOffset;
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _defaultBodyHeight = _bodyCollider.height;
        _defaultBodyPosition = _bodyCollider.transform.localPosition;
        OnConfigUpdated();

        _lastHeadPosition = _headCollider.transform.position;
        _lastHeadPositionForOffset = _lastHeadPosition;
        _lastPosition = transform.position;
        _lastLeftHandPosition = _leftHandFollower.transform.position;
        _lastRightHandPosition = _rightHandFollower.transform.position;
    }

    private void Update()
    {
        if (_bodyOffsetEnabled)
            _bodyCollider.transform.position = PositionWithOffset(_headCollider.transform, new Vector3(0, -0.1f, -0.1f)) + Vector3.down * (0.5f * _bodyCollider.height);
        else
            _bodyCollider.transform.localPosition = _defaultBodyPosition;

        _bodyCollider.transform.eulerAngles = new Vector3(0f, _headCollider.transform.eulerAngles.y, 0f);

        UpdateLocomotion();
        UpdateHands();
    }

    private void LateUpdate()
    {
        // Intentionally left empty
    }

    private void UpdateLocomotion()
    {
        // Compute per-hand contributions. If a hand is anchored, we constrain that hand to its anchor point
        Vector3 firstIterationLeftHand = Vector3.zero;
        Vector3 firstIterationRightHand = Vector3.zero;
        SurfaceType leftHitSurface;
        SurfaceType rightHitSurface;

        bool leftHandColliding;
        bool rightHandColliding;

        // Left Hand Logic
        if (LeftHandAnchorPosition.HasValue)
        {
            firstIterationLeftHand = LeftHandAnchorPosition.Value - CurrentLeftHandPosition;
            leftHandColliding = true;
            leftHitSurface = SurfaceType.Default;
        }
        else
        {
            leftHandColliding = IsHandColliding(out firstIterationLeftHand, leftHand: true, out leftHitSurface);
        }

        // Right Hand Logic
        if (RightHandAnchorPosition.HasValue)
        {
            firstIterationRightHand = RightHandAnchorPosition.Value - CurrentRightHandPosition;
            rightHandColliding = true;
            rightHitSurface = SurfaceType.Default;
        }
        else
        {
            rightHandColliding = IsHandColliding(out firstIterationRightHand, leftHand: false, out rightHitSurface);
        }

        var rigidBodyMovement = GetRigidbodyMovement(leftHandColliding, firstIterationLeftHand, rightHandColliding, firstIterationRightHand);
        if (rigidBodyMovement != Vector3.zero)
        {
            transform.position += rigidBodyMovement;
        }

        _lastHeadPosition = _headCollider.transform.position;

        // Update Last Positions based on Anchors or Hand Logic
        if (LeftHandAnchorPosition.HasValue)
        {
            _lastLeftHandPosition = LeftHandAnchorPosition.Value;
            leftHandColliding = true;
        }
        else
        {
            _lastLeftHandPosition = FinalHandPosition(_lastLeftHandPosition, ref leftHandColliding, rightHandColliding, leftHand: true);
        }

        if (RightHandAnchorPosition.HasValue)
        {
            _lastRightHandPosition = RightHandAnchorPosition.Value;
            rightHandColliding = true;
        }
        else
        {
            _lastRightHandPosition = FinalHandPosition(_lastRightHandPosition, ref rightHandColliding, leftHandColliding, leftHand: false);
        }

        StoreVelocities();

        ClampVelocity(leftHandColliding, rightHandColliding);

        if (!LeftHandAnchorPosition.HasValue)
            _lastLeftHandPosition = UnstickHand(_lastLeftHandPosition, ref leftHandColliding, leftHand: true);
        if (!RightHandAnchorPosition.HasValue)
            _lastRightHandPosition = UnstickHand(_lastRightHandPosition, ref rightHandColliding, leftHand: false);

        if (leftHandColliding && !_wasLeftHandTouching && leftHitSurface != SurfaceType.None)
        {
            SurfaceTouched?.Invoke(leftHitSurface, _lastLeftHandPosition);
        }

        if (rightHandColliding && !_wasRightHandTouching && rightHitSurface != SurfaceType.None)
        {
            SurfaceTouched?.Invoke(rightHitSurface, _lastRightHandPosition);
        }
        
        _wasLeftHandTouching = leftHandColliding;
        _wasRightHandTouching = rightHandColliding;
        
        if (!_wasLeftHandTouching && !_wasRightHandTouching && !IsBodyCollidingWithFloor())
        {
            _isGrounded = false;
        }
        else
        {
            _isGrounded = true;
        }
    }



    private void UpdateHands()
    {
        _leftHandFollower.position = _lastLeftHandPosition;
        _rightHandFollower.position = _lastRightHandPosition;

        _leftHandFollower.rotation = _leftHandController.transform.rotation;
        _rightHandFollower.rotation = _rightHandController.transform.rotation;
    }

    private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector) { return transformToModify.position + transformToModify.rotation * offsetVector; }

    private bool IsHandColliding(out Vector3 firstIteration, bool leftHand, out SurfaceType hitSurface)
    {
        firstIteration = Vector3.zero;
        var gravityOffset = 2f * GRAVITY * Time.deltaTime * Time.deltaTime * Vector3.down;
        var currentHandPos = leftHand ? CurrentLeftHandPosition : CurrentRightHandPosition;
        var lastHandPos = leftHand ? _lastLeftHandPosition : _lastRightHandPosition;

        Vector3 distanceTraveled = currentHandPos - lastHandPos + gravityOffset;

        if (IterativeCollisionSphereCast(lastHandPos, _minimumRaycastDistance, distanceTraveled, _defaultPrecision, out var finalPosition, true, out hitSurface))
        {
            //this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
            firstIteration = IsHandTouching(leftHand) ? lastHandPos - currentHandPos : finalPosition - currentHandPos;

            _rigidbody.linearVelocity = Vector3.zero;

            return true;
        }

        return false;
    }

    private Vector3 GetRigidbodyMovement(bool leftHandColliding, Vector3 firstIterationLeftHand, bool rightHandColliding, Vector3 firstIterationRightHand)
    {
        //average or add
        Vector3 rigidBodyMovement;
        if (_isDoubleGrabEnabled && (leftHandColliding || _wasLeftHandTouching) && (rightHandColliding || _wasRightHandTouching))
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
                false,
                out _))
        {
            rigidBodyMovement = finalHeadPosition - _lastHeadPosition;
            //last check to make sure the head won't phase through geometry
            if (Physics.Raycast(
                    _lastHeadPosition,
                    _headCollider.transform.position - _lastHeadPosition + rigidBodyMovement,
                    out _,
                    (_headCollider.transform.position - _lastHeadPosition + rigidBodyMovement).magnitude + _headCollider.radius * _defaultPrecision * 0.999f,
                    _locomotionEnabledLayers.value,
                    QueryTriggerInteraction.Ignore))
            {
                rigidBodyMovement = _lastHeadPosition - _headCollider.transform.position;
            }
        }

        return rigidBodyMovement;
    }

    private Vector3 FinalHandPosition(Vector3 lastHandPos, ref bool thisHandColliding, in bool otherHandColliding, bool leftHand)
    {
        //do final hand position
        var currentHandPos = leftHand ? CurrentLeftHandPosition : CurrentRightHandPosition;
        bool thisHandTouching = IsHandTouching(leftHand);
        bool otherHandTouching = IsHandTouching(!leftHand);
        var distanceTraveled = currentHandPos - lastHandPos;

        bool singleHandColliding = !((thisHandColliding || thisHandTouching) && (otherHandColliding || otherHandTouching));

        if (IterativeCollisionSphereCast(lastHandPos, _minimumRaycastDistance, distanceTraveled, _defaultPrecision, out var finalPosition, singleHandColliding, out _))
        {
            thisHandColliding = true;
            return finalPosition;
        }
        else
        {
            return currentHandPos;
        }
    }

    private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand, out SurfaceType hitSurface)
    {
        hitSurface = SurfaceType.None;

        //first spherecast from the starting position to the final position
        if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out var hitInfo))
        {
            hitSurface = GetSurfaceType(hitInfo);

            //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

            //take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
            Vector3 firstPosition = endPosition;
            float slipPercentage = !singleHand ? _defaultSlideFactor : 0.001f;
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
                     precision * 0.66f, out _ /*endPosition*/, out _))
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
        if (_disableMovement)
        {
            finalPosition = Vector3.zero;
            hitInfo = default;
            return false;
        }
        //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
        //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

        //initial spherecase
        if (Physics.SphereCast(
                startPosition,
                sphereRadius * precision,
                movementVector,
                out hitInfo,
                movementVector.magnitude + sphereRadius * (1 - precision),
                _locomotionEnabledLayers.value,
                QueryTriggerInteraction.Ignore))
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
                    _locomotionEnabledLayers.value,
                    QueryTriggerInteraction.Ignore))
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
                         _locomotionEnabledLayers.value,
                         QueryTriggerInteraction.Ignore))
            {
                finalPosition = startPosition;
                hitInfo = innerHit;
            }

            return true;
        }
        //anti-clipping through geometry check
        else if (Physics.Raycast(
                     startPosition,
                     movementVector,
                     out hitInfo,
                     movementVector.magnitude + sphereRadius * precision * 0.999f,
                     _locomotionEnabledLayers.value,
                     QueryTriggerInteraction.Ignore))
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
                _rigidbody.linearVelocity = _denormalizedVelocityAverage.magnitude * JumpMultiplier > _maxJumpSpeed
                    ? _denormalizedVelocityAverage.normalized * _maxJumpSpeed
                    : JumpMultiplier * _denormalizedVelocityAverage;
            }
        }
    }

    private Vector3 UnstickHand(Vector3 lastHandPos, ref bool leftHandColliding, bool leftHand)
    {
        var currentHandPos = leftHand ? CurrentLeftHandPosition : CurrentRightHandPosition;

        //check to see if hand is stuck and we should unstick it
        bool sphereCastLeft = !Physics.SphereCast(
            _headCollider.transform.position,
            _minimumRaycastDistance * _defaultPrecision,
            currentHandPos - _headCollider.transform.position,
            out _,
            (currentHandPos - _headCollider.transform.position).magnitude - _minimumRaycastDistance,
            _locomotionEnabledLayers.value,
            QueryTriggerInteraction.Ignore);

        if (leftHandColliding && (currentHandPos - lastHandPos).magnitude > _unStickDistance && sphereCastLeft)
        {
            leftHandColliding = false;
            return currentHandPos;
        }

        return lastHandPos;
    }

    public bool IsHandTouching(bool leftHand) { return leftHand ? _wasLeftHandTouching : _wasRightHandTouching; }

    public void Turn(float degrees)
    {
        Vector3 pivot = _headCollider.transform.position;
        Vector3 axis = transform.up;

        // Rotate the rig around the head pivot.
        transform.RotateAround(pivot, axis, degrees);

        // Build the same rotation so we can apply it to cached values.
        Quaternion rotation = Quaternion.AngleAxis(degrees, axis);

        // Prevent a velocity spike by making the last-position match the post-turn pose.
        _lastPosition = transform.position;

        // Rotate cached previous frame tracked positions so next-frame deltas stay correct.
        _lastHeadPosition = pivot + rotation * (_lastHeadPosition - pivot);
        _lastLeftHandPosition = pivot + rotation * (_lastLeftHandPosition - pivot);
        _lastRightHandPosition = pivot + rotation * (_lastRightHandPosition - pivot);

        // Rotate velocity history so momentum direction stays consistent after turning.
        _denormalizedVelocityAverage = rotation * _denormalizedVelocityAverage;

        for (int i = 0; i < _velocityHistory.Length; i++)
        {
            _velocityHistory[i] = rotation * _velocityHistory[i];
        }
    }

    public void SetDisableMovement(bool disableMovement) { _disableMovement = disableMovement; }

    public void SetRuntimeJumpMultiplier(float multiplier) { _runtimeJumpMultiplier = multiplier; }

    private SurfaceType GetSurfaceType(RaycastHit hitInfo)
    {
        if (Enum.TryParse<SurfaceType>(hitInfo.collider.tag, true, out var surfaceType))
        {
            return surfaceType;
        }

        return SurfaceType.Default;
    }

    private bool IsBodyCollidingWithFloor()
    {
        float rayDistance = _bodyCollider.height * 0.5f; // Check slightly below the collider

        return Physics.Raycast(
            _bodyCollider.transform.position,
            Vector3.down,
            out _,
            rayDistance,
            _locomotionEnabledLayers.value,
            QueryTriggerInteraction.Ignore
        );
    }
}