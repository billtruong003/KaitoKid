using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teabag.Core
{
public class Mark : MonoBehaviour
{
    public string mark;
    public bool findable
    {
        get
        {
            if (transform.parent != null)
                return transform.parent.gameObject.activeInHierarchy;

            return true;
        }
    }

    public static GameObject FindRandomMark(string mark)
    {
        List<GameObject> objects = FindObjectsWithMark(mark);
        if (objects.Count < 1)
        {
            Debug.LogError($"Couldn't find any object with Mark \"{mark}\"");
            return null;
        }

        return objects[Random.Range(0, objects.Count)];
    }

    public static List<GameObject> FindObjectsWithMark(string mark)
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (Mark m in FindObjectsOfType<Mark>(true))
        {
            if (m.mark == mark && m.findable)
                objects.Add(m.gameObject);
        }

        /*
        if (objects.Count < 1)
            Debug.LogError($"Couldn't find any object with Mark \"{mark}\"");
        */

        return objects;
    }

    public static GameObject FindObjectWithMark(string mark)
    {
        foreach (Mark m in FindObjectsOfType<Mark>(true))
        {
            if (m.mark == mark && m.findable)
                return m.gameObject;
        }

        Debug.LogError($"Couldn't find any object with Mark \"{mark}\"");

        return null;
    }
}
}
