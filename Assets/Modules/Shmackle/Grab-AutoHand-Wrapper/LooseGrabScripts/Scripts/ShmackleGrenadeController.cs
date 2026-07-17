using System;
using System.Collections;
using UnityEngine;
using Autohand;
using DG.Tweening;
using Fusion;
using MyPooler;
using Shmackle.Analytics;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;


public class ShmackleGrenadeController : ShmackleGrabbleObject
{
    [FoldoutGroup("Game Objects Reference")]
    public GameObject grenadeHolder;
    [FoldoutGroup("Game Objects Reference")]
    public GameObject grenadeUnActive;
    [FoldoutGroup("Game Objects Reference")]
    public GameObject grenadeActive;
    [FoldoutGroup("Game Objects Reference")]
    
    [FoldoutGroup("Audio")]
    public AudioClip explodeSound;
    [FoldoutGroup("Audio")]
    public AudioClip throwGrenadeSound;
    [FoldoutGroup("Audio")]
    public AudioClip grenadeActiveSound;
    [FoldoutGroup("Audio")]
    public AudioSource audioSource;
    
    [FoldoutGroup("Config")]
    public LayerMask notAllowLayer;
    [FoldoutGroup("Config")]
    public float explodeRadius = 3.0f;
    [FoldoutGroup("Config")]
    public float autoDeSpawnTime = 30;
    [FoldoutGroup("Config")] 
    public float delayAllowExplode = 2.0f;
    [FoldoutGroup("Config")] 
    public float delayAutoExplode = 9.0f;

    [FoldoutGroup("Explode Config")] 
    [SerializeField] protected LayerMask explodeLayer;
    [FoldoutGroup("Explode Config")] 
    [SerializeField] protected string explodeFXTag = "ExplodeArea";
    [FoldoutGroup("Explode Config")] 
    [SerializeField] protected string[] detectionTags;
    [FoldoutGroup("Explode Config")] 
    [SerializeField] protected int damage; // default
    [FoldoutGroup("Explode Config")] 
    [SerializeField] protected GameObject[] activeWhenExplode;
    
    
    private bool _isAllowExplode = false;
    private float _timer;
    private IHitable _owner;
    private bool _isInitizalized;
    private bool _isActivate = false;
    private bool _isActive;
    private bool _hasStateAuthority = false;
    
    private Tween _delayAllowExplodeTween;
    private Tween _delayAutoExplodeTween;
    private Tween _delayDestroyTween;
    
    protected override void Update()
    {
 
        if (_hasStateAuthority)
        {
            base.Update();
            if (grabbable.IsHeld())
            {
                if (ShmackleGameManager.Instance.shmackleLocalPlayer.autoHandLeft.holdingObj == grabbable)
                {
                    if (ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.leftTriggerState == PlayerInputListener.ButtonState.Pressed)
                    {
                        if (_isActive == false)
                        {
                            _isActive = true;
                            ActiveGrenade();
                        }
                    }
                }
                else if(ShmackleGameManager.Instance.shmackleLocalPlayer.autoHandRight.holdingObj == grabbable)
                {
                    if (ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.rightTriggerState == PlayerInputListener.ButtonState.Pressed)
                    {
                        if (_isActive == false)
                        {
                            _isActive = true;
                            ActiveGrenade();
                        }
                    }
                }
                
            }
            else
            {
                if (_timer > 0)
                {
                    _timer -= Time.deltaTime;
                    if (_timer <= 0)
                    {
                        DestroyGrenade();
                    }
                }
            }
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        if (!_isInitizalized)
        {
            grabbable.onGrab.AddListener(onGrabGrenade);
            grabbable.onRelease.AddListener(onReleaseGrenade);

            HandleOnGrab.AddListener((hand, grabbable) =>
            {
                _hasStateAuthority = HasStateAuthority;
            });
            
            _isInitizalized = true;
        }
        _isActive = false;
        _isActivate = false;

        //start count despawn time
        if (_hasStateAuthority)
        {
            if (autoDeSpawnTime > 0)
            {
                _timer = autoDeSpawnTime;
            }
        }
        else
        {
            grenadeHolder.SetActive(false);
            grabbable.handType = HandType.none;
        }
    }
    
    public override void ManuallyThrowObject()
    {
#if !UNITY_EDITOR
        Vector3 handVelocity = Vector3.zero;
        if (_hand.left)
        {
            handVelocity = playerController.physicsHandLeft.linearVelocity;
        }
        else
        {
            handVelocity = playerController.physicsHandRight.linearVelocity;
        }

        if (handVelocity.magnitude <= 2.5f)
        {
            isHeld = false;

            grabbable.body.isKinematic = false;
            grabbable.body.useGravity = true;

            grabbable.body.linearVelocity = handVelocity / 2 * grabbable.throwPower;

            if (!ContainsNaN(angularVelocity))
                grabbable.body.angularVelocity = angularVelocity;
            return;
        }
        #endif
        isHeld = false;
        if (HasStateAuthority)
        {
            grabbable.body.isKinematic = false;
            grabbable.body.useGravity = true;

            // grabbable.body.linearVelocity = linearVelocity * _throwMass;
#if !UNITY_EDITOR
            grabbable.body.linearVelocity = handVelocity *   grabbable.throwPower;
#else
            grabbable.body.linearVelocity = playerController.transform.forward *   grabbable.throwPower;
#endif
            if (!ContainsNaN(angularVelocity))
                grabbable.body.angularVelocity = angularVelocity;
        }
    }
    
    private void onGrabGrenade(Hand hand, Grabbable grabbable)
    {
        Debug.Log("On Grab Grenade: " + grabbable.handType);
        _hasStateAuthority = HasStateAuthority;
        if (_hasStateAuthority)
        {
            RPC_ShowGrenade();
            _owner = hand.GetComponentInParent<IHitable>();
        }
    }
    
    void onReleaseGrenade(Hand hand, Grabbable grab)
    {
        if(!_isInitizalized)
            return;

        if (rigidbody.linearVelocity.magnitude > 2)
        {
            audioSource.PlayOneShot(throwGrenadeSound);
        }

        //start count despawn time
        if (_hasStateAuthority)
        {
            _timer = autoDeSpawnTime;
        }

        if (!_isActivate)
        {
            _owner = null;
        }
    }


    //call by pin break call back event
    [ContextMenu("Active Grenade")]
    public void ActiveGrenade()
    {
        if (_isActivate)
        {
            return;
        }
        
        _isActivate = true;
        if (_hasStateAuthority)
        {
            audioSource.PlayOneShot(grenadeActiveSound);
            RPC_ActiveGrenade();

            _delayAutoExplodeTween = DOVirtual.DelayedCall(delayAutoExplode, () =>
            {
                SpawnExplode();
            });
        }
     
    }
    
    [ContextMenu("Spawn Explode")]
    public void SpawnExplode()
    {
        if (_delayAutoExplodeTween != null)
        {
            _delayAutoExplodeTween.Kill();
            _delayAutoExplodeTween = null;
        }
        Debug.Log("Spawn Explode --------");
        _isAllowExplode = false;
        grabbable.ForceHandsRelease();
        grabbable.enabled = false;
        
        _delayDestroyTween = DOVirtual.DelayedCall(2, () =>
        {
            DestroyGrenade();
        });
     
        StartCoroutine(IEExplodeNextFixed());
        // tracking
        AnalyticsHelper.RecordBomb();
    }
    
    private IEnumerator IEExplodeNextFixed()
    {
        RPC_SpawnExplode();
        yield return new WaitForFixedUpdate(); 
        Physics.SyncTransforms();
        Explode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, explodeRadius);
    }

    protected virtual void Explode()
    {
        for (int i = 0; i < activeWhenExplode.Length; i++)
        {
            activeWhenExplode[i].SetActive(true);
        }
        Collider[] colliders = Physics.OverlapSphere(transform.position, explodeRadius, explodeLayer);
        for(int j = 0; j < colliders.Length; j++)
        {
            Collider other = colliders[j];
            for (int i = 0; i < detectionTags.Length; i++)
            {
                if (other.CompareTag(detectionTags[i]))
                {
                    IHitable hitable = other.GetComponentInParent<IHitable>();
                
                    if (hitable == null)
                    {
                        return;
                    }
                        
                        
                    hitable.TakeDamage(_owner, 999, Vector3.zero, Vector3.zero, transform.position);
                }
            }

        }
    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (!_hasStateAuthority)
        {
            return;
        }
        
        if (other.gameObject && _isAllowExplode)
        {
            if (other.gameObject.layer == notAllowLayer)
                return;

            _isAllowExplode = false;
            SpawnExplode();
        }
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnExplode()
    {
        Debug.Log("Spawn explode");
        // var explode = Instantiate(explodeObject, transform.position, Quaternion.identity).GetComponent<TriggerExplodeArea>();
        ObjectPooler.Instance.GetFromPool(explodeFXTag, transform.position, Quaternion.identity);
        
        audioSource.PlayOneShot(explodeSound);
        grenadeHolder.SetActive(false);
 
    }


    //Grenade only appear when player owned grab it
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    protected void RPC_ShowGrenade()
    {
        Debug.Log("Show grenade");
        grenadeHolder.SetActive(true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActiveGrenade()
    {
        grenadeActive.SetActive(true);
        grenadeUnActive.SetActive(false);

        _delayAllowExplodeTween = DOVirtual.DelayedCall(delayAllowExplode, () =>
        {
            _isAllowExplode = true;
        });
    }

    private void DestroyGrenade()
    {
        if (_delayDestroyTween != null)
        {
            _delayDestroyTween.Kill();
            _delayDestroyTween = null;
        }

        if (_delayAllowExplodeTween != null)
        {
            _delayAllowExplodeTween.Kill();
            _delayAllowExplodeTween = null;
        }

        if (grabbable.IsHeld())
        {
            grabbable.ForceHandsRelease();
        }

        DOVirtual.DelayedCall(0, () =>
        {
            if (_delayDestroyTween != null)
            {
                _delayDestroyTween.Kill();
                _delayDestroyTween = null;
            }

            if (_delayAllowExplodeTween != null)
            {
                _delayAllowExplodeTween.Kill();
                _delayAllowExplodeTween = null;
            }
            
            Runner.Despawn(Object);
        });

    }
    
}
