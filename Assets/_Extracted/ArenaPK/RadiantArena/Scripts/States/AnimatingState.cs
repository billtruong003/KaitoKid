#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using RadiantArena.Trajectory;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Trajectory playback phase. Subscribes to ShotResolvedEvent (fired by
    /// NetClient when "shot_resolved" arrives), spawns TrajectoryRenderer, and
    /// on completion sends "animation_complete" so the server can advance the
    /// turn. PhaseChangedEvent fallback handles the empty/missing-shot case.
    /// </summary>
    public class AnimatingState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;
        Action<ShotResolvedEvent>? _onShot;
        TrajectoryRenderer? _renderer;
        bool _sentAck;

        public override void Enter()
        {
            Debug.Log("[Arena.Animating] Enter — awaiting shot_resolved");
            _sentAck = false;
            _renderer = null;
            _onPhase = OnPhaseChanged;
            _onShot = OnShotResolved;
            Bill.Events.Subscribe(_onPhase);
            Bill.Events.Subscribe(_onShot);

            // If shot_resolved arrived before the state transition (race), the
            // last payload is sitting in ArenaContext. Re-play it from cache.
            if (ArenaContext.LastTrajectory.Length > 0 || !string.IsNullOrEmpty(ArenaContext.LastShooterId))
            {
                Debug.Log("[Arena.Animating] replaying cached LastTrajectory (arrived before Enter)");
                PlayCached();
            }
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            if (_onShot != null) Bill.Events.Unsubscribe(_onShot);
            _onPhase = null;
            _onShot = null;

            // Renderer self-destructs after grace; if we're leaving early (e.g.
            // server-forced phase change), kill the renderer GO too.
            if (_renderer != null)
            {
                UnityEngine.Object.Destroy(_renderer.gameObject);
                _renderer = null;
            }
        }

        void OnShotResolved(ShotResolvedEvent e)
        {
            Debug.Log($"[Arena.Animating] shot_resolved received — playing {e.points.Length}-point trajectory (shooter={e.shooterId}, dmg={e.damage})");
            if (_renderer != null)
            {
                Debug.LogWarning("[Arena.Animating] shot_resolved arrived while renderer already running — ignoring duplicate");
                return;
            }
            _renderer = TrajectoryRenderer.Spawn();
            _renderer.Play(e.points, e.shooterId, e.damage, e.crit, OnPlaybackComplete);
        }

        void PlayCached()
        {
            _renderer = TrajectoryRenderer.Spawn();
            _renderer.Play(
                ArenaContext.LastTrajectory,
                ArenaContext.LastShooterId,
                ArenaContext.LastShotDamage,
                ArenaContext.LastShotCrit,
                OnPlaybackComplete);
        }

        void OnPlaybackComplete()
        {
            if (_sentAck) return;
            _sentAck = true;
            Debug.Log($"[Arena.Animating] sending animation_complete (round={ArenaContext.CurrentRound})");
            NetClient.Instance?.Send("animation_complete",
                new AnimationCompleteMsg { round = ArenaContext.CurrentRound });
            // Server now switches phase=active; PhaseChangedEvent triggers Exit().
        }

        void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.newPhase == "active")
            {
                var amSelf = !string.IsNullOrEmpty(ArenaContext.TurnPlayerId)
                             && ArenaContext.TurnPlayerId == ArenaContext.MyDiscordId;
                Debug.Log($"[Arena.Animating] phase=active, turn={ArenaContext.TurnPlayerId}, mine={amSelf}");
                if (amSelf) Bill.State.GoTo<MyTurnState>();
                else Bill.State.GoTo<OpponentTurnState>();
            }
            else if (e.newPhase == "ended")
            {
                Debug.Log("[Arena.Animating] phase=ended → EndState");
                Bill.State.GoTo<EndState>();
            }
        }
    }
}
