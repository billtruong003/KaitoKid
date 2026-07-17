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
    /// 3-second pre-active lock. Server flips phase=countdown after both
    /// players ready in lobby; then flips phase=active after COUNTDOWN_MS.
    /// Decides MyTurn vs OpponentTurn by reading ArenaContext.TurnPlayerId
    /// (server sets at countdown→active transition).
    ///
    /// D.U6: also opens HudPanel here (canonical combat HUD owner — stays open
    /// through MyTurn/OpponentTurn/Animating, closed by EndState).
    /// </summary>
    public class CountdownState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.Countdown] Enter — server lock, awaiting phase=active");
            // D.U7 juice will add a CountdownPanel overlay with 3-2-1 big text.
            if (!Bill.UI.IsOpen<HudPanel>()) Bill.UI.Open<HudPanel>();
            if (!Bill.UI.IsOpen<DamageNumberLayer>()) Bill.UI.Open<DamageNumberLayer>();
            _onPhase = OnPhaseChanged;
            Bill.Events.Subscribe(_onPhase);
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onPhase = null;
        }

        void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.newPhase == "active")
            {
                var amSelf = !string.IsNullOrEmpty(ArenaContext.TurnPlayerId)
                             && ArenaContext.TurnPlayerId == ArenaContext.MyDiscordId;
                Debug.Log($"[Arena.Countdown] phase=active, turn={ArenaContext.TurnPlayerId}, mine={amSelf}");
                if (amSelf) Bill.State.GoTo<MyTurnState>();
                else Bill.State.GoTo<OpponentTurnState>();
            }
            else if (e.newPhase == "ended")
            {
                Debug.Log("[Arena.Countdown] phase=ended → EndState");
                Bill.State.GoTo<EndState>();
            }
        }
    }
}
