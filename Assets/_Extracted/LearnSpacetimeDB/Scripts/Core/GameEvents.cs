#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using BillGameCore;
using UnityEngine;

namespace SpumOnline
{
    /// <summary>
    /// Custom BillGameCore events fired through Bill.Events for game-wide communication.
    /// </summary>

    /// <summary>Fired when the local player connects to SpacetimeDB.</summary>
    public struct PlayerConnectedEvent : IEvent
    {
        public string Identity;
    }

    /// <summary>Fired when the local player has been spawned in the game world.</summary>
    public struct PlayerSpawnedEvent : IEvent
    {
        public GameObject PlayerObject;
    }

    /// <summary>Fired when the local player disconnects.</summary>
    public struct PlayerDisconnectedEvent : IEvent { }

    /// <summary>Fired when subscription data has been fully applied.</summary>
    public struct SubscriptionAppliedEvent : IEvent { }

    /// <summary>Fired when any player's stats are updated (HP, MP, etc.).</summary>
    public struct PlayerStatsUpdatedEvent : IEvent
    {
        public string Identity;
    }

    /// <summary>Fired when the selected target changes.</summary>
    public struct TargetChangedEvent : IEvent
    {
        public GameObject Target;
        public string TargetName;
        public int CurrentHp;
        public int MaxHp;
    }

    /// <summary>Fired when a damage event occurs that affects the local player.</summary>
    public struct LocalPlayerDamagedEvent : IEvent
    {
        public int Damage;
    }

    /// <summary>Fired when the local player dies.</summary>
    public struct LocalPlayerDeathEvent : IEvent { }

    /// <summary>Fired when a skill is used, for UI to update cooldown display.</summary>
    public struct SkillUsedEvent : IEvent
    {
        public int SlotIndex;
        public float CooldownDuration;
    }

    /// <summary>Fired when inventory contents change.</summary>
    public struct InventoryChangedEvent : IEvent { }

    /// <summary>Fired when equipment changes.</summary>
    public struct EquipmentChangedEvent : IEvent { }

    /// <summary>Fired when a chat message is received.</summary>
    public struct ChatMessageReceivedEvent : IEvent
    {
        public string Sender;
        public string Message;
        public int Channel;
    }

    /// <summary>Fired when a loot drop appears in the world.</summary>
    public struct LootDropSpawnedEvent : IEvent
    {
        public GameObject LootObject;
    }
}

#endif // STDB_BINDINGS
