using UnityEngine;
using System.Collections;
using Autohand;

public class AutoGrabber : MonoBehaviour
{
    [Header("Target Joint")]
    [Tooltip("The transform joint that this script will attempt to auto-grab.")]
    public ConfigurableTransformJoint targetJoint;

    [Header("Auto Grab Settings")]
    [Tooltip("Maximum distance for the hand to auto-grab the object.")]
    public float grabDistanceThreshold = 0.5f;

    [Tooltip("Enable/disable proximity-based auto-grabbing.")]
    public bool enableProximityGrab = true;

    [Tooltip("Use left hand for proximity grabbing.")]
    public bool useLeftHandForProximity = true;
    [Tooltip("Use right hand for proximity grabbing.")]
    public bool useRightHandForProximity = true;

    [Header("Debug/Manual Grab")]
    [Tooltip("Key to trigger manual grab (for debugging).")]
    public KeyCode manualGrabKey = KeyCode.G;
    [Tooltip("Use left hand for manual grabbing.")]
    public bool useLeftHandForManualGrab = true;
    [Tooltip("Use right hand for manual grabbing.")]
    public bool useRightHandForManualGrab = true;

    private Hand leftHand;
    private Hand rightHand;
    private Grabbable targetGrabbable;
    private GrabEquip grabEquip;

    void Awake()
    {
        targetGrabbable = GetComponent<Grabbable>();
        grabEquip = GetComponent<GrabEquip>();
        StartCoroutine(InitializeHands());
    }

    private IEnumerator InitializeHands()
    {
        yield return new WaitUntil(() => ShmackleGameManager.Instance != null && ShmackleGameManager.Instance.shmackleLocalPlayer != null);
        yield return new WaitUntil(() => ShmackleGameManager.Instance.shmackleLocalPlayer.autoHandLeft != null && ShmackleGameManager.Instance.shmackleLocalPlayer.autoHandRight != null);

        leftHand = ShmackleGameManager.Instance.shmackleLocalPlayer.autoHandLeft;
        rightHand = ShmackleGameManager.Instance.shmackleLocalPlayer.autoHandRight;
        Debug.Log("AutoGrabber: Hands initialized successfully.");
    }

    void Update()
    {
        if (targetGrabbable == null || !enabled || leftHand == null || rightHand == null) return;

        // Handle manual grab (for debugging)
        if (Input.GetKeyDown(manualGrabKey))
        {
            if (useLeftHandForManualGrab)
                TryGrab(leftHand, targetGrabbable);
            if (useRightHandForManualGrab)
                TryGrab(rightHand, targetGrabbable);
        }

        // Handle proximity-based auto-grab
        if (enableProximityGrab)
        {
            if (useLeftHandForProximity)
                TryGrabIfClose(leftHand, targetGrabbable);
            if (useRightHandForProximity)
                TryGrabIfClose(rightHand, targetGrabbable);
        }
    }

    private void TryGrabIfClose(Hand hand, Grabbable grabbable)
    {
        if (hand == null || grabbable == null)
        {
            Debug.LogWarning($"AutoGrabber: Hand or Grabbable is null. Hand: {hand}, Grabbable: {grabbable}");
            return;
        }

        // Check if grabbable is not held and hand is not grabbing
        if (grabbable.IsHeld() || hand.IsGrabbing())
        {
            Debug.Log($"AutoGrabber: Skipped grab. Grabbable held: {grabbable.IsHeld()}, Hand grabbing: {hand.IsGrabbing()}");
            return;
        }

        // Calculate distance
        float distance = Vector3.Distance(hand.transform.position, grabbable.transform.position);
        if (distance < grabDistanceThreshold)
        {
            TryGrab(hand, grabbable);
        }
    }
    RaycastHit hit;
    private void TryGrab(Hand hand, Grabbable grabbable)
    {
        if (hand == null || grabbable == null)
        {
            Debug.LogWarning($"AutoGrabber: Cannot grab. Hand: {hand}, Grabbable: {grabbable}");
            return;
        }

        // Check if the grabbable is not held and the hand is not grabbing
        if (grabbable.IsHeld() || hand.IsGrabbing())
        {
            Debug.Log($"AutoGrabber: Grab failed. Grabbable held: {grabbable.IsHeld()}, Hand grabbing: {hand.IsGrabbing()}");
            return;
        }

        // Perform a raycast from the hand to the grabbable
        Vector3 handPos = hand.transform.position;
        Vector3 grabPos = grabbable.transform.position;
        Vector3 direction = (grabPos - handPos).normalized;
        float distance = Vector3.Distance(handPos, grabPos);

        Ray ray = new Ray(handPos, direction);
        RaycastHit hit;

        // Perform the raycast to detect the grabbable
        if (Physics.Raycast(ray, out hit, distance, LayerMask.GetMask("Grabbable")))
        {
            // Verify that the hit object is the intended grabbable
            if (hit.collider.gameObject == grabbable.gameObject || hit.collider.transform.IsChildOf(grabbable.transform))
            {
                hand.Grab(hit, grabbable);
                Debug.Log($"AutoGrabber: Successfully grabbed {grabbable.name} with {hand.name} using raycast.");
                // Trigger GrabEquip's OnGrab if needed
                // grabEquip?.OnGrab(hand, grabbable);
            }
            else
            {
                Debug.LogWarning($"AutoGrabber: Raycast hit wrong object: {hit.collider.gameObject.name}");
            }
        }
        else
        {
            // Fallback to simple grab if raycast fails (optional)
            hand.Grab(hit, grabbable);
            Debug.Log($"AutoGrabber: Raycast failed, used fallback grab for {grabbable.name} with {hand.name}.");
            // grabEquip?.OnGrab(hand, grabbable);
        }
    }

    private void OnEnable()
    {
        if (targetGrabbable != null)
        {
            targetGrabbable.onRelease.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (targetGrabbable != null)
        {
            targetGrabbable.onRelease.RemoveListener(OnRelease);
        }
    }

    private void OnRelease(Hand hand, Grabbable grabbable)
    {
        Debug.Log($"AutoGrabber: Released {grabbable.name} with {hand.name}.");
        //grabEquip.OnRelease(hand, grabbable);
    }

    void OnDrawGizmosSelected()
    {
        if (targetGrabbable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetGrabbable.transform.position, grabDistanceThreshold);
        }
    }
}