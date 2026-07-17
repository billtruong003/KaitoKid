#nullable enable
// EditorWindow: paste wsUrl + roomId + sessionId + discord_id + secret,
// mint a 15-minute HMAC token via DevTokenSigner, then connect to a running
// arena-server. Field values + secret survive across sessions in EditorPrefs.
//
// Interim tool until arena-server Lát D.3 ships the admin endpoint
// (POST /admin/create-room) — when that's available, NetClient can derive
// roomId + token from a single bot-issued URL and this window goes away.
//
// History: this file was originally ManualRoomConnect.cs but Unity's MonoScript
// asset cache for that filename's GUID got into a broken state where the file
// would not compile into Assembly-CSharp-Editor even after deleting+regenerating
// the .meta. Renaming the type + filename to ArenaConnectWindow gave it a fresh
// GUID and fixed the compile. Tracked as a Unity quirk; no code change beyond
// the type rename was needed.

using System;
using RadiantArena.Net;
using UnityEditor;
using UnityEngine;

namespace RadiantArena.Editor
{
    public class ArenaConnectWindow : EditorWindow
    {
        const string PrefWsUrl = "RadiantArena.WsUrl";
        const string PrefRoomId = "RadiantArena.RoomId";
        const string PrefSessionId = "RadiantArena.SessionId";
        const string PrefDiscordId = "RadiantArena.DiscordId";
        const string PrefSecret = "RadiantArena.ArenaTokenSecret";

        string _wsUrl = "ws://localhost:2567";
        string _roomId = "";
        string _sessionId = "test_session_001";
        string _discordId = "bill_test_001";
        string _secret = "";
        string _tokenPreview = "";
        Vector2 _scroll;

        [MenuItem("Window/Radiant Arena/Connect")]
        static void Open()
        {
            var win = GetWindow<ArenaConnectWindow>("Arena Connect");
            win.minSize = new Vector2(420, 320);
            win.Show();
        }

        void OnEnable()
        {
            _wsUrl = EditorPrefs.GetString(PrefWsUrl, _wsUrl);
            _roomId = EditorPrefs.GetString(PrefRoomId, _roomId);
            _sessionId = EditorPrefs.GetString(PrefSessionId, _sessionId);
            _discordId = EditorPrefs.GetString(PrefDiscordId, _discordId);
            _secret = EditorPrefs.GetString(PrefSecret, _secret);
        }

        void Persist()
        {
            EditorPrefs.SetString(PrefWsUrl, _wsUrl);
            EditorPrefs.SetString(PrefRoomId, _roomId);
            EditorPrefs.SetString(PrefSessionId, _sessionId);
            EditorPrefs.SetString(PrefDiscordId, _discordId);
            EditorPrefs.SetString(PrefSecret, _secret);
        }

        void OnLostFocus() => Persist();

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Radiant Arena — Manual Connect", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Start arena-server (pnpm dev → ws://localhost:2567)\n" +
                "2. Set ARENA_TOKEN_SECRET below to match server's .env\n" +
                "3. Paste roomId from server console\n" +
                "4. Enter Play mode → Mint Token → Connect",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _wsUrl = EditorGUILayout.TextField("WS URL", _wsUrl);
            _roomId = EditorGUILayout.TextField("Room ID", _roomId);
            _sessionId = EditorGUILayout.TextField("Session ID", _sessionId);
            _discordId = EditorGUILayout.TextField("Discord ID (me)", _discordId);
            _secret = EditorGUILayout.PasswordField("ARENA_TOKEN_SECRET", _secret);
            if (EditorGUI.EndChangeCheck()) Persist();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Token preview (read-only)", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextArea(_tokenPreview, GUILayout.MinHeight(48));
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_secret)))
            {
                if (GUILayout.Button("Mint Token (15 min)"))
                {
                    try
                    {
                        var exp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds();
                        _tokenPreview = DevTokenSigner.SignToken(_sessionId, _discordId, exp, _secret);
                        EditorGUIUtility.systemCopyBuffer = _tokenPreview;
                        Debug.Log($"[Arena.DevToken] Minted ({_tokenPreview.Length} chars), copied to clipboard.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Arena.DevToken] Mint failed: {e.Message}");
                        _tokenPreview = $"<error: {e.Message}>";
                    }
                }
            }

            bool canConnect = Application.isPlaying
                              && !string.IsNullOrEmpty(_tokenPreview)
                              && !_tokenPreview.StartsWith("<error")
                              && !string.IsNullOrEmpty(_roomId);
            using (new EditorGUI.DisabledScope(!canConnect))
            {
                if (GUILayout.Button("Connect"))
                {
                    var nc = NetClient.Instance;
                    if (nc == null)
                    {
                        Debug.LogError("[Arena.DevConnect] NetClient.Instance is null. Open Bootstrap.unity and enter Play mode first.");
                    }
                    else
                    {
                        var info = new ConnectionInfo
                        {
                            wsUrl = _wsUrl,
                            roomId = _roomId,
                            sessionId = _sessionId,
                            token = _tokenPreview,
                            discordId = _discordId,
                        };
                        _ = nc.ConnectAsync(info);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Disconnect"))
                {
                    NetClient.Instance?.Disconnect();
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
