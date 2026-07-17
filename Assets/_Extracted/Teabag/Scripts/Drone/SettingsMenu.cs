using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.UI;
using Teabag.Core;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu instance;
    bool wasPressing;
    public GameObject drone;
    public ScreenTab tabs;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public static void EnableDrone()
    {
        instance.drone.SetActive(true);
        Transform t = instance.drone.transform;
        var rig = instance.LocalHardwareRig;
        if (rig == null) return;
        Vector3 forward = rig.Headset.ForwardVector;
        forward.y = 0;
        t.position = rig.Headset.Position + forward;
        t.forward = rig.Headset.Position - t.position;
    }

    public static void DisableDrone()
    {
        instance.drone.SetActive(false);
    }

    private void Awake()
    {
        instance = this;
        DisableDrone();
        //DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (VRInputHandler.GetInputDown(true, InputType.Menu)
            #if UNITY_EDITOR
                || VRInputHandler.GetInputDown(true, InputType.Primary)
            #endif
            )
        {
            if (!wasPressing)
            {
                if (!drone.activeSelf)
                    EnableDrone();
                else
                    DisableDrone();

                wasPressing = true;
            }
        }
        else
            wasPressing = false;
        /*
        if (VRInputHandler.GetInputDown(true, InputType.Menu))
        {
            if (!drone.activeSelf && !wasPressing)
                EnableDrone();
            wasPressing = true;
        }
        else if (wasPressing)
        {
            if (!VRInputHandler.GetInputDown(true, InputType.Menu))
            {
                if (drone.activeSelf)
                    DisableDrone();
                wasPressing = false;
            }
        }
        */
    }

    /*
    public int Pressed()
    {
        int i = 0;
        if (VRInputHandler.GetInputDown(true, InputType.Primary))
            i++;
        if (VRInputHandler.GetInputDown(true, InputType.Secondary))
            i++;
        if (VRInputHandler.GetInputDown(false, InputType.Primary))
            i++;
        if (VRInputHandler.GetInputDown(false, InputType.Secondary))
            i++;
        return i;
    }
    */
}
