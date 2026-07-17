#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Waits for NetClient.ConnectAsync to fire NetConnectedEvent or
    /// NetErrorEvent, then routes to LobbyState on PhaseChangedEvent
    /// { newPhase="lobby" }. (D.U3 activated the LobbyState handoff;
    /// D.U2a only logged here.)
    /// </summary>
    public class ConnectingState : GameState
    {
        Action<NetConnectedEvent>? _onConnected;
        Action<NetErrorEvent>? _onError;
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.Connecting] Waiting for NetConnectedEvent / NetErrorEvent ...");

            _onConnected = e =>
            {
                Debug.Log($"[Arena.Connecting] Connected sessionId={e.sessionId} roomId={e.roomId}");
            };
            _onError = e =>
            {
                Debug.LogWarning($"[Arena.Connecting] NetErrorEvent code={e.code} message={e.message}");
                // D.U3+ will route to an error-screen state.
            };
            _onPhase = e =>
            {
                if (e.newPhase == "lobby")
                {
                    Debug.Log("[Arena.Connecting] phase -> lobby, transitioning to LobbyState");
                    Bill.State.GoTo<LobbyState>();
                }
            };
            Bill.Events.Subscribe(_onConnected);
            Bill.Events.Subscribe(_onError);
            Bill.Events.Subscribe(_onPhase);

            // Auto-connect if a URL-derived ConnectionInfo is sitting in CurrentInfo
            // already (production WebGL flow). Editor path goes through
            // ArenaConnectWindow which calls NetClient.ConnectAsync directly.
            var nc = NetClient.Instance;
            if (nc != null && !nc.IsConnected && nc.CurrentInfo.IsValid())
            {
                _ = nc.ConnectAsync(nc.CurrentInfo);
            }
        }

        public override void Exit()
        {
            if (_onConnected != null) Bill.Events.Unsubscribe(_onConnected);
            if (_onError != null) Bill.Events.Unsubscribe(_onError);
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onConnected = null;
            _onError = null;
            _onPhase = null;
        }
    }
}
