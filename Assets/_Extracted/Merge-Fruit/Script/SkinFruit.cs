using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FruitCasual
{
    [Serializable]
    public class FruitObject
    {
        [SerializeField] public SkinCode code;
        [SerializeField] public List<SkinFruit> skinFruits;
    }
    [Serializable]
    public class SkinFruit
    {
        [SerializeField] public ObjectLevel objectLevel;
        [SerializeField] public GameObject objectPrefab;
    }

}
