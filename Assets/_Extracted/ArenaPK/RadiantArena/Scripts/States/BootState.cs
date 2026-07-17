#nullable enable
using BillGameCore;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Initial state — parses Application.absoluteURL. If the URL provided
    /// a valid (room, token) pair, hands off to ConnectingState immediately.
    /// In the Editor (URL empty), stays put and waits for ManualRoomConnect
    /// to call NetClient.ConnectAsync directly.
    ///
    /// Note: Bill.State auto-logs "[Bill.State] {from} -> {to}" on every
    /// transition (GameStateMachine.GoImpl) — no manual [Arena.State] log
    /// needed here.
    /// </summary>
    public class BootState : GameState
    {
        public override void Enter()
        {
            var info = UrlParser.Parse(Application.absoluteURL);
            ArenaContext.MyDiscordId = info.discordId;
            ArenaContext.SessionId = info.sessionId;

            Debug.Log($"[Arena.Boot] URL parsed: wsUrl={info.wsUrl}, session={info.sessionId}, " +
                      $"token=({(info.token?.Length ?? 0)} chars), discordId={info.discordId}");

            if (info.IsValid())
            {
                Debug.Log("[Arena.Boot] URL valid — transitioning to Connecting.");
                Bill.State.GoTo<ConnectingState>();
            }
            else
            {
                Debug.Log("[Arena.Boot] URL not valid — staying in Boot. " +
                          "Use Window > Radiant Arena > Manual Room Connect to connect manually.");
            }
        }
    }
}
