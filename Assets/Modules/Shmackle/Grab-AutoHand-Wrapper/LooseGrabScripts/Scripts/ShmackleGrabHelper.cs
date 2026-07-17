using System;
using UnityEngine;
using Autohand;

public class ShmackleGrabHelper : MonoBehaviour
{
    public Grabbable grabbable;
    private Hand currentHand;
    private ShmackleNetworkRig currentLocalPlayer;

    Hand hand;


    private void Start()
    {
        grabbable.OnBeforeGrabEvent += OnGrab;
        grabbable.onGrab.AddListener(OnGrab);
        grabbable.onRelease.AddListener(OnRelease);
    }

    private void OnRelease(Hand arg0, Grabbable arg1)
    {
        if (currentLocalPlayer)
        {
            SetAutohandPhysic(false);
            currentHand = null;
            currentLocalPlayer = null;
        }
    }
    
    public void SetAutohandPhysic(bool isActive)
    {
        if (currentLocalPlayer)
        {
            currentLocalPlayer.isUseLeftAutohandPhysics = isActive;
            currentLocalPlayer.isUseRightAutohandPhysics = isActive;
        }

    }

    private void OnGrab(Hand arg0, Grabbable arg1)
    {
        ShmackleNetworkRig player = arg0.GetComponentInParent<ShmackleNetworkRig>();
        if (player != null && player.IsLocalNetworkRig)
        {
            arg0.TryGrab(grabbable);
            currentLocalPlayer = player;
            currentHand = hand;
            SetAutohandPhysic(true);
            Debug.Log("Using AutoHand physics");
        }
    }
    
    

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
    
        hand = other.GetComponent<Hand>();
        if (hand != null)
        {
            ShmackleNetworkRig player = hand.GetComponentInParent<ShmackleNetworkRig>();
            if (player != null && player.IsLocalNetworkRig)
            {
                Debug.Log("Using AutoHand physics");
                currentLocalPlayer = player;
                currentHand = hand;
                SetAutohandPhysic(true);
                //hand.TryGrab(grabbable);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other == null || currentHand == null) return;
    
        Hand hand = other.GetComponent<Hand>();
        if (hand != null && hand == currentHand && currentLocalPlayer != null)
        {
            if (currentLocalPlayer)
            {
                SetAutohandPhysic(false);
            }
            currentHand = null;
            currentLocalPlayer = null;
        }
    }
}