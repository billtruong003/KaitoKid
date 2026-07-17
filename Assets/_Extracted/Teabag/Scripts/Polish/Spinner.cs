using System;
using Teabag.Authentication;
using Teabag.Core;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    public bool x = true;
    public bool y = true;
    public bool z = true;
    public bool local = true;
    public float speed = 250;
    public bool networked = false;

    void Update()
    {
        if (!networked)
            Rotate();
        else
            RotateSynced();
    }

    public void Rotate()
    {
        float xF = x ? speed * Time.deltaTime : 0;
        float yF = y ? speed * Time.deltaTime : 0;
        float zF = z ? speed * Time.deltaTime : 0;
        transform.localRotation = Quaternion.Euler(xF, yF, zF) * transform.localRotation;
        if (transform.localEulerAngles.x > 360)
            transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x - 360, transform.localEulerAngles.y, transform.localEulerAngles.z);
        if (transform.localEulerAngles.y > 360)
            transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y - 360, transform.localEulerAngles.z);
        if (transform.localEulerAngles.z > 360)
            transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z - 360);

        if (!local)
            throw new NotImplementedException();
    }

    public void RotateSynced()
    {
        DateTime time = AuthenticationUtils.serverTime;
        float s = time.Second + time.Millisecond / 1000f;
        float t = time.Minute * 60 + s;
        float xF = x ? speed * t : transform.localEulerAngles.x;
        float yF = y ? speed * t : transform.localEulerAngles.y;
        float zF = z ? speed * t : transform.localEulerAngles.z;
        transform.localRotation = Quaternion.Euler(xF, yF, zF);

        if (!local)
            throw new NotImplementedException();
    }
}
