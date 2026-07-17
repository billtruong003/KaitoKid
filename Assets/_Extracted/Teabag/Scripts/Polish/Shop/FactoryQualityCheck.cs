using GorillaLocomotion;
using Teabag.Player.Rig;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Teabag.Player.Cosmetics;
using Teabag.Player;

public class FactoryQualityCheck : MonoBehaviour
{
    public Laser laser;
    public TextMeshPro textMeshPro;
    public List<string> str = new List<string>();

    void Update()
    {
        textMeshPro.text = "CHECK:\n";

        if (laser.target == null)
            return;

        if (laser.target.GetComponentInParent<PurchaseStand>() != null)
        {
            textMeshPro.text += str[(int)laser.target.GetComponentInParent<PurchaseStand>().button.cosmetic.rarity];
        }
        else if (laser.target.GetComponentInParent<Player>())
        {
            textMeshPro.text += "<color=grey>MONKE IN MACHINE!";
        }
        else if (laser.target.GetComponent<FactoryQualityOverride>())
        {
            textMeshPro.text += laser.target.GetComponent<FactoryQualityOverride>().overwrite;
        }
        else if (laser.target.GetComponentInParent<Train>() != null)
        {
            textMeshPro.text += "<color=green>PASS";
        }
    }
}
