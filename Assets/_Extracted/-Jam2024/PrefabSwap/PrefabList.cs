using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PrefabList", menuName = "ScriptableObjects/PrefabList", order = 1)]
public class PrefabList : ScriptableObject
{
    [System.Serializable]
    public struct PrefabInfo
    {
        public string name;
        public GameObject prefab;
    }

    public List<PrefabInfo> prefabList;

    public void GetPrefabName()
    {
        for (int i = 0; i < prefabList.Count; i++)
        {
            PrefabInfo prefabInfo = prefabList[i];
            prefabInfo.name = prefabInfo.prefab.name;
            prefabList[i] = prefabInfo; // Assign the modified struct back to the list
        }
    }
}
