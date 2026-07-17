using System;
using Shmackle;
using UnityEngine;
using UnityEngine.Events;
using Shmackle.SoundMaterial;

public class ShmackleRaycastLocomotion : MonoBehaviour
{
    [Header("References")]
    public ShmacklePlayerController playerController;
    public Transform parentTransform;
    public Rigidbody playerRigidBody;
    public SphereCollider headCollider;
    public CapsuleCollider bodyCollider;

    public Transform leftHandFollower;
    public Transform rightHandFollower;

    public Transform rightHandTransform;
    public Transform leftHandTransform;

    [Header("Settings")]
    public float gravity = 9.81f;
    public int velocityHistorySize = 8;
    public float maxArmLength = 1.5f;
    public float unStickDistance = 1f;

    public float velocityLimit = 0.4f;
    public float maxJumpSpeed = 6.5f;
    public float jumpMultiplier = 1.1f;
    public float minimumRaycastDistance = 0.05f;
    public float defaultSlideFactor = 0.03f;
    public float defaultPrecision = 0.995f;

    public Vector3 rightHandOffset;
    public Vector3 leftHandOffset;

    public LayerMask locomotionEnabledLayers;

    public bool disableMovement = false;
    public bool disableLeftHandFollower;
    public bool disableRightHandFollower;

    // --- Internal state ---
    private Vector3[] velocityHistory;
    private int velocityIndex;
    private Vector3 currentVelocity;
    private Vector3 denormalizedVelocityAverage;
    private Vector3 lastPosition;

    [HideInInspector]public Vector3 lastLeftHandPosition;
    [HideInInspector]public Vector3 lastRightHandPosition;
    private Vector3 lastHeadPosition;

    public bool wasLeftHandTouching;
    public bool wasRightHandTouching;


    //==== in update ====//
    public bool leftHandCollision = false;
    public bool rightHandCollision = false;
    Vector3 finalPosition;
    public Vector3 rigidBodyMovement = Vector3.zero;
    Vector3 firstIterationLeft = Vector3.zero;
    Vector3 firstIterationRight = Vector3.zero;
    RaycastHit[] raycastHitInfo = new RaycastHit[1];
    RaycastHit[] innerRaycastHitInfor = new RaycastHit[1];

    [Header("Callback Events")]
    public UnityEvent OnRightHandCollision;
    public UnityEvent OnLeftHandCollision;

    public bool isTeleporting = false;

    [Header("Sounds")]
    public AudioSource leftHandSound;
    public AudioSource rightHandSound;

    [Header("Sound System")]
    public MaterialSoundSystem materialSoundSystem;

    public Rigidbody elevatorRigidbody;
    public bool isLeftHandClimb;
    public bool isRightHandClimb;

    private bool _isInitialized;

    // ========= TIP-TOOL SUPPORT =========
    [Header("Tool Tip (Optional)")]                           // NEW
    public Transform leftToolTipTransform;                    // NEW
    public Transform rightToolTipTransform;                   // NEW
    public Transform leftToolTipFollower;                     // NEW
    public Transform rightToolTipFollower;                    // NEW
    public Vector3 leftToolOffset;                            // NEW
    public Vector3 rightToolOffset;                           // NEW
    public float toolTipRadius = 0.04f;                       // NEW
    public bool isHoldingLeftTool = false;                    // NEW
    public bool isHoldingRightTool = false;                   // NEW

    // Active tool states (per side)                             
    public bool leftToolCollision, rightToolCollision;        // NEW
    private Vector3 lastLeftToolPosition, lastRightToolPosition; // NEW
    private bool wasLeftToolTouching, wasRightToolTouching;   // NEW
    private bool usingLeftToolAnchor, usingRightToolAnchor;   // NEW
                                                              // ===================================


    [Header("Tool Callback Events")]                                      // NEW
    public UnityEvent OnLeftToolCollision;                                 // NEW
    public UnityEvent OnRightToolCollision;                                // NEW
    public UnityEvent<GameObject> OnLeftToolHitObject;                     // NEW
    public UnityEvent<GameObject> OnRightToolHitObject;                    // NEW

    [Header("Tool Sounds (Optional)")]                                     // NEW
    public AudioSource leftToolSound;                                      // NEW
    public AudioSource rightToolSound;                                     // NEW


    private void Awake()
    {
        InitializeFields();
    }

    private void Start()
    {
        Invoke(nameof(changeLayer), 1);
    }

    void changeLayer()
    {
        Debug.Log("change layer:  ---- ");
        _isInitialized = true;
        //locomotionEnabledLayers = 1 << LayerMask.NameToLayer("Default");
        locomotionEnabledLayers =
            (1 << LayerMask.NameToLayer("Default")) |
            (1 << LayerMask.NameToLayer("Environment"));
    }


    public void InitializeFields()
    {
        velocityHistory = new Vector3[velocityHistorySize];
        for (int i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Vector3.zero;
        }

        velocityIndex = 0;
        lastPosition = parentTransform.position;
        lastLeftHandPosition = leftHandFollower.position;
        lastRightHandPosition = rightHandFollower.position;
        lastHeadPosition = headCollider.transform.position;

        wasLeftHandTouching = false;
        wasRightHandTouching = false;

        // ---- seed tool states ----
        lastLeftToolPosition = (leftToolTipTransform ? ComputeOffsetPosition(leftToolTipTransform, leftToolOffset) : leftHandFollower.position);  // NEW
        lastRightToolPosition = (rightToolTipTransform ? ComputeOffsetPosition(rightToolTipTransform, rightToolOffset) : rightHandFollower.position); // NEW
        wasLeftToolTouching = false;                                                                                                                  // NEW
        wasRightToolTouching = false;                                                                                                                 // NEW
        usingLeftToolAnchor = false;                                                                                                                  // NEW
        usingRightToolAnchor = false;

    }

    // ---------------- Tool toggle API (call from your grab system) ----------------
    public void SetHoldingTool(bool left, bool isHolding, Transform toolTip = null) // NEW
    {
        if (left)
        {
            isHoldingLeftTool = isHolding;                                          // NEW
            if (toolTip != null) leftToolTipTransform = toolTip;                    // NEW
            lastLeftToolPosition = (leftToolTipTransform
                ? ComputeOffsetPosition(leftToolTipTransform, leftToolOffset)
                : lastLeftHandPosition);                                            // NEW
            wasLeftToolTouching = false;                                            // NEW
        }
        else
        {
            isHoldingRightTool = isHolding;                                         // NEW
            if (toolTip != null) rightToolTipTransform = toolTip;                   // NEW
            lastRightToolPosition = (rightToolTipTransform
                ? ComputeOffsetPosition(rightToolTipTransform, rightToolOffset)
                : lastRightHandPosition);                                           // NEW
            wasRightToolTouching = false;                                           // NEW
        }
    }
    // ------------------------------------------------------------------------------

    private Vector3 ComputeOffsetPosition(Transform sourceTransform, Vector3 offset)
    {
        return sourceTransform.position + sourceTransform.rotation * offset;
    }

    public  Vector3 GetClampedLeftHandPosition()
    {
        Vector3 desiredPos = ComputeOffsetPosition(leftHandTransform, leftHandOffset);
        Vector3 headPos = headCollider.transform.position;
        Vector3 delta = desiredPos - headPos;

        if (delta.magnitude < maxArmLength)
        {
            return desiredPos;
        }
        else
        {
            return headPos + delta.normalized * maxArmLength;
        }
    }

    public Vector3 GetClampedRightHandPosition()
    {
        Vector3 desiredPos = ComputeOffsetPosition(rightHandTransform, rightHandOffset);
        Vector3 headPos = headCollider.transform.position;
        Vector3 delta = desiredPos - headPos;

        if (delta.magnitude < maxArmLength)
        {
            return desiredPos;
        }
        else
        {
            return headPos + delta.normalized * maxArmLength;
        }
    }

    private void Update()
    {
        leftHandCollision = false;
        rightHandCollision = false;
        leftToolCollision = false;   // NEW
        rightToolCollision = false;  // NEW

        finalPosition = Vector3.zero;
        rigidBodyMovement = Vector3.zero;
        firstIterationLeft = Vector3.zero;
        firstIterationRight = Vector3.zero;

        // Align body collider orientation with head yaw
        bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);

        // =====================================================================
        // LEFT SIDE: evaluate both Hand & Tool candidates, pick best anchor
        // =====================================================================
        Vector3 leftHandNow = GetClampedLeftHandPosition();
        float leftHandRad = minimumRaycastDistance;
        Vector3 leftHandTravel = leftHandNow - lastLeftHandPosition
                                 + Vector3.down * 2f * gravity * Time.deltaTime * Time.deltaTime;

        bool leftHandHit = PerformIterativeSphereCast(
            lastLeftHandPosition, leftHandRad, leftHandTravel, defaultPrecision,
            out Vector3 leftHandFinal, true);                                                                             // NEW

        if (leftHandHit)
        {
            firstIterationLeft = (wasLeftHandTouching ? (lastLeftHandPosition - leftHandNow)
                                                      : (leftHandFinal - leftHandNow));
            playerRigidBody.linearVelocity = Vector3.zero;
        }

        Vector3 leftToolNow = (isHoldingLeftTool && leftToolTipTransform)
            ? ComputeOffsetPosition(leftToolTipTransform, leftToolOffset)
            : Vector3.zero;

        float leftToolRad = toolTipRadius;
        Vector3 leftToolTravel = (isHoldingLeftTool && leftToolTipTransform)
            ? (leftToolNow - lastLeftToolPosition + Vector3.down * 2f * gravity * Time.deltaTime * Time.deltaTime)
            : Vector3.zero;

        bool leftToolHit = false;
        Vector3 leftToolFinal = Vector3.zero;

        if (isHoldingLeftTool && leftToolTipTransform)
        {
            leftToolHit = PerformIterativeSphereCast(
                lastLeftToolPosition, leftToolRad, leftToolTravel, defaultPrecision,
                out leftToolFinal, true);                                                                                 // NEW

            if (leftToolHit) playerRigidBody.linearVelocity = Vector3.zero;
        }

        usingLeftToolAnchor = false;
        if (leftToolHit && !leftHandHit) usingLeftToolAnchor = true;
        else if (leftToolHit && leftHandHit)
        {
            Vector3 pullHand = (wasLeftHandTouching ? (lastLeftHandPosition - leftHandNow)
                                                    : (leftHandFinal - leftHandNow));
            Vector3 pullTool = (wasLeftToolTouching ? (lastLeftToolPosition - leftToolNow)
                                                    : (leftToolFinal - leftToolNow));
            usingLeftToolAnchor = (pullTool.sqrMagnitude > pullHand.sqrMagnitude);
        }

        leftToolCollision = leftToolHit;
        leftHandCollision = leftHandHit || leftHandCollision;

        // =====================================================================
        // RIGHT SIDE: evaluate both Hand & Tool candidates, pick best anchor
        // =====================================================================
        Vector3 rightHandNow = GetClampedRightHandPosition();
        float rightHandRad = minimumRaycastDistance;
        Vector3 rightHandTravel = rightHandNow - lastRightHandPosition
                                  + Vector3.down * 2f * gravity * Time.deltaTime * Time.deltaTime;

        bool rightHandHit = PerformIterativeSphereCast(
            lastRightHandPosition, rightHandRad, rightHandTravel, defaultPrecision,
            out Vector3 rightHandFinal, true);

        if (rightHandHit) playerRigidBody.linearVelocity = Vector3.zero;

        Vector3 rightToolNow = (isHoldingRightTool && rightToolTipTransform)
            ? ComputeOffsetPosition(rightToolTipTransform, rightToolOffset)
            : Vector3.zero;

        float rightToolRad = toolTipRadius;
        Vector3 rightToolTravel = (isHoldingRightTool && rightToolTipTransform)
            ? (rightToolNow - lastRightToolPosition + Vector3.down * 2f * gravity * Time.deltaTime * Time.deltaTime)
            : Vector3.zero;

        bool rightToolHit = false;
        Vector3 rightToolFinal = Vector3.zero;

        if (isHoldingRightTool && rightToolTipTransform)
        {
            rightToolHit = PerformIterativeSphereCast(
                lastRightToolPosition, rightToolRad, rightToolTravel, defaultPrecision,
                out rightToolFinal, true);

            if (rightToolHit)
            {
                playerRigidBody.linearVelocity = Vector3.zero;
            }

        }

        usingRightToolAnchor = false;
        if (rightToolHit && !rightHandHit) usingRightToolAnchor = true;
        else if (rightToolHit && rightHandHit)
        {
            Vector3 pullHand = (wasRightHandTouching ? (lastRightHandPosition - rightHandNow)
                                                     : (rightHandFinal - rightHandNow));
            Vector3 pullTool = (wasRightToolTouching ? (lastRightToolPosition - rightToolNow)
                                                     : (rightToolFinal - rightToolNow));
            usingRightToolAnchor = (pullTool.sqrMagnitude > pullHand.sqrMagnitude);
        }

        rightToolCollision = rightToolHit;
        rightHandCollision = rightHandHit || rightHandCollision;

        // =====================================================================
        // Callbacks / SFX (kept the same flags you already use)
        // =====================================================================
        if (leftHandCollision && !wasLeftHandTouching)
        {
            Debug.Log("Left Hand Collision");
            OnLeftHandCollision.Invoke();
            if (materialSoundSystem != null)
            {
                materialSoundSystem.ProcessSurfaceContact(raycastHitInfo[0].collider.gameObject, leftHandSound);
            }
            else if (!leftHandSound.isPlaying)
            {
                leftHandSound.Play();
            }
        }

        if (rightHandCollision && !wasRightHandTouching)
        {
            Debug.Log("Right Hand Collision");
            OnRightHandCollision.Invoke();
            if (materialSoundSystem != null)
            {
                materialSoundSystem.ProcessSurfaceContact(raycastHitInfo[0].collider.gameObject, rightHandSound);
            }
            else if (!rightHandSound.isPlaying)
            {
                rightHandSound.Play();
            }
        }

        // =====================================================================
        // Tool callbacks / SFX — fire ONLY on first contact this frame         
        // =====================================================================
        if (leftToolCollision && !wasLeftToolTouching)
        {
            Debug.Log("Left Tool Collision");
            OnLeftToolCollision?.Invoke();
            if (raycastHitInfo[0].collider != null)
                OnLeftToolHitObject?.Invoke(raycastHitInfo[0].collider.gameObject);

            // Optional: surface-aware sound, same as hands                     
            if (materialSoundSystem != null)
            {
                materialSoundSystem.ProcessSurfaceContact(
                    raycastHitInfo[0].collider.gameObject, leftToolSound);
            }
            else if (leftToolSound != null && !leftToolSound.isPlaying)
            {
                leftToolSound.Play();
            }
        }

        if (rightToolCollision && !wasRightToolTouching)
        {
            Debug.Log("Right Tool Collision");
            OnRightToolCollision?.Invoke();
            if (raycastHitInfo[0].collider != null)
                OnRightToolHitObject?.Invoke(raycastHitInfo[0].collider.gameObject);

            if (materialSoundSystem != null)
            {
                materialSoundSystem.ProcessSurfaceContact(
                    raycastHitInfo[0].collider.gameObject, rightToolSound);
            }
            else if (rightToolSound != null && !rightToolSound.isPlaying)
            {
                rightToolSound.Play();
            }
        }



        // =====================================================================
        // Combine movements from the selected anchors (hand OR tool per side)
        // =====================================================================
        Vector3 pullL = Vector3.zero;
        Vector3 pullR = Vector3.zero;

        bool leftAnchored = ((usingLeftToolAnchor ? leftToolCollision : leftHandCollision)
                           || (usingLeftToolAnchor ? wasLeftToolTouching : wasLeftHandTouching));

        bool rightAnchored = ((usingRightToolAnchor ? rightToolCollision : rightHandCollision)
                           || (usingRightToolAnchor ? wasRightToolTouching : wasRightHandTouching));

        if (leftAnchored)
        {
            Vector3 desired = usingLeftToolAnchor
                ? ((isHoldingLeftTool && leftToolTipTransform) ? ComputeOffsetPosition(leftToolTipTransform, leftToolOffset) : lastLeftToolPosition)
                : GetClampedLeftHandPosition();
            Vector3 planted = usingLeftToolAnchor ? lastLeftToolPosition : lastLeftHandPosition;
            pullL = planted - desired;
        }
        if (rightAnchored)
        {
            Vector3 desired = usingRightToolAnchor
                ? ((isHoldingRightTool && rightToolTipTransform) ? ComputeOffsetPosition(rightToolTipTransform, rightToolOffset) : lastRightToolPosition)
                : GetClampedRightHandPosition();
            Vector3 planted = usingRightToolAnchor ? lastRightToolPosition : lastRightHandPosition;
            pullR = planted - desired;
        }

        if (leftAnchored && rightAnchored) rigidBodyMovement = 0.5f * (pullL + pullR);
        else if (leftAnchored) rigidBodyMovement = pullL;
        else if (rightAnchored) rigidBodyMovement = pullR;
        else rigidBodyMovement = Vector3.zero;

        // --- Head collision check before applying movement (unchanged) ---
        Vector3 headStart = lastHeadPosition;
        Vector3 headEnd = headCollider.transform.position + rigidBodyMovement;
        Vector3 headDelta = headEnd - headStart;
        if (PerformIterativeSphereCast(
                lastHeadPosition,
                headCollider.radius,
                headDelta,
                defaultPrecision,
                out finalPosition,
                false))
        {
            rigidBodyMovement = finalPosition - lastHeadPosition;
            // Final raycast sanity check to avoid phasing through geometry
            if (Physics.RaycastNonAlloc(
                    lastHeadPosition,
                    (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).normalized,
                    raycastHitInfo,
                    (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).magnitude
                        + headCollider.radius * defaultPrecision * 0.999f,
                    locomotionEnabledLayers.value) > 0)
            {
                //set position to last head position if colliding with surface
                rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
            }
        }

        if (rigidBodyMovement != Vector3.zero && isTeleporting == false)
        {
            if (rigidBodyMovement.y <= 0)
            {
                rigidBodyMovement.y = 0;
            }

            parentTransform.position += rigidBodyMovement;
        }

        lastHeadPosition = headCollider.transform.position;

        // --- Final left-hand placement (unchanged logic) ---
        Vector3 leftFinalTravel = GetClampedLeftHandPosition() - lastLeftHandPosition;
        if (PerformIterativeSphereCast(
                lastLeftHandPosition,
                minimumRaycastDistance,
                leftFinalTravel,
                defaultPrecision,
                out finalPosition,
                !((leftHandCollision || wasLeftHandTouching) && (rightHandCollision || wasRightHandTouching))))
        {
            lastLeftHandPosition = finalPosition;
            leftHandCollision = true;
        }
        else
        {
            lastLeftHandPosition = GetClampedLeftHandPosition();
        }

        // --- Final right-hand placement (unchanged logic) ---
        Vector3 rightFinalTravel = GetClampedRightHandPosition() - lastRightHandPosition;
        if (PerformIterativeSphereCast(
                lastRightHandPosition,
                minimumRaycastDistance,
                rightFinalTravel,
                defaultPrecision,
                out finalPosition,
                !((leftHandCollision || wasLeftHandTouching) && (rightHandCollision || wasRightHandTouching))))
        {
            lastRightHandPosition = finalPosition;
            rightHandCollision = true;
        }
        else
        {
            lastRightHandPosition = GetClampedRightHandPosition();
        }

        // --- Final left tool placement ---
        if (isHoldingLeftTool && leftToolTipTransform)
        {
            Vector3 toolNow = ComputeOffsetPosition(leftToolTipTransform, leftToolOffset);
            Vector3 toolTravel = toolNow - lastLeftToolPosition;
            if (PerformIterativeSphereCast(
                    lastLeftToolPosition, toolTipRadius, toolTravel, defaultPrecision,
                    out finalPosition,
                    !((leftToolCollision || wasLeftToolTouching) && (rightToolCollision || wasRightToolTouching))))
            {
                lastLeftToolPosition = finalPosition;
                leftToolCollision = true;
            }
            else
            {
                lastLeftToolPosition = toolNow;
            }
        }

        // --- Final right tool placement ---
        if (isHoldingRightTool && rightToolTipTransform)
        {
            Vector3 toolNow = ComputeOffsetPosition(rightToolTipTransform, rightToolOffset);
            Vector3 toolTravel = toolNow - lastRightToolPosition;
            if (PerformIterativeSphereCast(
                    lastRightToolPosition, toolTipRadius, toolTravel, defaultPrecision,
                    out finalPosition,
                    !((leftToolCollision || wasLeftToolTouching) && (rightToolCollision || wasRightToolTouching))))
            {
                lastRightToolPosition = finalPosition;
                rightToolCollision = true;
            }
            else
            {
                lastRightToolPosition = toolNow;
            }
        }

        // Update velocity history (for average calculation)
        UpdateVelocityHistory();

        // Unstick logic for left hand (unchanged)
        if (leftHandCollision
            && (GetClampedLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance
            && Physics.SphereCastNonAlloc(
                   headCollider.transform.position,
                   minimumRaycastDistance * defaultPrecision,
                   (GetClampedLeftHandPosition() - headCollider.transform.position).normalized,
                   raycastHitInfo,
                   (GetClampedLeftHandPosition() - headCollider.transform.position).magnitude
                       - minimumRaycastDistance,
                   locomotionEnabledLayers.value) == 0)
        {
            lastLeftHandPosition = GetClampedLeftHandPosition();
            leftHandCollision = false;
        }

        // Unstick logic for right hand (unchanged)
        if (rightHandCollision
            && (GetClampedRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance
            && Physics.SphereCastNonAlloc(
                   headCollider.transform.position,
                   minimumRaycastDistance * defaultPrecision,
                   (GetClampedRightHandPosition() - headCollider.transform.position).normalized,
                    raycastHitInfo,
                   (GetClampedRightHandPosition() - headCollider.transform.position).magnitude
                       - minimumRaycastDistance,
                   locomotionEnabledLayers.value) == 0)
        {
            lastRightHandPosition = GetClampedRightHandPosition();
            rightHandCollision = false;
        }

        // ---- Unstick for tools ----
        if (leftToolCollision && isHoldingLeftTool && leftToolTipTransform)
        {
            Vector3 toolNow = ComputeOffsetPosition(leftToolTipTransform, leftToolOffset);
            if ((toolNow - lastLeftToolPosition).magnitude > unStickDistance &&
                Physics.SphereCastNonAlloc(
                    headCollider.transform.position,
                    toolTipRadius * defaultPrecision,
                    (toolNow - headCollider.transform.position).normalized,
                    raycastHitInfo,
                    (toolNow - headCollider.transform.position).magnitude - toolTipRadius,
                    locomotionEnabledLayers.value) == 0)
            {
                lastLeftToolPosition = toolNow;
                leftToolCollision = false;
            }
        }
        if (rightToolCollision && isHoldingRightTool && rightToolTipTransform)
        {
            Vector3 toolNow = ComputeOffsetPosition(rightToolTipTransform, rightToolOffset);
            if ((toolNow - lastRightToolPosition).magnitude > unStickDistance &&
                Physics.SphereCastNonAlloc(
                    headCollider.transform.position,
                    toolTipRadius * defaultPrecision,
                    (toolNow - headCollider.transform.position).normalized,
                    raycastHitInfo,
                    (toolNow - headCollider.transform.position).magnitude - toolTipRadius,
                    locomotionEnabledLayers.value) == 0)
            {
                lastRightToolPosition = toolNow;
                rightToolCollision = false;
            }
        }

        // Update follower transforms (hands) — unchanged
        if (!disableLeftHandFollower)
        {
            leftHandFollower.position = lastLeftHandPosition;
            leftHandFollower.rotation = leftHandTransform.rotation;
        }
        if (!disableRightHandFollower)
        {
            rightHandFollower.position = lastRightHandPosition;
            rightHandFollower.rotation = rightHandTransform.rotation;
        }

        // Tip followers (optional visuals)
        if (isHoldingLeftTool && leftToolTipFollower != null)
        {
            leftToolTipFollower.position = lastLeftToolPosition;
            leftToolTipFollower.rotation = (leftToolTipTransform ? leftToolTipTransform.rotation : leftHandTransform.rotation);
        }
        if (isHoldingRightTool && rightToolTipFollower != null)
        {
            rightToolTipFollower.position = lastRightToolPosition;
            rightToolTipFollower.rotation = (rightToolTipTransform ? rightToolTipTransform.rotation : rightHandTransform.rotation);
        }

        // Carry state to next frame
        wasLeftHandTouching = leftHandCollision;
        wasRightHandTouching = rightHandCollision;
        wasLeftToolTouching = leftToolCollision;
        wasRightToolTouching = rightToolCollision;
    }

    private void FixedUpdate()
    {
        if (disableMovement)
            return;
        if (rightHandCollision ||
             leftHandCollision ||
             isRightHandClimb ||
             isLeftHandClimb ||
             rightToolCollision ||
             leftToolCollision)
        {
            if (denormalizedVelocityAverage.magnitude > velocityLimit)
            {
                if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                {
                    playerRigidBody.linearVelocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
                }
                else
                {
                    playerRigidBody.linearVelocity = jumpMultiplier * denormalizedVelocityAverage;
                }
            }
        }
    }

    private bool PerformIterativeSphereCast(
        Vector3 startPosition,
        float sphereRadius,
        Vector3 movementVector,
        float precision,
        out Vector3 endPosition,
        bool singleHand)
    {
        RaycastHit hitInfo;
        Surface surface;

        // 1) Try a full-precision sphere cast from startPosition along movementVector
        if (PerformSphereCast(
                startPosition,
                sphereRadius * precision,
                movementVector,
                precision,
                out endPosition,
                out hitInfo))
        {
            // Print the name of the object we just hit
            // Debug.Log("Hit object: " + hitInfo.collider.gameObject.name);

            Vector3 initialHitPos = endPosition;

            // float slipPercent;
            // if (singleHand)
            // {
            //     slipPercent = 0;
            // }
            // else
            // {
            //     slipPercent = defaultSlideFactor;
            // }

            float slipPercent;
            surface = hitInfo.collider.GetComponent<Surface>();
            slipPercent = surface != null ? surface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);

            Vector3 remainingMotion = (startPosition + movementVector) - initialHitPos;
            Vector3 slideVector = Vector3.ProjectOnPlane(remainingMotion, hitInfo.normal) * slipPercent;

            // 2) Attempt to slide along the surface
            if (PerformSphereCast(
                    initialHitPos,
                    sphereRadius,
                    slideVector,
                    precision * precision,
                    out endPosition,
                    out hitInfo))
            {
                // Debug.Log("Slide hit object: " + hitInfo.collider.gameObject.name);
                return true;
            }

            // 3) If sliding fails, try projecting the rest of the original motion
            Vector3 secondStart = initialHitPos + slideVector;
            Vector3 restOfMotion = (startPosition + movementVector) - secondStart;

            if (PerformSphereCast(
                    secondStart,
                    sphereRadius,
                    restOfMotion,
                    precision * precision * precision,
                    out endPosition,
                    out hitInfo))
            {
                // Debug.Log("Projected-rest hit object: " + hitInfo.collider.gameObject.name);
                return true;
            }

            // 4) If both slide attempts failed, stay at the initial hit point
            endPosition = initialHitPos;
            return true;
        }
        // 5) Fallback: smaller-sphere sanity check
        else if (PerformSphereCast(
                     startPosition,
                     sphereRadius * precision * 0.66f,
                     movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f),
                     precision * 0.66f,
                     out endPosition,
                     out hitInfo))
        {
            // Debug.Log("Fallback hit object: " + hitInfo.collider.gameObject.name);
            endPosition = startPosition;
            return true;
        }
        else
        {
            // 6) No obstruction detected
            endPosition = Vector3.zero;
            return false;
        }

    }

    private bool PerformSphereCast(
        Vector3 startPosition,
        float sphereRadius,
        Vector3 movementVector,
        float precision,
        out Vector3 finalPosition,
        out RaycastHit hitInfo)
    {


        if (Physics.SphereCastNonAlloc(
                startPosition,
                sphereRadius * precision,
                movementVector.normalized,
                 raycastHitInfo,
                movementVector.magnitude + sphereRadius * (1f - precision),
                locomotionEnabledLayers.value) > 0)
        {
            hitInfo = raycastHitInfo[0];
            finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

            if (Physics.SphereCastNonAlloc(
                    startPosition,
                    sphereRadius * precision * precision,
                    (finalPosition - startPosition).normalized,
                     innerRaycastHitInfor,
                    (finalPosition - startPosition).magnitude + sphereRadius * (1f - precision * precision),
                    locomotionEnabledLayers.value) > 0)
            {
                float adjustedDistance = Mathf.Max(
                    0f,
                    hitInfo.distance - sphereRadius * (1f - precision * precision)
                );
                finalPosition = startPosition + (finalPosition - startPosition).normalized * adjustedDistance;
                hitInfo = innerRaycastHitInfor[0];
            }
            else if (Physics.RaycastNonAlloc(
                         startPosition,
                         (finalPosition - startPosition).normalized,
                         innerRaycastHitInfor,
                         (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f,
                         locomotionEnabledLayers.value) > 0)
            {
                finalPosition = startPosition;
                hitInfo = innerRaycastHitInfor[0];
                return true;
            }
            return true;
        }
        else if (Physics.RaycastNonAlloc(
                     startPosition,
                     movementVector.normalized,
                      raycastHitInfo,
                     movementVector.magnitude + sphereRadius * precision * 0.999f,
                     locomotionEnabledLayers.value) > 0)
        {
            finalPosition = startPosition;
            hitInfo = raycastHitInfo[0];
            return true;
        }
        else
        {
            finalPosition = Vector3.zero;
            hitInfo = raycastHitInfo[0];
            return false;
        }
    }


    private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
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

    public bool IsHandContactingSurface(bool checkLeftHand)
    {
        return checkLeftHand ? wasLeftHandTouching : wasRightHandTouching;
    }

    public void RotatePlayerAroundHead(float degrees)
    {
        parentTransform.RotateAround(headCollider.transform.position, parentTransform.up, degrees);
        denormalizedVelocityAverage = Quaternion.Euler(0f, degrees, 0f) * denormalizedVelocityAverage;

        for (int i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Quaternion.Euler(0f, degrees, 0f) * velocityHistory[i];
        }
    }

    private void UpdateVelocityHistory()
    {
        velocityIndex = (velocityIndex + 1) % velocityHistorySize;
        Vector3 oldestVelocity = velocityHistory[velocityIndex];
        currentVelocity = (parentTransform.position - lastPosition) / Time.deltaTime;
        denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
        velocityHistory[velocityIndex] = currentVelocity;
        lastPosition = parentTransform.position;
    }

    public void ActiveTeleport(bool isActive, Vector3 position)
    {
        if (isActive)
        {
            ToggleNoCollideMode(true);
            playerRigidBody.linearVelocity = Vector3.zero;
            //playerRigidBody.interpolation = RigidbodyInterpolation.None;
        }
        else
        {
            ToggleNoCollideMode(false);
            //rigidBodyMovement = position;
            playerRigidBody.linearVelocity = Vector3.zero;
            //playerRigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

    }



    #region NoCollideMode
    public enum NoCollideType
    {
        Full,
        Physics
    }

    private bool isNoCollideModeActive = false;

    private LayerMask cachedLocomotionLayers;

    public void ToggleNoCollideMode(bool enable, NoCollideType type = NoCollideType.Full)
    {
        if (!_isInitialized || isNoCollideModeActive == enable)
        {
            return;
        }

        isNoCollideModeActive = enable;

        if (enable)
        {
            playerRigidBody.linearVelocity = Vector3.zero;
            cachedLocomotionLayers = locomotionEnabledLayers;
            if (type == NoCollideType.Full)
            {
                locomotionEnabledLayers = 0;
            }
        }
        else
        {
            locomotionEnabledLayers = cachedLocomotionLayers;
        }
    }

    private void EnterNoCollideMode()
    {
        cachedLocomotionLayers = locomotionEnabledLayers;
        locomotionEnabledLayers = 0;
    }

    private void EnterNoCollideModePhysic()
    {
        cachedLocomotionLayers = locomotionEnabledLayers;
    }

    private void ExitNoCollideMode()
    {
        locomotionEnabledLayers = cachedLocomotionLayers;
    }

    #endregion

    public void SpeedUp(bool isActive)
    {
        if (isActive)
        {
            maxJumpSpeed = 6.6f;
            jumpMultiplier = 1.7f;
        }
        else
        {
            maxJumpSpeed = 6.5f;
            jumpMultiplier = 1.5f;
        }
    }

}
