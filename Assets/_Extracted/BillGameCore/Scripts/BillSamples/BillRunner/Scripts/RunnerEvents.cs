using UnityEngine;
using BillGameCore;

namespace BillSamples.Runner
{
    public struct RunnerStartEvent : IEvent { }
    public struct DistanceChangedEvent : IEvent { public int Meters; }
    public struct CoinCollectedEvent : IEvent { public int Value; public Vector3 Position; }
    public struct ItemPickedUpEvent : IEvent { public string ItemKey; public float Duration; }
    public struct ItemExpiredEvent : IEvent { public string ItemKey; }
    public struct PlayerHurtEvent : IEvent { public int RemainingHP; }
    public struct PlayerDiedEvent : IEvent { public int Distance; public int Coins; }
    public struct SpeedChangedEvent : IEvent { public float NewSpeed; }
}
