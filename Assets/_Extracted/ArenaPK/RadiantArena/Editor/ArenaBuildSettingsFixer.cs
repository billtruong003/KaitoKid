#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RadiantArena.EditorTools
{
    /// <summary>
    /// One-shot Editor menu — sets Bootstrap.unity as Scene 0 in Build Settings.
    /// Bill runs once when starting a fresh clone or after Build Settings drift
    /// (e.g., session loaded SampleScene by default — happened 2026-05-18).
    /// </summary>
    public static class ArenaBuildSettingsFixer
    {
        const string BootstrapPath = "Assets/RadiantArena/Scenes/Bootstrap.unity";

        [MenuItem("Tools/RadiantArena/Set Bootstrap as Scene 0")]
        public static void SetBootstrapScene0()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath);
            if (asset == null)
            {
                Debug.LogError($"[ArenaBuildSettingsFixer] Bootstrap.unity not found at {BootstrapPath}");
                return;
            }

            var existing = EditorBuildSettings.scenes;
            var seen = new HashSet<string>();
            var list = new List<EditorBuildSettingsScene>();

            // Bootstrap first.
            list.Add(new EditorBuildSettingsScene(BootstrapPath, enabled: true));
            seen.Add(BootstrapPath);

            // Preserve all other scenes in their existing order, skipping duplicates.
            foreach (var s in existing)
            {
                if (s == null || string.IsNullOrEmpty(s.path)) continue;
                if (seen.Contains(s.path)) continue;
                list.Add(new EditorBuildSettingsScene(s.path, s.enabled));
                seen.Add(s.path);
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[ArenaBuildSettingsFixer] Bootstrap set as Scene 0. Total scenes: {list.Count}");
        }
    }
}
#endif
