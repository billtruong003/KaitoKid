using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Shmackle.Data;
using Shmackle.Data.ScriptableObjects;
using Shmackle.Runtime;
using Shmackle.Runtime.UI.Phone;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Shmackle_Drip_Shop_Area : MonoBehaviour
{
    [Header("SETTINGS")] 
    public string       dripId = "";
    public Transform    dripHolder;
    public GameObject   UICanvas    = null;
    public GameObject   topPanel    = null;
    public GameObject   bottomPanel = null;
    
    [HorizontalLine]
    
    [Header("REFERENCES - TEXTS")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI purchasedText;

    [HorizontalLine]

    [Header("DEBUG")] 
    public float radius = .5f;
    public bool showDebug = true;
    
    [SerializeField, ReadOnly, Expandable, AllowNesting]
    private DripDataContainer dripDataContainer;

    [SerializeField, ReadOnly, AllowNesting]
    private DripData dripData = null;

    private void OnValidate()
    {
        dripDataContainer = Resources.Load<DripDataContainer>("DripDataContainer");
    }

    private void OnDrawGizmos()
    {
        if (dripHolder != null)
            Gizmos.DrawSphere(dripHolder.position, showDebug ? radius : 0);
    }

    private void Start()
    {
        var dripData = dripDataContainer.Find(dripId);
        if (dripData != null) SetDrip(dripData);
    }

    public void SetDrip(DripData dripData)
    {
        this.dripData = dripData;

        this.gameObject.name = $"Drip_Shop_Area_{dripData.name}";
        
        if (nameText)
            nameText.text = this.dripData.name;
        if (descriptionText)
            descriptionText.text = this.dripData.description;
        if (priceText)
            priceText.text = this.dripData.price.FormatPrice();
        
        UniTask.Void(async () =>
        {
            await UniTask.WaitUntil(() => RuntimeUserData.CacheUser != null, cancellationToken: this.destroyCancellationToken);
            var purchased = RuntimeUserData.CacheUser.inventory.items.Any(i => i == dripData.id);
            purchasedText.text = purchased ? "Purchased" : "Grab your phone to buy";
        });
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerBody"))
        {
            ShmackleNetworkRig shmackleNetworkRig = other.gameObject.GetComponentInParent<ShmackleNetworkRig>();
            if (shmackleNetworkRig.IsLocalNetworkRig)
            {
                ShowUICanvas();
            }
        }
        
        // if (RuntimeUserData.CacheUser == null)
        //     return;
        //
        // if (RuntimeUserData.CacheUser.inventory.items.Contains(this.dripData.id))
        //     return;
        //
        // var mobileDeviceController = other.GetComponent<PlayerMobileDeviceController>();
        // if (mobileDeviceController)
        // {
        //     mobileDeviceController.playerMobile.NotiPlayerBuyDrip();
        //     //mobileDeviceController.playerMobile.ForceOpenMoible();
        //
        //     if (this.dripData == null)
        //         return;
        //     
        //     var phoneUIDripPurchase = mobileDeviceController.playerMobile.purchaseCanvas.GetComponent<Phone_UI_DripPurchase>();
        //     phoneUIDripPurchase.Initialize(this.dripData);
        // }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerBody"))
        {
            ShmackleNetworkRig shmackleNetworkRig = other.gameObject.GetComponentInParent<ShmackleNetworkRig>();
            if (shmackleNetworkRig.IsLocalNetworkRig)
            {
                HideUICanvas();
            }
        }
    }

    private void ShowUICanvas()
    {
        UICanvas?.SetActive(true);
    }

    private void HideUICanvas()
    {
        UICanvas?.SetActive(false);
    }
}
