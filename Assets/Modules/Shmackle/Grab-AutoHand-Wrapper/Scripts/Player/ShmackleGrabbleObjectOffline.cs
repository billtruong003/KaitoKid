using System;
using System.Collections;
using Autohand;
using DG.Tweening;
using UnityEngine;

public class ShmackleGrabbleObjectOffline : MonoBehaviour
{
    [SerializeField]private Grabbable grabbable;
    [SerializeField]private Rigidbody rigidbody;
    private bool isHeld;
    ShmacklePlayerController playerController;
    private Rigidbody playerRigidbody;
    

    private Hand _hand;
    private ShmackleHandController handPhysics;
    private bool isFoundHand;
    
    
    //=========//
    private Transform targetController;
    private Transform targetPhysicsHand;
    Vector3 _previousPosition;

    private void Start()
    {
        playerController = ShmackleGameManager.Instance.shmackleLocalPlayer;
        grabbable.onGrab.AddListener(OnGrabObject);
        grabbable.onRelease.AddListener(onReleaseObject);
    }

    private Coroutine _setupGrabCoroutine;
    void OnGrabObject(Hand hand , Grabbable grabbable)
    {
        if(isHeld)
            return;
        Debug.Log("Grabbing" + gameObject.name);
        isHeld = true;
        _hand = hand;
        
        if (playerController == null)
        {
            playerController = ShmackleGameManager.Instance.shmackleLocalPlayer;
        }

        SetAutohandPhysic(hand.left, true);

        if (_setupGrabCoroutine != null)
        {
            StopCoroutine(_setupGrabCoroutine);
        }
        _setupGrabCoroutine = StartCoroutine(SetupGrabObject(hand, grabbable));

    }
    public void SetAutohandPhysic(bool isLeft, bool isActive)
    {
        if (isLeft)
        {
            playerController.playerModuleRef.shmackleNetworkRig.isUseLeftAutohandPhysics = isActive;
        }
        else
        {
            playerController.playerModuleRef.shmackleNetworkRig.isUseRightAutohandPhysics = isActive;
        }
    }
    
    IEnumerator SetupGrabObject(Hand hand , Grabbable grabbable)
    {
        yield return new WaitForSeconds(0.25f);
        Debug.Log("Setup GrabObject: " + hand.left +"-" + gameObject.name);

        if (hand.left)
        {
            handPhysics = playerController.physicsHandLeft;
        }
        else
        {

            handPhysics = playerController.physicsHandRight;
        }
        transform.SetParent(hand.transform);
        SetAutohandPhysic(hand.left, false);
        rigidbody.isKinematic = true;

        DOVirtual.DelayedCall(0.0f, () =>
        {
            _hand.ResetGrabConnectionOffset();
        });
    }
    
    void onReleaseObject(Hand hand, Grabbable grabbable)
    {
        Debug.Log(hand.left + "- Release: " + gameObject.name);
        
        if (_setupGrabCoroutine != null)
        {
            StopCoroutine(_setupGrabCoroutine);
        }        
        isHeld = false;
        transform.parent = null;
        rigidbody.isKinematic = false;
        rigidbody.useGravity = true;
        //SetAutohandPhysic(hand.left, false);
        playerController.playerModuleRef.shmackleNetworkRig.isUseLeftAutohandPhysics = false;
        playerController.playerModuleRef.shmackleNetworkRig.isUseRightAutohandPhysics = false;
        
        if (playerController.physicsHandLeft.handVelocity > 0.25f ||
            playerController.physicsHandRight.handVelocity > 0.25f)
        {
            ThrowObject();
        }
    }
    
    [ContextMenu("Throw Object")]
    void ThrowObject()
    {
        if (grabbable == null || handPhysics == null || playerController == null)
        {
            return;
        }
        
        grabbable.ForceHandRelease(_hand);
        // Ensure the object isn't being held before throwing
        if (!grabbable.IsHeld())
        {
            if (handPhysics.HandRigidbody == null)
            {
                return;
            }
            // Get the hand's velocities from its Rigidbody
            Vector3 throwDirection = handPhysics.HandRigidbody.linearVelocity;
            Vector3 throwAngularVelocity = handPhysics.HandRigidbody.angularVelocity;
            
            // Optional: adjust the linear velocity with a multiplier
            Vector3 adjustedThrowVelocity = throwDirection * grabbable.throwPower * 0.5f;
                
            grabbable.ManuallyThrowObject(adjustedThrowVelocity, throwAngularVelocity);
            Debug.Log("Object thrown manually!");
        }
        else
        {
            Debug.Log("Object is still held – release it first.");
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Find All Refs")]
    public void FindAllRef()
    {
        grabbable = GetComponent<Grabbable>();
        rigidbody = GetComponent<Rigidbody>();
    }
    #endif
}
