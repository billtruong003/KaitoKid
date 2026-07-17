#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using RadiantArena.UI;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Active during DuelState.phase == "lobby". Opens LobbyPanel, routes its
    /// UI events to NetClient.Send, observes PhaseChangedEvent to know when
    /// the server flips to countdown (CountdownState handoff stubbed — D.U4
    /// will add the real transition).
    /// </summary>
    public class LobbyState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;
        LobbyPanel? _panel;

        public override void Enter()
        {
            // Defensive — close any leftover combat HUD/result if we re-entered Lobby after a match.
            if (Bill.UI.IsOpen<HudPanel>())          Bill.UI.Close<HudPanel>();
            if (Bill.UI.IsOpen<DamageNumberLayer>()) Bill.UI.Close<DamageNumberLayer>();
            if (Bill.UI.IsOpen<ResultPanel>())       Bill.UI.Close<ResultPanel>();

            var weapons = ArenaContext.MyPlayer != null
                ? ArenaContext.MyPlayer.AvailableWeapons
                : Array.Empty<WeaponSnapshot>();

            _panel = Bill.UI.Open<LobbyPanel>(p =>
            {
                p.SetSessionId(ArenaContext.SessionId);
                p.SetAvailableWeapons(weapons);
                p.OnWeaponPicked += OnWeaponPicked;
                p.OnReadyClicked += OnReadyClicked;
                p.OnUnreadyClicked += OnUnreadyClicked;
            });

            _onPhase = OnPhaseChanged;
            Bill.Events.Subscribe(_onPhase);

            Debug.Log($"[Arena.Lobby] Opened LobbyPanel, {weapons.Length} weapons available");
        }

        public override void Exit()
        {
            if (_panel != null)
            {
                _panel.OnWeaponPicked -= OnWeaponPicked;
                _panel.OnReadyClicked -= OnReadyClicked;
                _panel.OnUnreadyClicked -= OnUnreadyClicked;
            }
            Bill.UI.Close<LobbyPanel>();

            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onPhase = null;
            _panel = null;
        }

        void OnWeaponPicked(string slug)
        {
            Debug.Log($"[Arena.Lobby] pick weapon: {slug}");
            NetClient.Instance?.Send("select_weapon", new SelectWeaponMsg { slug = slug });
        }

        void OnReadyClicked()
        {
            Debug.Log("[Arena.Lobby] ready");
            NetClient.Instance?.Send("ready", new ReadyMsg());
        }

        void OnUnreadyClicked()
        {
            Debug.Log("[Arena.Lobby] unready");
            NetClient.Instance?.Send("unready", new UnreadyMsg());
        }

        void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.newPhase == "countdown")
            {
                Debug.Log("[Arena.Lobby] phase -> countdown, transitioning to CountdownState");
                Bill.State.GoTo<CountdownState>();
            }
            else if (e.newPhase == "ended")
            {
                Debug.Log("[Arena.Lobby] phase=ended → EndState");
                Bill.State.GoTo<EndState>();
            }
        }
    }
}
