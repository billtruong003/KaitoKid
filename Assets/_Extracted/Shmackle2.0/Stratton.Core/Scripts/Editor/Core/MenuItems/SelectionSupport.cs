using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public class SelectionSupport : MonoBehaviour
    {
        #region Public Methods

        [MenuItem("GameObject/Select/StaticObjects", false, 0)]
        public static void SelectStaticObjects()
        {
            var gos = FindObjectsOfType<GameObject>();
            List<GameObject> staticGos = new List<GameObject>();
            foreach (var go in gos)
            {
                if (go.isStatic)
                {
                    staticGos.Add(go);
                }
            }

            Selection.objects = staticGos.ToArray();
        }

        [MenuItem("GameObject/Select/NonStaticObjects", false, 0)]
        public static void SelectNonStaticObjects()
        {
            var gos = FindObjectsOfType<GameObject>();
            List<GameObject> staticGos = new List<GameObject>();
            foreach (var go in gos)
            {
                if (!go.isStatic)
                {
                    staticGos.Add(go);
                }
            }

            Selection.objects = staticGos.ToArray();
        }

        #endregion
    }
}