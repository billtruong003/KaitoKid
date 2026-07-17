using Shmackle.Data;
using UnityEngine;
using Autohand;
public class DripEquip : MonoBehaviour
{
    public DripData_Runtime dripDataRuntime;
    DripManager dripManager;
    
    private bool isBuy;
    
    private void OnTriggerEnter(Collider other)
    {
        var hand = other.GetComponent<Hand>();
        if (hand)
        {
            Debug.Log("Hand touch drip button");
            dripManager = hand.GetComponentInParent<DripManager>();
            onBuy();
        }
    }

    public void onBuy()
    {
        if (dripManager && isBuy == false)
        {
            isBuy = true;
            Invoke(nameof(allowBuyAgain) , 1);

            //setupJ
            EquipDrip(this.dripDataRuntime);
        }
    }

    private void EquipDrip(DripData_Runtime dripDataRuntime)
    {
        //dripManager.EquipDrip(dripDataRuntime, isJ, isT);
    }


    void allowBuyAgain()
    {
        isBuy = false;
    }
}
