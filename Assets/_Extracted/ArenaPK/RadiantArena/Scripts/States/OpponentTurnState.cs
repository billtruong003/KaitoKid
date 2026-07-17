#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.UI;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Opponent's turn. Open TurnInputPanel in Spectator mode (power gauge
    /// hidden via .spectator USS class, hint text swapped). No input — just
    /// wait for phase=animating (opponent fired or timeout).
    /// </summary>
    public class OpponentTurnState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.OpponentTurn] Enter — spectator panel");
            Bill.UI.Open<TurnInputPanel>(p => p.SetMode(TurnMode.Spectator));

            _onPhase = e =>
            {
                if (e.newPhase == "animating") Bill.State.GoTo<AnimatingState>();
                else if (e.newPhase == "ended") Bill.State.GoTo<EndState>();
            };
            Bill.Events.Subscribe(_onPhase);
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onPhase = null;
            Bill.UI.Close<TurnInputPanel>();
        }
    }
}
