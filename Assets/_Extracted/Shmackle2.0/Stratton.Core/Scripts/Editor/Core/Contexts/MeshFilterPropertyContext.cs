using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public class MeshFilterPropertyContext : MonoBehaviour
    {
        #region Public Methods

        public static MeshFilter GetMeshFilterFromSelection()
        {
            MeshFilter mf = Selection.activeObject as MeshFilter;
            if (mf == null)
            {
                GameObject go = Selection.gameObjects[0];
                mf = go.GetComponent<MeshFilter>();
            }

            return mf;
        }

        [MenuItem("CONTEXT/MeshFilter/PrintMiddleOfMesh")]
        public static void PrintMiddleOfMesh()
        {
            var mf = GetMeshFilterFromSelection();
            if (mf)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    Log.Message(BaseLogChannel.Core, "No mesh");
                    return;
                }

                var vertices = mesh.vertices;
                Vector3 sum = Vector3.zero;
                for (int i = 0; i < vertices.Length; i++)
                {
                    sum += vertices[i];
                }

                Vector3 middle = sum / vertices.Length;
                Log.Message(BaseLogChannel.Core, middle.ToString());
            }
        }

        [MenuItem("CONTEXT/MeshFilter/ExportMeshToAsset")]
        public static void ExportMeshToAsset()
        {
            var mf = GetMeshFilterFromSelection();
            if (mf)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    Log.Message(BaseLogChannel.Core, "No mesh");
                    return;
                }

                mf.sharedMesh = ExportMeshToAsset(mesh);
            }
        }

        [MenuItem("CONTEXT/MeshCollider/ExportMeshToFBX")]
        public static void ExportMeshColliderToFBX()
        {
            ExportMeshToFBX();
        }

        [MenuItem("CONTEXT/MeshFilter/ExportMeshToFBX")]
        public static void ExportMeshToFBX()
        {
            var mf = GetMeshFilterFromSelection();
            if (mf)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    var mc = mf.GetComponent<MeshCollider>();
                    if (mc)
                    {
                        mesh = mc.sharedMesh;
                    }
                }

                if (mesh == null)
                {
                    Log.Message(BaseLogChannel.Core, "No mesh");
                    return;
                }

                string path = AssetDatabase.GetAssetPath(mesh);
                path = path.Substring(0, path.LastIndexOf('/'));
                path = EditorUtility.SaveFilePanelInProject("Chose path", mesh.name, "fbx", "", path);
                path = path.Replace(Application.dataPath, "Assets/");
                if (path.IsNullOrEmpty())
                {
                    return;
                }

                Log.Message(BaseLogChannel.Core, "Save to path: " + path);

                // Use some 3rd party script/plugin here to export the file

                AssetDatabase.Refresh();
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh != null)
                {
                    mf.sharedMesh = mesh;
                }
            }
        }

        [MenuItem("CONTEXT/MeshFilter/SplitSubmeshesAndExport")]
        public static void SplitSubmeshes()
        {
            var mf = GetMeshFilterFromSelection();
            if (mf)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    var mc = mf.GetComponent<MeshCollider>();
                    if (mc)
                    {
                        mesh = mc.sharedMesh;
                    }
                }

                if (mesh == null)
                {
                    Log.Message(BaseLogChannel.Core, "No mesh");
                    return;
                }

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] subMeshTris = mesh.GetTriangles(i);
                    CreateMesh(subMeshTris, i, mesh);
                }
            }
        }

        public static Mesh CreateMesh(int[] triangles, int index, Mesh mesh)
        {
            Mesh newMesh = new Mesh();
            newMesh.Clear();
            newMesh.vertices = mesh.vertices;
            newMesh.triangles = triangles;
            newMesh.uv = mesh.uv;
            newMesh.uv2 = mesh.uv2;
            newMesh.colors = mesh.colors;
            newMesh.subMeshCount = 1;
            newMesh.normals = mesh.normals;
            return ExportMeshToAsset(newMesh);
        }

        #endregion

        #region Private Methods

        private static Mesh ExportMeshToAsset(Mesh mesh)
        {
            string path =
                EditorUtility.SaveFilePanelInProject("Chose path", mesh.name, "asset", "").Replace(Application.dataPath,
                    "Assets/");
            if (path.IsNullOrEmpty())
            {
                return null;
            }

            Log.Message(BaseLogChannel.Core, "Save to path: " + path);
            mesh = Instantiate(mesh);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return mesh;
        }

        #endregion
    }
}