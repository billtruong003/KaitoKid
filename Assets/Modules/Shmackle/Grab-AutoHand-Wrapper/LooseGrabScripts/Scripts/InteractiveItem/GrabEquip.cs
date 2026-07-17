using System;
using System.Collections.Generic;
using Autohand;
using Shmackle.Data;
using Shmackle.Runtime;
using UnityEngine;
using Utils.Bill.InspectorCustom;

public class GrabEquip : MonoBehaviour
{
    [CustomHeader("Grabbable Settings", "#FFD700", "Settings for the Grabbable component")]
    [SerializeField] private Grabbable grabbable;

    [CustomHeader("Equipment Settings", "#FFD700", "Settings for gear equipment slot")]
    [SerializeField] private GearEquipmentSlot gearSlot;

    [CustomHeader("Weapon Configuration", "#FFD700", "Weapon code and ID settings")]
    [SerializeField] private string weaponCode = "WEAPON_HandGun";

    [SerializeField, SerializeGUID("#E0E0E0", true)] // Read-only GUID
    private string id;

    [SerializeField, ReadOnly("#E0E0E0")] // Read-only layer mask
    private LayerMask triggerLayerMask;

    [CustomButton("Generate GUID", "Generate a new GUID for this item")]
    public void GenGUID() => id = Guid.NewGuid().ToString();

    private GearManager gearManager;
    private GearItem gearItem;
    private bool isLocal;

    private void Awake()
    {
        gearItem = new GearItem();
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }
        collider.isTrigger = true;
        grabbable.onGrab.AddListener(OnGrab);
    }

    private GearItem FindIDHandGun()
    {
        List<GearItem> gearItems = RuntimeUserData.CacheUser.inventory.gearItems;
        foreach (var item in gearItems)
        {
            if (item.itemId == weaponCode)
                return item;
        }
        return null;
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.onGrab.RemoveListener(OnGrab);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & triggerLayerMask.value) != 0)
        {
            isLocal = ShmackleGameManager.Instance?.playerNetworkRig?.IsLocalNetworkRig ?? false;
            if (!isLocal)
            {
                return;
            }

            EquipItemOnGrab(isLocal);
            if (isLocal)
            {
                grabbable.gameObject.SetActive(false);
            }
        }
    }

    public void OnGrab(Hand hand, Grabbable grabbed)
    {
        isLocal = ShmackleGameManager.Instance?.playerNetworkRig?.IsLocalNetworkRig ?? false;
        if (!isLocal)
        {
            return;
        }

        EquipItemOnGrab(isLocal);
        if (isLocal)
        {
            grabbable.gameObject.SetActive(false);
        }
    }

    private void EquipItemOnGrab(bool isLocalNetworkRig)
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }

        gearItem = FindIDHandGun();
        ShmackleGameManager.Instance.playerNetworkRig.RPC_EquipGear(gearSlot, gearItem.itemId, gearItem.instanceId);
    }
    //Mark
    //RuntimeUserData.CacheUser.Achievements.Contains(PlayFabKeys.Achievements.TutorialGun);

    //Done
    //PlayFabClientAPIExtensions.AddAchievement(PlayFabKeys.Achievements.TutorialGun);
}