#if STDB_BINDINGS
using System;
using System.IO;
using UnityEngine;

namespace SpumOnline
{
    /// <summary>
    /// Handles persistence of the SpacetimeDB authentication token.
    /// Saves the token to a file in Application.persistentDataPath so it persists across sessions.
    /// </summary>
    public static class AuthManager
    {
        /// <summary>
        /// Save the authentication token to disk.
        /// </summary>
        /// <param name="token">The SpacetimeDB auth token string to persist.</param>
        public static void SaveToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[AuthManager] Attempted to save null or empty token.");
                return;
            }

            try
            {
                string path = NetworkConfig.TokenFilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, token);
                Debug.Log("[AuthManager] Token saved successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthManager] Failed to save token: {e.Message}");
            }
        }

        /// <summary>
        /// Load the authentication token from disk.
        /// </summary>
        /// <returns>The token string, or null if no token is persisted or reading fails.</returns>
        public static string LoadToken()
        {
            try
            {
                string path = NetworkConfig.TokenFilePath;
                if (!File.Exists(path))
                {
                    Debug.Log("[AuthManager] No saved token found.");
                    return null;
                }

                string token = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(token))
                {
                    Debug.LogWarning("[AuthManager] Saved token file is empty.");
                    return null;
                }

                Debug.Log("[AuthManager] Token loaded successfully.");
                return token.Trim();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthManager] Failed to load token: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete the persisted authentication token.
        /// </summary>
        public static void ClearToken()
        {
            try
            {
                string path = NetworkConfig.TokenFilePath;
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log("[AuthManager] Token cleared.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthManager] Failed to clear token: {e.Message}");
            }
        }

        /// <summary>
        /// Check whether a saved token exists.
        /// </summary>
        public static bool HasToken()
        {
            string path = NetworkConfig.TokenFilePath;
            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
    }
}

#endif // STDB_BINDINGS
