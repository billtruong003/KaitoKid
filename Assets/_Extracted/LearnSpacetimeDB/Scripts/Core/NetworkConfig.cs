#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using UnityEngine;

namespace SpumOnline
{
    /// <summary>
    /// Static configuration for SpacetimeDB connection settings.
    /// </summary>
    public static class NetworkConfig
    {
        public const string HOST = "ws://127.0.0.1:3000";
        public const string MODULE_NAME = "spum-online";

        /// <summary>
        /// File path for persisting the SpacetimeDB auth token across sessions.
        /// Uses Application.persistentDataPath so it works on all platforms.
        /// </summary>
        public static string TokenFilePath => Application.persistentDataPath + "/spacetimedb_token";

        /// <summary>
        /// Movement update rate in Hz. Throttles how often we send position updates to the server.
        /// </summary>
        public const int MOVEMENT_SEND_RATE_HZ = 15;

        /// <summary>
        /// Interval in seconds between movement update packets.
        /// </summary>
        public static float MovementSendInterval => 1f / MOVEMENT_SEND_RATE_HZ;

        /// <summary>
        /// Speed for lerping remote player positions toward their server-authoritative target.
        /// </summary>
        public const float REMOTE_LERP_SPEED = 12f;

        /// <summary>
        /// Speed for reconciling the local player's predicted position with server position.
        /// </summary>
        public const float LOCAL_RECONCILE_SPEED = 14f;

        /// <summary>
        /// Maximum number of visible chat messages in the chat panel.
        /// </summary>
        public const int MAX_CHAT_MESSAGES = 50;

        /// <summary>
        /// Number of inventory slots in the player inventory grid (6 columns x 5 rows).
        /// </summary>
        public const int INVENTORY_SLOT_COUNT = 30;

        /// <summary>
        /// Number of skill slots available to the player.
        /// </summary>
        public const int SKILL_SLOT_COUNT = 4;
    }
}

#endif // STDB_BINDINGS
