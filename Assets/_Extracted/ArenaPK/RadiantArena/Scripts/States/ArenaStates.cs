#nullable enable
using BillGameCore;

namespace RadiantArena.States
{
    /// <summary>
    /// Centralized registration helper for all RadiantArena.States.*. Called
    /// from ArenaBootstrap.InitArena() after Bill is Ready. Future láts append
    /// new state registrations here (LobbyState, MyTurnState, AnimatingState,
    /// EndState, etc.).
    /// </summary>
    public static class ArenaStates
    {
        public static void Register()
        {
            Bill.State.AddState(new BootState());
            Bill.State.AddState(new ConnectingState());
            Bill.State.AddState(new LobbyState());
            Bill.State.AddState(new CountdownState());
            Bill.State.AddState(new MyTurnState());
            Bill.State.AddState(new OpponentTurnState());
            Bill.State.AddState(new AnimatingState());
            Bill.State.AddState(new EndState());
        }
    }
}
