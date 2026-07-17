using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformFloating : MonoBehaviour
{

    public float floatSpeed = 1f;

    void Start()
    {
        transform.position = new Vector3(0, 12.6f); // 12.6f is 
    }

    void Update()
    {
        transform.Translate(Vector3.down * floatSpeed * Time.deltaTime);
        if (transform.localPosition.y <= -4)
        {
            transform.localPosition = new Vector3(0, 12.6f);
        }
    }
}
