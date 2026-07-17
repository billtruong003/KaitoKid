using System;
using System.Collections;
using System.Collections.Generic;
using Autohand;
using Fusion;

using UnityEngine;

public class ShmackleInventoryItem : NetworkBehaviour
{
    public Transform offsetPoint;
    public Grabbable handler;
    [HideInInspector]public NetworkObject networkObject;
    [HideInInspector] public Rigidbody rigidbodyItem;
    public Collider colliderItem;
    public bool isImportantItem = true;
    public string heldBy;
    
    
    Transform savedTransform;
    
    
    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        rigidbodyItem = GetComponent<Rigidbody>();
        //handler.onGrab.AddListener(onGrabItem);
        //handler.onRelease.AddListener(onReleaseItem);
        if(ShmackleGameManager.Instance && isImportantItem)
            ShmackleGameManager.Instance.inventoryItemList.Add(this);
        
        savedTransform = transform;
    }

#if MULTIPLAY_MODE
    [Rpc(RpcSources.All, RpcTargets.All)]
#endif
    void RPC_PutItem()
    {
        Debug.Log("Putting item");
        heldBy = networkObject.StateAuthority.ToString();
        if (networkObject.HasStateAuthority == false)
        {
            if(colliderItem)
                colliderItem.enabled = false;
            Debug.Log("disable collider for item");
        }
    }

#if MULTIPLAY_MODE
    [Rpc(RpcSources.All, RpcTargets.All)]
#endif
    public void RPC_ResetItem()
    {
        heldBy = null;
        StartCoroutine(WaitForAuthorityAndReset());
        Debug.Log("reset item");
    }
    
    private IEnumerator WaitForAuthorityAndReset()
    {
        while (!networkObject.HasStateAuthority)
        {
            yield return null; // Wait until the client has authority
        }

        Debug.Log("Authority received. Resetting item.");
        transform.position = savedTransform.position;
        transform.rotation = savedTransform.rotation;
        
        rigidbodyItem.isKinematic = false;
        if(colliderItem)
            colliderItem.enabled = true;
    }
    
#if MULTIPLAY_MODE
    [Rpc(RpcSources.All, RpcTargets.All)]
#endif
    public void RPC_GrabItem()
    {
        heldBy = networkObject.StateAuthority.ToString();
        if (rigidbodyItem.isKinematic)
            rigidbodyItem.isKinematic = false;
    }
    


    private void OnTriggerEnter(Collider other)
    {
        ShmackleInventorySlot slot = other.GetComponent<ShmackleInventorySlot>();
        if (slot)
        {
            RPC_PutItem();
        }
    }


    void onReleaseItem(Hand hand, Grabbable grab)
    {
        if (networkObject != null)
        {
            //RPC_ResetItem();
        }
    }


    void onGrabItem(Hand hand, Grabbable grab)
    {
        if (networkObject != null)
        {
            RPC_GrabItem();
        }
    }
    
    
    

    
}
