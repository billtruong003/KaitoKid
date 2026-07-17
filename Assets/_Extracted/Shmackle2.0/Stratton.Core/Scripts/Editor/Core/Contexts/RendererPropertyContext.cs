using UnityEditor;
using UnityEngine;

namespace Stratton.Core.Editor.Editor
{
    public class RendererPropertyContext : MonoBehaviour
    {
        #region Public Methods

        public static Renderer GetRendererFromSelection()
        {
            Renderer rend = Selection.activeObject as Renderer;
            if (rend == null)
            {
                GameObject go = Selection.gameObjects[0];
                rend = go.GetComponent<Renderer>();
            }
            return rend;
        }

        [MenuItem("CONTEXT/Renderer/Show Wireframe")]
        public static void ShowWireframe()
        {
            var rend = GetRendererFromSelection();
            if (rend)
            {
                EditorUtility.SetSelectedRenderState(rend, EditorSelectedRenderState.Wireframe);
            }
        }

        [MenuItem("CONTEXT/Renderer/Hide Wireframe")]
        public static void HideWireframe()
        {
            var rend = GetRendererFromSelection();
            if (rend)
            {
                EditorUtility.SetSelectedRenderState(rend, EditorSelectedRenderState.Hidden);
            }
        }

        [MenuItem("CONTEXT/MeshRenderer/Print min-max vert")]
        public static void PrintMinMaxVert()
        {
            MeshRenderer rend = Selection.activeObject as MeshRenderer;
            if (rend == null)
            {
                var go = Selection.activeObject as GameObject;
                if (go)
                {
                    rend = go.GetComponent<MeshRenderer>();
                }
            }
            if (rend == null)
            {
                Log.Warning(BaseLogChannel.Core, "Not found rend");
                return;
            }

            var vertices = rend.GetComponent<MeshFilter>().sharedMesh.vertices;
            Vector3 minVert = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 maxVert = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
            foreach (var vert in vertices)
            {
                minVert = minVert.Min(vert);
                maxVert = maxVert.Max(vert);
            }
            Log.Message(BaseLogChannel.Core, "MinVert: " + minVert + " MaxVert:" + maxVert);
        }

        #endregion
    }
}