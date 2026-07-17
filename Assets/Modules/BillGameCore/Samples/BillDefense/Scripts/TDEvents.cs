using UnityEngine;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    public struct WaveStartEvent : IEvent { public int WaveNum; public int EnemyCount; }
    public struct WaveCompleteEvent : IEvent { public int WaveNum; public int BonusGold; public bool Perfect; }
    public struct EnemySpawnedEvent : IEvent { public string Type; public int HP; }
    public struct EnemyKilledEvent : IEvent { public string Type; public int GoldReward; public Vector3 Position; }
    public struct EnemyLeakedEvent : IEvent { public string Type; public int LivesLeft; }
    public struct TowerPlacedEvent : IEvent { public string TowerType; public Vector2Int Tile; }
    public struct TowerUpgradedEvent : IEvent { public string TowerType; public int NewLevel; }
    public struct TowerSoldEvent : IEvent { public string TowerType; public int Refund; }
    public struct GoldChangedEvent : IEvent { public int NewGold; public int Delta; }
    public struct LivesChangedEvent : IEvent { public int LivesLeft; }
    public struct BuildPhaseTimerEvent : IEvent { public float SecondsLeft; }
    public struct TDGameOverEvent : IEvent { public int WavesCompleted; }
    public struct TDVictoryEvent : IEvent { public int Stars; public int TotalKills; }
}
