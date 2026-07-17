using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ShmackleKissDecalPooler : MonoBehaviour
{
    [Serializable]
    class MountDecalPool
    {
        [SerializeField] private string decalID;
        [SerializeField] GameObject decalPrefab;
        [SerializeField] private int spawnDefault = 10;
        
        public string DecalID => decalID;
        public GameObject DecalPrefab => decalPrefab;
        public int SpawnDefault => spawnDefault;
    }
    
    public static ShmackleKissDecalPooler Instance;
    [SerializeField] List<MountDecalPool> startDecalPools;
    
    Dictionary<string, MountDecalPool> _decalPools = new Dictionary<string, MountDecalPool>();
    
    public Dictionary<string, Queue<GameObject>> Pools { private set; get; } = new Dictionary<string, Queue<GameObject>>();
    public Dictionary<string, List<GameObject>> DecalActive { private set; get; } = new Dictionary<string, List<GameObject>>();
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Init();
    }

    private void Init()
    {
        for (int i = 0; i < startDecalPools.Count; i++)
        {
            _decalPools.Add(startDecalPools[i].DecalID, startDecalPools[i]);
            CreatePools(startDecalPools[i].DecalID, startDecalPools[i].DecalPrefab, startDecalPools[i].SpawnDefault);
            DecalActive.Add(startDecalPools[i].DecalID, new List<GameObject>());
        }
    }
    
    private void CreatePools(string poolID, GameObject decalObject, int order)
    {
        for (int i = 0; i < order; i++)
        {
            GameObject _object = Instantiate(decalObject, transform);
            _object.SetActive(false);

            if (!Pools.ContainsKey(poolID))
            {
                Pools.Add(poolID, new Queue<GameObject>());
            }
            
            Pools[poolID].Enqueue(_object);
        }
    }

    public GameObject GetFromPool(string decalID, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (_decalPools.Count <= 0 || !_decalPools.ContainsKey(decalID))
        {
            return null;
        }
        
        if (!Pools.ContainsKey(decalID))
        {
            CreatePools(decalID, _decalPools[decalID].DecalPrefab,1);
        }

        if (Pools[decalID].Count <= 0)
        {
            CreatePools(decalID, _decalPools[decalID].DecalPrefab,1);
        }
        
        GameObject _object = Pools[decalID].Dequeue();
        _object.transform.position = position;
        _object.transform.rotation = rotation;
        _object.transform.parent = parent;
        _object.SetActive(true);
        DecalActive[decalID].Add(_object);
        return _object;
    }

    public void ReturnToPool(string decalID, GameObject o)
    {
        Pools[decalID].Enqueue(o);
        DecalActive[decalID].Remove(o);
        o.transform.SetParent(transform);
        o.SetActive(false);
    }
}
