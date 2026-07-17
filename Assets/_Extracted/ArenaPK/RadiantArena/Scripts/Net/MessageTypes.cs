#nullable enable
// Outbound + inbound message DTOs for Radiant Arena's Colyseus protocol.
//
// Source: arena-unity/server-extract-2026-05-15.md §C (planned for arena-server
// Lát D.4 — NOT yet shipped). NetClient subscribes to inbound types as no-op
// stubs so the wiring is mechanical when D.4 lands; gameplay láts (D.U3+) start
// using outbound types via NetClient.Send().
//
// Update protocol: when server adds a new message type / payload field,
// patch the matching struct/class here. Keep snake_case wire names — Colyseus
// uses reflection on public fields during deserialization.

using System.Collections.Generic;

namespace RadiantArena.Net
{
    // ─────────────────────────────────────────────────────────────────────────
    // Outbound — Client → Server. Sent via NetClient.Send(type, payload).
    // Use struct (no GC alloc per fire). Phase listed = server-side validation.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>type="select_weapon" · phase=lobby · server validates slug ∈ available_weapons.</summary>
    public struct SelectWeaponMsg { public string slug; }

    /// <summary>type="ready" · phase=lobby with weapon selected · both ready → countdown.</summary>
    public struct ReadyMsg { }

    /// <summary>type="unready" · phase=lobby.</summary>
    public struct UnreadyMsg { }

    /// <summary>type="shoot" · phase=active + your turn · angle ∈ [0, 2π], power ∈ [0, 1] (server clamps).</summary>
    public struct ShootMsg { public float angle; public float power; }

    /// <summary>type="signature" · phase=active + your turn + signature_cd_until elapsed.</summary>
    public struct SignatureMsg { }

    /// <summary>type="concede" · any active phase · forfeit; opponent wins.</summary>
    public struct ConcedeMsg { }

    /// <summary>type="animation_complete" · phase=animating · switches turn (with timeout fallback).</summary>
    public struct AnimationCompleteMsg { public int round; }

    /// <summary>type="ping" · any phase · server replies with pong.</summary>
    public struct PingMsg { public long t; }

    // ─────────────────────────────────────────────────────────────────────────
    // Inbound — Server → Client. NetClient registers via Room.OnMessage<T>(type, handler).
    // Use class (Colyseus deserializer instantiates via reflection on public fields).
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>type="match_start" — fired after countdown ends.</summary>
    public class MatchStartMessage
    {
        public string first_turn_id = "";
        public int seed = 0;
    }

    /// <summary>type="shot_resolved" — fired after shot simulation, contains full trajectory.</summary>
    public class ShotResolvedMessage
    {
        public TrajectoryPointSchema[]? trajectory;
        public string shooter = "";
        public float damage_dealt = 0f;
        public bool crit = false;
    }

    /// <summary>type="turn_switched" — fired after animation_complete or timeout.</summary>
    public class TurnSwitchedMessage
    {
        public string new_turn_id = "";
        public long deadline_at = 0;
        public int round = 0;
    }

    /// <summary>type="signature_used" — fired when a player triggers their signature skill.</summary>
    public class SignatureUsedMessage
    {
        public string player_id = "";
        public string skill_id = "";
        public string fx_key = "";
    }

    /// <summary>type="match_ended" — terminal broadcast, final HP per Discord ID.</summary>
    public class MatchEndedMessage
    {
        public string winner = "";
        // '' | 'win' | 'timeout_join' | 'double_afk' | 'disconnect' | 'concede'
        public string outcome = "";
        public Dictionary<string, int>? final_hp;
    }

    /// <summary>type="pong" — server reply to PingMsg.</summary>
    public class PongMessage
    {
        public long t = 0;
        public long server_t = 0;
    }

    /// <summary>type="error" — server-side validation failure. Known codes: WEAPON_NOT_OWNED, NO_WEAPON_SELECTED.</summary>
    public class ErrorMessage
    {
        public string code = "";
        // Extra fields per code documented in server-extract §C.
        public string slug = "";
    }
}
