#nullable enable
// Hand-mirror of arena-server/src/rooms/schemas.ts (Colyseus 0.15.x, schema v2).
// Source of truth: arena-unity/server-extract-2026-05-15.md §B.
//
// Field-order rule: Schema v2 encodes by [Type(index, ...)] — declaration order
// in this file is documentation-only; the integer index drives the wire format.
// To keep mismatch debugging cheap, declaration order DOES match the TS source.
//
// Update protocol: when arena-server's schemas.ts changes:
//   1. Re-run the server-extract recipe (see arena-unity/server-extract-*.md).
//   2. Diff the new TS schemas against this file.
//   3. Patch fields + bump Type() indices in lock-step. Never reorder existing
//      indices — append new fields at the next free index.

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace RadiantArena.Net
{
    public partial class WeaponStatsSchema : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public WeaponStatsSchema() { }

        [Type(0, "float32")] public float power = 1.0f;
        [Type(1, "float32")] public float hitbox = 1.0f;
        [Type(2, "float32")] public float bounce = 0.5f;
        [Type(3, "float32")] public float damage_base = 20f;
        [Type(4, "uint8")] public byte pierce_count = 0;
        [Type(5, "float32")] public float crit_chance = 0f;
        [Type(6, "float32")] public float crit_multi = 1.5f;
    }

    public partial class WeaponVisualSchema : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public WeaponVisualSchema() { }

        [Type(0, "string")] public string model_prefab_key = "";
        [Type(1, "string")] public string particle_fx_key = "";
        [Type(2, "string")] public string trail_fx_key = "";
        [Type(3, "string")] public string hue = "#ffffff";
    }

    public partial class WeaponSkillSchema : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public WeaponSkillSchema() { }

        [Type(0, "string")] public string skill_id = "";
        // 'passive' | 'onHit' | 'onCrit' | 'onLowHp' | 'signature' (open string per server-extract §I.7)
        [Type(1, "string")] public string trigger = "passive";
        [Type(2, "float32")] public float magnitude = 0f;
        [Type(3, "float32")] public float cooldown = 0f;
        [Type(4, "string")] public string fx_key = "";
    }

    public partial class WeaponSchema : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public WeaponSchema() { }

        [Type(0, "string")] public string slug = "";
        [Type(1, "string")] public string display_name = "";
        // 'blunt' | 'pierce' | 'spirit'
        [Type(2, "string")] public string category = "blunt";
        // 'ban_menh' | 'pham' | 'dia' | 'thien' | 'tien'
        [Type(3, "string")] public string tier = "pham";
        [Type(4, "ref", typeof(WeaponStatsSchema))] public WeaponStatsSchema stats = new WeaponStatsSchema();
        [Type(5, "ref", typeof(WeaponVisualSchema))] public WeaponVisualSchema visual = new WeaponVisualSchema();
        [Type(6, "array", typeof(ArraySchema<WeaponSkillSchema>))] public ArraySchema<WeaponSkillSchema> skills = new ArraySchema<WeaponSkillSchema>();
    }

    public partial class TrajectoryPointSchema : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public TrajectoryPointSchema() { }

        // ms since shoot
        [Type(0, "uint16")] public ushort t = 0;
        [Type(1, "float32")] public float x = 0f;
        [Type(2, "float32")] public float y = 0f;
        // '' | 'wall_bounce' | 'pierce_player' | 'hit:<dmg>' | 'crit:<dmg>' | 'stop'
        [Type(3, "string")] public string @event = "";
    }

    public partial class PlayerSchema : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public PlayerSchema() { }

        [Type(0, "string")] public string discord_id = "";
        [Type(1, "string")] public string display_name = "";
        [Type(2, "float32")] public float x = 0f;
        [Type(3, "float32")] public float y = 0f;
        [Type(4, "uint16")] public ushort hp = 100;
        [Type(5, "uint16")] public ushort hp_max = 100;
        // Weapons the player can pick from in lobby — bot fills at room create.
        [Type(6, "array", typeof(ArraySchema<WeaponSchema>))] public ArraySchema<WeaponSchema> available_weapons = new ArraySchema<WeaponSchema>();
        // Server enforces ∈ available_weapons[].slug during lobby; locked at countdown.
        [Type(7, "string")] public string selected_weapon_slug = "";
        // Cloned from available_weapons on countdown→active.
        [Type(8, "ref", typeof(WeaponSchema))] public WeaponSchema weapon = new WeaponSchema();
        [Type(9, "boolean")] public bool ready = false;
        [Type(10, "boolean")] public bool connected = true;
        // Epoch ms until which the signature skill is on cooldown.
        [Type(11, "uint32")] public uint signature_cd_until = 0;
    }

    // 'waiting'   — room created, 0 players joined
    // 'lobby'     — 1-2 players present, picking weapons
    // 'countdown' — both ready, weapons locked, 3s pre-start
    // 'active'    — turn-based combat
    // 'animating' — shot resolved, waiting for client playback confirm
    // 'ended'     — terminal, result sent to bot, room disposing
    public partial class DuelState : Schema
    {
#if UNITY_5_3_OR_NEWER
        [Preserve]
#endif
        public DuelState() { }

        [Type(0, "string")] public string session_id = "";
        [Type(1, "string")] public string phase = "waiting";
        [Type(2, "map", typeof(MapSchema<PlayerSchema>))] public MapSchema<PlayerSchema> players = new MapSchema<PlayerSchema>();
        [Type(3, "string")] public string turn_player_id = "";
        [Type(4, "uint32")] public uint turn_deadline_at = 0;
        [Type(5, "uint32")] public uint join_deadline_at = 0;
        [Type(6, "uint16")] public ushort round = 0;
        [Type(7, "uint16")] public ushort stake = 0;
        [Type(8, "array", typeof(ArraySchema<TrajectoryPointSchema>))] public ArraySchema<TrajectoryPointSchema> last_trajectory = new ArraySchema<TrajectoryPointSchema>();
        [Type(9, "string")] public string last_shooter_id = "";
        [Type(10, "string")] public string winner_id = "";
        // '' | 'win' | 'timeout_join' | 'double_afk' | 'disconnect' | 'concede'
        [Type(11, "string")] public string outcome = "";
        [Type(12, "uint16")] public ushort map_width = 1000;
        [Type(13, "uint16")] public ushort map_height = 1000;
    }
}
