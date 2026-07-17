using UnityEditor;
using UnityEngine;
using Stratton.Core;

namespace Stratton.Core.Editor
{
    public class TransformContext : MonoBehaviour
    {
        #region Public Methods

        [MenuItem("CONTEXT/Transform/Print hierarchy path")]
        public static void PrintHierarchyPath()
        {
            var t = Selection.activeTransform;
            Log.Message(BaseLogChannel.Core, t.FullHierachyName());
        }

        [MenuItem("CONTEXT/Transform/Copy hierarchy path")]
        public static void CopyHierarchyPath()
        {
            var t = Selection.activeTransform;
            EditorGUIUtility.systemCopyBuffer = t.FullHierachyName();
        }

        #endregion
    }
}