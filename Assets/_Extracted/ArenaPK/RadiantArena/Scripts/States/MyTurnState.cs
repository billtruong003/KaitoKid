#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using RadiantArena.UI;
using RadiantArena.Weapons;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// My turn. Open TurnInputPanel (Self mode), spawn ArenaAimController GO,
    /// listen for ShotReleasedEvent → Send("shoot", ShootMsg) → GoTo&lt;AnimatingState&gt;.
    /// Phase=animating fallback (server-side timeout case) also transitions.
    /// </summary>
    public class MyTurnState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;
        Action<ShotReleasedEvent>? _onShot;
        TurnInputPanel? _panel;
        ArenaAimController? _aim;

        public override void Enter()
        {
            Debug.Log("[Arena.MyTurn] Enter — opening panel + spawning aim controller");

            _panel = Bill.UI.Open<TurnInputPanel>(p => p.SetMode(TurnMode.Self));

            var go = new GameObject("[ArenaAimController]");
            _aim = go.AddComponent<ArenaAimController>();
            _aim.SetOrigin(RadiantArena.Arena.ArenaSceneBuilder.Instance?.MyVisual?.transform);

            _onPhase = e =>
            {
                if (e.newPhase == "animating") Bill.State.GoTo<AnimatingState>();
                else if (e.newPhase == "ended") Bill.State.GoTo<EndState>();
            };
            _onShot = OnShotReleased;
            Bill.Events.Subscribe(_onPhase);
            Bill.Events.Subscribe(_onShot);
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            if (_onShot != null) Bill.Events.Unsubscribe(_onShot);
            _onPhase = null;
            _onShot = null;

            if (_aim != null)
            {
                UnityEngine.Object.Destroy(_aim.gameObject);
                _aim = null;
            }
            Bill.UI.Close<TurnInputPanel>();
            _panel = null;
        }

        void OnShotReleased(ShotReleasedEvent e)
        {
            Debug.Log($"[Arena.MyTurn] shot fired angle={e.angle:F2} power={e.power:F2}");
            NetClient.Instance?.Send("shoot", new ShootMsg { angle = e.angle, power = e.power });
            Bill.State.GoTo<AnimatingState>();
        }
    }
}
