using System;
using System.Collections;
using System.Collections.Generic;
using _Shmackle.Minigames;
using Cysharp.Threading.Tasks.Triggers;
using DG.Tweening;
using Fusion;
using Micosmo.SensorToolkit;
using NaughtyAttributes;
using Shmackle.Data;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public struct ShootingInput : INetworkInput
{
    public bool leftShoulderWeaponActive;
    public bool rightShoulderWeaponActive;
    public bool leftTriggerValue;
    public bool rightTriggerValue;
    public bool leftThumbTriggerValue;
}

public class ShmacklePlayerShooting : NetworkBehaviour
{
    [SerializeField] private ShmacklePlayerController _shmacklePlayerController;
    [SerializeField] private BasePlayerWeapon[] weapons = new BasePlayerWeapon[4];
    [SerializeField] private BasePlayerDroneController droneController;
    [SerializeField] private int chargeConsumeAmmo = 3;
    [SerializeField] private List<GameObject> armors;
    [SerializeField] private GameObject leftHandArmor;
    [SerializeField] private GameObject rightHandArmor;
    [SerializeField] private Transform chestWeaponPos;
    [SerializeField] private Transform leftShoulderWeaponPos;
    [SerializeField] private Transform rightShoulderWeaponPos;
    [SerializeField] private Transform leftHandPos;
    [SerializeField] private Transform rightHandPos;
    [SerializeField] private Transform dronePos;
    [SerializeField] private GameObject leftHandPanel;
    [SerializeField] private GameObject rightHandPanel;
    [SerializeField] private float distanceDefault;
    [SerializeField] private AudioSource equipSource;
    [SerializeField] private AudioClip equipSound;
    [SerializeField] private AudioSource unequipSource;
    [SerializeField] private AudioClip unequipSound;

    public int playerDamage = 20;
    public int playerFireRate = 0;
    public int ammos = 100;

    public EntityType[] targetTags = new EntityType[] { EntityType.Enemy, EntityType.Boss, EntityType.Item };
    public GameObject aimTarget;
    public LayerMask aimMask;
    public RangeSensor aimSensor;

    public InputActionProperty joyStickAxis;

    // Speed multiplier for joystick movement.
    public float aimSpeed = 5f;

    [Networked] private NetworkBool leftShoulderWeaponActive { get; set; }
    [Networked] private NetworkBool rightShoulderWeaponActive { get; set; }
    [Networked] private NetworkBool leftTriggerValue { get; set; }
    [Networked] private NetworkBool rightTriggerValue { get; set; }
    [Networked] private NetworkBool leftThumbTriggerValue { get; set; }
    [SerializeField] private bool _weaponReady;
    [SerializeField] private bool _previousReadyPress;

    //public DOTweenAnimation cameraShake;
    private Transform leftHand;
    private Transform rightHand;

    public GameObject crossHair;

    public LayerMask crossHairMask;

    public Vector3 AimPosition { set; get; }
    [Networked] public bool IsCombatActive{ get; set; }
    public bool isConsumeAmmo;
    public bool IsCanTriggerLeftHandWeaponAble { protected set; get; }
    public bool IsCanTriggerRightHandWeaponAble { protected set; get; }
    public bool IsCanTriggerLeftShoulderWeaponAble { protected set; get; }
    public bool IsCanTriggerRightShoulderWeaponAble { protected set; get; }
    public bool IsShootingMode { protected set; get; }

    private bool _isInitialized;
    private bool _isSpawned = false;
    
    [SerializeField] private bool _isCombatScene;
    private RaycastHit[] _raycastHits = new RaycastHit[1];

    private bool _hasStateAuthority;
    
    void Start()
    {
        _isCombatScene = ShmackleConnectionManager.Instance.IsOffShore();

        aimTarget.SetActive(false);
        leftHand = _shmacklePlayerController.leftHandPosition;
        rightHand = _shmacklePlayerController.rightHandPosition;
        for (int i = 0; i < weapons.Length; i++)
        {
            var weapon = weapons[i];
            if (weapon == null)
            {
                continue;
            }

            InitWeapon(weapon);
        }

        StartDrone();
        ActiveNotificationPanel(false, false);
        ActiveNotificationPanel(true, false);
        UpdateWeaponAmmo();
        SetActiveTriggerBothHandWeaponAble(true);
        SetActiveTriggerBothShoulderWeaponAble(true);

        _isInitialized = true;
    }

    public override void Spawned()
    {
        _hasStateAuthority = HasStateAuthority;
        if (!_hasStateAuthority)
        {
            RPC_RequestSync();
            isConsumeAmmo = false;
            
            if (!IsCombatActive)
            {
                IsCombatActive = false;
                _weaponReady = false;
                ShowHideCombatComponent();
                gameObject.SetActive(false);
            }
        }
        _isSpawned = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        _isSpawned = false;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        
        if (_shmacklePlayerController.playerHealth.IsDead)
        {
            return;
        }

        bool _puchLeftMode = ShmackleGameManager.Instance.shmackleLocalPlayer.playerAbilities.playerPunchColliderLeft
                                 .isActive ||
                             (ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding &&
                              ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding && 
                              !ShmackleGameManager.Instance.shmackleLocalPlayer.isGrounded);

        bool _puchRightMode = ShmackleGameManager.Instance.shmackleLocalPlayer.playerAbilities.playerPunchColliderRight
            .isActive || (ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding &&
                          ShmackleGameManager.Instance.shmackleLocalPlayer.playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding && 
                          !ShmackleGameManager.Instance.shmackleLocalPlayer.isGrounded);

        ShootingInput data = new ShootingInput();
        data.leftShoulderWeaponActive = (_shmacklePlayerController.playerInputListener.leftPrimaryButtonState == PlayerInputListener.ButtonState.Holding || Input.GetKey(KeyCode.G)) && !_puchLeftMode && IsCanTriggerLeftShoulderWeaponAble;
        data.rightShoulderWeaponActive = (_shmacklePlayerController.playerInputListener.rightPrimaryButtonState == PlayerInputListener.ButtonState.Holding || Input.GetKey(KeyCode.H)) && !_puchRightMode && IsCanTriggerRightShoulderWeaponAble;
#if UNITY_EDITOR
        data.leftTriggerValue = Input.GetKey(KeyCode.Z);
        data.rightTriggerValue = Input.GetKey(KeyCode.X);
#else
        data.leftTriggerValue = _shmacklePlayerController.playerInputListener.leftTriggerState == PlayerInputListener.ButtonState.Holding;
        data.rightTriggerValue = _shmacklePlayerController.playerInputListener.rightTriggerState == PlayerInputListener.ButtonState.Holding; 
#endif

        data.leftThumbTriggerValue =  Input.GetKey(KeyCode.Q) || _shmacklePlayerController.playerInputListener.leftThumbClickState == PlayerInputListener.ButtonState.Holding;
        
        
        if (!_puchLeftMode && IsCanTriggerLeftHandWeaponAble)
        {

        }
        else
        {
            data.leftTriggerValue = false;
        }

        if (!_puchRightMode && IsCanTriggerRightHandWeaponAble)
        {

        }
        else
        {
            data.rightTriggerValue = false;
        }

        if (ammos <= 0 && isConsumeAmmo)
        {
            data.leftTriggerValue = false;
            data.rightTriggerValue = false;
            data.rightShoulderWeaponActive = false;
            data.leftShoulderWeaponActive = false;
            data.leftThumbTriggerValue = false;
            return;
        }

        input.Set(data);
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!_isInitialized)
        {
            return;
        }

        if (!IsCombatActive)
        {
            return;
        }

        if (ammos <= 0 && isConsumeAmmo)
        {
            return;
        }
        
        if (_hasStateAuthority && GetInput(out ShootingInput inputData))
        {
            leftShoulderWeaponActive = inputData.leftShoulderWeaponActive;
            rightShoulderWeaponActive = inputData.rightShoulderWeaponActive;
             leftTriggerValue = inputData.leftTriggerValue;
             rightTriggerValue = inputData.rightTriggerValue;
             leftThumbTriggerValue = inputData.leftThumbTriggerValue;
            
            // leftThumbTriggerValue = _shmacklePlayerController.playerInputListener.leftThumbClickState == PlayerInputListener.ButtonState.Holding;
            // leftShoulderWeaponActive = (_shmacklePlayerController.playerInputListener.leftPrimaryButtonState == PlayerInputListener.ButtonState.Holding || Input.GetKey(KeyCode.G)) && !_puchLeftMode;
            // rightShoulderWeaponActive = (_shmacklePlayerController.playerInputListener.rightPrimaryButtonState == PlayerInputListener.ButtonState.Holding || Input.GetKey(KeyCode.H)) && !_puchRightMode;
            //
            // leftTriggerValue = Input.GetKey(KeyCode.Z) || _shmacklePlayerController.playerInputListener.leftTriggerState == PlayerInputListener.ButtonState.Holding;
            // rightTriggerValue =  Input.GetKey(KeyCode.X) || _shmacklePlayerController.playerInputListener.rightTriggerState == PlayerInputListener.ButtonState.Holding;
        }
    }

    public void OnDead()
    {
        rightShoulderWeaponActive = false;
        _weaponReady = false;
        leftShoulderWeaponActive = false;
        ActiveWeapons(false);
        SetActiveCrossHair(false);
        for (int index = 0; index < weapons.Length; index++)
        {
            if (!CheckWeaponAvailable(index))
            {
                continue;
            }

            weapons[index].ResetWeaponStatus();

            if (index == 0 || index == 1)
            {
                //weapons[index].SetActiveLaserSign(false, true);
            }
        }

        if (_hasStateAuthority)
        {
            if (droneController != null)
            {
                droneController.SetState(DroneState.STOP);
            }
        }
    }

    public bool CheckWeaponAvailable(int weaponIndex)
    {
        if (weaponIndex >= weapons.Length)
        {
            return false;
        }

        // #if !UNITY_EDITOR
        //     if (weapons[weaponIndex].EquipmentSlotType == WeaponEquipmentSlotType.Shoulder)
        //     {
        //         return false;
        //     }
        // #endif

        return weapons[weaponIndex];
    }


    public void SetActiveCombat(bool active)
    {
        if (!_hasStateAuthority)
        {
            return;
        }
        RPC_OnCombatActive(active);
        for (int i = 0; i < weapons.Length; i++)
        {
            if (!CheckWeaponAvailable(i))
            {
                continue;
            }

            if (weapons[i].Type == WeaponAttackType.Range && weapons[i].EquipmentSlotType == WeaponEquipmentSlotType.Hand)
            {
                weapons[i].SetActiveLaserSign(active && _hasStateAuthority, true);
            }
        }

        if (droneController != null)
        {
            droneController.SetDroneActive(active);
            droneController.ShowHideDrone(IsCombatActive);
        }

        if (!active)
        {
            SetActiveCrossHair(false);
        }
    }

    public void SetCanConsumeAmmo(bool canConsumeAmmo)
    {
        isConsumeAmmo = canConsumeAmmo && _hasStateAuthority;
    }

    public void ActiveWeaponsLaser(bool active)
    {
        bool _active = active && _hasStateAuthority;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (!CheckWeaponAvailable(i))
            {
                continue;
            }

            if (weapons[i].EquipmentSlotType != WeaponEquipmentSlotType.Shoulder)
            {
                weapons[i].SetActiveLaserSign(_active, true);
            }
            else
            {
                weapons[i].SetActiveLaserSign(_weaponReady && _active, true);
            }
        }
    }

    public void OnRespawn()
    {
        if (!_hasStateAuthority)
        {
            return;
        }

        SetAmmo(1000);
        RPC_OnReSpawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnReSpawn()
    {
        ActiveWeapons(IsCombatActive);
        ActiveDrone(IsCombatActive);
        
        if (_hasStateAuthority)
        {
            if (droneController != null)
            {
                droneController.SetState(DroneState.IDLE);
            }
        }
    }

    public void ActiveWeapons(bool isActive)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (!CheckWeaponAvailable(i))
            {
                continue;
            }

            weapons[i].gameObject.SetActive(isActive);

            if (isActive && weapons[i].gameObject.activeInHierarchy)
            {
                weapons[i].SetCanUseWeapon(IsCombatActive);
            }
        }
    }

    public void ActiveDrone(bool isActive)
    {
        if (droneController != null)
        {
            droneController.gameObject.SetActive(isActive);
        }
    }

    public void ActiveArmors(bool active)
    {
        for (int index = 0; index < armors.Count; index++)
        {
            armors[index].gameObject.SetActive(active);
        }
    }

    public void InitWeapon(BasePlayerWeapon weapon)
    {
        if (weapon == null)
        {
            // Debug.LogError($"[InitWeapon] weapon is null.");
            return;
        }

        weapon.Init(_shmacklePlayerController, playerDamage, playerFireRate, 1, targetTags, aimMask);
        weapon.SetAllowPlayAudio(_hasStateAuthority);
        weapon.SetAllowUpdateUI(_hasStateAuthority);
        weapon.SetAllowAnimation(_hasStateAuthority);
        if (weapon.Type == WeaponAttackType.Range)
        {
            weapon.SetActiveLaserSign(IsCombatActive && _hasStateAuthority, true);
        }
    }

    private void AssignInputAction(InputActionReference actionReference, System.Action<bool> callback)
    {
        if (actionReference.action == null) return;

        actionReference.action.performed += ctx => callback(true);
        actionReference.action.canceled += ctx => callback(false);
    }

    private void Update()
    {
        if (!_isSpawned)
        {
            return;
        }

        if (!_isInitialized)
        {
            return;
        }

        if (!IsCombatActive)
        {
            return;
        }

        if (!_shmacklePlayerController.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            return;
        }

        ControlAim();

    }

    private void LateUpdate()
    {
        if (!_isSpawned)
        {
            return;
        }

        if (!_isInitialized)
        {
            return;
        }

        if (!IsCombatActive)
        {
            return;
        }

        Shooting();

        if (!_shmacklePlayerController.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            return;
        }

        UpdateWeaponAim();


    }

    public void ActiveNotificationPanel(bool isRightHand, bool active)
    {
        if (isRightHand)
        {
            if (rightHandPanel)
            {
                rightHandPanel.SetActive(active);
            }
        }
        else
        {
            if (leftHandPanel)
            {
                leftHandPanel.SetActive(active);
            }
        }
    }

    public void UpdateWeaponLaserSign(BasePlayerWeapon weapon, Vector3 startPoint, Vector3 direction)
    {
        if (weapon == null)
        {
            return;
        }

        // Vector3 _aimPoint = Vector3.zero;
        // AimPoint(startPoint, direction, distanceDefault, out _aimPoint);
        // weapon.UpdateLaserSign(_aimPoint);
    }

    public void UpdateWeaponAim()
    {
        if (!IsCombatActive)
        {
            return;
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            var weapon = weapons[i];
            if (weapon == null)
            {
                continue;
            }

            if (weapon.laserSign != null)
            {
                if (weapon.EquipmentSlotType == WeaponEquipmentSlotType.Shoulder)
                {
                    if (_weaponReady)
                    {
                        UpdateWeaponLaserSign(weapons[i], _shmacklePlayerController.HeadCamera.transform.position, _shmacklePlayerController.HeadCamera.transform.forward);
                    }
                }
                else
                {
                    UpdateWeaponLaserSign(weapons[i], weapon.laserSign.transform.position, weapon.laserSign.transform.forward);
                }
            }
        }
    }

    private void AimFromHead()
    {
        if (!_hasStateAuthority)
        {
            return;
        }

        if (!IsCombatActive || !_weaponReady)
        {
            return;
        }

        Vector3 origin = _shmacklePlayerController.HeadCamera.transform.position;
        Vector3 direction = _shmacklePlayerController.HeadCamera.transform.forward;
        float _distance = distanceDefault;
        Vector3 _aimPosition = origin + direction * _distance;
        // Define a maximum distance for the raycast
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _distance, crossHairMask))
        {
            if (hit.collider)
            {
                _aimPosition = hit.point;
            }
        }

        AimPosition = _aimPosition;
        aimTarget.transform.position = AimPosition;
    }

    public void Shooting()
    {

        // if (!HasStateAuthority ||
        //     ShmackleGameManager.Instance.shmackleLocalPlayer.playerHealth.IsDead)
        // {
        //     return;
        // }

        bool _leftShoulderWeaponActive = (leftShoulderWeaponActive) && _weaponReady;
        bool _rightShoulderWeaponActive = (rightShoulderWeaponActive) && _weaponReady;
        bool _leftHandWeaponActive = leftTriggerValue;
        bool _rightHandWeaponActive = rightTriggerValue;

        // if (!isCombatActive)
        // {
        //     bool _leftWeaponTrigger = _leftShoulderWeaponActive || _leftHandWeaponActive;
        //     bool _rightWeaponTrigger = _rightShoulderWeaponActive || _rightHandWeaponActive;
        //     
        //     if (CheckWeaponAvailable(2) && _leftWeaponTrigger)
        //     {
        //         if (!weapons[2].IsCanUseWeapon)
        //         {
        //             ActiveNotificationPanel(false,true);
        //             return;
        //         }
        //     }
        //
        //     if (CheckWeaponAvailable(3) && _rightWeaponTrigger)
        //     {
        //         if (!weapons[3].IsCanUseWeapon)
        //         {
        //             ActiveNotificationPanel(true,true);
        //             return;
        //         }
        //     }
        //     return;
        // }

        //LEFT SHOULDER GUN
        WeaponAttack(0, _leftShoulderWeaponActive);

        //RIGHT SHOULDER GUN 
        WeaponAttack(1, _rightShoulderWeaponActive);

        //LEFT HAND GUN
        WeaponAttack(2, _leftHandWeaponActive);

        //RIGHT HAND GUN
        WeaponAttack(3, _rightHandWeaponActive);
    }

    public bool TryEquipWeapon(BasePlayerWeapon weapon, GearEquipmentSlot gearSlot, bool canPlaySound = true)
    {
        bool _isPropHunt = PropHuntGameManager.Instance;
        if (_isPropHunt)
        {
            if (weapon != null)
            {
                weapon.gameObject.SetActive(false);
            }
            return false;
        }
        
        var weaponIndex = (int)gearSlot;
        if (weaponIndex > weapons.Length)
        {
            // Debug.LogError($"[TryEquipWeapon] weaponIndex {weaponIndex} gearSlot {gearSlot} is out of range weapons.Length {weapons.Length}");
            return false;
        }

        switch (gearSlot)
        {
            case GearEquipmentSlot.LeftShoulder: // left shoulder weapon
            case GearEquipmentSlot.RightShoulder: // right shoulder weapon
                {
                    if (weapon.EquipmentSlotType != WeaponEquipmentSlotType.Shoulder)
                    {
                        // Debug.LogError($"weapon.EquipmentSlotType {weapon.EquipmentSlotType} is not WeaponEquipmentSlotType.Shoulder");
                        return false;
                    }
                    break;
                }
            case GearEquipmentSlot.LeftHand:      // left hand weapon
            case GearEquipmentSlot.RightHand: // right hand weapon
                {
                    if (weapon.EquipmentSlotType != WeaponEquipmentSlotType.Hand)
                    {
                        // Debug.LogError($"weapon.EquipmentSlotType {weapon.EquipmentSlotType} is not WeaponEquipmentSlotType.Hand");
                        return false;
                    }
                    break;
                }
            default: throw new NotImplementedException(gearSlot.ToString());
        }

        weapons[weaponIndex] = weapon;
        InitWeapon(weapons[weaponIndex]);
        weapons[weaponIndex].SetCanUseWeapon(IsCombatActive);
        
        if (IsCombatActive)
        {
            if (weapon.Type == WeaponAttackType.Range && weapon.EquipmentSlotType == WeaponEquipmentSlotType.Shoulder)
            {
                weapons[weaponIndex].SetActiveWeaponReady(_weaponReady);
                weapons[weaponIndex].SetWeaponOpenFire(_weaponReady);
            }
            else
            {
                weapons[weaponIndex].SetActiveLaserSign(true && _hasStateAuthority);
            }
        }

        if (_hasStateAuthority && canPlaySound)
        {
            equipSource.Play();
        }
        return true;
    }

    public bool TryEquipDrone(BasePlayerDroneController droneControllerPrefab, bool canPlaySound = true)
    {
        bool _isPropHunt = PropHuntGameManager.Instance;
        if (_isPropHunt)
        {
            if (droneControllerPrefab != null)
            {
                droneControllerPrefab.gameObject.SetActive(false);
            }

            return false;
        }
        
        if (droneController != null)
        {
            TryUnEquipDrone(droneController);
            droneController = null;
        }

        if (ShmackleGameManager.Instance == null)
        {
            // Debug.LogError($"[TryUnEquipDrone] ShmackleGameManager.Instance is null.");
            return false;
        }

        if (ShmackleGameManager.Instance._runner == null)
        {
            // Debug.LogError($"[TryUnEquipDrone] ShmackleGameManager.Instance._runner is null.");
            return false;
        }

        try
        {
            if (!IsCombatActive)
            {
                return true;
            }

            if (SceneManager.GetActiveScene().name == "Shore")
            {
                return true;
            }
            
            droneController = ShmackleGameManager.Instance._runner.Spawn(droneControllerPrefab);
            StartDrone();

            if (IsCombatActive)
            {
                droneController.SetDroneActive(true);
            }
            
            droneController.ShowHideDrone(IsCombatActive);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }

        if (_hasStateAuthority && canPlaySound)
        {
            equipSource.Play();
        }
        return true;
    }

    public bool TryUnEquipWeapon(GearEquipmentSlot gearSlot, out BasePlayerWeapon unequipedWeapon)
    {
        unequipedWeapon = null;

        var weaponIndex = (int)gearSlot;
        if (weaponIndex > weapons.Length)
        {
            // Debug.LogError($"[TryUnEquipWeapon] weaponIndex {weaponIndex} gearSlot {gearSlot} is out of range weapons.Length {weapons.Length}");
            return false;
        }

        var weapon = weapons[weaponIndex];
        if (weapon == null)
        {
            return false;
        }

        weapons[weaponIndex] = null;
        unequipedWeapon = weapon;

        weapon.SetActiveLaserSign(false);
        weapon.SetCanUseWeapon(false);
        weapon.SetActiveWeaponReady(false);
        if (_hasStateAuthority)
        {
            unequipSource.Play();
        }
        return true;
    }

    public bool TryUnEquipDrone()
    {
        return TryUnEquipDrone(droneController);
    }

    public bool TryUnEquipDrone(BasePlayerDroneController droneController)
    {
        if (droneController == null)
        {
            // Debug.LogError($"[TryUnEquipDrone] droneController is null.");
            return false;
        }

        if (ShmackleGameManager.Instance == null)
        {
            // Debug.LogError($"[TryUnEquipDrone] ShmackleGameManager.Instance is null.");
            return false;
        }

        if (ShmackleGameManager.Instance._runner == null)
        {
            // Debug.LogError($"[TryUnEquipDrone] ShmackleGameManager.Instance._runner is null.");
            return false;
        }

        try
        {
            ShmackleGameManager.Instance._runner.Despawn(droneController.Object);
            if (_hasStateAuthority)
            {
                unequipSource.Play();
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    void WeaponAttack(int weaponIndex, bool active)
    {
        if (!CheckWeaponAvailable(weaponIndex))
        {
            return;
        }

        if (!active && weapons[weaponIndex].CurrentIsWeaponTrigger)
        {
            weapons[weaponIndex].StopAttack();
        }

        if (!active && weapons[weaponIndex].CurrentWeaponPhase == WeaponPhaseType.IDLE)
        {
            return;
        }

        if (weapons[weaponIndex].CurrentWeaponPhase == WeaponPhaseType.LOCK)
        {
            return;
        }

        if (!weapons[weaponIndex].IsCanUseWeapon)
        {
            return;
        }

        if (!weapons[weaponIndex].IsAttacking)
        {
            if (weapons[weaponIndex].Type == WeaponAttackType.Range)
            {
                if (ammos <= 0 && isConsumeAmmo && active)
                {
                    WeaponOutOfAmmo(weaponIndex);
                    WeaponExitTrigger(weaponIndex);
                    return;
                }
                
                Vector3 origin = weapons[weaponIndex].GetMuzzlePoint();
                // Calculate the direction from the origin to the aim target and normalize it
                Vector3 direction = weapons[weaponIndex].transform.forward;

                bool _isOutOfAmmo = ammos <= weapons[weaponIndex].ConsumeAmountAmmo && isConsumeAmmo;

                if (active == weapons[weaponIndex].CurrentIsWeaponTrigger)
                {
                    if (_isOutOfAmmo)
                    {
                        WeaponOutOfAmmo(weaponIndex);
                        WeaponExitTrigger(weaponIndex);
                        return;
                    }
                }

                WeaponTrigger(weaponIndex, origin, direction, active);

                if (isConsumeAmmo && weapons[weaponIndex].IsAttacking)
                {
                    ChangeAmmo(-weapons[weaponIndex].ConsumeAmountAmmo);
                }
                // switch (weapons[weaponIndex].CurrentWeaponPhase)
                // {
                //     case WeaponPhaseType.SINGLE:
                //     {
                //         if (weapons[weaponIndex].IsAllowCharge)
                //         {
                //             weapons[weaponIndex].ChangeWeaponPhase(WeaponPhaseType.CHARGE);
                //         }
                //         else
                //         {
                //             weapons[weaponIndex].PlayerShootSound();
                //             RPC_Shooting(weaponIndex, origin, direction);
                //             ChangeAmmo(-1);
                //             if (IsWeaponCoolDown(weaponIndex))
                //             {
                //                 weapons[weaponIndex].PlayCoolDownSound();
                //             }
                //         }
                //         break;
                //     }
                //     case WeaponPhaseType.CHARGE:
                //     {
                //         if (active)
                //         {
                //             weapons[weaponIndex].ChanceChargeProcess(Time.deltaTime);
                //             float _percent = weapons[weaponIndex].Charge_CurrentChargeProcess / weapons[weaponIndex].Charge_ChargeTime;
                //             switch (weapons[weaponIndex].CurrentChargePhase)
                //             {
                //                 case ChargePhaseType.CHARGE_START:
                //                 {
                //                     if (_percent >= 0.25f)
                //                     {
                //                         if (ammos < chargeConsumeAmmo && isConsumeAmmo)
                //                         {
                //                             WeaponOutOfAmmo(weaponIndex);
                //                             return;
                //                         }
                //                         weapons[weaponIndex].PlayChargeSound();
                //                         RPC_ChangeChargePhase(weaponIndex, ChargePhaseType.CHARGING);
                //                     }
                //                     break;
                //                 }
                //                 case ChargePhaseType.CHARGING:
                //                 {
                //                     if (ammos < chargeConsumeAmmo && isConsumeAmmo)
                //                     {
                //                         WeaponOutOfAmmo(weaponIndex);
                //                         Debug.Log("Out of charging");
                //                         return;
                //                     }
                //                     
                //                     if (_percent >= 1f)
                //                     {
                //                         RPC_ChangeChargePhase(weaponIndex, ChargePhaseType.CHARGE_COMPLETE);
                //                     }
                //                     break;
                //                 }
                //                 case ChargePhaseType.CHARGE_COMPLETE:
                //                 {
                //                     if (ammos < chargeConsumeAmmo && isConsumeAmmo)
                //                     {
                //                         WeaponOutOfAmmo(weaponIndex);
                //                         Debug.Log("Out of complete");
                //                     }
                //                     break;
                //                 }
                //             }
                //             //RPC_Chasing(weaponIndex);
                //         }
                //         else
                //         {
                //             if (weapons[weaponIndex].Charge_CurrentChargeProcess >= weapons[weaponIndex].Charge_ChargeTime)
                //             {
                //                 if (ammos < chargeConsumeAmmo && isConsumeAmmo)
                //                 {
                //                     weapons[weaponIndex].PlayLockChargeSound();
                //                     RPC_ChangeChargePhase(weaponIndex, ChargePhaseType.CHARGE_START);
                //                     Debug.Log("Out of charge attack");
                //                     return;
                //                 }
                //                 weapons[weaponIndex].PlayChargeAttackSound();
                //                 RPC_ChargeShooting(weaponIndex, origin, direction);
                //                 ChangeAmmo(-chargeConsumeAmmo);
                //                 if (IsWeaponCoolDown(weaponIndex))
                //                 {
                //                     weapons[weaponIndex].PlayCoolDownSound();
                //                 }
                //                 return;
                //             }
                //             
                //             weapons[weaponIndex].PlayerShootSound();
                //             RPC_Shooting(weaponIndex, origin, direction);
                //             ChangeAmmo(-1);
                //             if (IsWeaponCoolDown(weaponIndex))
                //             {
                //                 weapons[weaponIndex].PlayCoolDownSound();
                //             }
                //         }
                //         break;
                //     }
                //     case WeaponPhaseType.OVERHEAT:
                //     {
                //         break;
                //     }
                // }
            }
        }
    }

    public void UpdateWeaponAmmo()
    {
        for (int index = 0; index < weapons.Length; index++)
        {
            if (!CheckWeaponAvailable(index))
            {
                continue;
            }
            weapons[index].UpdateAmmo(ammos);
        }
    }

    public void SetAmmo(int ammo)
    {
        ammos = ammo;
        UpdateWeaponAmmo();
    }
    public void ChangeAmmo(int changeValue)
    {
        ammos = Mathf.Max(0, ammos + changeValue);
        UpdateWeaponAmmo();
    }

    public virtual void WeaponOutOfAmmo(int weaponIndex)
    {
        weapons[weaponIndex].ChangeWeaponPhase(WeaponPhaseType.IDLE);
        weapons[weaponIndex].PlayLockChargeSound();
    }

    void WeaponTrigger(int weaponIndex, Vector3 shootPoint, Vector3 direction, bool active)
    {
        if (!CheckWeaponAvailable(weaponIndex) || CantShootInShootingRange())
        {
            return;
        }
        weapons[weaponIndex].HandleWeaponTrigger(direction, shootPoint, active);
    }

    void WeaponExitTrigger(int weaponIndex)
    {
        if (!CheckWeaponAvailable(weaponIndex) || CantShootInShootingRange())
        {
            return;
        }
        weapons[weaponIndex].HandleWeaponExitTrigger();
    }

    private void ControlAim()
    {
        if (!_hasStateAuthority)
        {
            return;
        }

        if (!IsCombatActive)
        {
            return;
        }

        if (leftThumbTriggerValue)
        {
            if (_previousReadyPress == leftThumbTriggerValue ||
                _shmacklePlayerController.playerHealth.CurrentHealth <= 0)
            {
                return;
            }

            _weaponReady = !_weaponReady;
            RPC_OnReadyWeapon(_weaponReady);

            if (CheckWeaponAvailable(0) || CheckWeaponAvailable(1))
            {
                SetActiveCrossHair(_weaponReady);
            }

            // for (int i = 0; i < weapons.Length; i++)
            // {
            //     if (CheckWeaponAvailable(i) && weapons[i].EquipmentSlotType == WeaponEquipmentSlotType.Shoulder)
            //     {
            //         weapons[i].SetActiveLaserSign(_weaponReady);
            //     }
            // }
        }

        _previousReadyPress = leftThumbTriggerValue;

        //HandAim(leftHand.position, leftHand.up, distanceDefault, out _leftAimPosition);
        //HandAim(rightHand.position, rightHand.up, distanceDefault, out _rightAimPosition);
        //leftCrosshair.transform.position = _leftAimPosition;
        //rightCrosshair.transform.position = _rightAimPosition;
    }

    private void SetActiveCrossHair(bool active)
    {
        crossHair.SetActive(active);
    }

    private void AimPoint(Vector3 startPoint, Vector3 direction, float distance, out Vector3 aimPoint)
    {
        Vector3 _aimPos = startPoint + direction * distance;


        if (Physics.RaycastNonAlloc(startPoint, direction, _raycastHits, distance, crossHairMask) > 0)
        // if (Physics.Raycast(startPoint, direction, out RaycastHit hit, distance, crossHairMask))
        {
            _aimPos = _raycastHits[0].point;
        }

        aimPoint = _aimPos;
    }

    public void StartDrone()
    {
        if (droneController == null)
        {
            return;
        }

        droneController.Init(playerDamage, _shmacklePlayerController, 1, targetTags, aimMask);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnEquipWeapon(int weaponIndex, string weaponID)
    {
        GameObject _weapon = new GameObject(); // CAI NAY LA DEMO, DOI THANH GAMEOBJECT CUA SUNG
        switch (weaponIndex)
        {
            case 0: // left shoulder weapon
                {
                    _weapon.transform.SetParent(leftShoulderWeaponPos);
                    break;
                }
            case 1: // right shoulder weapon
                {
                    _weapon.transform.SetParent(rightShoulderWeaponPos);
                    break;
                }
            case 2: // left hand weapon
                {
                    _weapon.transform.SetParent(leftHandPos);
                    break;
                }
            case 3: // right hand weapon
                {
                    _weapon.transform.SetParent(rightHandPos);
                    break;
                }
        }

        _weapon.transform.localPosition = Vector3.zero;
        _weapon.transform.localRotation = Quaternion.identity;
        _weapon.transform.localScale = Vector3.one;
        weapons[weaponIndex] = _weapon.GetComponent<BasePlayerWeapon>();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnUnEquipWeapon(int weaponIndex)
    {
        if (weapons[weaponIndex] != null)
        {
            Destroy(weapons[weaponIndex]);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnReadyWeapon(bool weaponReady)
    {
        _weaponReady = weaponReady;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                continue;
            }
            weapons[i].SetActiveWeaponReady(weaponReady);

        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnCombatActive(bool active)
    {
        if (active)
        {
            gameObject.SetActive(true);
            IsCombatActive = true;
            _weaponReady = false;
            ShowHideCombatComponent();
            return;
        }
        IsCombatActive = false;
        _weaponReady = false;
        ShowHideCombatComponent();
        gameObject.SetActive(false);
    }

    [Rpc(RpcSources.Proxies, RpcTargets.StateAuthority)]
    public void RPC_RequestSync(RpcInfo info = default)
    {
        NetworkObject drone = null;

        if (droneController != null)
        {
            drone = droneController.GetComponent<NetworkObject>();
        }
        RPC_OnRequestSyncData(_weaponReady, drone, info.Source);
    }

    // 2) StateAuthority sends its position back to all proxies
    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    public void RPC_OnRequestSyncData(bool isReady, NetworkObject drone, PlayerRef target, RpcInfo info = default)
    {
        // Only apply on the client that actually requested it
        if (Runner.LocalPlayer != target)
            return;
        if (!IsCombatActive)
        {
            gameObject.SetActive(false);
        }
        _weaponReady = isReady;
        if (drone != null)
        {
            droneController = drone.GetComponent<BasePlayerDroneController>();
            StartDrone();
        }
    }

    private void ShowHideCombatComponent()
    {
        for (int index = 0; index < armors.Count; index++)
        {
            armors[index].SetActive(IsCombatActive);
        }

        if (CheckWeaponAvailable(0))
        {
            weapons[0].SetActiveWeaponReady(_weaponReady);
        }

        if (CheckWeaponAvailable(1))
        {
            weapons[1].SetActiveWeaponReady(_weaponReady);
        }

        for (int index = 0; index < weapons.Length; index++)
        {
            if (CheckWeaponAvailable(index))
            {
                weapons[index].SetCanUseWeapon(IsCombatActive);

                if (IsCombatActive)
                {
                    var weapon = weapons[index];
                    InitWeapon(weapon);
                }
            }
        }
        
        if (droneController != null)
        {
            ActiveDrone(IsCombatActive);
        }
    }
    
    public void SetActiveShootingRangeMode(bool active)
    {
        IsShootingMode = active;
    }

    private bool CantShootInShootingRange()
    {
        return !_hasStateAuthority && !_isCombatScene;
    }

    public void SetActiveTriggerHandWeaponAble(bool isRightHand, bool active)
    {
        if (isRightHand)
        {
            IsCanTriggerRightHandWeaponAble = active;
            return;
        }
        IsCanTriggerLeftHandWeaponAble = active;
    }

    public void SetActiveTriggerShoulderWeaponAble(bool isRightShoulder, bool active)
    {
        if (isRightShoulder)
        {
            IsCanTriggerRightShoulderWeaponAble = active;
            return;
        }
        IsCanTriggerLeftShoulderWeaponAble = active;
    }

    public void SetActiveTriggerBothHandWeaponAble(bool active)
    {
        IsCanTriggerRightHandWeaponAble = active;
        IsCanTriggerLeftHandWeaponAble = active;
    }

    public void SetActiveTriggerBothShoulderWeaponAble(bool active)
    {
        IsCanTriggerRightShoulderWeaponAble = active;
        IsCanTriggerLeftShoulderWeaponAble = active;
    }

    public GearEquipmentSlot CheckWeaponSlotType(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                {
                    return GearEquipmentSlot.LeftHand;
                }
            case 1:
                {
                    return GearEquipmentSlot.LeftShoulder;
                }
            case 2:
                {
                    return GearEquipmentSlot.LeftHand;
                }
            case 3:
                {
                    return GearEquipmentSlot.RightHand;
                }
            default:
                {
                    return GearEquipmentSlot.Drone;
                }
        }
    }
}
