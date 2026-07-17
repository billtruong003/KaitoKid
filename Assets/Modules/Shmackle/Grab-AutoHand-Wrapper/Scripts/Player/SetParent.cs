using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetParent : MonoBehaviour
{
    public Transform parent;
    public float delay;

    private void OnEnable()
    {
        Invoke(nameof(setParent), delay);
    }

    void setParent()
    {
        transform.parent = parent;
    }
}
