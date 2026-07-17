using UnityEditor;
using UnityEngine;

namespace Teabag.Editor
{
    /// <summary>
    /// Editor toggle for Online/Offline play mode.
    /// When offline (default), FusionNetworkService uses GameMode.Single (local runner, no Photon).
    /// When online, FusionNetworkService connects to Photon normally.
    /// Persists via the OFFLINE_MODE scripting define symbol.
    /// </summary>
    public static class EditorOfflineMode
    {
        private const string DEFINE_OFFLINE = "OFFLINE_MODE";
        private const string DEFINE_SKIP_AUTH = "SKIP_PLATFORM_AUTH";

        public static bool IsOffline
        {
            get => EditorUtils.HasDefineSymbol(DEFINE_OFFLINE);
            set
            {
                EditorUtils.SetDefineSymbol(DEFINE_OFFLINE, value);

                // Skip auth in offline mode but don't force it in online mode
                if(value)
                    EditorUtils.SetDefineSymbol(DEFINE_SKIP_AUTH, true);
            }
        }

        [MenuItem("Tools/Gorilla Royale/Toggle Offline Mode")]
        public static void Toggle()
        {
            IsOffline = !IsOffline;
            Debug.Log($"[Editor] Offline mode: {(IsOffline ? "ON (GameMode.Single)" : "OFF (Online)")}");
        }

        [MenuItem("Tools/Gorilla Royale/Toggle Offline Mode", true)]
        private static bool ToggleOfflineModeValidate()
        {
            Menu.SetChecked("Tools/Gorilla Royale/Toggle Offline Mode", IsOffline);
            return true;
        }
    }
}
