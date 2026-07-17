using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

public class Popup : MonoBehaviour
{
    public TextMeshPro textMeshPro;
    float s = 0.15f;
    float t = 0;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public void Initialise(string text, Color colour, Vector3 position, float size)
    {
        StopAllCoroutines();
        t = 0;

        s = 0.15f * size;
        transform.localScale = Vector3.one * s;
        transform.position = position;
        textMeshPro.color = colour;
        textMeshPro.text = text;
        transform.rotation = TargetRotation();
        StartCoroutine(IECoroutine());
    }

    private void Update()
    {
        if (t < Mathf.PI)
            t += Time.deltaTime * 6;
        else
            t = Mathf.PI;

        transform.localScale = Vector3.one * (s + (Mathf.Sin(t) / 20));
        transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation(), 10 * Time.deltaTime);
    }

    Quaternion TargetRotation()
    {
        var rig = LocalHardwareRig;
        if (rig == null) return Quaternion.identity;
        Vector3 direction = rig.Headset.Position - transform.position;
        Quaternion quaternion = new Quaternion(0, Quaternion.LookRotation(direction).y, 0, Quaternion.LookRotation(direction).w);
        return quaternion;
    }

    private IEnumerator IECoroutine()
    {
        while (textMeshPro.color.a > 0)
        {
            transform.position += Vector3.up * Time.deltaTime;
            textMeshPro.color = new Color(textMeshPro.color.r, textMeshPro.color.g, textMeshPro.color.b, textMeshPro.color.a - Time.deltaTime * 0.15f);
            yield return null;
        }

        textMeshPro.color = new Color(textMeshPro.color.r, textMeshPro.color.g, textMeshPro.color.b, 1f); // Reset alpha

        var poolObject = GetComponent<PoolObject>();
        if (poolObject != null)
        {
            poolObject.Return();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
