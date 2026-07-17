using UnityEngine;
using Autohand;
using DG.Tweening;
using Utils.Bill;

public class GrabObjectSnapConsumable : MonoBehaviour
{
    public ShmackleQuickSlot currentQuickSlotSelect;
    
    [SerializeField] private ShmackleQuickSlot[] quickSlot;
    [SerializeField] private Mesh mesh;
    [SerializeField] private MeshFilter meshHologram;
    [SerializeField] private MeshRenderer rendererHologram;
    [SerializeField] private float maxDistance = 5f; 
    [SerializeField] private LayerMask locomotionPlayerLayer; 
    [SerializeField] private RotateWithRolling rotateWithRolling; 
    [SerializeField] private ProgressRolling progressRolling;
    [SerializeField] private bool enableDebugLogs = true; 

    private Hand currentHand;
    private bool wasTriggerPressed;
    private bool hasGrabbed; 
    private Vector3 lastPosition;
    private bool isHandInTrigger;

    private void Awake()
    {
        SelectClosestQuickSlot();
    }
    
    void Start()
    {
        if (quickSlot == null || quickSlot.Length == 0)
        {
            BillDebugUtils.LogWarning(Developer.DEV3,"QuickSlot array is empty or not assigned!");
        }
        if (rotateWithRolling == null)
        {
            BillDebugUtils.LogWarning(Developer.DEV3,"RotateWithRolling is not assigned!");
        }
        
        lastPosition = transform.position;
    }

    void Update()
    {
        if (isHandInTrigger && currentHand != null)
        {
            ProcessHandTrigger();
        }
    }

    private void ProcessHandTrigger()
    {
        if (ShmackleGameManager.Instance == null || 
            ShmackleGameManager.Instance.shmackleLocalPlayer == null || 
            ShmackleGameManager.Instance.shmackleLocalPlayer.playerAbilities == null)
        {
            BillDebugUtils.LogError(Developer.DEV3,"ShmackleGameManager or its components are not initialized!");
            return;
        }

        bool isTriggerPressed = ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Pressed || Input.GetKeyDown(KeyCode.P);
        if (isTriggerPressed && !wasTriggerPressed && !hasGrabbed)
        {
            OnHandGrabbed(currentHand);
            hasGrabbed = true; 
        }
        else if (!isTriggerPressed && wasTriggerPressed)
        {
            hasGrabbed = false; 
        }
        wasTriggerPressed = isTriggerPressed;
    }

    private bool IsValidHand(Collider other)
    {
        bool isValid = ((1 << other.gameObject.layer) & locomotionPlayerLayer) != 0 && other.CompareTag("PlayerHand");
        if (!isValid)
        {
            BillDebugUtils.LogWarning(Developer.DEV3,$"Invalid hand detected: {other.name}");
        }
        return isValid;
    }

    void OnTriggerEnter(Collider other)
    {
        BillDebugUtils.Log(Developer.DEV3,$"OnTriggerEnter called with {other.name}");
        if (!IsValidHand(other)) return;

        if (!other.gameObject.TryGetComponent<Hand>(out Hand hand))
        {
            BillDebugUtils.Log(Developer.DEV3,($"No Hand component found on {other.name}"));
            return;
        }

        BillDebugUtils.Log(Developer.DEV3,($"Hand detected: {hand.name}, IsLeft: {hand.left}"));
        if (!hand.left) return;

        currentHand = hand;
        isHandInTrigger = true;
        BillDebugUtils.Log(Developer.DEV3,($"Hand {hand.name} entered trigger."));
    }

    void OnTriggerExit(Collider other)
    {
        BillDebugUtils.Log(Developer.DEV3,($"OnTriggerExit called with {other.name}"));
        if (!IsValidHand(other)) return;

        if (other.gameObject.TryGetComponent<Hand>(out Hand hand) && hand == currentHand)
        {
            BillDebugUtils.Log(Developer.DEV3,($"Hand {hand.name} exited trigger."));
            currentHand = null;
            isHandInTrigger = false;
            wasTriggerPressed = false;
            hasGrabbed = false;
        }
        else
        {
            BillDebugUtils.Log(Developer.DEV3,($"Ignoring OnTriggerExit for {other.name}: Not currentHand or no Hand component."));
        }
    }

    private void OnHandGrabbed(Hand hand)
    {
        if (CanGrabQuickSlot())
        {
            currentQuickSlotSelect.Grab(hand);
            BillDebugUtils.Log(Developer.DEV3,($"Grabbed QuickSlot {currentQuickSlotSelect.name} with hand {hand.name} after rotation completed."));
        }
        else
        {
            LogGrabFailureReason();
        }
    }

    private bool CanGrabQuickSlot()
    {
        return currentQuickSlotSelect != null && 
               currentHand.left && 
               IsRotationComplete() && 
               !currentQuickSlotSelect.isCooldown;
    }

    private void LogGrabFailureReason()
    {
        if (!IsRotationComplete())
            BillDebugUtils.Log(Developer.DEV3,("Cannot grab: RotateWithRolling is still rotating."));
        else if (currentQuickSlotSelect != null && currentQuickSlotSelect.isCooldown)
            BillDebugUtils.Log(Developer.DEV3,($"Cannot grab: QuickSlot {currentQuickSlotSelect.name} is in cooldown."));
        else
            BillDebugUtils.Log(Developer.DEV3,("Cannot grab: No valid QuickSlot selected or hand is not left."));
    }

    private bool IsRotationComplete()
    {
        var tween = rotateWithRolling?.GetRotationTween();
        return (tween == null || !tween.IsPlaying()) && progressRolling?.IsRotationComplete() == true;
    }

    public void SelectClosestQuickSlot()
    {
        if (quickSlot == null || quickSlot.Length == 0)
        {
            currentQuickSlotSelect = null;
            BillDebugUtils.Log(Developer.DEV3,("No QuickSlot selected: Array is null or empty."));
            return;
        }

        ShmackleQuickSlot closestSlot = null;
        float minSqrDistance = maxDistance * maxDistance;
        Vector3 currentPosition = transform.position;

        foreach (var slot in quickSlot)
        {
            if (slot == null || slot.transform == null)
                continue;

            float sqrDistance = (currentPosition - slot.transform.position).sqrMagnitude;
            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                closestSlot = slot;
            }
        }

        if (closestSlot != null && currentQuickSlotSelect != closestSlot)
        {
            currentQuickSlotSelect = closestSlot;
            Grabbable grabbable = closestSlot.currentEquipItem;
            if (grabbable != null)
            {
                ConsumableGrab consumable = grabbable.GetComponent<ConsumableGrab>();
                if (consumable != null)
                {
                    mesh = consumable.Mesh;
                    meshHologram.mesh = consumable.Mesh;
                    rendererHologram.material = consumable.Material;
                    BillDebugUtils.Log(Developer.DEV3,($"Selected QuickSlot: {closestSlot.name}, SqrDistance: {minSqrDistance:F2}"));
                }
                else
                {
                    BillDebugUtils.LogError(Developer.DEV3,$"ConsumableGrab component missing on {grabbable.name}");
                }
            }
            else
            {
                BillDebugUtils.LogError(Developer.DEV3,"No grabbable item found in selected QuickSlot.");
            }
        }
        else if (closestSlot == null && currentQuickSlotSelect != null)
        {
            currentQuickSlotSelect = null;
            BillDebugUtils.Log(Developer.DEV3,"No QuickSlot selected (no match within max distance).");
        }
    }
}