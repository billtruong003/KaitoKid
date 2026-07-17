using System.Linq;
using DG.Tweening;
using Fusion;
using MyPooler;
using Shmackle.Sound;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR;

public class FistBump : NetworkBehaviour
{
    [FoldoutGroup("Others")][SerializeField] private ShmackleNetworkRig _networkRig;
    [FoldoutGroup("Others")][SerializeField] private FistBump oppsiteFistBump;
    [FoldoutGroup("Others")][SerializeField] private GameObject _bumpEffect;
    [FoldoutGroup("Others")][SerializeField] private AudioClip  _bumpSfx;
    [FoldoutGroup("Others")][SerializeField] private float      _bumpDuration   = 0.25f;
    [FoldoutGroup("Others")][SerializeField] private float      _friendDuration = 2f;
    [FoldoutGroup("Others")][SerializeField] private bool       _bumpLeft;
    private                                          bool       _isBumpOnCoolDown = false;
    
    [Networked, OnChangedRender(nameof(OnTriggeredChanged))] public bool  isTriggered {get;  set; }

    public ShmackleNetworkRig networkRig => _networkRig;
    public bool IsLeft => _bumpLeft;

    public bool isActive
    {
        get { return _isActive; }
        set
        {
            #if UNITY_EDITOR
            if (_isDebugBump == false)
            #endif
            {
                _isActive = value;

                if (_isActive != isTriggered)
                {
                    isTriggered = _isActive;
                    if (IsLeft)
                    {
                        networkRig.playerController.playerAbilities.highFiveLeft.gameObject.SetActive(!_isActive);
                    }
                    else
                    {
                        networkRig.playerController.playerAbilities.highFiveRight.gameObject.SetActive(!_isActive);
                    }
                }
            }
        }
    }
    
    private bool     _isActive = false;
    [SerializeField]private FistBump _targetFistBump;
    private float    _timeCount;
        
    #if UNITY_EDITOR
    private bool _isDebugBump;
    #endif
    

    private void Update()
    {
        if (_isActive && networkRig.IsLocalNetworkRig)
        {
            if (_targetFistBump != null &&
                _targetFistBump != oppsiteFistBump)
            {
                if (_targetFistBump != null && _targetFistBump != oppsiteFistBump)
                {
                    //_timeCount += Time.deltaTime;
            
                    //if no fishBump active then active friend up instead
                    // if (_timeCount >= _bumpDuration * 2)
                    // {
                    //     networkRig.EffectHub.friendUpChargeEffect.SetActive(true);
                    //     if (_targetFistBump)
                    //     {
                    //         Vector3 midPoint = (_targetFistBump.transform.position + transform.position) / 2;
                    //         networkRig.EffectHub.friendUpChargeEffect.transform.position = midPoint;
                    //     }
                    // }
                    //
                    // if (_timeCount >= _friendDuration)
                    // {
                    //     var shmacklePlayer = ShmackleGameManager.Instance.shmacklePlayerList.FirstOrDefault(p => p.playerId == _targetFistBump.networkRig.playerID);
                    //     if (shmacklePlayer != null)
                    //     {
                    //         Debug.Log("Friend Up with " + shmacklePlayer.playerName);
                    //         ShmackleGameManager.Instance.SendPartyInvite(shmacklePlayer.playfabsId, shmacklePlayer.playerName);
                    //         networkRig.EffectHub.friendUpChargeEffect.SetActive(false);
                    //         _targetFistBump = null;
                    //     }
                    // }
                }
            }
        }
        else
        {
            if (_targetFistBump != null)
            {
                _targetFistBump = null;
                
                if(networkRig.EffectHub.friendUpChargeEffect)
                    networkRig.EffectHub.friendUpChargeEffect.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isActive == false)
        {
            return;
        }
        
        if (other.CompareTag("PlayerHand") == false)
        {
            return;
        }
        
        if (_isBumpOnCoolDown == true)
        {
            return;
        }

        if (_targetFistBump != null)
        {
            return;
        }
        
        var fistBump = other.GetComponent<FistBump>();
        if (fistBump == null || fistBump.isActive == false)
        {
            return;
        }

        Debug.Log($"[FistBump] FistBump triggered {other.name}]");

        _targetFistBump = fistBump;
        _timeCount      = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isActive == false)
        {
            return;
        }
        
        if (other.CompareTag("PlayerHand") == false)
        {
            return;
        }

        if (_targetFistBump == null)
        {
            return;
        }
        
        if(networkRig.EffectHub.friendUpChargeEffect)
            networkRig.EffectHub.friendUpChargeEffect.SetActive(false);
        
        var fistBump = other.GetComponent<FistBump>();
        if (fistBump == null || fistBump != _targetFistBump)
        {
            return;
        }

        if(networkRig.EffectHub.friendUpChargeEffect.activeInHierarchy)
            return;
        
        //if (_timeCount >= _bumpDuration)
        {
            _isBumpOnCoolDown = true;

            if (_bumpEffect != null)
            {
                if (ObjectPooler.Instance.poolDictionary.ContainsKey(_bumpEffect.name))
                {
                    ObjectPooler.Instance.GetFromPool(_bumpEffect.name, transform.position, Quaternion.identity);
                }
                else
                {
                    Instantiate(_bumpEffect, transform.position, Quaternion.identity);
                }
            }

            if (_bumpSfx != null)
            {
                SoundManager.Instance.PlayOneShot(_bumpSfx);
            }

            // 2 FistBump on the same player
            if (this.HasStateAuthority && _targetFistBump.HasStateAuthority)
            {
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, 0.5f, 0.2f);
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, 0.5f, 0.2f);
            }
            else if (this.HasStateAuthority)
            {
                if (_bumpLeft)
                {
                    InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, 0.5f, 0.2f);
                }
                else
                {
                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, 0.5f, 0.2f);
                }
            }
            else if (_targetFistBump.HasStateAuthority)
            {
                if (_targetFistBump.IsLeft)
                {
                    InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, 0.5f, 0.2f);
                }
                else
                {
                    InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, 0.5f, 0.2f);
                }
            }

            DOVirtual.DelayedCall(_bumpDuration, () =>
                                                 {
                                                     _isBumpOnCoolDown = false;
                                                 });
        }
        _targetFistBump = null;
    }

    private void OnTriggeredChanged()
    {
        Debug.Log($"OnTriggeredChanged {isTriggered}");
        _isActive = isTriggered;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Fist Bump Triggered")]
    private void FistBumpTriggered()
    {
        _isDebugBump = true;
        isTriggered  = true;
        OnTriggeredChanged();
        
        if (IsLeft)
        {
            networkRig.playerController.playerAbilities.highFiveLeft.gameObject.SetActive(!_isActive);
        }
        else
        {
            networkRig.playerController.playerAbilities.highFiveRight.gameObject.SetActive(!_isActive);
        }
    }
    
    [ContextMenu("Fist Bump Disable")]
    private void FistBumpDisable()
    {
        _isDebugBump = true;
        isTriggered  = false;
        OnTriggeredChanged();
    }
    #endif
}