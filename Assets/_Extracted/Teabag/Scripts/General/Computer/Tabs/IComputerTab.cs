using System;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;

[Serializable]
public abstract class IComputerTab : MonoBehaviour
{

    protected IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    [Tooltip("Max 5 characters")]
    public string TabName;

    public bool IsOpen;

    public Computer computer
    {
        get
        {
            return GetComponentInParent<Computer>();
        }
    }

    public void Open()
    {
        Debug.Log("Opened " + TabName);
        IsOpen = true;
        OnOpen();
    }

    public abstract void OnOpen();

    public void Close()
    {
        IsOpen = false;
        //Debug.Log("Closed " + TabName);
        OnClose();
    }

    public abstract void OnClose();

    public abstract void Press(KeyCode key);

    public static bool IsNumKey(KeyCode key)
    {
        string str = key.ToString();
        return str.StartsWith("Keypad");
    }

    public static int KeyToNum(KeyCode key)
    {
        string str = key.ToString();
        if (str.StartsWith("Keypad"))
        {
            str = str.Replace("Keypad", "");
            int num = int.Parse(str);
            return num;
        }

        return 0;
    }
}
