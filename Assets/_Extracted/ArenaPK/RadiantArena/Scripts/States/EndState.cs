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
    /// Terminal state. Opens ResultPanel from MatchEndedEvent (or from
    /// ArenaContext.LastMatch* if the event landed before Enter — race fallback).
    /// Closes HudPanel + TurnInputPanel defensively. Replay/lobby buttons
    /// in ResultPanel just log for now (D.U10/D.U11 will wire them).
    /// </summary>
    public class EndState : GameState
    {
        Action<MatchEndedEvent>? _onMatch;
        bool _rendered;

        public override void Enter()
        {
            Debug.Log("[Arena.End] Enter — closing HUD + opening ResultPanel");
            _rendered = false;

            // Close all combat UI.
            if (Bill.UI.IsOpen<HudPanel>())          Bill.UI.Close<HudPanel>();
            if (Bill.UI.IsOpen<DamageNumberLayer>()) Bill.UI.Close<DamageNumberLayer>();
            if (Bill.UI.IsOpen<TurnInputPanel>())    Bill.UI.Close<TurnInputPanel>();

            // Open ResultPanel — may not have payload yet; race-fallback below fills it.
            var panel = Bill.UI.Open<ResultPanel>();

            _onMatch = OnMatchEnded;
            Bill.Events.Subscribe(_onMatch);

            // Race fallback — if NetClient already cached last match, render now.
            if (!string.IsNullOrEmpty(ArenaContext.LastMatchWinnerId)
                || !string.IsNullOrEmpty(ArenaContext.LastMatchOutcome))
            {
                Debug.Log("[Arena.End] replaying cached LastMatch* (arrived before Enter)");
                panel?.Render(
                    ArenaContext.LastMatchWinnerId,
                    ArenaContext.LastMatchOutcome,
                    ArenaContext.LastMatchFinalHp);
                _rendered = true;
            }
        }

        public override void Exit()
        {
            if (_onMatch != null) Bill.Events.Unsubscribe(_onMatch);
            _onMatch = null;
            if (Bill.UI.IsOpen<ResultPanel>()) Bill.UI.Close<ResultPanel>();
        }

        void OnMatchEnded(MatchEndedEvent e)
        {
            if (_rendered) return;
            _rendered = true;
            // Open<T> is idempotent (GetOrCreate + Show) so calling it again is safe.
            var panel = Bill.UI.Open<ResultPanel>();
            panel?.Render(e.winnerId, e.outcome, e.finalHp);
        }
    }
}
