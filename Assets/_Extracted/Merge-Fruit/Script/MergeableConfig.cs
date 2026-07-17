using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FruitCasual
{
    [CreateAssetMenu(fileName = "MergeableConfig", menuName = "FruitCasual/MergeableConfig")]
    public class MergeableConfig : ScriptableObject
    {
        [SerializeField] public GameObject[] objectMerges = new GameObject[10];
        [SerializeField] public List<FruitObject> fruitObjects;
    }
}