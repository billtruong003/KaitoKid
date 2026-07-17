using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tweening : MonoBehaviour
{
    public void MakeBigger(Image image)
    {
        LeanTween.scale(image.gameObject, new Vector3(1.1f, 1.1f, 1f), 0.1f);
    }

    public void MakeSmaller(Image image)
    {
        LeanTween.scale(image.gameObject, new Vector3(1f, 1f, 1f), 0.1f);
    }

    public void TitlePulsate(Image title)
    {
        LeanTween.scale(title.gameObject, new Vector3(1.1f, 1.1f, 1f), 0.5f).setLoopPingPong();
    }

    public void PopUp(GameObject obj, float time)
    {
        obj.transform.localScale = new Vector3(0, 0, 0);
        LeanTween.scale(obj, new Vector3(1f, 1f, 1f), time);
    }
}
