using Fusion.XR.Shared;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.Player.Grab
{
    public class ShmackleGrabbable : MonoBehaviour, IGrabbable
    {
        #region Public Properties

        public ShmackleGrabber CurrentGrabber;

        public Transform[] SnappingPointsTransforms;

        [Tooltip("If false, it is only possible to grab a grabbable previously hovered")]
        public bool AllowedCloseHandGrabing = true;

        public bool IsGrabbed => CurrentGrabber != null;

        public Vector3 LocalPositionOffset => _localPositionOffset;

        public Quaternion LocalRotationOffset => _localRotationOffset;

        // IGrabbable events
        public UnityEvent<GameObject> OnLocalUserGrab => _onLocalUserGrab;
        public UnityEvent OnGrab => _onGrab;
        public UnityEvent OnUngrab => _onUngrab;

        public bool ExpectedIsKinematic
        {
            get => _expectedIsKinematic;
            set => _expectedIsKinematic = value;
        }

        #endregion

        [SerializeField] private bool _applyVelocityOnRelease = false;

        [SerializeField, Tooltip("Select a snapping point if you want to position the grabbable object at a specific position in the hand.\nNone - ignore snapping.\nRigPartSide - Only requires 2 snapping point transfroms, use 0 for left side, 1 for right side\nNearest - Choose whatever is the nearest snap point from the grabber")]
        private SnapType _snapType;

        private Rigidbody _grabbableRigidbody;

        private ShmackleNetworkGrabbable _networkGrabbable;
        private Vector3 _localPositionOffset;
        private Quaternion _localRotationOffset;
        private ShmackleGrabber _lastGrabber;
        private bool _expectedIsKinematic = true;

        [Header("Events")]
        [Tooltip("Called only for the local grabber, when they may wait for authority before grabbing. OnGrab will be called on all users")]
        [SerializeField] private UnityEvent<GameObject> _onLocalUserGrab = new UnityEvent<GameObject>();

        [Tooltip("Called only for the local grabber, on grab")]
        [SerializeField] private UnityEvent _onGrab = new UnityEvent();

        [Tooltip("Called only for the local grabber, on ungrab")]
        [SerializeField] private UnityEvent _onUngrab = new UnityEvent();

        #region Velocity estimation

        // Velocity computation
        private const int _velocityBufferSize = 5;
        private Vector3 _lastPosition;
        private Quaternion _previousRotation;
        private readonly Vector3[] _lastMoves = new Vector3[_velocityBufferSize];
        private readonly Vector3[] _lastAngularVelocities = new Vector3[_velocityBufferSize];
        private readonly float[] _lastDeltaTime = new float[_velocityBufferSize];
        private int _lastMoveIndex = 0;

        private Vector3 Velocity
        {
            get
            {
                Vector3 move = Vector3.zero;
                float time = 0;
                for (int i = 0; i < _velocityBufferSize; i++)
                {
                    if (_lastDeltaTime[i] != 0)
                    {
                        move += _lastMoves[i];
                        time += _lastDeltaTime[i];
                    }
                }

                if (time == 0) return Vector3.zero;
                return move / time;
            }
        }

        private Vector3 AngularVelocity
        {
            get
            {
                Vector3 culmulatedAngularVelocity = Vector3.zero;
                int step = 0;
                for (int i = 0; i < _velocityBufferSize; i++)
                {
                    if (_lastDeltaTime[i] != 0)
                    {
                        culmulatedAngularVelocity += _lastAngularVelocities[i];
                        step++;
                    }
                }

                if (step == 0) return Vector3.zero;
                return culmulatedAngularVelocity / step;
            }
        }

        #endregion

        private enum SnapType
        {
            None,
            ByRigPartSide,
            Nearest
        }

        protected virtual void Awake()
        {
            _networkGrabbable = GetComponent<ShmackleNetworkGrabbable>();

            if (_grabbableRigidbody == null)
                _grabbableRigidbody = GetComponent<Rigidbody>();

            if (_networkGrabbable == null && _grabbableRigidbody != null)
            {
                _expectedIsKinematic = _grabbableRigidbody.isKinematic;
            }
        }

        protected virtual void Update()
        {
            TrackVelocity();

            if (_networkGrabbable == null || _networkGrabbable.Object == null)
            {
                // Offline behavior: follow the grabber directly
                if (CurrentGrabber != null)
                {
                    var grabberPose = CurrentGrabber.RigPart.RigPartPose;
                    Follow(followedTransformPosition: grabberPose.position,
                        followedTransformRotation: grabberPose.rotation,
                        _localPositionOffset,
                        _localRotationOffset
                    );
                }
            }
        }

        private void TrackVelocity()
        {
            _lastMoves[_lastMoveIndex] = transform.position - _lastPosition;
            _lastAngularVelocities[_lastMoveIndex] = _previousRotation.AngularVelocityChange(transform.rotation, Time.deltaTime);
            _lastDeltaTime[_lastMoveIndex] = Time.deltaTime;
            _lastMoveIndex = (_lastMoveIndex + 1) % _velocityBufferSize;
            _lastPosition = transform.position;
            _previousRotation = transform.rotation;
        }

        private void ResetVelocityTracking()
        {
            for (int i = 0; i < _velocityBufferSize; i++)
                _lastDeltaTime[i] = 0;

            _lastMoveIndex = 0;
        }

        private Transform GetSnapTransform(ShmackleGrabber source)
        {
            if (_snapType == SnapType.None)
                return null;

            if (SnappingPointsTransforms == null)
                return null;

            int snappingPointsCount = SnappingPointsTransforms.Length;
            if (snappingPointsCount == 0)
                return null;

            Transform finalSnapPoint = null;

            if (snappingPointsCount == 1)
            {
                finalSnapPoint = SnappingPointsTransforms[0];
            }
            else
            {
                if (_snapType == SnapType.ByRigPartSide)
                {
                    if (source.RigPart != null)
                    {
                        int sideIndex = 0;
                        if (source.RigPart.GrabSide == RigPartSide.Right)
                            sideIndex = 1;

                        finalSnapPoint = SnappingPointsTransforms[sideIndex];
                    }
                }
                else
                {
                    float nearestDistSqr = Mathf.Infinity;
                    Vector3 currentPos = source.transform.position;

                    for (int i = 0; i < snappingPointsCount; i++)
                    {
                        Transform snappingPointTransform = SnappingPointsTransforms[i];
                        if (snappingPointTransform == null)
                            continue;

                        float distSqr =
                            (snappingPointTransform.position - currentPos).sqrMagnitude;
                        if (distSqr < nearestDistSqr)
                        {
                            nearestDistSqr = distSqr;
                            finalSnapPoint = snappingPointTransform;
                        }
                    }
                }
            }

            return finalSnapPoint;
        }

        public virtual void TryGrab(ShmackleGrabber newGrabber, Transform grabPointTransform = null)
        {
            if (CurrentGrabber == newGrabber)
                return;

            if (_networkGrabbable && !_networkGrabbable.TryGrab())
                return;

            if (CurrentGrabber != null)
                CurrentGrabber.TryRelease();

            _onLocalUserGrab?.Invoke(newGrabber.gameObject);

            Transform snapTransform = GetSnapTransform(newGrabber);
            if (snapTransform != null)
            {
                transform.rotation = newGrabber.transform.rotation * Quaternion.Inverse(snapTransform.localRotation);
                transform.position = newGrabber.transform.position - (snapTransform.position - transform.position);
            }

            var grabberPose = newGrabber.RigPart.RigPartPose;

            (_localPositionOffset, _localRotationOffset) = TransformManipulations.UnscaledOffset(
                referenceTransformPosition: grabberPose.position,
                grabberPose.rotation,
                transformToOffset: transform
            );

            CurrentGrabber = newGrabber;
            _lastGrabber = CurrentGrabber;

            if (_networkGrabbable)
            {
                _networkGrabbable.LocalGrab();
            }
            else
            {
                LockObjectPhysics();
            }

            _onGrab?.Invoke();
        }

        public virtual void TryRelease()
        {
            CurrentGrabber = null;

            if (_networkGrabbable)
            {
                _networkGrabbable.LocalUngrab();
            }
            else
            {
                UnlockObjectPhysics();
            }

            _onUngrab?.Invoke();
        }

        public virtual void LockObjectPhysics()
        {
            if (_grabbableRigidbody)
                _grabbableRigidbody.isKinematic = true;
        }

        public virtual void UnlockObjectPhysics()
        {
            if (_grabbableRigidbody) _grabbableRigidbody.isKinematic = _expectedIsKinematic;

            if (_applyVelocityOnRelease && _grabbableRigidbody && _grabbableRigidbody.isKinematic == false)
            {
                Vector3 trackedVelocity = Velocity;
                float speed = trackedVelocity.magnitude;
                Vector3 finalVelocity = trackedVelocity;

                if (_lastGrabber != null)
                {
                    float verticalForce = _lastGrabber.VerticalForce;
                    float horizontalForce = _lastGrabber.HorizontalForce;
                    float throwPower = _lastGrabber.ThrowPower;
                    
                    Vector3 stableForward = trackedVelocity.normalized;

                    // Assistive components
                    Vector3 forwardAssist = stableForward * horizontalForce;
                    Vector3 upwardAssist = Vector3.up * verticalForce;

                    // Final assisted direction
                    Vector3 assistedDirection = (stableForward + forwardAssist + upwardAssist).normalized;

                    // Final throw speed
                    float finalSpeed = speed * throwPower;
                    finalVelocity = assistedDirection * finalSpeed;
                }

#if UNITY_6000_0_OR_NEWER
                _grabbableRigidbody.linearVelocity = finalVelocity;
#else
                _grabbableRigidbody.velocity = finalVelocity;
#endif
                _grabbableRigidbody.angularVelocity = AngularVelocity;
            }

            ResetVelocityTracking();
        }

        public void Follow(Transform followedTransform,
            Vector3 localPositionOffsetToFollowed,
            Quaternion localRotationOffsetTofollowed)
        {
            Follow(followedTransform.position,
                followedTransform.rotation,
                localPositionOffsetToFollowed,
                localRotationOffsetTofollowed);
        }

        public virtual void Follow(Vector3 followedTransformPosition,
            Quaternion followedTransformRotation,
            Vector3 localPositionOffsetToFollowed,
            Quaternion localRotationOffsetTofollowed)
        {
            (transform.position, transform.rotation) =
                TransformManipulations.ApplyUnscaledOffset(
                    referenceTransformPosition: followedTransformPosition,
                    referenceTransformRotation: followedTransformRotation,
                    offset: localPositionOffsetToFollowed,
                    rotationOffset: localRotationOffsetTofollowed
                );
        }

        public Rigidbody GetRigidbody() { return _grabbableRigidbody; }
    }
}