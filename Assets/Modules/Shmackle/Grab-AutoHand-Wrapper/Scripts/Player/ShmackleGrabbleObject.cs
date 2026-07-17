
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Autohand;

using DG.Tweening;
using Fusion;
using Fusion.XR.Shared;
using Sirenix.OdinInspector;
using UnityEngine.Events;


public class ShmackleGrabbleObject : NetworkBehaviour
{
    [SerializeField] protected Grabbable grabbable;
    [SerializeField] protected Rigidbody rigidbody;

    [SerializeField] private Collider[] colliders;

    private GameObject networkRenderObject;

    public ShmacklePlayerController playerController { private set; get; }


    public bool isHeld;
    public bool isUsePhysic;

    [Fusion.ReadOnly] public Hand _hand;
    private ShmackleHandController handPhysics;
    private bool isFoundHand;

    //=========//
    private bool _isColliding;
    private Rigidbody playerRigidbody;
    private Transform targetController;
    private Transform targetPhysicsHand;
    Vector3 _previousPosition;

    //======//
    public bool isTakingAuthority = false;

    public bool dontAllowGrabByOther;

    public bool onlyAuthorityUser;

    public bool dontSetParent;
    public bool dontSetParentForRemotePlayer = false;
    public bool dontAllowDoubleJumpWhenGrabbing = false;

    private Transform lastGrabbableTransform;

    [SuffixLabel("Optional")] public Collider[] colliderList;

    public UnityEvent<Hand, Grabbable> HandleOnGrab { get; set; } = new UnityEvent<Hand, Grabbable>();
    public UnityEvent<Hand, Grabbable> HandleOnRelease { get; set; } = new UnityEvent<Hand, Grabbable>();


    private Tween _tween;
    private Tween _delayTween;
    public Transform tip;
    private bool _isSpawned = false;
    private bool _isGrabbing = false; // Check grabbing state locally
    private CancellationTokenSource _grabCts;

    protected virtual void Update()
    {
        if (!isUsePhysic && grabbable.IsHeld())
        {
            TrackVelocities();
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        grabbable.OnBeforeGrabEvent += (hand, grabbable1) =>
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].isTrigger = true;
            }
        };
        grabbable.OnBeforeReleaseEvent += (hand, grabbable1) =>
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].isTrigger = false;
            }
        };
        grabbable.onGrab.AddListener(OnGrabObject);
        grabbable.onRelease.AddListener(onReleaseObject);

        _isSpawned = true;
        Debug.Log("register grab and release event");
        playerController = ShmackleGameManager.Instance.shmackleLocalPlayer;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        _grabCts?.Cancel();
        _grabCts?.Dispose();

        if (_delayTween != null)
        {
            _delayTween.Kill();
            _delayTween = null;
        }

        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
    }

    protected virtual void OnGrabObject(Hand hand , Grabbable grabbable)
    {

        if (isHeld)
            return;
        Debug.Log("Grabbing" + gameObject.name + "-" + hand.left);
        isHeld = true;
        _hand = hand;

        playerController = ShmackleGameManager.Instance.shmackleLocalPlayer;

        if (hand.left)
        {
            handPhysics = playerController.physicsHandLeft;
        }
        else
        {
            handPhysics = playerController.physicsHandRight;
        }

        Grab(hand);
    }



    public async void Grab(Hand hand)
    {
        _grabCts?.Cancel();
        _grabCts = new CancellationTokenSource();
        var token = _grabCts.Token;

        isTakingAuthority = true;
        Debug.Log("Before Grab --- " + _hand.left);
        await Object.WaitForStateAuthority();
        Debug.Log(">> after get authority");

        if (token.IsCancellationRequested)
        {
            isTakingAuthority = false;
            return;
        }

        if (dontAllowGrabByOther || onlyAuthorityUser)
        {
            if (Object.HasStateAuthority)
            {
                RPC_DontAllowOthersGrab();
            }

            if (grabbable.handType == HandType.none)
            {
                ChangeHandType(HandType.both);
                foreach (var col in colliderList)
                {
                    col.enabled = true;
                }
            }
        }

        isTakingAuthority = false;

        if (ShmackleConnectionManager.Instance.IsBloodJmanMinigame() &&  playerController.playerHealth.IsDead)
        {
            hand.ForceReleaseGrab();
            return;
        }

        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        if (_delayTween != null)
        {
            _delayTween.Kill();
            _delayTween = null;
        }

        //await Task.Delay(500);
        _isGrabbing = true;

        _hand = hand;
        if (isUsePhysic)
        {
            SetAutohandPhysic(hand.left, true);
            HandleOnGrab?.Invoke(hand, grabbable);
        }
        else
        {
            SetAutohandPhysic(hand.left, true);
            Debug.Log("Before Grab --- " + _hand.left);
            _tween = DOVirtual.DelayedCall(0.25f , () =>
            {
                Debug.Log("after Grab --- " + _hand.left);
                if (HasStateAuthority)
                {
                    RPC_GrabNoPhysics();
                }


                _tween = DOVirtual.DelayedCall(0.0f, () =>
                {
                    Vector3 heldPosition = grabbable.transform.localPosition;
                    if (_hand != null)
                    {
                        _hand.ResetGrabConnectionOffset();
                        grabbable.transform.localPosition = heldPosition;
                        SetAutohandPhysic(hand.left, false);
                        playerController.playerAbilities.dontAllowDoubleJump = false;
                        Debug.Log(grabbable.transform.localPosition + "----");
                    }
                });

                HandleOnGrab?.Invoke(hand, grabbable);
            });
        }


        if (dontAllowDoubleJumpWhenGrabbing && playerController != null &&
            playerController.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            playerController.playerAbilities.dontAllowDoubleJump = true;
        }

        if (tip == null)
            return;

        playerController.physicsRig.disableMovement = true;
        playerController.physicsRig.ToggleNoCollideMode(true);
        if (hand.left)
        {
            playerController.physicsRig.SetHoldingTool(true , true, tip);
        }
        else
        {
            playerController.physicsRig.SetHoldingTool(false , true, tip);
        }

        _delayTween = DOVirtual.DelayedCall(0.25f, () =>
        {

            Debug.Log("Reset Disable Movement");
            playerController.physicsRig.disableMovement = false;
            playerController.physicsRig.ToggleNoCollideMode(false);
        });

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

    void onReleaseObject(Hand hand, Grabbable grabbable)
    {
        Debug.Log("Release" + gameObject.name);

        isHeld = false;

        if (playerController == null)
        {
            return;
        }

        _isGrabbing = false;
        _grabCts?.Cancel();

        SetAutohandPhysic(hand.left, false);
        handPhysics = null;

        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }

        if (dontAllowDoubleJumpWhenGrabbing && playerController != null &&
            playerController.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            playerController.playerAbilities.dontAllowDoubleJump = false;
        }


        if (_delayTween != null)
        {
            _delayTween.Kill();
            _delayTween = null;

            playerController.physicsRig.disableMovement = false;
            playerController.physicsRig.ToggleNoCollideMode(false);
        }

        if (!isUsePhysic && HasStateAuthority)
        {

            RPC_ReleaseNoPhysics();
        }

        _tween = DOVirtual.DelayedCall(0.25f, () =>
        {
            if (Object.HasStateAuthority && dontAllowGrabByOther && !onlyAuthorityUser)
            {
                RPC_AllowGrab();
            }

            HandleOnRelease?.Invoke(hand, grabbable);
        });

        if (tip == null)
            return;

        if (hand.left)
        {
            playerController.physicsRig.SetHoldingTool(true, false); // On left hand release:
        }
        else
        {
            playerController.physicsRig.SetHoldingTool(false, false);
        }
    }

    public virtual void ChangeHandType(HandType handType)
    {
        grabbable.handType = handType;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DontAllowOthersGrab()
    {
        if (!Object.HasStateAuthority)
        {
            if (_isGrabbing)
            {
                grabbable.ForceHandsRelease();
            }

            //set current hand type to none so no one can grab it
            ChangeHandType(HandType.none);
            foreach (var col in colliderList)
            {
                col.enabled = false;
            }
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AllowGrab()
    {
        Debug.Log("AllowGrab");
        ChangeHandType(HandType.both);
        foreach (var col in colliderList)
        {
            col.enabled = true;
        }
    }

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_GrabNoPhysics()
    {
        if (!dontSetParent)
        {
            bool hasStateAuthority = HasStateAuthority;
            if (hasStateAuthority ||  !dontSetParentForRemotePlayer)
            {
                grabbable.transform.parent = _hand.transform;
            }
        }
        rigidbody.isKinematic = true;
        
        Debug.Log("Set Parent: ---"  + _hand.left);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReleaseNoPhysics()
    {
        if (!dontSetParent)
        {
            bool hasStateAuthority = HasStateAuthority;
            if (hasStateAuthority ||  !dontSetParentForRemotePlayer)
            {
                transform.SetParent(null);
            }
          
        }
 
        Debug.Log(">> parent: " + grabbable.transform.parent);
        grabbable.ForceHandRelease(_hand);
        grabbable.body.useGravity = true;
        grabbable.body.isKinematic = false;

        if (_hand == null)
        {
            return;
        }
        
        if (_hand.left)
        {
          //  #if !UNITY_EDITOR
            if (playerController.physicsHandLeft.linearVelocity.magnitude > 0.1f)
// #endif
            {
                //To Do throw object on left hand
                ManuallyThrowObject();
            }
        }
        else
        {
  // #if !UNITY_EDITOR
            if (playerController.physicsHandRight.linearVelocity.magnitude > 0.1f)
            // #endif
            {
                //To Do throw object on right hand
                ManuallyThrowObject();
            }
        }
    }


    #region Manually Throw
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    protected Vector3 linearVelocity;
    protected Vector3 angularVelocity;
    
    // Call this when the object is released
    public virtual void ManuallyThrowObject()
    {
        Debug.Log("Manually Throw");
        
        isHeld = false;

        grabbable.body.isKinematic = false;
        grabbable.body.useGravity = true;

        grabbable.body.linearVelocity = linearVelocity * grabbable.throwPower;
        
        if (!ContainsNaN(angularVelocity))
            grabbable.body.angularVelocity = angularVelocity;
    }
    
    protected virtual void TrackVelocities()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // Linear velocity
        linearVelocity = (currentPosition - lastPosition) / Time.deltaTime;

        // Angular velocity
        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
        if (angleInDegrees > 180f) angleInDegrees -= 360f;
        angularVelocity = rotationAxis * angleInDegrees * Mathf.Deg2Rad / Time.deltaTime;

        // Update last frame data
        lastPosition = currentPosition;
        lastRotation = currentRotation;
    }

    protected bool ContainsNaN(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
    }
    
    

    #endregion
    
    #if UNITY_EDITOR
    [ContextMenu("Auto Find Ref")]
    public void AutoFindRef()
    {
        grabbable = GetComponent<Grabbable>();
        rigidbody = GetComponent<Rigidbody>();
    }
#endif
}
