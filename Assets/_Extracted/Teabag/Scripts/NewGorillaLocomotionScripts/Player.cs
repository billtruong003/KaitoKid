using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Teabag.Core;
using Squido.JungleXRKit.Core;
using Teabag.Player;

namespace GorillaLocomotion
{
    using System.Collections.Generic;
    using Teabag.Core;
    using Teabag.Player;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [DefaultExecutionOrder(-50)]
    public class Player : MonoBehaviour
    {
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        [Header("Hands")]
        public Transform leftHandFollower;
        public Transform rightHandFollower;

        public Transform leftHandTransform;
        public Transform rightHandTransform;

        public Transform leftHandOriginal;
        public Transform rightHandOriginal;

        public Transform leftHand;
        public Transform rightHand;

        public Transform leftHandResting;
        public Transform rightHandResting;

        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private Vector3 lastHeadPosition;
        private Vector3 lastHeadLocalPosition;

        public Vector3 leftHandDifference;
        public Vector3 rightHandDifference;

        [Header("Options")]

        public Rigidbody playerRigidBody;

        public int velocityHistorySize;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;

        private float defaultVelocityLimit;
        public float velocityLimit;
        private float defaultMaxJumpSpeed;
        private float defaultJumpMultiplier;
        public float maxJumpSpeed;
        public float jumpMultiplier;
        public float minimumRaycastDistance = 0.05f;
        public float defaultSlideFactor = 0.03f;
        public float defaultPrecision = 0.995f;
        public float maxFollowerDistance = 0.5f;

        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 currentVelocity;
        public Vector3 CurrentVelocity { get => currentVelocity; }
        private Vector3 denormalizedVelocityAverage;
        private bool jumpHandIsLeft;
        private Vector3 lastPosition;

        // NonAlloc raycast buffer for body floor check
        private readonly RaycastHit[] _bodyRayHits = new RaycastHit[4];

        public Vector3 rightHandOffset;
        public Vector3 leftHandOffset;

        public LayerMask locomotionEnabledLayers;

        public bool wasLeftHandTouching;
        public bool wasRightHandTouching;
        public bool wasBodyTouching;

        public bool respawning = false;

        public bool disableMovement = false;

        [Header("Hand Taps")]
        public HandTap handTap;
        public List<HandTapType> handTaps = new List<HandTapType>();

        public bool movingHand;
        public bool bodyTouching;

        [Header("Important")]
        public Fusion.NetworkObject snowball; // Snowball prefab (Gameplay assembly)

        [Header("Abilities")]
        public PlayerAbilities playerAbilities;
        public bool lockIsKinematic;

        Vector3 targetVelocity;

        GorillaTouchingTarget target;
        Vector3 gravity;

        public static bool isPaused;

        private IGorillaService _gorillaService;

        private void OnApplicationFocus(bool focus)
        {
#if !UNITY_EDITOR
            isPaused = !focus;
#endif
        }

        private void Awake()
        {
            InitializeValues();

            var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.renderScale = SystemInfo.deviceName == "Quest 2" ? 0.8f : 1;
            //BetaLogger.OnRecievedLog(SystemInfo.deviceName, SystemInfo.deviceModel, LogType.Log);

            _gorillaService = ServiceLocator.Get<IGorillaService>();
        }

        public void ForceForceExplosion(float force, Vector3 position, float radius)
        {
            lastLeftHandPosition = headCollider.transform.position;
            lastRightHandPosition = headCollider.transform.position;
            playerRigidBody.AddExplosionForce(force, position, radius);

            currentVelocity = playerRigidBody.linearVelocity;
            transform.position += currentVelocity * Time.deltaTime;
            //lastPosition = transform.position;
        }

        public void ForceForce(Vector3 force)
        {
            lastLeftHandPosition = headCollider.transform.position;
            lastRightHandPosition = headCollider.transform.position;
            playerRigidBody.AddForce(force);

            currentVelocity = playerRigidBody.linearVelocity;
            transform.position += currentVelocity * Time.deltaTime;
            //lastHeadPosition = headCollider.transform.position;
            //lastPosition = transform.position;
        }

        public void Respawn()
        {
            //Transform t = Mark.FindObjectWithMark("Plane").transform;
            //Teleport(t.position + Vector3.up * 2);
            var mapService = ServiceLocator.Get<IMapService>();
            var spawnPoint = mapService?.OnGetSpawnPoint?.Invoke();
            if (spawnPoint.HasValue)
                Teleport(spawnPoint.Value);
            if (Physics.gravity == Vector3.zero)
            {
                lastLeftHandPosition = headCollider.transform.position;
                lastRightHandPosition = headCollider.transform.position;
                playerRigidBody.AddForce(Vector3.down * 10);
            }
        }

        public void Teleport(Vector3 position)
        {
            GameLogger.Info(this, "Teleporting to: " + position);

            var prevInterpolation = playerRigidBody.interpolation;
            playerRigidBody.interpolation = RigidbodyInterpolation.None;

            transform.position = position;
            headCollider.transform.position = position;
            leftHandOriginal.position = position + (leftHandOriginal.position - transform.position);
            rightHandOriginal.position = position + (rightHandOriginal.position - transform.position);

            lastPosition = transform.position;
            lastHeadPosition = headCollider.transform.position;
            lastHeadLocalPosition = headCollider.transform.localPosition;
            lastLeftHandPosition = leftHandTransform.position;
            lastRightHandPosition = rightHandTransform.position;

            playerRigidBody.interpolation = prevInterpolation;

            // Standard velocity reset
            ResetVelocity();
        }

        /// <summary>
        /// Instantly zeroes out all locomotion velocities and history.
        /// Matches Jungle's LocomotionController.Reset() pattern.
        /// </summary>
        public void ResetVelocity()
        {
            if (playerRigidBody != null)
            {
                playerRigidBody.linearVelocity = Vector3.zero;
                playerRigidBody.angularVelocity = Vector3.zero;
            }

            if (velocityHistory != null)
            {
                System.Array.Clear(velocityHistory, 0, velocityHistory.Length);
            }

            velocityIndex = 0;
            currentVelocity = Vector3.zero;
            denormalizedVelocityAverage = Vector3.zero;
            lastPosition = transform.position;

            // Reset trackers to current positions to prevent enormous deltas on next frame
            lastHeadPosition = headCollider.transform.position;
            lastLeftHandPosition = leftHandTransform.position;
            lastRightHandPosition = rightHandTransform.position;
        }

        /// <summary>
        /// Moves the rig root to the target position while preserving child local positions.
        /// headCollider, hands, etc. are children of this transform — setting transform.position
        /// moves them automatically. We do NOT touch them again (Teleport() zeroes the head offset
        /// and has a bug where hands don't move at all).
        /// Also clears the full velocity state (history + average) to prevent phantom jumps.
        /// Use this when restoring the rig root position after a scene reload.
        /// </summary>
        public void TeleportRigRoot(Vector3 rigRootPosition)
        {
            GameLogger.Info(this, $"TeleportRigRoot to: {rigRootPosition} " +
                $"(from={transform.position}, head={headCollider.transform.position}, " +
                $"headLocal={headCollider.transform.localPosition})");

            // Moving the parent automatically moves all children (headCollider, hands)
            // while preserving their local positions (VR tracking offset, hand offsets).
            // CRITICAL: Must set playerRigidBody.position FIRST — for non-kinematic bodies,
            // the physics system can override transform.position on the next FixedUpdate.
            playerRigidBody.position = rigRootPosition;
            transform.position = rigRootPosition;

            // Sync all position tracking state so the locomotion system
            // doesn't interpret the teleport as movement on the next frame.
            lastPosition = transform.position;
            lastHeadPosition = headCollider.transform.position;
            lastHeadLocalPosition = headCollider.transform.localPosition;
            lastLeftHandPosition = leftHandTransform.position;
            lastRightHandPosition = rightHandTransform.position;

            // Clear velocity state completely to prevent phantom jumps.
            // velocityHistory[] retains stale data from the old Player instance's
            // position — StoreVelocities() would compute a huge delta without this.
            playerRigidBody.linearVelocity = Vector3.zero;
            playerRigidBody.angularVelocity = Vector3.zero;
            currentVelocity = Vector3.zero;
            denormalizedVelocityAverage = Vector3.zero;
            for (int i = 0; i < velocityHistory.Length; i++)
                velocityHistory[i] = Vector3.zero;
        }

        /// <summary>
        /// Clears all velocity tracking state without teleporting.
        /// Call when the rig stops being repositioned by an external system
        /// (e.g., subway or train tracking) to prevent phantom jumps.
        /// </summary>
        public void ResetVelocityState()
        {
            playerRigidBody.linearVelocity = Vector3.zero;
            playerRigidBody.angularVelocity = Vector3.zero;
            currentVelocity = Vector3.zero;
            denormalizedVelocityAverage = Vector3.zero;
            for (int i = 0; i < velocityHistory.Length; i++)
                velocityHistory[i] = Vector3.zero;
            lastPosition = transform.position;
            lastHeadPosition = headCollider.transform.position;
            lastHeadLocalPosition = headCollider.transform.localPosition;
            lastLeftHandPosition = leftHandTransform.position;
            lastRightHandPosition = rightHandTransform.position;
        }

        public void InitializeValues()
        {
            playerRigidBody = GetComponent<Rigidbody>();
            velocityHistory = new Vector3[velocityHistorySize];
            lastLeftHandPosition = leftHandFollower.transform.position;
            lastRightHandPosition = rightHandFollower.transform.position;
            lastHeadPosition = headCollider.transform.position;
            velocityIndex = 0;
            lastPosition = transform.position;
            defaultJumpMultiplier = jumpMultiplier;
            defaultMaxJumpSpeed = maxJumpSpeed;
            defaultVelocityLimit = velocityLimit;
        }

        private Vector3 CurrentLeftHandPosition()
        {
            if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(leftHandTransform, leftHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 CurrentRightHandPosition()
        {
            if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(rightHandTransform, rightHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private void Update()
        {

            /*
            if (isPaused)
            {
                leftHandOriginal.position = headCollider.transform.position;
                rightHandOriginal.position = headCollider.transform.position;
                lastLeftHandPosition = leftHandTransform.position;
                lastRightHandPosition = rightHandTransform.position;
            }
            */

            float maxVelocity = 20;
            if (playerRigidBody.linearVelocity.magnitude > maxVelocity)
            {
                playerRigidBody.linearVelocity = Vector3.ClampMagnitude(playerRigidBody.linearVelocity, maxVelocity);
            }

            if (Vector3.Distance(lastHeadLocalPosition, headCollider.transform.localPosition) / Time.deltaTime > 5)
            {
                transform.position += lastHeadPosition - headCollider.transform.position;
            }
            lastHeadLocalPosition = headCollider.transform.localPosition;

            float speedMultiplier = 1;
            if ((GameServices.IsModEnabled?.Invoke("Ultra Speedboost") ?? false))
                speedMultiplier = 8;
            else if ((GameServices.IsModEnabled?.Invoke("Quick Speedboost") ?? false))
                speedMultiplier = 4;
            else if ((GameServices.IsModEnabled?.Invoke("Speedboost") ?? false))
                speedMultiplier = 2;

            velocityLimit = defaultVelocityLimit * speedMultiplier;
            maxJumpSpeed = defaultMaxJumpSpeed * speedMultiplier;
            jumpMultiplier = defaultJumpMultiplier * speedMultiplier;
            /*
            if (speedMultiplier)
            {
                //Debug.Log("Boosting");
                float speedMultiplier = 2;
                velocityLimit = defaultVelocityLimit * speedMultiplier;
                maxJumpSpeed = defaultMaxJumpSpeed * speedMultiplier;
                jumpMultiplier = defaultJumpMultiplier * speedMultiplier;
            }
            else
            {
                velocityLimit = defaultVelocityLimit;
                maxJumpSpeed = defaultMaxJumpSpeed;
                jumpMultiplier = defaultJumpMultiplier;
            }
            */

            if ((GameServices.IsModEnabled?.Invoke("Wall Gravity") ?? false))
            {
                if (playerRigidBody.useGravity)
                {
                    playerRigidBody.linearVelocity += -Physics.gravity * Time.deltaTime;
                    playerRigidBody.linearVelocity += gravity * Time.deltaTime;
                }
                else
                    gravity = Physics.gravity;
            }
            else
                gravity = Physics.gravity;

            if ((GameServices.IsModEnabled?.Invoke("Low Gravity") ?? false))
            {
                //Debug.Log("Low gravity");
                if (playerRigidBody.useGravity)
                    playerRigidBody.linearVelocity += (-gravity / 2) * Time.deltaTime;
            }

            if ((_gorillaService?.LocalGorilla as Gorilla)?.health?.isDead ?? false)
            {
                if (playerRigidBody.useGravity)
                    playerRigidBody.linearVelocity += (-gravity / 4) * Time.deltaTime;
            }

            if (!VRInputHandler.IsTracked(true))
            {
                leftHandOriginal.position = leftHandResting.position;
                leftHandOriginal.rotation = leftHandResting.rotation;
                lastLeftHandPosition = leftHandTransform.position;
            }

            if (!VRInputHandler.IsTracked(false))
            {
                rightHandOriginal.position = rightHandResting.position;
                rightHandOriginal.rotation = rightHandResting.rotation;
                lastRightHandPosition = rightHandTransform.position;
            }

#if UNITY_EDITOR
            leftHand.transform.localRotation = Quaternion.Euler(-45, 0, 90);
            rightHand.transform.localRotation = Quaternion.Euler(-(90 + 45), 180, 90);
#endif

            var runner = GameServices.GetRunner?.Invoke();
            if (runner != null && !lockIsKinematic)
                playerRigidBody.isKinematic = runner.IsSceneManagerBusy;

            // if (GameServices.IsInBlimp != null)
            // {
            //     if (!(GameServices.IsInRoom?.Invoke() ?? false) && !(GameServices.IsInBlimp?.Invoke() ?? false))
            //     {
            //         Respawn();
            //     }
            // }

            if (target != null)
            {
                if (!target.gameObject.activeInHierarchy)
                    DestroyTarget();
            }

            //Physics.SyncTransforms();

            bool leftHandColliding = false;
            bool rightHandColliding = false;
            Vector3 finalPosition;
            Vector3 rigidBodyMovement = Vector3.zero;
            Vector3 firstIterationLeftHand = Vector3.zero;
            Vector3 firstIterationRightHand = Vector3.zero;
            RaycastHit hitInfo;

            bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

            if (target != null)
            {
                target.Calculate();
                targetVelocity = TargetVelocity();
            }

            Vector3 joystickMovement;
            if (GameServices.FlyModeEnabled)
            {
                if(!playerRigidBody.isKinematic)
                {
                    playerRigidBody.isKinematic = true;
                }
                joystickMovement = FlyMove();
            }
            else
            {
                joystickMovement = JoystickMovement();
            }

            lastLeftHandPosition += targetVelocity + joystickMovement;
            lastRightHandPosition += targetVelocity + joystickMovement;

            // Legacy blimp hand clamping
            if ((GameServices.IsInBlimp?.Invoke() ?? false))
            {
                if (Vector3.Distance(lastLeftHandPosition, leftHandTransform.position) > maxFollowerDistance)
                    lastLeftHandPosition = leftHandTransform.position;

                if (Vector3.Distance(lastRightHandPosition, rightHandTransform.position) > maxFollowerDistance)
                    lastRightHandPosition = rightHandTransform.position;
            }

            float d = 0.3f;
            bodyTouching = false;
            int bodyHitCount = Physics.RaycastNonAlloc(bodyCollider.transform.position, Vector3.down, _bodyRayHits, d, locomotionEnabledLayers.value);
            if (bodyHitCount > 0)
            {
                hitInfo = _bodyRayHits[0];
                if (hitInfo.collider != null && hitInfo.collider.gameObject != null)
                {
                    SetTarget(hitInfo.collider.gameObject, bodyCollider.transform.position);
                }
                bodyTouching = true;
                Debug.DrawLine(bodyCollider.transform.position, bodyCollider.transform.position + Vector3.down * d, Color.green);
            }
            else
            {
                bodyTouching = false;
                Debug.DrawLine(bodyCollider.transform.position, bodyCollider.transform.position + Vector3.down * d, Color.white);
            }

            //left hand

            Vector3 distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition + Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;
            leftHandDifference = distanceTraveled;

            if (GameServices.FlyModeEnabled)
            {
                firstIterationLeftHand = lastLeftHandPosition - CurrentLeftHandPosition();
            }
            else
            {
                if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out hitInfo, out finalPosition, true))
                {
                    firstIterationLeftHand = (wasLeftHandTouching ? lastLeftHandPosition : finalPosition) - CurrentLeftHandPosition();

                    playerRigidBody.linearVelocity = Vector3.zero;

                    if (hitInfo.collider != null)
                    {
                        gravity = hitInfo.normal * Physics.gravity.y;

                        SetTarget(hitInfo.collider.gameObject, CurrentLeftHandPosition());
                    }

                    leftHandColliding = true;
                }
            }

            //right hand

            distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition + Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;
            rightHandDifference = distanceTraveled;

            if (GameServices.FlyModeEnabled)
            {
                firstIterationRightHand = lastRightHandPosition - CurrentRightHandPosition();
            }
            else
            {
                if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out hitInfo, out finalPosition, true))
                {
                    firstIterationRightHand = (wasRightHandTouching ? lastRightHandPosition : finalPosition) - CurrentRightHandPosition();

                    playerRigidBody.linearVelocity = Vector3.zero;

                    if (hitInfo.collider != null)
                    {
                        gravity = hitInfo.normal * Physics.gravity.y;

                        SetTarget(hitInfo.collider.gameObject, CurrentRightHandPosition());
                    }

                    rightHandColliding = true;
                }
            }

            //average or add

            if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
            {
                //this lets you grab stuff with both hands at the same time
                rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
            }
            else
            {
                rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
            }

            //check valid head movement

            if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, headCollider.transform.position + rigidBodyMovement - lastHeadPosition, defaultPrecision, out RaycastHit hit, out finalPosition, false))
            {
                rigidBodyMovement = finalPosition - lastHeadPosition;
                //last check to make sure the head won't phase through geometry
                if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + rigidBodyMovement, out hitInfo, (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
                }
            }

            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position = transform.position + rigidBodyMovement;
            }

            lastHeadPosition = headCollider.transform.position;

            //do final left hand position

            distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition;

            if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out hitInfo, out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
            {
                lastLeftHandPosition = finalPosition;
                leftHandColliding = true;
            }
            else
            {
                lastLeftHandPosition = CurrentLeftHandPosition();
            }

            //do final right hand position

            distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition;

            if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision, out hitInfo, out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
            {
                lastRightHandPosition = finalPosition;
                rightHandColliding = true;
            }
            else
            {
                lastRightHandPosition = CurrentRightHandPosition();
            }

            if (bodyTouching && !leftHandColliding && !rightHandColliding)
            {
                if (target != null)
                {
                    transform.position += targetVelocity;
                }
            }

            StoreVelocities();

            if ((rightHandColliding || leftHandColliding) && !disableMovement)
            {
                if (denormalizedVelocityAverage.magnitude > velocityLimit)
                {
                    if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                    {
                        playerRigidBody.linearVelocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
                        //playerRigidBody.velocity = denormalizedVelocityAverage.normalized * maxJumpSpeed + TargetVelocity();
                    }
                    else
                    {
                        playerRigidBody.linearVelocity = jumpMultiplier * denormalizedVelocityAverage;
                        //playerRigidBody.velocity = jumpMultiplier * denormalizedVelocityAverage + TargetVelocity();
                    }
                }
            }

            //check to see if left hand is stuck and we should unstick it

            if (leftHandColliding && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentLeftHandPosition() - headCollider.transform.position, out hitInfo, (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
            {
                lastLeftHandPosition = CurrentLeftHandPosition();
                leftHandColliding = false;
            }

            //check to see if right hand is stuck and we should unstick it

            if (rightHandColliding && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance && !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision, CurrentRightHandPosition() - headCollider.transform.position, out hitInfo, (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance, locomotionEnabledLayers.value))
            {
                lastRightHandPosition = CurrentRightHandPosition();
                rightHandColliding = false;
            }

            leftHandFollower.position = lastLeftHandPosition + targetVelocity;
            rightHandFollower.position = lastRightHandPosition + targetVelocity;

            //if (!rightHandColliding && !leftHandColliding && !bodyTouching)
            //{
            //    //Debug.Log("Just destroying for fun");
            //    if (wasRightHandTouching || wasLeftHandTouching || wasBodyTouching)
            //        DestroyTarget();
            //}

            wasLeftHandTouching = leftHandColliding;
            wasRightHandTouching = rightHandColliding;
            wasBodyTouching = bodyTouching;
        }

        public Vector3 JoystickMovement()
        {
            return Vector3.zero;

            if (!bodyTouching)
                return Vector3.zero;

            Vector3 direction = headCollider.transform.forward;
            direction.y = 0;
            direction = direction.normalized;

            Vector2 joystick = VRInputHandler.GetJoystick(true);
            direction *= joystick.y;

            return direction * maxJumpSpeed * Time.deltaTime / 2;
        }

        public Vector3 FlyMove()
        {
            Vector2 leftJoystick = VRInputHandler.GetJoystick(true);
            Vector2 rightJoystick = VRInputHandler.GetJoystick(false);

            Vector3 headForward = headCollider.transform.forward;
            Vector3 headRight = headCollider.transform.right;
            headForward.y = 0;
            headForward = headForward.normalized;

            headRight.y = 0;
            headRight = headRight.normalized;
            Vector3 moveDirection = (headForward * leftJoystick.y) + (headRight * leftJoystick.x);
            Vector3 upDownDirection = Vector3.up * rightJoystick.y;
            Vector3 flyDirection = moveDirection + upDownDirection;
            return flyDirection * velocityLimit * 4 * Time.deltaTime;
        }

        void SetTarget(GameObject obj, Vector3 position)
        {
            Transform objRoot = obj.transform.root;
            if (objRoot.TryGetComponent<Gorilla>(out _) || objRoot == transform.root)
                return;

            if (obj.TryGetComponent(out SurfaceFollowPermission permission))
            {
                if (!permission.isAllowed)
                    return;
            }

            if (target != null)
            {
                //if (FindObjectOfType<GorillaTouchingTarget>().gameObject != obj)
                if (target.gameObject != obj)
                {
                    DestroyTarget();
                    target = obj.AddComponent<GorillaTouchingTarget>();
                    target.Initialise(position);
                    //Debug.Log("Targeting " + target.name);
                }
            }
            else
            {
                target = obj.AddComponent<GorillaTouchingTarget>();
                target.Initialise(position);
                //Debug.Log("Targeting " + target.name);
            }
        }

        void DestroyTarget()
        {
            if (target != null)
            {
                Destroy(target);
                target = null;
            }
        }

        public bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out RaycastHit hitInfo, out Vector3 endPosition, bool singleHand)
        {
            Vector3 movementToProjectedAboveCollisionPlane;
            Surface gorillaSurface;
            float slipPercentage;
            //first spherecast from the starting position to the final position
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out hitInfo))
            {
                //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

                //take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
                Vector3 firstPosition = endPosition;
                gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                //Debug.Log(hitInfo.transform.name);

                //maxJumpSpeed = defaultMaxJumpSpeed;
                //jumpMultiplier = defaultJumpMultiplier;
                if (gorillaSurface != null)
                {
                    if (gorillaSurface.overrideJumpValues)
                    {
                        maxJumpSpeed = gorillaSurface.maxJumpSpeed;
                        jumpMultiplier = gorillaSurface.jumpSpeedMultiplier;
                    }
                }
                slipPercentage = gorillaSurface != null ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
                movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
                if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface
                else if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius, startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition), precision * precision * precision, out endPosition, out hitInfo))
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
            else if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f, movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out hitInfo))
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

        public bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
            //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial spherecase
            RaycastHit innerHit;
            if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1 - precision), locomotionEnabledLayers.value))
            {
                //if we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                //check a spherecase from the original position to the intended final position
                if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision), locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                //bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
                else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }
                return true;
            }
            //anti-clipping through geometry check
            else if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
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

        public bool IsHandTouching(bool forLeftHand)
        {
            if (forLeftHand)
            {
                return wasLeftHandTouching;
            }
            else
            {
                return wasRightHandTouching;
            }
        }

        public void Turn(float degrees)
        {
            // Temporarily disable interpolation so it doesn't blend
            // from the pre-turn position, causing a visual revert.
            var prevInterpolation = playerRigidBody.interpolation;
            playerRigidBody.interpolation = RigidbodyInterpolation.None;

            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            playerRigidBody.position = transform.position;
            playerRigidBody.rotation = transform.rotation;

            playerRigidBody.interpolation = prevInterpolation;

            denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
            }
        }

        private void StoreVelocities()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistorySize;
            Vector3 oldestVelocity = velocityHistory[velocityIndex];
            currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
            velocityHistory[velocityIndex] = currentVelocity;
            lastPosition = transform.position;
        }

        public Vector3 TargetVelocity()
        {
            if (target != null)
                return target.velocity;

            return Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            //Debug.Log("Hit: " + StaticRoyaleObject.FullName(collision.collider.gameObject));
        }

        public HandTapType PlayHandTap(RaycastHit hit, bool isLeftHand, bool isMine)
        {
            if (isMine)
                VRInputHandler.VibrateController(isLeftHand, 0.1f, 0.1f);

            return PlayHandTap(hit, isLeftHand);
        }

        public HandTapType PlayHandTap(RaycastHit hit, bool isLeftHand)
        {
            GameObject surface = hit.collider.gameObject;
            HandTapType type = GetHandTapType(hit);

            if (type == null)
                return null;

            Transform t = PlayHandTap(hit.point, hit.normal, isLeftHand, type);
            t.parent = surface.transform;

            return type;
        }

        public HandTapType GetHandTapType(RaycastHit hit)
        {
            GameObject surface = hit.collider.gameObject;
            HandTapType type = null;

            if (surface.TryGetComponent(out SurfaceHitSound hitSound))
            {
                type = handTaps[(int)hitSound.index];
            }
            else if (hit.collider.TryGetComponent(out Renderer renderer))
            {
                hit.collider.TryGetComponent(out MeshFilter filter);
                Texture texture = null;
                if (filter != null && renderer.materials[GetSubmeshFromTriangle(hit.triangleIndex, filter.sharedMesh)].HasProperty("_MainTexture"))
                    texture = renderer.materials[GetSubmeshFromTriangle(hit.triangleIndex, filter.sharedMesh)].mainTexture;

                if (texture == null && renderer.material.HasProperty("_Sides"))
                    texture = renderer.material.GetTexture("_Sides");

                if (texture == null  && renderer.material.HasProperty("_MainTexture"))
                    texture = renderer.material.mainTexture;

                if (texture != null)
                    type = GetHandTap(texture);
                /*
                if (!renderer.enabled)
                {
                    texture = null;
                }
                else
                {

                }
                */
            }
            else if (surface.GetComponent<Terrain>() != null)
            {
                Terrain terrain = hit.collider.GetComponent<Terrain>();
                Vector3 terrainPosition = hit.point - hit.collider.transform.position;
                Vector3 splatMapPosition = new Vector3(terrainPosition.x / terrain.terrainData.size.x, 0, terrainPosition.z / terrain.terrainData.size.z);
                int x = Mathf.FloorToInt(splatMapPosition.x * terrain.terrainData.alphamapWidth);
                int z = Mathf.FloorToInt(splatMapPosition.z * terrain.terrainData.alphamapHeight);

                float[,,] alphaMap = terrain.terrainData.GetAlphamaps(x, z, 1, 1);
                int index = 0;
                for (int i = 0; i < alphaMap.Length; i++)
                {
                    if (alphaMap[0, 0, i] > alphaMap[0, 0, index])
                    {
                        index = i;
                    }
                }

                Texture texture = terrain.terrainData.terrainLayers[index].diffuseTexture;
                type = GetHandTap(texture);
            }

            return type;
        }

        public int GetSubmeshFromTriangle(int triangleIndex, Mesh mesh)
        {
            try
            {
                if (!mesh.isReadable)
                    return 0;

                if (mesh.subMeshCount < 1)
                    return 0;

                int[] hitTriangles = new int[]
                {
                    mesh.triangles[triangleIndex * 3],
                    mesh.triangles[triangleIndex * 3 + 1],
                    mesh.triangles[triangleIndex * 3 + 2]
                };

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] tris = mesh.GetTriangles(i);
                    for (int j = 0; j < tris.Length; j += 3)
                        if (tris[j] == hitTriangles[0] & tris[j + 1] == hitTriangles[1] & tris[j + 2] == hitTriangles[2])
                        {
                            return i;
                        }
                }
            }
            catch (System.Exception e)
            {
                // Nobody cares
            }

            return 0;
        }

        public Transform PlayHandTap(Vector3 position, Vector3 normal, bool isLeftHand, Texture texture) => PlayHandTap(position, normal, isLeftHand, GetHandTap(texture));

        public Transform PlayHandTap(Vector3 position, Vector3 normal, bool isLeftHand, HandTapType type)
        {
            HandTap tap = Instantiate(handTap);
            tap.transform.position = position + normal * 0.01f;
            tap.transform.up = normal;
            tap.transform.localScale = isLeftHand ? Vector3.one : new Vector3(-1, 1, 1);
            tap.Tap(type);

            return tap.transform;
        }

        public HandTapType GetHandTap(Texture texture)
        {
            foreach (HandTapType type in handTaps)
            {
                foreach (Texture activationTexture in type.activationTextures)
                {
                    if (activationTexture == texture)
                    {
                        ForceDebug.Log("Hit: " + type.name);
                        return type;
                    }

                }
            }

            return handTaps[0];
        }

    }
}
