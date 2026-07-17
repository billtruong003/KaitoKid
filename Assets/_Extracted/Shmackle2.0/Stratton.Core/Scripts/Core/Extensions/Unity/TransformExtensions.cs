using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Stratton.Core
{
    public static class TransformExtensions
    {
        #region Public Methods

        public static T GetOrAddComponent<T>(this Transform trans) where T : Component
        {
            T component = trans.GetComponent<T>();
            if (component == null)
            {
                component = trans.gameObject.AddComponent<T>();
            }
            return component;
        }

        public static void ResetLocals(this Transform trans)
        {
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }

        public static string GetStringPath(this Transform trans)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(trans.name);
            Transform parent = trans.parent;
            while (parent != null)
            {
                sb.Insert(0, parent.name + "/");
                parent = parent.parent;
            }
            return sb.ToString();
        }

        public static string FullHierachyName(this Transform transform)
        {
            StringBuilder sb = new StringBuilder();
            if (transform.parent != null)
            {
                RecursiveHierachyName(transform.parent, sb);
            }
            sb.Append(transform.name);
            return sb.ToString();
        }

        public static void SetNewLayer(this Transform transform, int newLayer, ref Dictionary<Transform, int> oldLayer)
        {
            SetLayerNewRec(transform, newLayer, ref oldLayer);
        }

        public static void SetLayer(this Transform trans, Dictionary<Transform, int> layer)
        {
            SetLayerRec(trans, layer);
        }

        public static void SetLayer(this Transform transform, int layer)
        {
            SetLayerRec(transform, layer);
        }

        public static Transform At(this Transform[] transforms, Enum en)
        {
            return transforms[(int) ((object) en)];
        }
        
        public static void DestroyAllChildren(this Transform transform, bool setLoose = false)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (setLoose)
                    child.SetParent(null);
                Object.Destroy(child.gameObject);
            }
        }

        public static void DestroyImmediateAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
        }

        #endregion

        #region Private Methods

        private static void RecursiveHierachyName(Transform e, StringBuilder sb)
        {
            if (e.parent != null)
            {
                RecursiveHierachyName(e.parent, sb);
            }
            sb.Append(e.name);
            sb.Append('/');
        }

        private static void SetLayerNewRec(Transform trans, int newLayer, ref Dictionary<Transform, int> oldLayer)
        {
            oldLayer[trans] = trans.gameObject.layer;
            trans.gameObject.layer = newLayer;
            foreach (Transform t in trans)
            {
                SetLayerNewRec(t, newLayer, ref oldLayer);
            }
        }

        private static void SetLayerRec(Transform trans, Dictionary<Transform, int> layers)
        {
            int layer = 0;
            if (layers.TryGetValue(trans, out layer))
            {
                trans.gameObject.layer = layers[trans];
                foreach (Transform t in trans)
                {
                    SetLayerRec(t, layer);
                }
            }
            else
            {
                Debug.LogError("SetLayerRec layer for " + trans.name + " not found");
            }
        }

        private static void SetLayerRec(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;
            foreach (Transform t in trans)
            {
                SetLayerRec(t, layer);
            }
        }

        #endregion
    }
}