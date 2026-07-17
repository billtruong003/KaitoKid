using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Stratton.Core.Editor
{
    public class CopyPathToClipboard
    {
        [MenuItem("Assets/Copy path to clipboard", false, 999999)]
        public static void CopyPathToClip()
        {
            Object []selectedObjects = Selection.objects;

            var obj = Selection.activeObject;
            if (obj == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (var selectedObject in selectedObjects)
            {
                sb.Append(AssetDatabase.GetAssetPath(selectedObject.GetInstanceID()));
                sb.Append("\n");
            }
            sb.Remove(sb.Length - 1, 1);

            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }
    }
}
