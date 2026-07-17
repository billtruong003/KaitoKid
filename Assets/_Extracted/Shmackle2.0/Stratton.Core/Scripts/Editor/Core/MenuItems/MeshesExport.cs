using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public static class MeshesExport
    {
        #region Public Methods

        [MenuItem("GameObject/Support/ExportMeshesToFBX", false, 0)]
        public static void ExportMeshesToFBX()
        {
            var go = GetGameObjectFromSelection();
            if (go)
            {
                MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
                Mesh[] meshes = new Mesh[mfs.Length];
                for (int i = 0; i < mfs.Length; i++)
                {
                    meshes[i] = mfs[i].sharedMesh;
                }

                string path = AssetDatabase.GetAssetPath(meshes[0]);
                path = EditorUtility.SaveFilePanelInProject("Chose path", go.name, "fbx", "").Replace(Application.dataPath,
                    "Assets/");
                if (path.IsNullOrEmpty())
                {
                    return;
                }

                Log.Message(BaseLogChannel.Core, "Save to path: " + path);

                // Use some 3rd party script/plugin here to export the file

                AssetDatabase.Refresh();
            }
        }

        public static GameObject GetGameObjectFromSelection()
        {
            GameObject go = Selection.activeObject as GameObject;
            if (go == null)
            {
                go = Selection.gameObjects[0];
            }

            return go;
        }

        #endregion
    }
}