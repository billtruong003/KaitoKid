using UnityEngine;
using BillGameCore;

namespace BillSamples.Flappy
{
    public struct GameStartEvent : IEvent { }
    public struct ScoreChangedEvent : IEvent { public int Score; }
    public struct NewBestEvent : IEvent { public int Score; }
    public struct BirdDiedEvent : IEvent { }
    public struct BirdTapEvent : IEvent { }
}
