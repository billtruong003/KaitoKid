using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Teabag.UI;

//[ExecuteAlways]
public class KeyboardKey : GorillaButton
{
    //public string c;
    public KeyCode key;
    //TextMeshPro tmp;

    public Computer computer
    {
        get
        {
            return GetComponentInParent<Computer>();
        }
    }

    private void Update()
    {
        /*
        if (tmp == null)
            tmp = GetComponentInChildren<TextMeshPro>();
        */
        
        //if (!string.IsNullOrEmpty(c))
        //    key = (KeyCode)Enum.Parse(typeof(KeyCode), c);

        //tmp.text = key.ToString().Last().ToString();
    }

    public override void OnPress()
    {
        computer.ButtonPress(key);
    }
}
