#nullable enable
using BillGameCore;

namespace RadiantArena.Events
{
    // Real events for D.U2 added below. Future láts:
    //   D.U5 — ShotResolvedEvent, PlayerHitEvent, WallBounceEvent
    //   D.U6 — MatchEndedEvent (separate from MatchEndedMessage which is the
    //                          raw inbound DTO; this event is the gameplay-facing
    //                          signal after damage/HP have been applied locally).

    /// <summary>Fired by ArenaBootstrap once Bill is Ready. Net layer subscribes.</summary>
    public struct ArenaBootstrapReadyEvent : IEvent { }

    /// <summary>Fired by NetClient after Colyseus JoinById success.</summary>
    public struct NetConnectedEvent : IEvent
    {
        public string sessionId;
        public string roomId;
    }

    /// <summary>Fired by NetClient on Room.OnLeave (server-initiated or local Disconnect).</summary>
    public struct NetDisconnectedEvent : IEvent
    {
        /// <summary>Colyseus close code. 4215 = auth rejected by onAuth().</summary>
        public int code;
        public string reason;
    }

    /// <summary>Fired by NetClient on join failure or server-side "error" message.</summary>
    public struct NetErrorEvent : IEvent
    {
        /// <summary>Known codes: MISSING_TOKEN, JOIN_FAILED, AUTH_REJECTED, WEAPON_NOT_OWNED, NO_WEAPON_SELECTED.</summary>
        public string code;
        public string message;
    }

    /// <summary>Fired by NetClient when DuelState.phase changes (waiting/lobby/countdown/active/animating/ended).</summary>
    public struct PhaseChangedEvent : IEvent
    {
        public string oldPhase;
        public string newPhase;
    }

    /// <summary>Fired once on the first OnStateChange after JoinById — guarantees ArenaContext is populated.</summary>
    public struct InitialStateReceivedEvent : IEvent
    {
        public string sessionId;
    }

    /// <summary>Fired when a new turn starts (server transitioned to phase=active). Carries which player + deadline.</summary>
    public struct TurnStartedEvent : IEvent
    {
        public string turnPlayerId;
        public long deadlineAt;
        public int round;
    }

    /// <summary>Fired every frame while dragging during my turn. Power 0..1, angle in radians.</summary>
    public struct AimUpdatedEvent : IEvent
    {
        public float angle;
        public float power;
    }

    /// <summary>Fired when drag-aim is canceled or completed. Panel resets power gauge.</summary>
    public struct AimClearedEvent : IEvent { }

    /// <summary>Fired by ArenaAimController on valid shot release (power past dead zone). MyTurnState routes to NetClient.Send.</summary>
    public struct ShotReleasedEvent : IEvent
    {
        public float angle;
        public float power;
    }

    /// <summary>
    /// Fired by NetClient when "shot_resolved" arrives. Plain-C# snapshot of the
    /// server payload — gameplay never reads the live Colyseus schema.
    /// </summary>
    public struct ShotResolvedEvent : IEvent
    {
        public RadiantArena.Net.TrajectoryPoint[] points;
        public string shooterId;
        public int damage;
        public bool crit;
    }

    /// <summary>
    /// Fired by TrajectoryRenderer when a trajectory point with event="hit:N" or
    /// event="crit:N" is reached. D.U6 HudPanel subscribes to animate HP; D.U7
    /// adds camera shake / hit-stop / damage numbers.
    /// </summary>
    public struct PlayerHitEvent : IEvent
    {
        public int damage;
        public bool isCrit;
        public string victimId;
        public UnityEngine.Vector3 point;
    }

    /// <summary>
    /// Fired by TrajectoryRenderer when a trajectory point with event="wall_bounce"
    /// is reached. D.U7 wires camera shake + wall-dust FX.
    /// </summary>
    public struct WallBounceEvent : IEvent
    {
        public UnityEngine.Vector3 point;
    }

    /// <summary>
    /// Fired by TrajectoryRenderer when playback completes (stop event reached or
    /// end of points array). AnimatingState subscribes to send animation_complete.
    /// </summary>
    public struct TrajectoryFinishedEvent : IEvent
    {
        public string shooterId;
        public int totalDamage;
    }

    /// <summary>
    /// Fired by NetClient when a player's hp changes in the state diff. Snapshots
    /// the (player, old, new, max) into a plain-C# payload so HudPanel never reads
    /// the live Colyseus schema. D.U6 HudPanel subscribes; D.U7 juice can subscribe
    /// to drive flash / shake.
    /// </summary>
    public struct HpChangedEvent : IEvent
    {
        public string playerId;
        public int oldHp;
        public int newHp;
        public int hpMax;
    }

    /// <summary>
    /// Fired by NetClient on "match_ended" inbound. Plain-C# snapshot of the
    /// server payload. EndState opens ResultPanel from this; ArenaContext.LastMatch*
    /// caches as race-fallback if event arrives before EndState.Enter.
    /// </summary>
    public struct MatchEndedEvent : IEvent
    {
        public string winnerId;
        /// <summary>'' | 'win' | 'timeout_join' | 'double_afk' | 'disconnect' | 'concede'</summary>
        public string outcome;
        public System.Collections.Generic.Dictionary<string, int> finalHp;
    }
}
