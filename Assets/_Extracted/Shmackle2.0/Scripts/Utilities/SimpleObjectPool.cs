using Stratton.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shmackle.Utilities
{
    public class SimpleObjectPool<T> where T : MonoBehaviour
    {
        #region Private Fields
        
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly bool _canExpand;
        
        #endregion
        
        #region Public Fields
        
        public event Action<T> ObjectAlived;
        public event Action<T> ObjectReleased;
        
        #endregion
        
        public SimpleObjectPool(T prefab, int initialSize, Transform parent = null, bool canExpand = true)
        {
            _prefab = prefab;
            _parent = parent;
            _canExpand = canExpand;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNewObject();
                // Simulate release for pre-warmed objects
                Release(obj);
            }
        }

        #region Public Methods
        
        public T Get()
        {
            T newObject;
            if (_pool.Count == 0)
            {
                if (_canExpand)
                {
                    newObject = CreateNewObject();
                }
                else
                {
                    Stratton.Core.Log.Error(BaseLogChannel.ObjectPool, "Pool limit reached.");
                    return null;
                }
            }
            else
            {   
                newObject = _pool.Dequeue();
            }
            
            if (ObjectAlived == null)
            {
                newObject.gameObject.SetActive(true);
            }
            else
            {
                ObjectAlived?.Invoke(newObject);
            }

            return newObject;
        }

        public void Release(T obj)
        {
            if (ObjectReleased == null)
            {
                obj.gameObject.SetActive(false);
            }
            else
            {
                ObjectReleased?.Invoke(obj);
            }
            _pool.Enqueue(obj);
        }
        
        #endregion

        #region Private Methods
        
        private T CreateNewObject()
        {
            T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            return obj;
        }
        
        #endregion
    }
}