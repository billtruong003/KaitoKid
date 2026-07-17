using System;
using UnityEngine;

namespace Stratton.Core
{
    public static class GameObjectExtensions
    {
        #region Public Methods

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }
            return component;
        }

        public static Component GetOrAddComponent(this GameObject go, Type type)
        {
            Component component = go.GetComponent(type);
            if (component == null)
            {
                component = go.AddComponent(type);
            }
            return component;
        }

        public static bool GetActive(this GameObject target)
        {
#if UNITY_3_5
        return target.active;
#else
            return target.activeInHierarchy;
#endif
        }

        #endregion
    }
}