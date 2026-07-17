#nullable enable
// Editor dev menu — Editor-only, no production-build cost.
// Provides single-click playtest helpers so Bill doesn't need execute_code
// reflection or arena-server connection to exercise client states.
//
// All entries require Application.isPlaying (validated via MenuItem
// validation methods so they grey out in Edit mode).
//
// Workflow per playtest:
//   1. Press Play in Unity.
//   2. Window > Radiant Arena > Dev > Mock to MyTurn (with scaffold).
//   3. Game View now in MyTurnState — drag mouse to aim, release to fire.
//
// To experiment with state transitions further, the Phase fire helpers
// below let you push specific PhaseChangedEvents without reflection.

using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using RadiantArena.States;
using UnityEditor;
using UnityEngine;

namespace RadiantArena.Editor
{
    public static class ArenaDevMenu
    {
        const string Root = "Window/Radiant Arena/Dev/";

        [MenuItem(Root + "Mock to MyTurn (with scaffold)", false, 100)]
        static void MockToMyTurn() => MockToTurn(isMe: true);

        [MenuItem(Root + "Mock to MyTurn (with scaffold)", true)]
        static bool MockToMyTurnValidate() => Application.isPlaying;

        [MenuItem(Root + "Mock to OpponentTurn (with scaffold)", false, 101)]
        static void MockToOppTurn() => MockToTurn(isMe: false);

        [MenuItem(Root + "Mock to OpponentTurn (with scaffold)", true)]
        static bool MockToOppTurnValidate() => Application.isPlaying;

        [MenuItem(Root + "Scaffold scene only", false, 110)]
        static void ScaffoldOnly() => SetupScaffold();

        [MenuItem(Root + "Scaffold scene only", true)]
        static bool ScaffoldOnlyValidate() => Application.isPlaying;

        [MenuItem(Root + "Fire phase=animating", false, 120)]
        static void FireAnimating() => FirePhase("active", "animating");

        [MenuItem(Root + "Fire phase=animating", true)]
        static bool FireAnimatingValidate() => Application.isPlaying;

        [MenuItem(Root + "Fire phase=ended", false, 121)]
        static void FireEnded() => FirePhase("animating", "ended");

        [MenuItem(Root + "Fire phase=ended", true)]
        static bool FireEndedValidate() => Application.isPlaying;

        static void MockToTurn(bool isMe)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Arena.Dev] enter Play mode first");
                return;
            }
            if (!Bill.IsReady)
            {
                Debug.LogWarning("[Arena.Dev] Bill not ready yet — give it a moment after Play.");
                return;
            }

            // Mock 6 weapons + me/opp snapshots.
            var weapons = new[]
            {
                MakeWeapon("weapon_thiet_con_01",   "Thiết Côn",   "blunt",  "pham",  "#c0c0c0"),
                MakeWeapon("weapon_chuy_01",        "Chuỳ Đồng",    "blunt",  "dia",   "#b87333"),
                MakeWeapon("weapon_kiem_01",        "Kiếm Sương",   "pierce", "thien", "#88ccff"),
                MakeWeapon("weapon_thiet_phien_01", "Thiết Phiến",  "pierce", "pham",  "#909090"),
                MakeWeapon("weapon_di_hoa_01",      "Dị Hoả",       "spirit", "thien", "#ff5544"),
                MakeWeapon("weapon_le_bang_01",     "Lệ Bằng",      "spirit", "tien",  "#ffd166"),
            };
            var me = new PlayerSnapshot { DiscordId = "billtruong", DisplayName = "Bill", AvailableWeapons = weapons, Connected = true, Hp = 100, HpMax = 100 };
            var opp = new PlayerSnapshot { DiscordId = "rival_dev", DisplayName = "Rival", Connected = true, Hp = 100, HpMax = 100 };

            ArenaContext.MyDiscordId = "billtruong";
            ArenaContext.OpponentDiscordId = "rival_dev";
            ArenaContext.SessionId = "dev_session";
            SetPrivate("MyPlayer", me);
            SetPrivate("OpponentPlayer", opp);

            // Push state graph: Lobby → Countdown → MyTurn / OpponentTurn
            ArenaContext.CurrentPhase = "lobby";
            Bill.State.GoTo<LobbyState>();

            ArenaContext.CurrentPhase = "countdown";
            ArenaContext.TurnPlayerId = isMe ? "billtruong" : "rival_dev";
            ArenaContext.TurnDeadlineAt = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeMilliseconds();
            Bill.Events.Fire(new PhaseChangedEvent { oldPhase = "lobby", newPhase = "countdown" });

            ArenaContext.CurrentPhase = "active";
            Bill.Events.Fire(new PhaseChangedEvent { oldPhase = "countdown", newPhase = "active" });

            SetupScaffold();

            var label = isMe ? "MyTurn" : "OpponentTurn";
            Debug.Log($"[Arena.Dev] Mocked to {label}. Drag in Game View now.");
        }

        static void FirePhase(string oldPhase, string newPhase)
        {
            if (!Application.isPlaying || !Bill.IsReady) return;
            ArenaContext.CurrentPhase = newPhase;
            Bill.Events.Fire(new PhaseChangedEvent { oldPhase = oldPhase, newPhase = newPhase });
            Debug.Log($"[Arena.Dev] Fired PhaseChangedEvent {oldPhase} → {newPhase}");
        }

        static WeaponSnapshot MakeWeapon(string slug, string name, string cat, string tier, string hue)
            => new WeaponSnapshot { Slug = slug, DisplayName = name, Category = cat, Tier = tier, Hue = hue };

        static void SetPrivate(string propName, object value)
        {
            var prop = typeof(ArenaContext).GetProperty(propName);
            if (prop == null) return;
            var setter = prop.GetSetMethod(true);
            setter?.Invoke(null, new[] { value });
        }

        static void SetupScaffold()
        {
            // Camera tilt to 3/4 isometric
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 8f, -5f);
                cam.transform.eulerAngles = new Vector3(50f, 0f, 0f);
                cam.fieldOfView = 50f;
            }

            // Ground plane (idempotent — destroy old then re-create)
            DestroyByName("[ArenaDevGround]");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "[ArenaDevGround]";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
            var matG = MakeUrpLitMaterial(new Color(0.18f, 0.22f, 0.28f));
            if (matG != null) ground.GetComponent<MeshRenderer>().material = matG;

            // Origin marker
            DestroyByName("[ArenaDevOriginMarker]");
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "[ArenaDevOriginMarker]";
            marker.transform.position = new Vector3(0f, 0.3f, 0f);
            marker.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            var matM = MakeUrpLitMaterial(new Color(0.4f, 0.95f, 0.55f));
            if (matM != null) marker.GetComponent<MeshRenderer>().material = matM;

            Debug.Log("[Arena.Dev] Scene scaffold ready (camera tilted + ground + origin marker)");
        }

        static void DestroyByName(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) UnityEngine.Object.Destroy(existing);
        }

        static Material? MakeUrpLitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
