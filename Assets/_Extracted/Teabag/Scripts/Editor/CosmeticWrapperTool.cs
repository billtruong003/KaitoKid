using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Teabag.Editor
{
    public class CosmeticWrapperTool : EditorWindow
    {
        private string _targetFolder = "Assets/Resources/Cosmetics";
        private bool _overwriteOriginals = true;

        [MenuItem("Teabag/Cosmetic Wrapper Tool")]
        public static void ShowWindow()
        {
            GetWindow<CosmeticWrapperTool>("Cosmetic Wrapper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Cosmetic Wrapper Tool", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            _targetFolder = EditorGUILayout.TextField("Target Root Folder", _targetFolder);
            _overwriteOriginals = EditorGUILayout.Toggle("Overwrite Originals (DANGEROUS)", _overwriteOriginals);
            
            if (_overwriteOriginals)
            {
                EditorGUILayout.HelpBox("Overwriting originals will replace your existing prefabs in Resources/Cosmetics/. Make sure you have a backup!", MessageType.Warning);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Wrap All Cosmetics (Head, Face, Nuts)"))
            {
                WrapAll();
            }
        }

        private void WrapAll()
        {
            var slots = Enum.GetValues(typeof(CosmeticSlot));
            int totalProcessed = 0;

            foreach (CosmeticSlot slot in slots)
            {
                string category = slot.ToString();
                string sourcePath = $"Assets/Resources/Cosmetics/{category}";
                string destinationPath = _overwriteOriginals ? sourcePath : $"{_targetFolder}/{category}";

                if (!Directory.Exists(sourcePath))
                {
                    Debug.LogWarning($"Source directory not found: {sourcePath}");
                    continue;
                }

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { sourcePath });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    // Ensure we only process prefabs directy in the folder, not subfolders or duplicates
                    if (Path.GetDirectoryName(assetPath).Replace("\\", "/") != sourcePath) continue;

                    GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (original == null) continue;

                    WrapPrefab(original, destinationPath);
                    totalProcessed++;
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Cosmetic Wrapper", $"Successfully processed {totalProcessed} cosmetics.", "OK");
        }

        private void WrapPrefab(GameObject original, string destinationPath)
        {
            // 1. Create a temporary parent GameObject
            GameObject wrapper = new GameObject(original.name);
            wrapper.transform.position = Vector3.zero;
            wrapper.transform.rotation = Quaternion.identity;
            wrapper.transform.localScale = Vector3.one;

            // 2. Instantiate original as child
            // Note: We use Instantiate instead of PrefabUtility.InstantiatePrefab to avoid cyclic nesting errors when overwriting.
            GameObject child = Instantiate(original);
            child.name = original.name;
            child.transform.SetParent(wrapper.transform, false);

            // Re-apply original local transform values to ensure they are "unchanged" relative to the wrapper
            // Note: PrefabUtility.InstantiatePrefab(original) already keeps local values if parent is null or (0,0,0)
            // But we'll be explicit to fulfill the "không đổi" requirement.
            child.transform.localPosition = original.transform.localPosition;
            child.transform.localRotation = original.transform.localRotation;
            child.transform.localScale = original.transform.localScale;

            // 3. Save as new prefab
            string savePath = Path.Combine(destinationPath, original.name + ".prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(wrapper, savePath);

            // 4. Cleanup
            DestroyImmediate(wrapper);
        }
    }
}
