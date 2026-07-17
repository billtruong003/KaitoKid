#nullable enable
// WebGL-safe URL parser.
//
// Why custom: System.Web.HttpUtility is NOT available in IL2CPP WebGL.
// Manual split keeps the parser usable across all build targets.
//
// Production URL shape (D.U10 onward):
//   https://arena.billthedev.com/?room=ABC123&t=<token>&session=<uuid>&did=<discord_id>
//
// Editor fallback: Application.absoluteURL is empty or unparseable; UrlParser
// returns a ConnectionInfo with wsUrl=ws://localhost:2567 + all other fields
// blank so IsValid() returns false. ManualRoomConnect (Editor) then provides
// the missing roomId/token/discord_id.

using UnityEngine;
using UnityEngine.Networking;

namespace RadiantArena.Net
{
    public static class UrlParser
    {
        const string DefaultDevWsUrl = "ws://localhost:2567";
        const string ProdHost = "arena.billthedev.com";
        const string ProdWsUrl = "wss://arena-api.billthedev.com";

        public static ConnectionInfo Parse(string fullUrl)
        {
            var fallback = new ConnectionInfo
            {
                wsUrl = DefaultDevWsUrl,
                roomId = "",
                sessionId = "",
                token = "",
                discordId = "",
            };

            if (string.IsNullOrEmpty(fullUrl)) return fallback;

            string host;
            string query;
            try
            {
                int schemeEnd = fullUrl.IndexOf("://", System.StringComparison.Ordinal);
                int hostStart = schemeEnd < 0 ? 0 : schemeEnd + 3;
                int pathStart = fullUrl.IndexOf('/', hostStart);
                int queryStart = fullUrl.IndexOf('?', hostStart);
                int hostEnd = pathStart < 0 ? fullUrl.Length : pathStart;
                if (queryStart >= 0 && queryStart < hostEnd) hostEnd = queryStart;
                host = fullUrl.Substring(hostStart, hostEnd - hostStart);
                // Strip :port from host for comparison.
                int colonIdx = host.IndexOf(':');
                if (colonIdx >= 0) host = host.Substring(0, colonIdx);
                query = queryStart >= 0 ? fullUrl.Substring(queryStart + 1) : "";
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Arena.Url] Parse failed for '{fullUrl}': {e.Message}. Using dev fallback.");
                return fallback;
            }

            string roomId = "";
            string token = "";
            string sessionId = "";
            string discordId = "";

            if (!string.IsNullOrEmpty(query))
            {
                var pairs = query.Split('&');
                foreach (var pair in pairs)
                {
                    if (string.IsNullOrEmpty(pair)) continue;
                    int eq = pair.IndexOf('=');
                    if (eq < 0) continue;
                    var key = pair.Substring(0, eq);
                    var rawValue = pair.Substring(eq + 1);
                    var value = UnityWebRequest.UnEscapeURL(rawValue);
                    switch (key)
                    {
                        case "room": roomId = value; break;
                        case "t": token = value; break;
                        case "session": sessionId = value; break;
                        case "did": discordId = value; break;
                    }
                }
            }

            string wsUrl = host == ProdHost ? ProdWsUrl : DefaultDevWsUrl;

            return new ConnectionInfo
            {
                wsUrl = wsUrl,
                roomId = roomId,
                sessionId = sessionId,
                token = token,
                discordId = discordId,
            };
        }
    }
}
