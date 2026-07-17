#nullable enable
namespace RadiantArena.Net
{
    /// <summary>
    /// Everything NetClient needs to call Colyseus.JoinById. Populated by
    /// UrlParser (production WebGL) or ManualRoomConnect EditorWindow (dev).
    /// </summary>
    public struct ConnectionInfo
    {
        /// <summary>e.g. "ws://localhost:2567" or "wss://arena-api.billthedev.com".</summary>
        public string wsUrl;

        /// <summary>9-char auto-generated Colyseus roomId. NOT derived from session_id.</summary>
        public string roomId;

        /// <summary>Server-side session_id (embedded in token payload).</summary>
        public string sessionId;

        /// <summary>HMAC-signed token "{payloadB64Url}.{sigHex}" per server-extract §D.</summary>
        public string token;

        /// <summary>Discord ID used as MapSchema key — must match an entry in PlayerSchema.discord_id.</summary>
        public string discordId;

        public bool IsValid() =>
            !string.IsNullOrEmpty(wsUrl) &&
            !string.IsNullOrEmpty(roomId) &&
            !string.IsNullOrEmpty(token);
    }
}
