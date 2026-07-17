# 🎮 SPUM Online — Game Design Document
### 2D Pixel MMORPG với SpacetimeDB + Unity + SPUM

---

## 1. Tổng Quan Dự Án

**Tên game:** SPUM Online  
**Engine:** Unity 6 LTS (Universal 2D)  
**Backend:** SpacetimeDB (C# server module)  
**Art Asset:** SPUM (2D Pixel Unit Maker)  
**Thể loại:** 2D Top-down MMORPG  
**Mục tiêu:** Học SpacetimeDB thông qua xây dựng multiplayer game có đầy đủ các hệ thống cơ bản

---

## 2. Phân Pha Phát Triển (Milestone)

Dự án chia thành **4 phase**, mỗi phase tập trung vào 1 nhóm tính năng SpacetimeDB cụ thể. Bạn nên hoàn thành từng phase trước khi sang phase tiếp theo.

### Phase 1 — Foundation (Tuần 1–2)
> **SpacetimeDB concepts:** Tables, Reducers, Subscriptions, Identity, Client Connection

| Feature | Mô tả |
|---------|--------|
| Project Setup | Unity 2D + SpacetimeDB SDK + SPUM integration |
| Character Customization | Chọn hair, armor, weapon… lưu vào SpacetimeDB table |
| Spawn vào World | Sau customize → spawn prefab SPUM vào shared world |
| Movement Sync | WASD di chuyển, position sync qua SpacetimeDB |
| Animation Sync | Idle/Walk/Run state sync giữa các client |
| Chat System | Global chat box, tin nhắn lưu trong DB |

### Phase 2 — Combat & Interaction (Tuần 3–4)
> **SpacetimeDB concepts:** Scheduled Reducers (game tick), Complex Queries, Authorization

| Feature | Mô tả |
|---------|--------|
| Basic Stats | HP, MP, ATK, DEF, Speed — table PlayerStats |
| PvP Combat | Click target → attack, damage tính server-side |
| Skill System | 3–4 skill cơ bản (melee slash, ranged shot, heal, dash) |
| Death & Respawn | Chết → respawn tại spawn point sau 5s |
| Damage Numbers | Floating damage text hiển thị client-side |

### Phase 3 — PvE & Economy (Tuần 5–6)
> **SpacetimeDB concepts:** Scheduled Tables (mob spawn timer), Complex table relationships

| Feature | Mô tả |
|---------|--------|
| Mob Spawning | Mob NPC spawn theo config (vị trí, số lượng, loại) |
| Mob AI | Patrol → Aggro → Chase → Attack → Return |
| Loot System | Mob chết drop item vào world |
| Inventory | Grid-based inventory, pick up/drop/equip |
| Equipment | Equip weapon/armor thay đổi SPUM visual + stats |
| Item Database | Bảng items với stats, rarity, sprite info |

### Phase 4 — Admin & Stress Test (Tuần 7–8)
> **SpacetimeDB concepts:** Procedures, Admin authorization, Performance tuning

| Feature | Mô tả |
|---------|--------|
| Admin Panel | In-game debug UI để spawn mob, give item, teleport |
| Spawn Stress Test | Slider chỉnh số mob spawn (10→500) để test performance |
| Random Spawn Config | Tool tạo random mob wave với số lượng/loại ngẫu nhiên |
| Leaderboard | Kill count, damage dealt, online time |
| World Persistence | Tất cả state persist khi server restart |

---

## 3. Kiến Trúc Hệ Thống

### 3.1 SpacetimeDB Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    SpacetimeDB Host                      │
│  ┌───────────────────────────────────────────────────┐  │
│  │              Server Module (C#)                    │  │
│  │                                                    │  │
│  │  ┌──────────┐  ┌──────────┐  ┌────────────────┐  │  │
│  │  │  Tables   │  │ Reducers │  │ Scheduled Tasks│  │  │
│  │  │──────────│  │──────────│  │────────────────│  │  │
│  │  │Player    │  │move()    │  │game_tick()     │  │  │
│  │  │Position  │  │attack()  │  │spawn_mobs()    │  │  │
│  │  │Inventory │  │equip()   │  │regen_hp()      │  │  │
│  │  │MobState  │  │chat()    │  │respawn_dead()  │  │  │
│  │  │ChatMsg   │  │loot()    │  │cleanup()       │  │  │
│  │  │Item      │  │spawn_mob │  │                │  │  │
│  │  │Equipment │  │admin_*() │  │                │  │  │
│  │  └──────────┘  └──────────┘  └────────────────┘  │  │
│  └───────────────────────────────────────────────────┘  │
│         │ WebSocket (auto-sync subscribed tables)        │
│         ▼                                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ Unity Client  │  │ Unity Client  │  │ Unity Client  │  │
│  │  (Player 1)   │  │  (Player 2)   │  │  (Admin)      │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

**Nguyên lý hoạt động:**
- Client gọi **Reducer** (ví dụ: `move(direction)`) → SpacetimeDB chạy logic, update **Table**
- Table thay đổi → SpacetimeDB tự động push update đến tất cả client đã **Subscribe**
- Không cần viết networking code, serialization, hay deploy server riêng

### 3.2 Project Structure

```
spum-online/
├── Assets/
│   ├── SPUM/                      # SPUM asset (từ Asset Store)
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs           # Init connection, manage state
│   │   │   ├── NetworkConfig.cs         # HOST, DB_NAME constants
│   │   │   └── AuthManager.cs           # Token persistence
│   │   ├── Player/
│   │   │   ├── LocalPlayerController.cs # Input → call reducers
│   │   │   ├── RemotePlayerController.cs# Sync từ DB → visual
│   │   │   ├── PlayerSpawner.cs         # Spawn/despawn player prefabs
│   │   │   └── CharacterVisualSync.cs   # SPUM parts sync
│   │   ├── Combat/
│   │   │   ├── CombatManager.cs         # Handle damage events
│   │   │   ├── SkillController.cs       # Skill VFX & cooldown UI
│   │   │   └── DamagePopup.cs           # Floating numbers
│   │   ├── NPC/
│   │   │   ├── MobController.cs         # Visual sync cho mob
│   │   │   ├── MobSpawnerUI.cs          # Admin spawn tool
│   │   │   └── LootDrop.cs              # Loot visual
│   │   ├── UI/
│   │   │   ├── CharacterCustomizeUI.cs  # Customize screen
│   │   │   ├── InventoryUI.cs           # Inventory grid
│   │   │   ├── ChatUI.cs                # Chat box
│   │   │   ├── HUD.cs                   # HP/MP bars
│   │   │   ├── AdminPanel.cs            # Debug tools
│   │   │   └── LeaderboardUI.cs         # Rankings
│   │   └── Utility/
│   │       ├── SpriteUtil.cs            # SPUM sprite helpers
│   │       └── VectorExtensions.cs      # DbVector2 ↔ Vector2
│   ├── Prefabs/
│   │   ├── PlayerPrefab.prefab
│   │   ├── MobPrefab.prefab
│   │   ├── LootPrefab.prefab
│   │   └── DamagePopup.prefab
│   ├── Scenes/
│   │   ├── CharacterSelect.unity
│   │   └── GameWorld.unity
│   └── module_bindings/              # Auto-generated bởi spacetime generate
│
└── spacetimedb/                      # Server module (C#)
    ├── Lib.cs                        # Main: tables + reducers
    ├── Tables/
    │   ├── PlayerTable.cs
    │   ├── PositionTable.cs
    │   ├── InventoryTable.cs
    │   ├── ItemTable.cs
    │   ├── MobTable.cs
    │   ├── ChatTable.cs
    │   └── ConfigTable.cs
    ├── Reducers/
    │   ├── PlayerReducers.cs
    │   ├── MovementReducers.cs
    │   ├── CombatReducers.cs
    │   ├── InventoryReducers.cs
    │   ├── ChatReducers.cs
    │   ├── MobReducers.cs
    │   └── AdminReducers.cs
    └── Utils/
        ├── MathUtils.cs
        └── SpawnConfig.cs
```

---

## 4. Database Schema (SpacetimeDB Tables)

### 4.1 Player Tables

```csharp
// ===== PLAYER IDENTITY =====
[SpacetimeDB.Table(Name = "player", Public = true)]
public partial struct Player
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;          // SpacetimeDB identity (auto từ connection)

    public string Username;
    public bool IsOnline;
    public bool IsAdmin;
    public long CreatedAt;           // Unix timestamp
    public long LastLogin;
}

// ===== CHARACTER APPEARANCE (SPUM) =====
[SpacetimeDB.Table(Name = "player_appearance", Public = true)]
public partial struct PlayerAppearance
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    // SPUM part indices — mỗi field là index trong SPUM sprite array
    public int BodyType;             // 0-8
    public int EyeType;             // 0-15
    public int HairType;            // 0-47
    public int HairColor;           // RGB packed int
    public int FaceHairType;        // 0-6
    public int ClothType;           // 0-24
    public int PantType;            // 0-15
    public int HelmetType;          // 0-21 (visual only, -1 = none)
    public int ArmorType;           // 0-20 (visual only, -1 = none)
    public int WeaponType;          // 0-43 (visual only, -1 = none)
    public int BackType;            // 0-6  (-1 = none)
}

// ===== POSITION (High-frequency update) =====
[SpacetimeDB.Table(Name = "player_position", Public = true)]
public partial struct PlayerPosition
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    public float PosX;
    public float PosY;
    public float DirX;               // Movement direction (normalized)
    public float DirY;
    public int AnimState;            // 0=Idle, 1=Walk, 2=Run, 3=Attack, 4=Die
    public bool FacingRight;
}

// ===== COMBAT STATS =====
[SpacetimeDB.Table(Name = "player_stats", Public = true)]
public partial struct PlayerStats
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    public int Level;
    public int Exp;
    public int MaxHp;
    public int CurrentHp;
    public int MaxMp;
    public int CurrentMp;
    public int Atk;
    public int Def;
    public float Speed;
    public int TotalKills;
    public int TotalDeaths;
}
```

### 4.2 Item & Inventory Tables

```csharp
// ===== ITEM DEFINITIONS (Static data, set trong init reducer) =====
[SpacetimeDB.Table(Name = "item_def", Public = true)]
public partial struct ItemDef
{
    [SpacetimeDB.PrimaryKey]
    public uint ItemId;

    public string Name;
    public string Description;
    public int ItemType;             // 0=Weapon, 1=Armor, 2=Helmet, 3=Consumable, 4=Material
    public int Rarity;               // 0=Common, 1=Uncommon, 2=Rare, 3=Epic, 4=Legendary
    public int SpriteIndex;          // Index trong SPUM sprite array (hoặc custom sprite)
    public int BonusAtk;
    public int BonusDef;
    public int BonusHp;
    public int BonusMp;
    public int BonusSpeed;
    public bool Stackable;
    public int MaxStack;
}

// ===== INVENTORY =====
[SpacetimeDB.Table(Name = "inventory_slot", Public = true)]
public partial struct InventorySlot
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint SlotId;

    [SpacetimeDB.Index.BTree]
    public Identity Owner;

    public uint ItemId;              // FK → ItemDef.ItemId
    public int Quantity;
    public int SlotIndex;            // 0-29 (30 slots)
}

// ===== EQUIPMENT =====
[SpacetimeDB.Table(Name = "equipment", Public = true)]
public partial struct Equipment
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    public uint WeaponSlotId;        // FK → InventorySlot.SlotId (0 = empty)
    public uint ArmorSlotId;
    public uint HelmetSlotId;
}

// ===== LOOT DROP (Items trên mặt đất) =====
[SpacetimeDB.Table(Name = "loot_drop", Public = true)]
public partial struct LootDrop
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint DropId;

    public uint ItemId;
    public int Quantity;
    public float PosX;
    public float PosY;
    public long DroppedAt;           // Auto despawn sau 60s
}
```

### 4.3 Mob Tables

```csharp
// ===== MOB DEFINITIONS =====
[SpacetimeDB.Table(Name = "mob_def", Public = true)]
public partial struct MobDef
{
    [SpacetimeDB.PrimaryKey]
    public uint MobDefId;

    public string Name;
    public int SpritePresetIndex;    // SPUM preset cho mob
    public int MaxHp;
    public int Atk;
    public int Def;
    public float Speed;
    public float AggroRange;
    public float AttackRange;
    public int ExpReward;
    public string LootTableJson;     // JSON: [{itemId, dropRate, minQty, maxQty}]
}

// ===== ACTIVE MOBS =====
[SpacetimeDB.Table(Name = "mob_instance", Public = true)]
public partial struct MobInstance
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint MobId;

    public uint MobDefId;            // FK → MobDef
    public float PosX;
    public float PosY;
    public float SpawnX;             // Điểm spawn gốc (để return)
    public float SpawnY;
    public int CurrentHp;
    public int AiState;              // 0=Idle, 1=Patrol, 2=Chase, 3=Attack, 4=Return
    public Identity TargetPlayer;    // Player đang aggro (Identity.ZERO = none)
    public float DirX;
    public float DirY;
    public int AnimState;
    public bool FacingRight;
}

// ===== SPAWN CONFIG (Admin có thể chỉnh) =====
[SpacetimeDB.Table(Name = "spawn_config", Public = true)]
public partial struct SpawnConfig
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint ConfigId;

    public uint MobDefId;
    public float ZoneMinX;
    public float ZoneMinY;
    public float ZoneMaxX;
    public float ZoneMaxY;
    public int MaxCount;             // Max mob trong zone này
    public int RespawnTimeSec;
    public bool IsActive;
}
```

### 4.4 Chat & System Tables

```csharp
// ===== CHAT =====
[SpacetimeDB.Table(Name = "chat_message", Public = true)]
public partial struct ChatMessage
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong MessageId;

    public Identity Sender;
    public string SenderName;
    public string Content;
    public int Channel;              // 0=Global, 1=System, 2=Admin
    public long Timestamp;
}

// ===== GAME CONFIG (Singleton) =====
[SpacetimeDB.Table(Name = "game_config", Public = true)]
public partial struct GameConfig
{
    [SpacetimeDB.PrimaryKey]
    public uint Id;                  // Always 0

    public float WorldMinX;
    public float WorldMinY;
    public float WorldMaxX;
    public float WorldMaxY;
    public float SpawnPointX;
    public float SpawnPointY;
    public int MaxPlayersPerWorld;
    public int TickRateMs;           // Game tick interval (mặc định 50ms = 20 ticks/s)
}

// ===== SCHEDULED TIMERS =====
[SpacetimeDB.Table(Name = "game_tick_timer", Scheduled = "game_tick", Public = false)]
public partial struct GameTickTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public SpacetimeDB.ScheduleAt ScheduledAt;
}

[SpacetimeDB.Table(Name = "mob_spawn_timer", Scheduled = "check_mob_spawns", Public = false)]
public partial struct MobSpawnTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public SpacetimeDB.ScheduleAt ScheduledAt;
}

[SpacetimeDB.Table(Name = "loot_cleanup_timer", Scheduled = "cleanup_loot", Public = false)]
public partial struct LootCleanupTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public SpacetimeDB.ScheduleAt ScheduledAt;
}
```

---

## 5. Reducer Design (Server Logic)

### 5.1 Core Reducers

```csharp
// ===== LIFECYCLE =====
[SpacetimeDB.Reducer(ReducerKind.Init)]
public static void Init(ReducerContext ctx) { ... }
// → Seed ItemDef, MobDef, GameConfig
// → Schedule game_tick (50ms), check_mob_spawns (5s), cleanup_loot (10s)

[SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
public static void OnConnect(ReducerContext ctx) { ... }
// → Mark player online, log connection

[SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
public static void OnDisconnect(ReducerContext ctx) { ... }
// → Mark player offline, clear mob aggro targeting this player

// ===== PLAYER =====
[SpacetimeDB.Reducer]
public static void CreateCharacter(ReducerContext ctx, string username,
    int body, int eye, int hair, int hairColor, int faceHair,
    int cloth, int pant) { ... }
// → Insert Player, PlayerAppearance, PlayerPosition, PlayerStats, Equipment

[SpacetimeDB.Reducer]
public static void EnterWorld(ReducerContext ctx) { ... }
// → Set position to spawn point, mark online

// ===== MOVEMENT =====
[SpacetimeDB.Reducer]
public static void UpdateMovement(ReducerContext ctx,
    float dirX, float dirY, int animState, bool facingRight) { ... }
// → Update PlayerPosition direction & anim state
// → Actual position update happens in game_tick

// ===== COMBAT =====
[SpacetimeDB.Reducer]
public static void AttackPlayer(ReducerContext ctx, Identity targetId) { ... }
// → Range check, damage calc (atk - def), update HP
// → If target dies: award XP, increment kills/deaths

[SpacetimeDB.Reducer]
public static void AttackMob(ReducerContext ctx, uint mobId) { ... }
// → Range check, damage calc, update mob HP
// → If mob dies: generate loot drops, award XP

[SpacetimeDB.Reducer]
public static void UseSkill(ReducerContext ctx, int skillId, float targetX, float targetY) { ... }
// → MP check, cooldown check, apply skill effect

// ===== INVENTORY =====
[SpacetimeDB.Reducer]
public static void PickupLoot(ReducerContext ctx, uint dropId) { ... }
// → Range check, add to inventory, delete LootDrop

[SpacetimeDB.Reducer]
public static void DropItem(ReducerContext ctx, uint slotId) { ... }
// → Remove from inventory, create LootDrop at player pos

[SpacetimeDB.Reducer]
public static void EquipItem(ReducerContext ctx, uint slotId) { ... }
// → Move item to equipment slot, update PlayerAppearance visual
// → Recalculate stats

[SpacetimeDB.Reducer]
public static void UnequipItem(ReducerContext ctx, int equipSlot) { ... }
// → Move equipment back to inventory

// ===== CHAT =====
[SpacetimeDB.Reducer]
public static void SendChat(ReducerContext ctx, string content) { ... }
// → Validate length, insert ChatMessage

// ===== SCHEDULED (Game Tick) =====
[SpacetimeDB.Reducer]
public static void GameTick(ReducerContext ctx, GameTickTimer timer) { ... }
// → Move all players based on direction × speed × dt
// → Move mobs (AI state machine)
// → Check collisions (player↔mob, player↔loot range)
// → HP/MP regen

[SpacetimeDB.Reducer]
public static void CheckMobSpawns(ReducerContext ctx, MobSpawnTimer timer) { ... }
// → For each SpawnConfig: if active mob count < maxCount → spawn new mob

[SpacetimeDB.Reducer]
public static void CleanupLoot(ReducerContext ctx, LootCleanupTimer timer) { ... }
// → Delete LootDrop entries older than 60s
```

### 5.2 Admin Reducers

```csharp
// Helper: Check admin rights
private static void RequireAdmin(ReducerContext ctx) {
    var player = ctx.Db.player.Owner.Find(ctx.Sender);
    if (player == null || !player.Value.IsAdmin)
        throw new Exception("Unauthorized: Admin only");
}

[SpacetimeDB.Reducer]
public static void AdminSpawnMob(ReducerContext ctx,
    uint mobDefId, float x, float y, int count) { ... }
// → Spawn N mobs tại vị trí chỉ định

[SpacetimeDB.Reducer]
public static void AdminSpawnRandomWave(ReducerContext ctx,
    int totalCount, float centerX, float centerY, float radius) { ... }
// → Spawn wave ngẫu nhiên: random loại mob, random vị trí trong radius

[SpacetimeDB.Reducer]
public static void AdminGiveItem(ReducerContext ctx,
    Identity targetPlayer, uint itemId, int quantity) { ... }

[SpacetimeDB.Reducer]
public static void AdminTeleport(ReducerContext ctx,
    Identity targetPlayer, float x, float y) { ... }

[SpacetimeDB.Reducer]
public static void AdminSetStats(ReducerContext ctx,
    Identity targetPlayer, int hp, int mp, int atk, int def) { ... }

[SpacetimeDB.Reducer]
public static void AdminToggleSpawnConfig(ReducerContext ctx,
    uint configId, bool isActive) { ... }

[SpacetimeDB.Reducer]
public static void AdminUpdateSpawnConfig(ReducerContext ctx,
    uint configId, int maxCount, int respawnTimeSec) { ... }
// → Chỉnh real-time số mob spawn, không cần restart
```

---

## 6. Unity Client Architecture

### 6.1 GameManager Flow

```
App Start
  │
  ├─ Load AuthToken (file-based persistence)
  │
  ├─ DbConnection.Builder()
  │     .WithUri("ws://127.0.0.1:3000")
  │     .WithModuleName("spum-online")
  │     .WithToken(token)
  │     .OnConnect(HandleConnect)
  │     .Build()
  │
  ├─ OnConnect:
  │     └─ Subscribe to all public tables
  │
  ├─ OnSubscriptionApplied:
  │     ├─ Check if Player row exists for this Identity
  │     │     ├─ NO  → Show CharacterCustomizeUI
  │     │     └─ YES → Call EnterWorld reducer → Load GameWorld scene
  │     │
  │     └─ Register table callbacks:
  │           ├─ PlayerPosition.OnInsert / OnUpdate → Spawn/Update player visuals
  │           ├─ PlayerPosition.OnDelete → Destroy player visual
  │           ├─ MobInstance.OnInsert / OnUpdate / OnDelete → Mob visuals
  │           ├─ LootDrop.OnInsert / OnDelete → Loot visuals
  │           ├─ ChatMessage.OnInsert → Append to chat UI
  │           ├─ PlayerStats.OnUpdate → Update HUD
  │           └─ PlayerAppearance.OnUpdate → Refresh SPUM visual
  │
  └─ Update Loop:
        └─ SpacetimeDBNetworkManager processes messages automatically
```

### 6.2 SPUM Integration

```csharp
// CharacterVisualSync.cs — Áp dụng DB appearance lên SPUM prefab
public class CharacterVisualSync : MonoBehaviour
{
    private SPUM_Prefabs _spumPrefab;

    public void ApplyAppearance(PlayerAppearance appearance)
    {
        // SPUM lưu character data dưới dạng sprite path list
        // Mỗi part (hair, cloth, weapon...) là 1 sprite trong Resources/SPUM/
        // Ta map index từ DB → SPUM sprite path

        var unit = _spumPrefab.GetComponent<SPUM_SpriteList>();

        // Ví dụ mapping:
        unit._hairList[0].sprite = LoadSpumSprite("Hair", appearance.HairType);
        unit._clothList[0].sprite = LoadSpumSprite("Cloth", appearance.ClothType);
        unit._weaponList[0].sprite = LoadSpumSprite("Weapon", appearance.WeaponType);
        // ... tương tự cho các part khác

        // Recolor hair
        unit._hairList[0].color = IntToColor(appearance.HairColor);
    }

    // Khi equip item → update appearance visual
    public void ApplyEquipmentVisual(Equipment equip, ItemDef weaponDef, ItemDef armorDef)
    {
        if (equip.WeaponSlotId != 0)
            unit._weaponList[0].sprite = LoadSpumSprite("Weapon", weaponDef.SpriteIndex);
        if (equip.ArmorSlotId != 0)
            unit._armorList[0].sprite = LoadSpumSprite("Armor", armorDef.SpriteIndex);
    }

    // Animation state sync
    public void SetAnimState(int state)
    {
        switch (state)
        {
            case 0: _spumPrefab.PlayAnimation(PlayerState.IDLE); break;
            case 1: _spumPrefab.PlayAnimation(PlayerState.MOVE); break;
            case 2: _spumPrefab.PlayAnimation(PlayerState.ATTACK); break;
            case 3: _spumPrefab.PlayAnimation(PlayerState.DEATH); break;
        }
    }
}
```

### 6.3 Movement System

```
Client (LocalPlayerController):                Server (game_tick reducer):
┌──────────────────────────┐                   ┌──────────────────────────┐
│ Input: WASD/Arrow keys    │                   │ Mỗi 50ms (20 tick/s):   │
│         │                 │                   │                          │
│ Tính direction (normalized│                   │ For each online player:  │
│         │                 │                   │   pos += dir * speed * dt│
│ Call UpdateMovement()  ───┼──► Reducer ──►    │   Clamp to world bounds  │
│  (dirX, dirY, animState)  │                   │   Update PlayerPosition  │
│         │                 │                   │         │                │
│ Client prediction:        │                   └─────────┼────────────────┘
│   Move visual immediately │                             │
│         │                 │     ◄── Subscription push ──┘
│ Server reconciliation:    │
│   Lerp visual to server   │
│   position on update      │
└──────────────────────────┘

Note: Client prediction + server reconciliation tránh lag cảm giác.
      Lerp speed ≈ 10-15 để smooth.
```

---

## 7. Admin Debug Tools

### 7.1 In-Game Admin Panel (F12 toggle)

```
┌─────────────────────────────────────────────┐
│  🔧 ADMIN PANEL                        [X]  │
│─────────────────────────────────────────────│
│                                              │
│  [Mob Spawner]                               │
│  Mob Type: [▼ Slime     ]                    │
│  Count:    [████████░░] 50                   │
│  Position: [Click Map] or [Player Pos]       │
│  Radius:   [████░░░░░░] 5.0                  │
│  [Spawn Now]  [Random Wave]                  │
│                                              │
│  [Item Giver]                                │
│  Player:  [▼ Select Player ]                 │
│  Item:    [▼ Iron Sword    ]                 │
│  Qty:     [███░░░░░░░] 1                     │
│  [Give Item]                                 │
│                                              │
│  [World Controls]                            │
│  [Kill All Mobs]  [Heal All Players]         │
│  [Toggle Spawn Zone 1] [Toggle Zone 2]       │
│                                              │
│  [Stats]                                     │
│  Online Players: 12                          │
│  Active Mobs: 234                            │
│  Items on Ground: 56                         │
│  Server Tick: 20/s                           │
│                                              │
└─────────────────────────────────────────────┘
```

### 7.2 Stress Test Workflow

1. Mở Admin Panel → Mob Spawner
2. Set Count = 100, chọn "Random Wave"
3. Click "Spawn Now" → Gọi `AdminSpawnRandomWave`
4. Quan sát FPS, network bandwidth, client performance
5. Tăng dần: 100 → 200 → 500
6. SpacetimeDB handles server-side; bottleneck thường ở client rendering

---

## 8. Thư Viện & Dependencies

### 8.1 Unity Packages

| Package | Mục đích | Install |
|---------|----------|---------|
| **SpacetimeDB Unity SDK** | Core multiplayer | Unity Package Manager → Git URL: `https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk.git` |
| **SPUM** | Character art | Unity Asset Store (paid) |
| **TextMeshPro** | UI text | Built-in (Unity Registry) |
| **DOTween** | Smooth animations | Free trên Asset Store, dùng cho lerp, damage popup |
| **UniTask** | Async/await cho Unity | `https://github.com/Cysharp/UniTask.git` — optional nhưng recommended |

### 8.2 SpacetimeDB CLI

```bash
# Install SpacetimeDB CLI
# macOS / Linux:
curl -sSf https://install.spacetimedb.com | bash

# Windows (PowerShell):
iwr https://windows.spacetimedb.com -useb | iex

# Start local server
spacetime start

# Init server module (C#)
spacetime init --lang csharp --server-only spum-online

# Publish
spacetime publish --server local spum-online

# Generate client bindings
spacetime generate --lang csharp --out-dir ../Assets/module_bindings

# View logs
spacetime logs --server local spum-online

# Xóa data (fresh start)
spacetime publish --server local spum-online --delete-data
```

### 8.3 Open-Source Libraries Gợi Ý

| Library | Dùng cho | Link |
|---------|----------|------|
| **LeanTween / DOTween** | Tween cho movement lerp, UI animation | Asset Store |
| **NaughtyAttributes** | Better inspector cho debug | github.com/dbrizov/NaughtyAttributes |
| **SerializableCollections** | Dictionary serialize cho Unity | NuGet |
| **Newtonsoft.Json** | Parse loot table JSON | Unity built-in (com.unity.nuget.newtonsoft-json) |

---

## 9. Hướng Dẫn Bắt Đầu (Step-by-Step)

### Step 1: Setup

```bash
# 1. Install SpacetimeDB CLI
curl -sSf https://install.spacetimedb.com | bash

# 2. Tạo Unity Project (Universal 2D, Unity 2022.3+)
# Tên project: spum-online

# 3. Import SPUM từ Asset Store

# 4. Add SpacetimeDB SDK via Package Manager
# Window → Package Manager → + → Add package from git URL:
# https://github.com/clockworklabs/com.clockworklabs.spacetimedbsdk.git

# 5. Init server module
cd spum-online
spacetime init --lang csharp --server-only spum-online

# 6. Start local SpacetimeDB
spacetime start
```

### Step 2: Server Module Cơ Bản (Phase 1)

Bắt đầu với 3 tables (Player, PlayerPosition, ChatMessage) và 4 reducers (Init, OnConnect, CreateCharacter, UpdateMovement). Đây là đủ để test multiplayer movement sync.

### Step 3: Unity Client Cơ Bản

1. Tạo GameManager + SpacetimeDBNetworkManager
2. Connect → Subscribe → Spawn player prefabs
3. SPUM prefab cho mỗi player
4. Input → Call reducer → Server update → Subscription push → Visual update

### Step 4: Iterate

Thêm từng feature theo Milestone order. Mỗi feature mới = thêm Table + Reducer ở server, thêm UI + Controller ở client.

---

## 10. SpacetimeDB Patterns Quan Trọng

### Pattern 1: Tách table theo tần suất update
```
PlayerPosition (60Hz update)  ← Tách riêng
PlayerStats (occasional)      ← Tách riêng  
PlayerAppearance (rare)       ← Tách riêng
Player (very rare)            ← Tách riêng
```
→ Client subscribe PlayerPosition nhận update thường xuyên, nhưng không bị flood bởi data không cần thiết.

### Pattern 2: Server-authoritative
Mọi thay đổi state ĐỀU phải qua Reducer. Client KHÔNG BAO GIỜ tự modify data. Client chỉ gửi "intent" (ý định), server validate và thực hiện.

### Pattern 3: Scheduled Reducer = Game Loop
```csharp
// Game tick chạy mỗi 50ms trên server
// Tương đương Update() loop nhưng ở server-side
// Dùng cho: movement, AI, collision, regen
```

### Pattern 4: Identity-based Authorization
```csharp
// ctx.Sender là Identity của client đang gọi reducer
// LUÔN dùng ctx.Sender để xác thực, không tin parameter
[SpacetimeDB.Reducer]
public static void Move(ReducerContext ctx, float dirX, float dirY) {
    var pos = ctx.Db.player_position.Owner.Find(ctx.Sender); // ← Dùng ctx.Sender
    // KHÔNG BAO GIỜ: Move(ctx, Identity playerId, ...) rồi trust playerId
}
```

### Pattern 5: Client Cache = Local View
SDK duy trì "client cache" — bản sao local của các table đã subscribe. Query data từ cache, không cần gọi server. Data tự động sync khi server update.

---

## 11. Tổng Kết Scope

| Metric | Giá trị |
|--------|---------|
| Thời gian ước tính | 6–8 tuần (part-time) |
| Số SpacetimeDB Tables | ~15 |
| Số Reducers | ~25 |
| Số Unity Scripts | ~20 |
| Concepts học được | Tables, Reducers, Subscriptions, Scheduled Reducers, Identity/Auth, Client Cache, Code Generation |
| Scalability | SpacetimeDB xử lý hàng trăm concurrent players với schema này |

**Lời khuyên:** Đừng cố build hết 1 lần. Phase 1 (movement + chat + character) đã đủ để hiểu SpacetimeDB. Mỗi phase thêm complexity dần dần.
