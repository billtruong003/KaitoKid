using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NullParent : MonoBehaviour
{
    public float delay = 1;
    private void OnEnable()
    {
        Invoke(nameof(nullParent) , delay);
    }

    void nullParent()
    {
        transform.parent = null;
    }
}
