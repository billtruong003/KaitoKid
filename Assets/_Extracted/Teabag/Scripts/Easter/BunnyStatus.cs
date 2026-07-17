using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using System.Collections;
using System.Collections.Generic;
using Teabag.Core;
using TMPro;
using UnityEngine;

public class BunnyStatus : MonoBehaviour
{
    TMP_Text text;
    public Bunny bunny;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (bunny.grabber != null)
        {
            text.enabled = bunny.grabber.isMine;
            if (bunny.grabber.isMine)
            {
                transform.position = bunny.transform.position + new Vector3(0, 0.173f, 0);
                var rig = LocalHardwareRig;
                if (rig != null)
                    transform.rotation = Quaternion.LookRotation(transform.position, rig.Headset.Position);
            }
        }
        else
            text.enabled = false;
    }
}
