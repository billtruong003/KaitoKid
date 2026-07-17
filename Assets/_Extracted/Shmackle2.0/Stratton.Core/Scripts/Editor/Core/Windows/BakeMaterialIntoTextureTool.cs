using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

namespace Stratton.Core.Editor
{
    public class BakeMaterialIntoTextureTool : EditorWindow
    {
        #region Fields

        private static Material _mat;
        private static Vector2 _texSize = new Vector2(1024, 1024);
        private static string _lastFilePath = "";
        private static Mesh _mesh;
        private static Color _emptyMeshPlaceColor = new Color(0.5f, 0.5f, 1f);

        #endregion

        #region Public Methods

        [MenuItem("Tools/BakeMaterialIntoTextureTool")]
        public static void NormalMapMergeToolShow()
        {
            BakeMaterialIntoTextureTool window =
                (BakeMaterialIntoTextureTool)GetWindow(typeof(BakeMaterialIntoTextureTool));
        }

        #endregion

        #region Private Methods

        void OnGUI()
        {
            _mat = (Material)EditorGUILayout.ObjectField("Material:", _mat, typeof(Material), true);
            _mesh = (Mesh)EditorGUILayout.ObjectField("Mesh(not req):", _mesh, typeof(Mesh), true);
            if (_mesh != null)
            {
                _emptyMeshPlaceColor = EditorGUILayout.ColorField("EmptyMeshPlaceColor:", _emptyMeshPlaceColor);
            }
            _texSize = EditorGUILayout.Vector2Field("Texture size:", _texSize);

            if (_mat != null && GUILayout.Button("Bake"))
            {
                Bake();
            }
        }

        void Bake()
        {
            RenderTexture rt = new RenderTexture((int)_texSize.x, (int)_texSize.y, 32, RenderTextureFormat.ARGB32);
            Texture2D texture = new Texture2D((int)_texSize.x, (int)_texSize.y, TextureFormat.ARGB32, false);
            if (_mesh == null)
            {
                Graphics.Blit(null, rt, _mat);
            }
            else
            {
                LayerMask layer = LayerMask.NameToLayer("Ignore Raycast");
                GameObject camGo = new GameObject();
                Camera cam = camGo.AddComponent<Camera>();
                cam.cullingMask = 1 << layer;
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = _emptyMeshPlaceColor;
                cam.targetTexture = rt;
                cam.Render();
                GameObject go = new GameObject();
                go.layer = layer;
                var rend = go.AddComponent<MeshRenderer>();
                rend.material = _mat;
                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = _mesh;
                go.transform.position = cam.transform.position + cam.transform.forward * 2 * _mesh.bounds.max.magnitude;
                RenderTexture.active = rt;
                cam.Render();
                DestroyImmediate(camGo);
                DestroyImmediate(go);
            }

            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();

            try
            {
                if (string.IsNullOrEmpty(_lastFilePath))
                {
                    _lastFilePath = Application.dataPath + "/";
                }
                var bytes = texture.EncodeToPNG();
                _lastFilePath = EditorUtility.SaveFilePanel("Save as...",
                    _lastFilePath.Substring(0, _lastFilePath.LastIndexOf("/")), _mat.name, "png");
                File.WriteAllBytes(_lastFilePath, bytes);
                Log.Message(BaseLogChannel.Core, "Texture saved in: " + _lastFilePath, texture);
            }
            catch (Exception e)
            {
                Log.Error(BaseLogChannel.Core, e.ToString());
            }
        }

        #endregion
    }
}