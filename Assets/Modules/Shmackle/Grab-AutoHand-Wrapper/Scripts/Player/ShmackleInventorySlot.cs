using Autohand;
using UnityEngine;

public class ShmackleInventorySlot : MonoBehaviour
{
    public GameObject currentStoredItem;
    
    //public Transform offsetTransform;
    [HideInInspector]public ShmacklePlayerController playerController;
    [HideInInspector]public ShmackleInventoryController inventoryController;

    
    public enum InventoryPos
    {
        Left,
        Right
    }

    public InventoryPos currentInventoryPos = InventoryPos.Right;

    private bool isHoldItem;
    private bool allowGetItem;


    public enum inventorySlotStage
    {
        Idle,
        PutItem,
        GetItem,
    }
    public inventorySlotStage currentInventorySlotStage = inventorySlotStage.Idle;

    private void Update()
    {
        if (playerController.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Released ||
            playerController.playerInputListener.rightGripState == PlayerInputListener.ButtonState.Released ||
            Input.GetKeyDown(KeyCode.Z))
        {
            currentInventorySlotStage = inventorySlotStage.Idle;
        }

        if (currentStoredItem && gameObject.transform.childCount == 0)
        {
            currentStoredItem = null;
            currentInventorySlotStage = inventorySlotStage.Idle;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(inventoryController.isRelease)
            return;
        Grabbable grab = other.GetComponent<Grabbable>();
        if (grab)
        {
            if (currentStoredItem == null)
            {
                putItemInInventory(grab);
            }
        }
    }


    private void OnTriggerStay(Collider other)
    {
        Hand hand = other.GetComponent<Hand>();
        if (hand)
        {
            //release item
            if (currentStoredItem &&
                currentInventorySlotStage == inventorySlotStage.Idle)
            {
                if (hand.left)
                {
                    if (playerController.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Released)
                    {
                        currentInventorySlotStage = inventorySlotStage.Idle;
                    }
                    
                    if (playerController.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding &&
                        currentInventorySlotStage == inventorySlotStage.Idle)
                    {
                        inventoryController.releaseItem();
                        onGetItemFromInventory(hand);
                    }
                }
                else
                {
                    if (playerController.playerInputListener.rightGripState == PlayerInputListener.ButtonState.Released)
                    {
                        currentInventorySlotStage = inventorySlotStage.Idle;
                    }
                    
                    if (playerController.playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding &&
                        currentInventorySlotStage == inventorySlotStage.Idle)
                    {
                        inventoryController.releaseItem();
                        onGetItemFromInventory(hand);
                    }
                    
                    
                }

#if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.G))
                {
                    inventoryController.releaseItem();
                    onGetItemFromInventory(hand);
                }
#endif
            }
            
        }
    }

    //stored item
    public void putItemInInventory(Grabbable grab)
    {
        if(currentStoredItem || isHoldItem)
            return;
        
        
        Debug.Log(grab.name + " put in inventory");
        //find item component on grabble object
        ShmackleInventoryItem item = grab.GetComponent<ShmackleInventoryItem>();
        
        if (item)
        {
            currentInventorySlotStage = inventorySlotStage.PutItem;
            
            //release hand
            Grabbable[] grabs = grab.GetComponentsInChildren<Grabbable>();
            foreach (Grabbable g in grabs)
            {
                g.HandsRelease();
            }

            //save item to inventory
            currentStoredItem = grab.body.gameObject;
            
            //freeze item in socket position
            Rigidbody rig = currentStoredItem.GetComponent<Rigidbody>();
            rig.isKinematic = true;
            
            //disable collider
            // Collider[] colliders = currentStoredItem.GetComponentsInChildren<Collider>();
            // foreach (Collider c in colliders)
            // {
            //     c.enabled = false;
            // }
            
            //adjust position
            currentStoredItem.transform.SetParent(transform);
            //currentStoredItem.transform.localPosition = Vector3.zero;
            if (item.offsetPoint)
            {
                currentStoredItem.transform.localPosition = Vector3.zero + item.offsetPoint.localPosition;
                currentStoredItem.transform.localRotation = item.offsetPoint.localRotation;
            }
            else
            {
                currentStoredItem.transform.localPosition = Vector3.zero;
                currentStoredItem.transform.localRotation = Quaternion.identity;
            }

            
            inventoryController.audioSource.PlayOneShot(inventoryController.putItemSound);
            isHoldItem = true;
            
            Invoke(nameof(delayAllowGetItem),3);
        }
    }


    void delayAllowGetItem()
    {
        allowGetItem = true;
    }
    
    void onGetItemFromInventory(Hand hand)
    {
        if(allowGetItem == false)
            return;
        isHoldItem = false;
        
        currentInventorySlotStage = inventorySlotStage.GetItem;
        
        inventoryController.audioSource.PlayOneShot(inventoryController.getItemSound);
        
        //enable collider
        // Collider[] colliders = currentStoredItem.GetComponentsInChildren<Collider>();
        // foreach (Collider c in colliders)
        // {
        //     c.enabled = true;
        // }
        
        Rigidbody rig = currentStoredItem.GetComponent<Rigidbody>();
        rig.isKinematic = false;
        
        ShmackleInventoryItem item = currentStoredItem.GetComponent<ShmackleInventoryItem>();
        hand.TryGrab(item.handler);
        
        currentStoredItem.transform.parent  = null;
        currentStoredItem = null;
    }
}
