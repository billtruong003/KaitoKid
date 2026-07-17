using UnityEngine;
using BillGameCore;

namespace Mythfall.Events
{
    /// <summary>Fired when the player takes damage. Subscribed by HUD HP bar, audio, vfx.</summary>
    public struct PlayerDamagedEvent : IEvent
    {
        public float damage;
        public float currentHP;
        public float maxHP;
    }

    /// <summary>Fired when player HP reaches 0. State machine routes to DefeatState.</summary>
    public struct PlayerDiedEvent : IEvent
    {
        public Mythfall.Player.PlayerBase player;
        public Vector3 position;
    }

    /// <summary>Fired every time a player attack lands on an enemy. Subscribed by VFX, audio, hitstop, damage numbers.</summary>
    public struct EnemyHitEvent : IEvent
    {
        public Mythfall.Player.PlayerBase attacker;
        public Mythfall.Enemy.EnemyBase victim;
        public float damage;
        public bool isCrit;
        public Vector3 hitPoint;
    }

    /// <summary>Fired when an enemy dies. Subscribed by XP gem spawner (Sprint 3), kill counter, audio, VFX.</summary>
    public struct EnemyKilledEvent : IEvent
    {
        public Mythfall.Enemy.EnemyBase enemy;
        public Mythfall.Player.PlayerBase killer;
        public Vector3 position;
    }

    /// <summary>Fired from CharacterSelectPanel when user picks a hero. InventoryService persists the choice.</summary>
    public struct CharacterSelectedEvent : IEvent
    {
        public string characterId;
    }

    /// <summary>Fired by GameplaySpawner once the player is alive in GameplayScene. Subscribed by HUD HP binding, camera follow (Sprint 4), and any system that needs a player reference without polling FindGameObjectWithTag.</summary>
    public struct CharacterSpawnedEvent : IEvent
    {
        public Mythfall.Player.PlayerBase player;
        public Transform transform;
        public string characterId;
    }

    /// <summary>Fired by WaveSpawner when boss timer elapses. Sprint 2 Day 2 BossController subscribes to spawn the boss prefab. Sprint 4 also subscribes for arena lighting / music cue.</summary>
    public struct BossSpawnTriggeredEvent : IEvent
    {
        public string poolKey;
        public Vector3 position;
    }

    /// <summary>Fired by polish layer to request a camera shake. Subscribed by CameraShake on Main Camera. Multiple firings stack via max() so simultaneous events don't cancel.</summary>
    public struct ScreenShakeEvent : IEvent
    {
        public float intensity;
        public float duration;
    }
}
