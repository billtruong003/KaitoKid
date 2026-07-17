#nullable enable
// NetClient — singleton MonoBehaviour that owns the Colyseus connection.
//
// Design notes:
// - This is the ONLY type that touches Colyseus.Client / Colyseus.Room<T>.
//   Everything else reads from ArenaContext and listens to Bill.Events.
// - Bypasses Bill.Net by design — INetworkAdapter is Photon-shaped
//   (CreateRoom/JoinRoom) and doesn't fit Colyseus JoinById<T> + state diff.
//   See PLAN.md §6.1.
// - ConnectAsync is fail-closed: every exception fires NetErrorEvent and
//   returns. State machine listens to NetErrorEvent for retry/error UI.
// - OnMessage<T> handlers are stubs for D.U2a (just log). D.U3+ gameplay
//   láts replace the lambdas with real handlers.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BillGameCore;
using Colyseus;
using Client = Colyseus.ColyseusClient;
using RadiantArena.Events;
using UnityEngine;

namespace RadiantArena.Net
{
    public class NetClient : MonoBehaviour
    {
        public static NetClient? Instance { get; private set; }

        public ColyseusRoom<DuelState>? Room { get; private set; }
        public bool IsConnected => Room != null;
        public ConnectionInfo CurrentInfo { get; private set; }

        Client? _client;
        string _lastPhase = "";
        readonly Dictionary<string, int> _lastHp = new Dictionary<string, int>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            Disconnect();
            if (Instance == this) Instance = null;
        }

        public async Task ConnectAsync(ConnectionInfo info)
        {
            if (!info.IsValid())
            {
                Debug.LogWarning($"[Arena.Net] ConnectionInfo invalid (wsUrl='{info.wsUrl}' roomId='{info.roomId}' tokenLen={info.token?.Length ?? 0}) — firing MISSING_TOKEN.");
                Bill.Events.Fire(new NetErrorEvent { code = "MISSING_TOKEN", message = "ConnectionInfo.IsValid()=false" });
                return;
            }

            if (IsConnected)
            {
                Debug.LogWarning("[Arena.Net] ConnectAsync called while still connected — disconnecting first.");
                Disconnect();
            }

            CurrentInfo = info;
            ArenaContext.MyDiscordId = info.discordId;
            ArenaContext.SessionId = info.sessionId;
            _lastPhase = "";

            Debug.Log($"[Arena.Net] Connecting to {info.wsUrl} room={info.roomId} as did={info.discordId} ...");

            try
            {
                _client = new Client(info.wsUrl);
                var options = new Dictionary<string, object> { ["token"] = info.token };
                Room = await _client.JoinById<DuelState>(info.roomId, options);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Arena.Net] Join failed: {e.Message}");
                Bill.Events.Fire(new NetErrorEvent { code = "JOIN_FAILED", message = e.Message });
                Room = null;
                return;
            }

            // State + leave events.
            Room.OnStateChange += OnStateChange;
            Room.OnLeave += OnLeave;

            // Inbound messages — stubs for D.U2a; D.U3+ replaces with real handlers.
            Room.OnMessage<MatchStartMessage>("match_start",
                _ => Debug.Log("[Arena.Net] match_start (no handler — D.U4+)"));
            Room.OnMessage<ShotResolvedMessage>("shot_resolved", OnShotResolved);
            Room.OnMessage<TurnSwitchedMessage>("turn_switched",
                _ => Debug.Log("[Arena.Net] turn_switched (no handler — D.U4)"));
            Room.OnMessage<SignatureUsedMessage>("signature_used",
                _ => Debug.Log("[Arena.Net] signature_used (no handler — D.U7)"));
            Room.OnMessage<MatchEndedMessage>("match_ended", OnMatchEnded);
            Room.OnMessage<PongMessage>("pong",
                _ => { /* silent */ });
            Room.OnMessage<ErrorMessage>("error", m =>
            {
                Debug.LogWarning($"[Arena.Net] server error code={m.code} slug={m.slug}");
                Bill.Events.Fire(new NetErrorEvent { code = m.code, message = m.slug });
            });

            Debug.Log($"[Arena.Net] Joined room {info.roomId} (sessionId={Room.SessionId})");
            Bill.Events.Fire(new NetConnectedEvent { sessionId = info.sessionId, roomId = info.roomId });
        }

        void OnStateChange(DuelState state, bool isFirstState)
        {
            ArenaContext.HydrateFrom(state);

            if (state.phase != _lastPhase)
            {
                var old = _lastPhase;
                _lastPhase = state.phase;
                Debug.Log($"[Arena.Phase] {old} -> {state.phase}");
                Bill.Events.Fire(new PhaseChangedEvent { oldPhase = old, newPhase = state.phase });
            }

            // HP diff — fire HpChangedEvent per player whose hp changed since last tick.
            foreach (var keyObj in state.players.Keys)
            {
                if (!(keyObj is string pid)) continue;
                var p = state.players[pid];
                if (p == null) continue;
                int now = p.hp;
                int max = p.hp_max;
                if (_lastHp.TryGetValue(pid, out int prev))
                {
                    if (prev != now)
                    {
                        Bill.Events.Fire(new HpChangedEvent
                        {
                            playerId = pid, oldHp = prev, newHp = now, hpMax = max,
                        });
                    }
                }
                _lastHp[pid] = now;
            }

            if (isFirstState)
            {
                Bill.Events.Fire(new InitialStateReceivedEvent { sessionId = CurrentInfo.sessionId });
            }
        }

        void OnLeave(int code)
        {
            Debug.Log($"[Arena.Net] OnLeave code={code}");
            Room = null;
            _lastPhase = "";
            _lastHp.Clear();
            ArenaContext.Reset();
            Bill.Events.Fire(new NetDisconnectedEvent { code = code, reason = $"OnLeave code={code}" });
        }

        void OnShotResolved(ShotResolvedMessage m)
        {
            // Snapshot the live Colyseus schema array into a plain-C# DTO so
            // gameplay code never holds onto a reference the server mutates.
            var raw = m.trajectory;
            TrajectoryPoint[] points;
            if (raw == null || raw.Length == 0)
            {
                points = System.Array.Empty<TrajectoryPoint>();
            }
            else
            {
                points = new TrajectoryPoint[raw.Length];
                for (int i = 0; i < raw.Length; i++)
                {
                    var p = raw[i];
                    if (p == null) continue;
                    points[i] = new TrajectoryPoint
                    {
                        t = p.t,
                        x = p.x,
                        y = p.y,
                        evt = p.@event ?? string.Empty,
                    };
                }
            }

            ArenaContext.LastTrajectory = points;
            ArenaContext.LastShooterId = m.shooter ?? "";
            ArenaContext.LastShotDamage = Mathf.RoundToInt(m.damage_dealt);
            ArenaContext.LastShotCrit = m.crit;

            Debug.Log($"[Arena.Net] shot_resolved — points={points.Length} shooter={m.shooter} dmg={m.damage_dealt} crit={m.crit}");

            Bill.Events.Fire(new ShotResolvedEvent
            {
                points = points,
                shooterId = m.shooter ?? "",
                damage = Mathf.RoundToInt(m.damage_dealt),
                crit = m.crit,
            });
        }

        void OnMatchEnded(MatchEndedMessage m)
        {
            // Snapshot final_hp into a fresh dict — never hold a reference Colyseus
            // might mutate (defensive even though match_ended is terminal).
            var finalHp = new Dictionary<string, int>();
            if (m.final_hp != null)
            {
                foreach (var kv in m.final_hp) finalHp[kv.Key] = kv.Value;
            }

            ArenaContext.LastMatchWinnerId = m.winner ?? "";
            ArenaContext.LastMatchOutcome  = m.outcome ?? "";
            ArenaContext.LastMatchFinalHp  = finalHp;

            Debug.Log($"[Arena.Net] match_ended — winner={m.winner} outcome={m.outcome} hpEntries={finalHp.Count}");

            Bill.Events.Fire(new MatchEndedEvent
            {
                winnerId = m.winner ?? "",
                outcome  = m.outcome ?? "",
                finalHp  = finalHp,
            });
        }

        public void Send(string type, object payload)
        {
            if (Room == null)
            {
                Debug.LogWarning($"[Arena.Net] Send({type}) ignored — not connected.");
                return;
            }
            // Colyseus Send returns Task; fire-and-forget here. Errors land in OnLeave/OnError.
            _ = Room.Send(type, payload);
        }

        public void Disconnect()
        {
            if (Room != null)
            {
                try { _ = Room.Leave(); } catch (Exception e) { Debug.LogWarning($"[Arena.Net] Leave threw: {e.Message}"); }
                Room = null;
            }
            _client = null;
            _lastPhase = "";
            _lastHp.Clear();
            ArenaContext.Reset();
            Debug.Log("[Arena.Net] Disconnected");
        }
    }
}
