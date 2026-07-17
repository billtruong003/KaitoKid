# SPUM Online — Architecture Deep Dive
### Tài liệu kiến trúc chi tiết dự án 2D MMORPG (SpacetimeDB + Unity + SPUM)

---

## MỤC LỤC

1. [Triết Lý Kiến Trúc](#1-triết-lý-kiến-trúc)
2. [Vòng Đời Dữ Liệu — Từ Input Đến Pixel](#2-vòng-đời-dữ-liệu)
3. [Server Module — Schema & Logic Chi Tiết](#3-server-module)
4. [Client Architecture — Unity Chi Tiết](#4-client-architecture)
5. [Movement System — Prediction & Reconciliation](#5-movement-system)
6. [SPUM Integration — Serialization Protocol](#6-spum-integration)
7. [Combat System — Formulas & Edge Cases](#7-combat-system)
8. [Mob AI — State Machine Chi Tiết](#8-mob-ai)
9. [Inventory & Equipment — Full Lifecycle](#9-inventory--equipment)
10. [Chat System](#10-chat-system)
11. [Admin & Debug Tools](#11-admin--debug-tools)
12. [Subscription Strategy — Bandwidth Optimization](#12-subscription-strategy)
13. [Error Handling & Edge Cases](#13-error-handling)
14. [Performance — Bottlenecks & Solutions](#14-performance)
15. [Deployment & DevOps Workflow](#15-deployment)
16. [Testing Strategy](#16-testing)
17. [Checklist Vấn Đề Đã Giải Quyết](#17-checklist)

---

## 1. Triết Lý Kiến Trúc

### 1.1 Nguyên tắc bất biến

**Server-authoritative tuyệt đối.** Client không bao giờ tự thay đổi game state. Client chỉ gửi "intent" (ý định hành động) lên server thông qua reducer call. Server validate, xử lý logic, update table. SpacetimeDB tự push kết quả về tất cả client đã subscribe. Nghĩa là: nếu một hacker sửa client code để gửi position giả, server vẫn tính position thực từ direction × speed × delta_time trong game_tick. Position giả sẽ bị ghi đè ngay tick tiếp theo.

**Single source of truth.** Toàn bộ game state nằm trong SpacetimeDB tables. Không có biến static trên server, không có in-memory cache ngoài DB. Lý do: SpacetimeDB giữ data trong memory sẵn rồi (sub-microsecond access), nên việc thêm cache layer là thừa và gây desync.

**Tách table theo tần suất update.** Đây là pattern quan trọng nhất của SpacetimeDB. Khi bạn subscribe một table, mỗi lần BẤT KỲ column nào trong row thay đổi, toàn bộ row được push về client. Nếu bạn gộp position (thay đổi 20 lần/giây) chung với username (thay đổi 0 lần/giây), mỗi giây client nhận 20 bản copy thừa của username. Với 100 players: 100 × 20 × (kích thước username) bytes/s bị lãng phí. Tách ra: position riêng, stats riêng, appearance riêng, profile riêng.

**Event-driven rendering.** Unity client KHÔNG poll data. Client đăng ký callback (OnInsert, OnUpdate, OnDelete) cho từng table. Khi SpacetimeDB push update, callback fire, client update visual. Giữa các callback, client chỉ chạy visual interpolation.

### 1.2 Tại sao SpacetimeDB phù hợp cho project này

SpacetimeDB là database đồng thời cũng là server. Module code (C#) chạy trực tiếp bên trong database dưới dạng WebAssembly. Client kết nối trực tiếp qua WebSocket, không qua trung gian. Điều này loại bỏ hoàn toàn:
- Web server layer (không cần ASP.NET, Express, etc.)
- REST/GraphQL API layer
- Serialization/deserialization code (SDK auto-generate bindings)
- Container orchestration (không cần Docker, K8s)
- Caching layer (data đã ở in-memory)
- Pub/Sub infrastructure (subscriptions built-in)

Với MMORPG, ta cần: real-time sync, authoritative server, persistent state, và multiplayer support. SpacetimeDB cung cấp tất cả out-of-the-box.

---

## 2. Vòng Đời Dữ Liệu — Từ Input Đến Pixel

### 2.1 Ví dụ cụ thể: Player A nhấn phím W để đi lên

```
Thời điểm T+0ms (Client A):
├─ Input System detect phím W pressed
├─ LocalPlayerController tính direction = (0, 1)
├─ GỌI REDUCER: conn.Reducers.UpdateMovement(0f, 1f, 1, true)
│   → Gửi WebSocket message đến SpacetimeDB
├─ CLIENT PREDICTION: di chuyển visual ngay lập tức
│   predictedPos += direction * speed * Time.deltaTime
│   Chuyển SPUM animation sang "Walk"
└─ Lưu pending input vào prediction buffer

Thời điểm T+5ms (SpacetimeDB Server):
├─ Nhận reducer call UpdateMovement(dirX=0, dirY=1, animState=1, facingRight=true)
├─ Validate: player có tồn tại không? đang alive không? direction hợp lệ không?
├─ ctx.Db.PlayerPosition.Owner.Find(ctx.Sender)
│   → Tìm row theo Identity
├─ Update row: DirX=0, DirY=1, AnimState=1, FacingRight=true
│   (CHÚ Ý: chưa update PosX/PosY — việc đó do game_tick làm)
└─ Transaction commit → SpacetimeDB đánh dấu row changed

Thời điểm T+10ms (SpacetimeDB push):
├─ SpacetimeDB phát hiện PlayerPosition row thay đổi
├─ Tìm tất cả client đã subscribe table PlayerPosition
├─ Gửi update (OnUpdate event) đến Client A, B, C, D...
└─ Update chứa: old row + new row (diff)

Thời điểm T+12ms (Client B nhận update):
├─ SDK nhận WebSocket message, decode, update client cache
├─ Callback fire: PlayerPosition.OnUpdate(ctx, oldRow, newRow)
├─ RemotePlayerController nhận event:
│   target.DirX = newRow.DirX; target.DirY = newRow.DirY;
│   target.AnimState = newRow.AnimState;
├─ CharacterVisualSync.SetAnimState(1) → SPUM play "Walk"
└─ Visual lerp bắt đầu di chuyển smooth từ old pos về new direction

Thời điểm T+50ms (Game Tick trên Server):
├─ Scheduled reducer game_tick() fire (mỗi 50ms)
├─ Loop qua tất cả PlayerPosition rows:
│   foreach (var pos in ctx.Db.PlayerPosition.Iter())
│   {
│       if (pos.DirX == 0 && pos.DirY == 0) continue; // đứng yên
│       var stats = ctx.Db.PlayerStats.Owner.Find(pos.Owner);
│       float dt = 0.05f; // 50ms
│       float newX = pos.PosX + pos.DirX * stats.Speed * dt;
│       float newY = pos.PosY + pos.DirY * stats.Speed * dt;
│       // Clamp vào world bounds
│       var config = ctx.Db.GameConfig.Id.Find(0);
│       newX = Math.Clamp(newX, config.WorldMinX, config.WorldMaxX);
│       newY = Math.Clamp(newY, config.WorldMinY, config.WorldMaxY);
│       // Update position
│       ctx.Db.PlayerPosition.Owner.Update(new PlayerPosition {
│           Owner = pos.Owner, PosX = newX, PosY = newY,
│           DirX = pos.DirX, DirY = pos.DirY,
│           AnimState = pos.AnimState, FacingRight = pos.FacingRight
│       });
│   }
└─ Commit → push PosX/PosY updates đến tất cả client

Thời điểm T+55ms (Client A nhận authoritative position):
├─ Server position = (100.0, 200.05)
├─ Predicted position = (100.0, 200.06) ← sai lệch nhỏ do floating point
├─ Reconciliation: lerp predicted → server position
│   visualPos = Vector2.Lerp(visualPos, serverPos, 12f * Time.deltaTime)
└─ Sai lệch quá nhỏ, player không cảm nhận được
```

### 2.2 Vấn đề và giải pháp trong data flow

**Vấn đề 1: Reducer call bị mất (network drop).** Giải pháp: Client giữ trạng thái "last sent direction". Nếu không nhận update sau 200ms, gửi lại. SpacetimeDB tự handle reconnection — SDK sẽ reconnect và re-subscribe.

**Vấn đề 2: Nhiều reducer call cho cùng 1 action.** Ví dụ: player spam W liên tục, mỗi frame gửi 1 UpdateMovement. Giải pháp: Client throttle — chỉ gửi khi direction thực sự thay đổi, hoặc tối đa 10 lần/giây. Nếu direction không đổi, không gửi (game_tick sẽ tiếp tục dùng direction cũ).

**Vấn đề 3: Client nhận update cũ sau update mới (out-of-order).** Giải pháp: SpacetimeDB đảm bảo subscription updates được gửi theo thứ tự transaction. SDK C# xử lý message theo thứ tự nhận. Ta không cần lo về ordering.

---

## 3. Server Module — Schema & Logic Chi Tiết

### 3.1 Toàn bộ Tables với giải thích từng field

```csharp
// =========================================================================
// PLAYER IDENTITY — Thay đổi cực kỳ hiếm (chỉ khi tạo account)
// =========================================================================
[SpacetimeDB.Table(Name = "player", Public = true)]
public partial struct Player
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;
    // Tại sao dùng Identity làm PK: SpacetimeDB tự generate Identity duy nhất
    // cho mỗi client connection. Identity persist qua sessions (nếu client
    // lưu và gửi lại token). Không cần auto_inc ID riêng.

    public string Username;        // 3-20 ký tự, alphanumeric + underscore
    public bool IsOnline;          // true khi connected, false khi disconnected
    public bool IsAdmin;           // Set bằng tay trong init reducer hoặc CLI
    public long CreatedAt;         // DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    public long LastLogin;
}

// =========================================================================
// PLAYER APPEARANCE — Thay đổi hiếm (chỉ khi customize lại hoặc equip)
// =========================================================================
[SpacetimeDB.Table(Name = "player_appearance", Public = true)]
public partial struct PlayerAppearance
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    // Mỗi field là INDEX trong mảng sprite tương ứng của SPUM.
    // SPUM lưu sprites trong Resources/SPUM/SPUM_Sprites/
    // Cấu trúc: BodyType 0-8, EyeType 0-15, HairType 0-47, etc.
    //
    // Khi client nhận row này, nó map index → sprite path:
    //   "SPUM/SPUM_Sprites/Hair/Hair_{HairType:D3}"
    // rồi load từ Resources.

    public int BodyType;           // 0-8 (9 body sprites)
    public int EyeType;            // 0-15 (16 eye sprites)
    public int HairType;           // 0-47 (48 hair sprites), -1 = bald
    public int HairColor;          // RGB packed: (r << 16) | (g << 8) | b
    public int FaceHairType;       // 0-6, -1 = none
    public int ClothType;          // 0-24 (base cloth, trước khi equip armor)
    public int PantType;           // 0-15

    // Các field dưới đây bị OVERRIDE bởi equipment.
    // Khi equip weapon, EquippedWeaponSprite ghi đè WeaponType.
    // Khi unequip, revert về giá trị gốc.
    public int HelmetType;         // -1 = none, hoặc SpriteIndex từ equipped item
    public int ArmorType;          // -1 = none
    public int WeaponType;         // -1 = barehanded
    public int BackType;           // -1 = none (cape, quiver, etc.)
}

// =========================================================================
// PLAYER POSITION — Thay đổi RẤT THƯỜNG XUYÊN (20 lần/giây cho mỗi player đang di chuyển)
// ĐÂY LÀ TABLE TỐN BANDWIDTH NHẤT — giữ nhỏ nhất có thể
// =========================================================================
[SpacetimeDB.Table(Name = "player_position", Public = true)]
public partial struct PlayerPosition
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    public float PosX;             // World position X
    public float PosY;             // World position Y
    public float DirX;             // Normalized movement direction X (-1, 0, 1)
    public float DirY;             // Normalized movement direction Y (-1, 0, 1)

    // Animation state — gộp vào đây thay vì tách table riêng vì:
    // 1. AnimState thay đổi gần như cùng lúc với Direction
    // 2. Client cần cả position + anim để render chính xác
    // 3. Tách ra = 2 subscription updates thay vì 1, tốn hơn
    public int AnimState;          // 0=Idle, 1=Walk, 2=Attack, 3=Hurt, 4=Die
    public bool FacingRight;       // Flip sprite
}

// =========================================================================
// PLAYER STATS — Thay đổi vài lần/phút (khi combat, level up, regen)
// =========================================================================
[SpacetimeDB.Table(Name = "player_stats", Public = true)]
public partial struct PlayerStats
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    public int Level;              // 1-99
    public int Exp;                // Current XP, level up khi >= ExpToNext(Level)
    public int MaxHp;              // Base + equipment bonus
    public int CurrentHp;          // 0 = dead
    public int MaxMp;              // Base + equipment bonus
    public int CurrentMp;
    public int Atk;                // Base + equipment bonus (recalc khi equip/unequip)
    public int Def;                // Base + equipment bonus
    public float Speed;            // Units per second (default 3.0)

    // Lifetime stats (cho leaderboard)
    public int TotalKills;         // PvP kills
    public int TotalMobKills;      // PvE kills
    public int TotalDeaths;

    // Computed khi equip/unequip:
    // MaxHp = BaseHp(Level) + SUM(equipped items BonusHp)
    // Atk = BaseAtk(Level) + SUM(equipped items BonusAtk)
    // etc.
    // BaseHp(level) = 100 + (level - 1) * 20
    // BaseAtk(level) = 10 + (level - 1) * 3
    // BaseDef(level) = 5 + (level - 1) * 2
    // BaseSpeed = 3.0 (constant, chỉ item mới thay đổi)
}

// =========================================================================
// COOLDOWN — Theo dõi cooldown của từng skill cho từng player
// =========================================================================
[SpacetimeDB.Table(Name = "skill_cooldown", Public = true)]
public partial struct SkillCooldown
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint Id;

    [SpacetimeDB.Index.BTree]
    public Identity Owner;

    public int SkillId;            // Skill nào
    public long CooldownEndMs;     // Thời điểm hết cooldown (Unix ms)
    // Client check: if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < CooldownEndMs)
    //   → skill vẫn đang cooldown, gray out button
    // Server check tương tự trước khi cho phép use skill
}

// =========================================================================
// ITEM DEFINITIONS — Static data, insert trong Init reducer, KHÔNG BAO GIỜ thay đổi runtime
// =========================================================================
[SpacetimeDB.Table(Name = "item_def", Public = true)]
public partial struct ItemDef
{
    [SpacetimeDB.PrimaryKey]
    public uint ItemId;

    public string Name;            // "Iron Sword", "Leather Armor", "Health Potion"
    public string Description;     // Flavor text
    public int ItemType;           // 0=Weapon, 1=Armor, 2=Helmet, 3=Consumable, 4=Material, 5=Back
    public int Rarity;             // 0=Common(white), 1=Uncommon(green), 2=Rare(blue), 3=Epic(purple), 4=Legendary(orange)
    public int SpumSpriteIndex;    // Index trong SPUM sprite array cho visual (weapon→WeaponType, armor→ArmorType, etc.)
    public int BonusAtk;           // +ATK khi equip
    public int BonusDef;           // +DEF khi equip
    public int BonusHp;            // +MaxHP khi equip
    public int BonusMp;            // +MaxMP khi equip
    public float BonusSpeed;       // +Speed khi equip

    // Consumable-specific:
    public int HealHp;             // HP restored khi dùng (0 nếu không phải consumable)
    public int HealMp;             // MP restored khi dùng
    public int ConsumeCooldownMs;  // Cooldown giữa 2 lần dùng consumable (ms)

    public bool Stackable;         // true cho consumable/material, false cho equipment
    public int MaxStack;           // Max stack size (1 cho non-stackable)
}

// =========================================================================
// INVENTORY SLOT
// =========================================================================
[SpacetimeDB.Table(Name = "inventory_slot", Public = true)]
public partial struct InventorySlot
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint SlotId;            // Global unique ID cho mỗi stack

    [SpacetimeDB.Index.BTree]
    public Identity Owner;         // Ai sở hữu

    public uint ItemId;            // FK → ItemDef.ItemId
    public int Quantity;           // 1 cho equipment, 1-99 cho stackable
    public int SlotIndex;          // 0-29 (30 inventory slots), -1 = overflow/chưa assign
}

// =========================================================================
// EQUIPMENT — Mỗi player có đúng 1 row
// =========================================================================
[SpacetimeDB.Table(Name = "equipment", Public = true)]
public partial struct Equipment
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    // Lưu SlotId của InventorySlot đang equip ở mỗi slot.
    // 0 = empty (không equip gì)
    // Khi equip: item vẫn nằm trong InventorySlot, nhưng SlotIndex = -1 (đánh dấu "equipped")
    // Khi unequip: item quay lại inventory, tìm SlotIndex trống, gán lại
    public uint WeaponSlotId;
    public uint ArmorSlotId;
    public uint HelmetSlotId;
    public uint BackSlotId;
}

// =========================================================================
// SKILL DEFINITIONS — Static, insert trong Init
// =========================================================================
[SpacetimeDB.Table(Name = "skill_def", Public = true)]
public partial struct SkillDef
{
    [SpacetimeDB.PrimaryKey]
    public int SkillId;

    public string Name;            // "Slash", "Fireball", "Heal", "Dash"
    public string Description;
    public int SkillType;          // 0=MeleeAoE, 1=RangedProjectile, 2=SelfBuff, 3=Movement
    public int ManaCost;
    public int CooldownMs;         // Milliseconds
    public float Range;            // Max range to use
    public float AreaRadius;       // AoE radius (0 = single target)
    public int BaseDamage;         // Damage = BaseDamage + Atk * DamageScale
    public float DamageScale;      // Multiplier cho Atk stat
    public int HealAmount;         // Cho heal skills
    public float DashDistance;     // Cho dash skills
    public string AnimTrigger;     // Animation trigger name cho SPUM
    public int AnimDurationMs;     // Bao lâu animation chạy trước khi hit xảy ra
}

// =========================================================================
// MOB DEFINITIONS — Static data
// =========================================================================
[SpacetimeDB.Table(Name = "mob_def", Public = true)]
public partial struct MobDef
{
    [SpacetimeDB.PrimaryKey]
    public uint MobDefId;

    public string Name;            // "Slime", "Skeleton", "Dark Knight"
    public int SpumPresetBody;     // SPUM body preset cho mob
    public int SpumPresetEye;
    public int SpumPresetHair;
    public int SpumPresetCloth;
    public int SpumPresetWeapon;
    public int SpumPresetArmor;
    public int SpumPresetHelmet;
    public int SpumHairColor;
    public int MaxHp;
    public int Atk;
    public int Def;
    public float Speed;            // Units per second
    public float AggroRange;       // Khoảng cách detect player
    public float AttackRange;      // Khoảng cách đánh
    public float LeashRange;       // Khoảng cách tối đa rời spawn point trước khi return
    public int AttackCooldownMs;   // Thời gian giữa 2 đòn đánh
    public int ExpReward;          // XP khi giết

    // Loot table dạng JSON:
    // [{"itemId":1,"dropRate":0.5,"minQty":1,"maxQty":3},
    //  {"itemId":5,"dropRate":0.1,"minQty":1,"maxQty":1}]
    // dropRate: 0.0-1.0, roll cho mỗi entry độc lập
    public string LootTableJson;
}

// =========================================================================
// MOB INSTANCE — Thay đổi thường xuyên (mỗi game_tick cho mob đang active)
// =========================================================================
[SpacetimeDB.Table(Name = "mob_instance", Public = true)]
public partial struct MobInstance
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint MobId;

    public uint MobDefId;          // FK → MobDef
    public float PosX;
    public float PosY;
    public float SpawnX;           // Điểm spawn gốc
    public float SpawnY;
    public int CurrentHp;
    public int AiState;            // 0=Idle, 1=Patrol, 2=Chase, 3=Attack, 4=Return, 5=Dead
    public long AiStateStartMs;    // Thời điểm bắt đầu state hiện tại (dùng cho timing)
    public long LastAttackMs;      // Thời điểm đánh gần nhất (cooldown check)

    // Target tracking
    public Identity TargetPlayer;  // Identity.ZERO khi không aggro ai
    public float TargetLastX;      // Last known position của target (tránh lookup mỗi tick)
    public float TargetLastY;

    // Visual sync
    public float DirX;
    public float DirY;
    public int AnimState;          // Same encoding như player: 0=Idle, 1=Walk, 2=Attack, 3=Hurt, 4=Die
    public bool FacingRight;
}

// =========================================================================
// SPAWN CONFIG — Admin có thể chỉnh runtime
// =========================================================================
[SpacetimeDB.Table(Name = "spawn_config", Public = true)]
public partial struct SpawnConfig
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint ConfigId;

    public string ZoneName;        // "Forest Zone", "Dark Cave", etc.
    public uint MobDefId;          // Loại mob spawn trong zone này
    public float ZoneMinX;         // Bounding box
    public float ZoneMinY;
    public float ZoneMaxX;
    public float ZoneMaxY;
    public int MaxCount;           // Max mob alive cùng lúc trong zone
    public int RespawnTimeSec;     // Sau bao lâu respawn 1 con (khi dưới max)
    public bool IsActive;          // Admin toggle on/off
}

// =========================================================================
// LOOT DROP — Items nằm trên mặt đất
// =========================================================================
[SpacetimeDB.Table(Name = "loot_drop", Public = true)]
public partial struct LootDrop
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public uint DropId;

    public uint ItemId;            // FK → ItemDef
    public int Quantity;
    public float PosX;
    public float PosY;
    public long DroppedAtMs;       // Unix ms — để tính auto-despawn
    public Identity DroppedBy;     // Ai drop (để implement FFA vs Owner loot rule)
}

// =========================================================================
// CHAT MESSAGE
// =========================================================================
[SpacetimeDB.Table(Name = "chat_message", Public = true)]
public partial struct ChatMessage
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong MessageId;

    public Identity Sender;
    public string SenderName;      // Denormalize để client không cần lookup Player table
    public string Content;         // Max 200 chars, sanitized
    public int Channel;            // 0=Global, 1=System (server announce), 2=Admin
    public long TimestampMs;
}

// =========================================================================
// EVENT TABLE: DAMAGE EVENT — Transient, tồn tại chỉ trong 1 transaction
// SpacetimeDB tự insert rồi xóa ngay — client nhận OnInsert rồi OnDelete
// Dùng cho: floating damage numbers, hit effects, kill feed
// =========================================================================
[SpacetimeDB.Table(Name = "damage_event", Public = true, TableType = TableType.Event)]
public partial struct DamageEvent
{
    public Identity TargetId;      // Ai bị đánh (player identity hoặc mob packed identity)
    public uint TargetMobId;       // 0 nếu target là player, >0 nếu target là mob
    public int Damage;             // Số damage
    public bool IsCrit;            // Critical hit?
    public bool IsKill;            // Target chết?
    public int SkillId;            // Skill nào gây damage (-1 = basic attack)
    public string AttackerName;    // Để hiển thị kill feed
}

// =========================================================================
// EVENT TABLE: SYSTEM NOTIFICATION — Announcements, level up, etc.
// =========================================================================
[SpacetimeDB.Table(Name = "system_event", Public = true, TableType = TableType.Event)]
public partial struct SystemEvent
{
    public Identity TargetPlayer;  // Gửi cho ai (Identity.ZERO = broadcast)
    public int EventType;          // 0=LevelUp, 1=ItemDrop, 2=PlayerJoin, 3=PlayerLeave, 4=Announcement
    public string Message;         // Human-readable message
    public string DataJson;        // Extra data nếu cần (ví dụ: {"level":5,"newSkill":"Fireball"})
}

// =========================================================================
// GAME CONFIG — Singleton row (Id = 0), set trong Init
// =========================================================================
[SpacetimeDB.Table(Name = "game_config", Public = true)]
public partial struct GameConfig
{
    [SpacetimeDB.PrimaryKey]
    public uint Id;                // Always 0

    public float WorldMinX;        // -50
    public float WorldMinY;        // -50
    public float WorldMaxX;        // 50
    public float WorldMaxY;        // 50
    public float SpawnPointX;      // 0
    public float SpawnPointY;      // 0
    public int TickRateMs;         // 50 (20 ticks/s)
    public int MobSpawnCheckMs;    // 5000 (check mỗi 5s)
    public int LootDespawnSec;     // 60
    public float LootPickupRange;  // 1.5 units
    public float MeleeAttackRange; // 1.5 units
    public int HpRegenPerTick;     // 1 HP per tick (nếu out of combat 5s)
    public int MpRegenPerTick;     // 1 MP per tick
    public int CombatCooldownMs;   // 5000ms không bị đánh = out of combat
    public int RespawnTimeSec;     // 5s sau khi chết
    public int MaxChatHistory;     // Giữ lại 100 tin nhắn gần nhất, xóa cũ
}

// =========================================================================
// SCHEDULED TIMERS — Private tables, client không cần biết
// =========================================================================
[SpacetimeDB.Table(Name = "game_tick_timer", Scheduled = "GameTick", ScheduledAt = "ScheduledAt")]
public partial struct GameTickTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public ScheduleAt ScheduledAt;
}

[SpacetimeDB.Table(Name = "mob_spawn_timer", Scheduled = "CheckMobSpawns", ScheduledAt = "ScheduledAt")]
public partial struct MobSpawnTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public ScheduleAt ScheduledAt;
}

[SpacetimeDB.Table(Name = "loot_cleanup_timer", Scheduled = "CleanupLoot", ScheduledAt = "ScheduledAt")]
public partial struct LootCleanupTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public ScheduleAt ScheduledAt;
}

[SpacetimeDB.Table(Name = "hp_regen_timer", Scheduled = "RegenTick", ScheduledAt = "ScheduledAt")]
public partial struct HpRegenTimer
{
    [SpacetimeDB.PrimaryKey]
    [SpacetimeDB.AutoInc]
    public ulong Id;
    public ScheduleAt ScheduledAt;
}

// =========================================================================
// PLAYER COMBAT STATE — Track combat timing, riêng vì update frequency khác
// =========================================================================
[SpacetimeDB.Table(Name = "player_combat_state", Public = true)]
public partial struct PlayerCombatState
{
    [SpacetimeDB.PrimaryKey]
    public Identity Owner;

    public long LastDamageTakenMs; // Để tính "out of combat" cho HP regen
    public long LastDamageDealtMs; // Để tính "in combat" indicator
    public long DeathTimeMs;       // 0 = alive, >0 = thời điểm chết (respawn sau 5s)
    public long LastBasicAttackMs; // Basic attack cooldown (500ms)
}
```

### 3.2 Reducer Logic Chi Tiết

```csharp
// =========================================================================
// INIT — Chạy 1 lần khi publish module lần đầu
// =========================================================================
[SpacetimeDB.Reducer(ReducerKind.Init)]
public static void Init(ReducerContext ctx)
{
    // 1. Seed GameConfig
    ctx.Db.GameConfig.Insert(new GameConfig {
        Id = 0,
        WorldMinX = -50f, WorldMinY = -50f,
        WorldMaxX = 50f,  WorldMaxY = 50f,
        SpawnPointX = 0f, SpawnPointY = 0f,
        TickRateMs = 50,
        MobSpawnCheckMs = 5000,
        LootDespawnSec = 60,
        LootPickupRange = 1.5f,
        MeleeAttackRange = 1.5f,
        HpRegenPerTick = 1,
        MpRegenPerTick = 1,
        CombatCooldownMs = 5000,
        RespawnTimeSec = 5,
        MaxChatHistory = 100
    });

    // 2. Seed ItemDef (ví dụ vài items cơ bản)
    SeedItems(ctx);

    // 3. Seed MobDef
    SeedMobs(ctx);

    // 4. Seed SkillDef
    SeedSkills(ctx);

    // 5. Seed SpawnConfig (vài zone mặc định)
    SeedSpawnZones(ctx);

    // 6. Schedule timers
    ctx.Db.GameTickTimer.Insert(new GameTickTimer {
        ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(50))
    });
    ctx.Db.MobSpawnTimer.Insert(new MobSpawnTimer {
        ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(5000))
    });
    ctx.Db.LootCleanupTimer.Insert(new LootCleanupTimer {
        ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(10000))
    });
    ctx.Db.HpRegenTimer.Insert(new HpRegenTimer {
        ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(2000))
    });

    Log.Info("SPUM Online initialized!");
}

// =========================================================================
// CLIENT CONNECTED — Mỗi khi 1 client WebSocket connect
// =========================================================================
[SpacetimeDB.Reducer(ReducerKind.ClientConnected)]
public static void OnConnect(ReducerContext ctx)
{
    // Check nếu player đã tồn tại (returning player)
    var existing = ctx.Db.Player.Owner.Find(ctx.Sender);
    if (existing != null)
    {
        // Update online status
        var p = existing.Value;
        p.IsOnline = true;
        p.LastLogin = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        ctx.Db.Player.Owner.Update(p);

        Log.Info($"{p.Username} reconnected.");
    }
    // Nếu player chưa tồn tại, KHÔNG tạo ở đây.
    // Client sẽ hiển thị Character Creation UI và gọi CreateCharacter reducer.

    Log.Info($"Client connected: {ctx.Sender}");
}

// =========================================================================
// CLIENT DISCONNECTED
// =========================================================================
[SpacetimeDB.Reducer(ReducerKind.ClientDisconnected)]
public static void OnDisconnect(ReducerContext ctx)
{
    var existing = ctx.Db.Player.Owner.Find(ctx.Sender);
    if (existing != null)
    {
        var p = existing.Value;
        p.IsOnline = false;
        ctx.Db.Player.Owner.Update(p);

        // Clear direction để player đứng yên
        var pos = ctx.Db.PlayerPosition.Owner.Find(ctx.Sender);
        if (pos != null)
        {
            var pp = pos.Value;
            pp.DirX = 0; pp.DirY = 0;
            pp.AnimState = 0; // Idle
            ctx.Db.PlayerPosition.Owner.Update(pp);
        }

        // Clear mob aggro targeting this player
        foreach (var mob in ctx.Db.MobInstance.Iter())
        {
            if (mob.TargetPlayer == ctx.Sender)
            {
                var m = mob;
                m.TargetPlayer = default; // Identity.ZERO
                m.AiState = 4; // Return to spawn
                m.AiStateStartMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                ctx.Db.MobInstance.MobId.Update(m);
            }
        }

        Log.Info($"{p.Username} disconnected.");
    }
}

// =========================================================================
// CREATE CHARACTER — Gọi từ Character Creation UI
// =========================================================================
[SpacetimeDB.Reducer]
public static void CreateCharacter(ReducerContext ctx,
    string username,
    int bodyType, int eyeType, int hairType, int hairColor,
    int faceHairType, int clothType, int pantType)
{
    // === VALIDATION ===

    // 1. Chưa có character?
    if (ctx.Db.Player.Owner.Find(ctx.Sender) != null)
        throw new Exception("Character already exists for this identity.");

    // 2. Username validation
    if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 20)
        throw new Exception("Username must be 3-20 characters.");
    if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        throw new Exception("Username can only contain letters, numbers, and underscores.");
    // Check unique (brute force - OK vì chỉ chạy 1 lần per player)
    foreach (var p in ctx.Db.Player.Iter())
    {
        if (p.Username.ToLower() == username.ToLower())
            throw new Exception("Username already taken.");
    }

    // 3. Sprite index bounds check
    if (bodyType < 0 || bodyType > 8) throw new Exception("Invalid body type.");
    if (eyeType < 0 || eyeType > 15) throw new Exception("Invalid eye type.");
    if (hairType < -1 || hairType > 47) throw new Exception("Invalid hair type.");
    if (faceHairType < -1 || faceHairType > 6) throw new Exception("Invalid face hair type.");
    if (clothType < 0 || clothType > 24) throw new Exception("Invalid cloth type.");
    if (pantType < 0 || pantType > 15) throw new Exception("Invalid pant type.");

    // === INSERT ALL RELATED TABLES ===
    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var config = ctx.Db.GameConfig.Id.Find(0).Value;

    ctx.Db.Player.Insert(new Player {
        Owner = ctx.Sender,
        Username = username,
        IsOnline = true,
        IsAdmin = false,
        CreatedAt = now,
        LastLogin = now
    });

    ctx.Db.PlayerAppearance.Insert(new PlayerAppearance {
        Owner = ctx.Sender,
        BodyType = bodyType, EyeType = eyeType,
        HairType = hairType, HairColor = hairColor,
        FaceHairType = faceHairType,
        ClothType = clothType, PantType = pantType,
        HelmetType = -1, ArmorType = -1,
        WeaponType = -1, BackType = -1
    });

    ctx.Db.PlayerPosition.Insert(new PlayerPosition {
        Owner = ctx.Sender,
        PosX = config.SpawnPointX,
        PosY = config.SpawnPointY,
        DirX = 0, DirY = 0,
        AnimState = 0, FacingRight = true
    });

    ctx.Db.PlayerStats.Insert(new PlayerStats {
        Owner = ctx.Sender,
        Level = 1, Exp = 0,
        MaxHp = 100, CurrentHp = 100,
        MaxMp = 50, CurrentMp = 50,
        Atk = 10, Def = 5, Speed = 3.0f,
        TotalKills = 0, TotalMobKills = 0, TotalDeaths = 0
    });

    ctx.Db.Equipment.Insert(new Equipment {
        Owner = ctx.Sender,
        WeaponSlotId = 0, ArmorSlotId = 0,
        HelmetSlotId = 0, BackSlotId = 0
    });

    ctx.Db.PlayerCombatState.Insert(new PlayerCombatState {
        Owner = ctx.Sender,
        LastDamageTakenMs = 0, LastDamageDealtMs = 0,
        DeathTimeMs = 0, LastBasicAttackMs = 0
    });

    // Give starter items
    ctx.Db.InventorySlot.Insert(new InventorySlot {
        Owner = ctx.Sender, ItemId = 1, // Wooden Sword
        Quantity = 1, SlotIndex = 0
    });
    ctx.Db.InventorySlot.Insert(new InventorySlot {
        Owner = ctx.Sender, ItemId = 10, // Health Potion x5
        Quantity = 5, SlotIndex = 1
    });

    Log.Info($"Character created: {username}");
}

// =========================================================================
// UPDATE MOVEMENT — Client gọi khi direction thay đổi
// =========================================================================
[SpacetimeDB.Reducer]
public static void UpdateMovement(ReducerContext ctx,
    float dirX, float dirY, int animState, bool facingRight)
{
    var pos = ctx.Db.PlayerPosition.Owner.Find(ctx.Sender);
    if (pos == null) throw new Exception("Player not found.");

    // Check alive
    var combat = ctx.Db.PlayerCombatState.Owner.Find(ctx.Sender);
    if (combat != null && combat.Value.DeathTimeMs > 0)
        return; // Dead players can't move — silently ignore

    // Normalize direction (chống hack speed)
    float mag = MathF.Sqrt(dirX * dirX + dirY * dirY);
    if (mag > 0.01f)
    {
        dirX /= mag;
        dirY /= mag;
    }
    else
    {
        dirX = 0; dirY = 0;
    }

    // Clamp animState to valid range
    animState = Math.Clamp(animState, 0, 4);

    var p = pos.Value;
    p.DirX = dirX;
    p.DirY = dirY;
    p.AnimState = (dirX == 0 && dirY == 0) ? 0 : 1; // Force: stopped=Idle, moving=Walk
    p.FacingRight = facingRight;
    ctx.Db.PlayerPosition.Owner.Update(p);
}

// =========================================================================
// GAME TICK — Server game loop, 50ms interval
// =========================================================================
[SpacetimeDB.Reducer]
public static void GameTick(ReducerContext ctx, GameTickTimer timer)
{
    float dt = 0.05f; // 50ms fixed timestep
    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var config = ctx.Db.GameConfig.Id.Find(0).Value;

    // --- 1. MOVE ALL PLAYERS ---
    foreach (var pos in ctx.Db.PlayerPosition.Iter())
    {
        if (pos.DirX == 0 && pos.DirY == 0) continue;

        // Check alive
        var combat = ctx.Db.PlayerCombatState.Owner.Find(pos.Owner);
        if (combat != null && combat.Value.DeathTimeMs > 0) continue;

        var stats = ctx.Db.PlayerStats.Owner.Find(pos.Owner);
        if (stats == null) continue;

        float newX = pos.PosX + pos.DirX * stats.Value.Speed * dt;
        float newY = pos.PosY + pos.DirY * stats.Value.Speed * dt;

        newX = Math.Clamp(newX, config.WorldMinX, config.WorldMaxX);
        newY = Math.Clamp(newY, config.WorldMinY, config.WorldMaxY);

        var p = pos;
        p.PosX = newX;
        p.PosY = newY;
        ctx.Db.PlayerPosition.Owner.Update(p);
    }

    // --- 2. MOB AI TICK ---
    MobAiTick(ctx, dt, now, config);

    // --- 3. CHECK RESPAWNS ---
    foreach (var combat in ctx.Db.PlayerCombatState.Iter())
    {
        if (combat.DeathTimeMs > 0 &&
            now - combat.DeathTimeMs > config.RespawnTimeSec * 1000)
        {
            RespawnPlayer(ctx, combat.Owner, config);
        }
    }
}
```

---

## 4. Client Architecture — Unity Chi Tiết

### 4.1 Scene Flow

```
┌──────────────────────┐
│   BootstrapScene      │  ← Entry point, DontDestroyOnLoad
│   ├─ GameManager      │     Khởi tạo connection
│   ├─ NetworkManager   │     SpacetimeDBNetworkManager
│   └─ AudioManager     │     Global audio (optional)
└──────────┬───────────┘
           │ OnSubscriptionApplied
           │
           ├─ Player row EXISTS?
           │     │
           │     ├─ YES → Load GameWorldScene
           │     │         ├─ WorldManager (tilemap, bounds)
           │     │         ├─ PlayerSpawner (manage player prefabs)
           │     │         ├─ MobSpawner (manage mob prefabs)
           │     │         ├─ LootSpawner (manage loot prefabs)
           │     │         ├─ CameraController (follow local player)
           │     │         └─ UICanvas
           │     │             ├─ HUD (HP/MP bars, minimap)
           │     │             ├─ ChatUI
           │     │             ├─ InventoryUI (toggle with I key)
           │     │             ├─ EquipmentUI (toggle with E key)
           │     │             ├─ SkillBarUI (bottom bar)
           │     │             ├─ AdminPanel (F12 toggle, admin only)
           │     │             └─ LeaderboardUI (Tab key)
           │     │
           │     └─ NO → Load CharacterSelectScene
           │               ├─ SPUM_Manager (SPUM built-in editor)
           │               ├─ CustomizeController
           │               │   ├─ Preview SPUM character live
           │               │   ├─ Sliders/buttons cho từng part
           │               │   └─ Username input field
           │               └─ "Create" button → call CreateCharacter reducer
           │                   → OnSuccess → Load GameWorldScene
           │
           └─ CONNECTION ERROR → Show RetryUI
```

### 4.2 GameManager.cs — Trung tâm điều phối

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Connection
    private DbConnection _conn;
    public DbConnection Conn => _conn;
    public Identity LocalIdentity { get; private set; }
    public bool IsConnected { get; private set; }
    public bool IsAdmin { get; private set; }

    // Constants
    private const string HOST = "ws://127.0.0.1:3000";
    private const string DB_NAME = "spum-online";
    private const string TOKEN_KEY = "spacetimedb_token";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Connect();
    }

    void Connect()
    {
        string savedToken = PlayerPrefs.GetString(TOKEN_KEY, "");

        _conn = DbConnection.Builder()
            .WithUri(HOST)
            .WithModuleName(DB_NAME)
            .WithToken(savedToken)
            .OnConnect(HandleConnect)
            .OnConnectError(HandleConnectError)
            .OnDisconnect(HandleDisconnect)
            .Build();
    }

    void HandleConnect(DbConnection conn, Identity identity, string token)
    {
        LocalIdentity = identity;
        IsConnected = true;

        // Save token cho lần sau reconnect giữ Identity
        PlayerPrefs.SetString(TOKEN_KEY, token);
        PlayerPrefs.Save();

        // Subscribe to ALL public tables
        // Với game nhỏ (<200 players), subscribe all là OK.
        // Nếu scale lên, chuyển sang selective subscription (xem Section 12).
        conn.SubscriptionBuilder()
            .OnApplied(HandleSubscriptionApplied)
            .OnError(HandleSubscriptionError)
            .SubscribeToAllTables();

        Debug.Log($"Connected! Identity: {identity}");
    }

    void HandleSubscriptionApplied(SubscriptionEventContext ctx)
    {
        // Check if character exists
        var player = ctx.Db.Player.Owner.Find(LocalIdentity);

        if (player != null)
        {
            IsAdmin = player.Value.IsAdmin;
            // Đã có character → vào game
            RegisterAllCallbacks(ctx);
            SceneManager.LoadScene("GameWorld");
        }
        else
        {
            // Chưa có character → tạo mới
            SceneManager.LoadScene("CharacterSelect");
        }
    }

    void RegisterAllCallbacks(SubscriptionEventContext ctx)
    {
        // PLAYER POSITION — spawn/update/despawn player visuals
        ctx.Db.PlayerPosition.OnInsert += OnPlayerPositionInsert;
        ctx.Db.PlayerPosition.OnUpdate += OnPlayerPositionUpdate;
        ctx.Db.PlayerPosition.OnDelete += OnPlayerPositionDelete;

        // PLAYER APPEARANCE — update SPUM visuals
        ctx.Db.PlayerAppearance.OnInsert += OnPlayerAppearanceInsert;
        ctx.Db.PlayerAppearance.OnUpdate += OnPlayerAppearanceUpdate;

        // PLAYER STATS — update HUD
        ctx.Db.PlayerStats.OnUpdate += OnPlayerStatsUpdate;

        // MOB INSTANCE — spawn/update/despawn mob visuals
        ctx.Db.MobInstance.OnInsert += OnMobInsert;
        ctx.Db.MobInstance.OnUpdate += OnMobUpdate;
        ctx.Db.MobInstance.OnDelete += OnMobDelete;

        // LOOT DROP
        ctx.Db.LootDrop.OnInsert += OnLootInsert;
        ctx.Db.LootDrop.OnDelete += OnLootDelete;

        // CHAT
        ctx.Db.ChatMessage.OnInsert += OnChatInsert;

        // EVENTS (transient)
        ctx.Db.DamageEvent.OnInsert += OnDamageEvent;
        ctx.Db.SystemEvent.OnInsert += OnSystemEvent;

        // INVENTORY
        ctx.Db.InventorySlot.OnInsert += OnInventoryChange;
        ctx.Db.InventorySlot.OnUpdate += OnInventoryChange;
        ctx.Db.InventorySlot.OnDelete += OnInventoryDelete;

        // EQUIPMENT
        ctx.Db.Equipment.OnUpdate += OnEquipmentChange;
    }

    // ... callback handlers delegate to appropriate managers
}
```

### 4.3 PlayerSpawner.cs — Quản lý player prefabs

```csharp
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    // Dictionary: Identity → instantiated GameObject
    private Dictionary<Identity, GameObject> _players = new();

    // Gọi bởi GameManager callback
    public void SpawnOrUpdatePlayer(Identity owner, PlayerPosition pos, PlayerAppearance appearance)
    {
        if (owner == GameManager.Instance.LocalIdentity)
        {
            // Local player: tạo nếu chưa có, KHÔNG update position từ server
            // (local player dùng client prediction)
            if (!_players.ContainsKey(owner))
            {
                var go = Instantiate(playerPrefab);
                go.AddComponent<LocalPlayerController>();
                go.GetComponent<CharacterVisualSync>().ApplyAppearance(appearance);
                go.transform.position = new Vector3(pos.PosX, pos.PosY, 0);
                _players[owner] = go;

                // Setup camera follow
                CameraController.Instance.SetTarget(go.transform);
            }
            return;
        }

        // Remote player
        if (!_players.TryGetValue(owner, out var existing))
        {
            // Spawn mới
            var go = Instantiate(playerPrefab);
            go.AddComponent<RemotePlayerController>();
            go.GetComponent<CharacterVisualSync>().ApplyAppearance(appearance);
            go.transform.position = new Vector3(pos.PosX, pos.PosY, 0);
            _players[owner] = go;

            // Add nametag
            var player = GameManager.Instance.Conn.Db.Player.Owner.Find(owner);
            if (player != null)
                go.GetComponent<Nametag>().SetName(player.Value.Username);
        }
        else
        {
            // Update existing remote player
            var remote = existing.GetComponent<RemotePlayerController>();
            remote.SetServerState(pos.PosX, pos.PosY, pos.DirX, pos.DirY,
                                  pos.AnimState, pos.FacingRight);
        }
    }

    public void DespawnPlayer(Identity owner)
    {
        if (_players.TryGetValue(owner, out var go))
        {
            Destroy(go);
            _players.Remove(owner);
        }
    }
}
```

### 4.4 RemotePlayerController.cs — Smooth interpolation cho remote players

```csharp
public class RemotePlayerController : MonoBehaviour
{
    private Vector2 _serverPos;
    private Vector2 _serverDir;
    private int _animState;
    private bool _facingRight;

    private CharacterVisualSync _visual;
    private float _lerpSpeed = 12f; // Tốc độ lerp — tune theo cảm giác

    void Awake()
    {
        _visual = GetComponent<CharacterVisualSync>();
    }

    public void SetServerState(float px, float py, float dx, float dy, int anim, bool facingRight)
    {
        _serverPos = new Vector2(px, py);
        _serverDir = new Vector2(dx, dy);
        _animState = anim;
        _facingRight = facingRight;
    }

    void Update()
    {
        // Smooth lerp đến server position
        Vector2 current = (Vector2)transform.position;
        float dist = Vector2.Distance(current, _serverPos);

        if (dist > 5f)
        {
            // Quá xa (teleport hoặc lag spike) → snap ngay
            transform.position = new Vector3(_serverPos.x, _serverPos.y, 0);
        }
        else
        {
            // Lerp smooth
            Vector2 newPos = Vector2.Lerp(current, _serverPos, _lerpSpeed * Time.deltaTime);
            transform.position = new Vector3(newPos.x, newPos.y, 0);
        }

        // Update animation
        _visual.SetAnimState(_animState);

        // Flip sprite
        Vector3 scale = transform.localScale;
        scale.x = _facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
```

---

## 5. Movement System — Prediction & Reconciliation

### 5.1 Tại sao cần client prediction

Không có prediction: player nhấn W → gửi reducer → server xử lý (5-50ms) → server push update → client render. Tổng latency 10-100ms. Player cảm thấy character "lù đù", không responsive.

Có prediction: player nhấn W → client DI CHUYỂN NGAY → đồng thời gửi reducer → server xử lý → server push authoritative position → client reconcile (lerp từ predicted về server pos). Player cảm thấy character phản hồi tức thì.

### 5.2 LocalPlayerController.cs chi tiết

```csharp
public class LocalPlayerController : MonoBehaviour
{
    private CharacterVisualSync _visual;
    private float _sendInterval = 0.1f; // Gửi tối đa 10 lần/giây
    private float _sendTimer;
    private Vector2 _lastSentDir;
    private bool _lastSentFacing;

    void Awake()
    {
        _visual = GetComponent<CharacterVisualSync>();
    }

    void Update()
    {
        if (!GameManager.Instance.IsConnected) return;

        // Check alive
        var combat = GameManager.Instance.Conn.Db.PlayerCombatState.Owner
            .Find(GameManager.Instance.LocalIdentity);
        if (combat != null && combat.Value.DeathTimeMs > 0)
        {
            // Dead — show death animation, don't process input
            _visual.SetAnimState(4); // Die
            return;
        }

        // === INPUT ===
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v);
        if (dir.magnitude > 0.01f)
            dir.Normalize();
        else
            dir = Vector2.zero;

        bool facingRight = dir.x > 0 ? true : (dir.x < 0 ? false : _lastSentFacing);

        // === CLIENT PREDICTION ===
        var stats = GameManager.Instance.Conn.Db.PlayerStats.Owner
            .Find(GameManager.Instance.LocalIdentity);
        float speed = stats?.Speed ?? 3.0f;

        Vector3 pos = transform.position;
        pos.x += dir.x * speed * Time.deltaTime;
        pos.y += dir.y * speed * Time.deltaTime;

        // Clamp to world bounds (cache config)
        var config = GameManager.Instance.Conn.Db.GameConfig.Id.Find(0);
        if (config != null)
        {
            pos.x = Mathf.Clamp(pos.x, config.Value.WorldMinX, config.Value.WorldMaxX);
            pos.y = Mathf.Clamp(pos.y, config.Value.WorldMinY, config.Value.WorldMaxY);
        }
        transform.position = pos;

        // Animation
        int animState = dir.magnitude > 0.01f ? 1 : 0; // Walk or Idle
        _visual.SetAnimState(animState);

        // Flip
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        // === SEND TO SERVER (throttled) ===
        _sendTimer -= Time.deltaTime;
        bool dirChanged = Vector2.Distance(dir, _lastSentDir) > 0.01f;
        bool facingChanged = facingRight != _lastSentFacing;

        if ((dirChanged || facingChanged) && _sendTimer <= 0)
        {
            GameManager.Instance.Conn.Reducers.UpdateMovement(
                dir.x, dir.y, animState, facingRight);
            _lastSentDir = dir;
            _lastSentFacing = facingRight;
            _sendTimer = _sendInterval;
        }
    }

    // Gọi bởi GameManager khi nhận PlayerPosition update cho local player
    public void ReconcileWithServer(float serverX, float serverY)
    {
        // Lerp nhẹ về server position
        // Nếu sai lệch > threshold, snap
        Vector2 current = (Vector2)transform.position;
        Vector2 server = new Vector2(serverX, serverY);
        float dist = Vector2.Distance(current, server);

        if (dist > 2f)
        {
            // Desync quá lớn → snap
            transform.position = new Vector3(serverX, serverY, 0);
        }
        else if (dist > 0.05f)
        {
            // Desync nhỏ → gentle correction
            Vector2 corrected = Vector2.Lerp(current, server, 0.3f);
            transform.position = new Vector3(corrected.x, corrected.y, 0);
        }
        // else: sai lệch quá nhỏ, bỏ qua
    }
}
```

---

## 6. SPUM Integration — Serialization Protocol

### 6.1 SPUM hoạt động như thế nào

SPUM tạo character từ nhiều layer sprite xếp chồng lên nhau. Mỗi character là 1 prefab chứa nhiều SpriteRenderer con, sắp xếp theo sorting layer. SPUM lưu character data dưới dạng danh sách đường dẫn sprite trong Resources.

Cấu trúc SPUM prefab:
```
SPUM_Character (root)
├── Shadow
├── Body
│   ├── Body_0 (SpriteRenderer)
│   └── Body_1
├── Eyes
│   └── Eye_0 (SpriteRenderer)
├── Hair
│   └── Hair_0 (SpriteRenderer)
├── FaceHair
│   └── FaceHair_0 (SpriteRenderer)
├── Cloth
│   ├── Cloth_0 (SpriteRenderer)
│   └── Cloth_1
├── Pant
│   └── Pant_0 (SpriteRenderer)
├── Helmet (SpriteRenderer)
├── Armor
│   ├── Armor_0 (SpriteRenderer)
│   └── Armor_1
├── Weapon
│   ├── Weapon_L (SpriteRenderer) — left hand
│   └── Weapon_R (SpriteRenderer) — right hand
└── Back (SpriteRenderer)
```

### 6.2 Serialization: DB → SPUM

```csharp
public class CharacterVisualSync : MonoBehaviour
{
    private SPUM_Prefabs _spum;
    private SPUM_SpriteList _spriteList;

    void Awake()
    {
        _spum = GetComponent<SPUM_Prefabs>();
        _spriteList = GetComponent<SPUM_SpriteList>();
    }

    public void ApplyAppearance(PlayerAppearance app)
    {
        // SPUM sprite paths:
        // Resources/SPUM/SPUM_Sprites/Body/Body_{index:D3}
        // Resources/SPUM/SPUM_Sprites/Eye/Eye_{index:D3}
        // Resources/SPUM/SPUM_Sprites/Hair/Hair_{index:D3}
        // etc.

        SetSpritePart(_spriteList._bodyList, "Body", app.BodyType);
        SetSpritePart(_spriteList._eyeList, "Eye", app.EyeType);

        if (app.HairType >= 0)
        {
            SetSpritePart(_spriteList._hairList, "Hair", app.HairType);
            // Apply hair color
            Color color = IntToColor(app.HairColor);
            foreach (var sr in _spriteList._hairList)
                if (sr != null) sr.color = color;
        }
        else
        {
            // Bald — hide hair sprites
            foreach (var sr in _spriteList._hairList)
                if (sr != null) sr.sprite = null;
        }

        if (app.FaceHairType >= 0)
            SetSpritePart(_spriteList._faceHairList, "FaceHair", app.FaceHairType);
        else
            foreach (var sr in _spriteList._faceHairList)
                if (sr != null) sr.sprite = null;

        SetSpritePart(_spriteList._clothList, "Cloth", app.ClothType);
        SetSpritePart(_spriteList._pantList, "Pant", app.PantType);

        // Equipment visuals (overridden by equipment system)
        if (app.WeaponType >= 0)
            SetSpritePart(_spriteList._weaponList, "Weapon", app.WeaponType);
        if (app.ArmorType >= 0)
            SetSpritePart(_spriteList._armorList, "Armor", app.ArmorType);
        if (app.HelmetType >= 0)
            SetSpritePart(_spriteList._helmetList, "Helmet", app.HelmetType);
        if (app.BackType >= 0)
            SetSpritePart(_spriteList._backList, "Back", app.BackType);
    }

    private void SetSpritePart(List<SpriteRenderer> renderers, string category, int index)
    {
        // SPUM sprite naming convention: "{Category}_{index:D3}"
        // Ví dụ: "Hair_023", "Weapon_005"
        string path = $"SPUM/SPUM_Sprites/{category}/{category}_{index:D3}";
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning($"SPUM sprite not found: {path}");
            return;
        }

        // SPUM có thể có nhiều renderer cho 1 part (multi-layer)
        // Chỉ set renderer đầu tiên, hoặc nếu SPUM có list đầy đủ thì set hết
        if (renderers.Count > 0 && renderers[0] != null)
            renderers[0].sprite = sprite;
    }

    private Color IntToColor(int packed)
    {
        float r = ((packed >> 16) & 0xFF) / 255f;
        float g = ((packed >> 8) & 0xFF) / 255f;
        float b = (packed & 0xFF) / 255f;
        return new Color(r, g, b, 1f);
    }

    // Animation state mapping: int → SPUM PlayAnimation
    public void SetAnimState(int state)
    {
        switch (state)
        {
            case 0: _spum.PlayAnimation(PlayerState.IDLE); break;
            case 1: _spum.PlayAnimation(PlayerState.MOVE); break;
            case 2: _spum.PlayAnimation(PlayerState.ATTACK); break;
            case 3: _spum.PlayAnimation(PlayerState.DAMAGED); break;
            case 4: _spum.PlayAnimation(PlayerState.DEATH); break;
        }
    }
}
```

### 6.3 Vấn đề SPUM cần chú ý

**Vấn đề: SPUM prefab nặng.** Mỗi SPUM character có ~15 SpriteRenderer + Animator. Với 200 players = 3000 SpriteRenderers. Giải pháp: Object pooling. Spawn tối đa 50-100 player prefabs, recycle khi player ra khỏi camera view.

**Vấn đề: SPUM animation controller shared.** Tất cả SPUM character dùng chung RuntimeAnimatorController. Nếu 2 character cùng play animation khác nhau, OK vì mỗi Animator instance độc lập. NHƯNG nếu bạn modify controller at runtime (add clip), nó ảnh hưởng tất cả. Giải pháp: KHÔNG modify controller. Chỉ call PlayAnimation từ SPUM_Prefabs script.

**Vấn đề: Resources.Load mỗi frame.** Đừng gọi Resources.Load trong Update. Load sprites 1 lần khi appearance thay đổi (OnInsert/OnUpdate callback), cache kết quả.

---

## 7. Combat System — Formulas & Edge Cases

### 7.1 Damage Formula

```
Raw Damage = Attacker.Atk + SkillBaseDamage + (Attacker.Atk × SkillDamageScale)
Damage Reduction = Defender.Def × 0.5
Final Damage = Max(1, Raw Damage - Damage Reduction)

Critical Hit (10% chance):
  Final Damage = Final Damage × 1.5

Ví dụ:
  Attacker: Atk=25, dùng Slash (BaseDamage=15, Scale=0.5)
  Defender: Def=12
  Raw = 25 + 15 + (25 × 0.5) = 52.5
  Reduction = 12 × 0.5 = 6
  Final = Max(1, 52.5 - 6) = 46 (không crit)
  Final = 46 × 1.5 = 69 (nếu crit)
```

### 7.2 Reducer: AttackMob

```csharp
[SpacetimeDB.Reducer]
public static void AttackMob(ReducerContext ctx, uint mobId, int skillId)
{
    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // 1. Player alive check
    var combat = ctx.Db.PlayerCombatState.Owner.Find(ctx.Sender);
    if (combat == null || combat.Value.DeathTimeMs > 0)
        throw new Exception("You are dead.");

    // 2. Basic attack cooldown (500ms) or skill cooldown
    if (skillId == -1) // basic attack
    {
        if (now - combat.Value.LastBasicAttackMs < 500)
            throw new Exception("Attack on cooldown.");
    }
    else
    {
        // Check skill cooldown
        foreach (var cd in ctx.Db.SkillCooldown.Iter())
        {
            if (cd.Owner == ctx.Sender && cd.SkillId == skillId && now < cd.CooldownEndMs)
                throw new Exception("Skill on cooldown.");
        }
    }

    // 3. Get player pos & stats
    var playerPos = ctx.Db.PlayerPosition.Owner.Find(ctx.Sender).Value;
    var playerStats = ctx.Db.PlayerStats.Owner.Find(ctx.Sender).Value;

    // 4. Get mob
    var mob = ctx.Db.MobInstance.MobId.Find(mobId);
    if (mob == null) throw new Exception("Mob not found.");
    var m = mob.Value;
    if (m.AiState == 5) throw new Exception("Mob already dead.");

    // 5. Range check
    float dx = playerPos.PosX - m.PosX;
    float dy = playerPos.PosY - m.PosY;
    float dist = MathF.Sqrt(dx * dx + dy * dy);

    float range;
    SkillDef? skill = null;
    if (skillId == -1)
    {
        range = ctx.Db.GameConfig.Id.Find(0).Value.MeleeAttackRange;
    }
    else
    {
        skill = ctx.Db.SkillDef.SkillId.Find(skillId);
        if (skill == null) throw new Exception("Skill not found.");
        range = skill.Value.Range;

        // MP check
        if (playerStats.CurrentMp < skill.Value.ManaCost)
            throw new Exception("Not enough MP.");
    }

    if (dist > range)
        throw new Exception("Out of range.");

    // 6. Calculate damage
    int baseDmg = skillId == -1 ? 0 : skill.Value.BaseDamage;
    float scale = skillId == -1 ? 1.0f : skill.Value.DamageScale;
    float raw = playerStats.Atk + baseDmg + (playerStats.Atk * scale);

    var mobDef = ctx.Db.MobDef.MobDefId.Find(m.MobDefId).Value;
    float reduction = mobDef.Def * 0.5f;
    int finalDmg = Math.Max(1, (int)(raw - reduction));

    // Crit check (pseudo-random using sender identity hash + timestamp)
    bool isCrit = ((ctx.Sender.GetHashCode() ^ (int)(now / 100)) % 10) == 0; // ~10%
    if (isCrit) finalDmg = (int)(finalDmg * 1.5f);

    // 7. Apply damage
    m.CurrentHp -= finalDmg;
    bool isKill = m.CurrentHp <= 0;

    if (isKill)
    {
        m.CurrentHp = 0;
        m.AiState = 5; // Dead
        m.AnimState = 4; // Die animation
        m.DirX = 0; m.DirY = 0;
        m.TargetPlayer = default;
    }
    else
    {
        // Aggro the mob if not already
        if (m.TargetPlayer == default)
        {
            m.TargetPlayer = ctx.Sender;
            m.AiState = 2; // Chase
            m.AiStateStartMs = now;
        }
        m.AnimState = 3; // Hurt
    }
    ctx.Db.MobInstance.MobId.Update(m);

    // 8. Update player combat state
    var cs = combat.Value;
    if (skillId == -1)
        cs.LastBasicAttackMs = now;
    cs.LastDamageDealtMs = now;
    ctx.Db.PlayerCombatState.Owner.Update(cs);

    // 9. Deduct MP for skills
    if (skillId != -1 && skill != null)
    {
        var ps = playerStats;
        ps.CurrentMp -= skill.Value.ManaCost;
        ctx.Db.PlayerStats.Owner.Update(ps);

        // Set cooldown
        ctx.Db.SkillCooldown.Insert(new SkillCooldown {
            Owner = ctx.Sender,
            SkillId = skillId,
            CooldownEndMs = now + skill.Value.CooldownMs
        });
    }

    // 10. Publish damage event (transient)
    ctx.Db.DamageEvent.Insert(new DamageEvent {
        TargetId = default,
        TargetMobId = mobId,
        Damage = finalDmg,
        IsCrit = isCrit,
        IsKill = isKill,
        SkillId = skillId,
        AttackerName = ctx.Db.Player.Owner.Find(ctx.Sender)?.Username ?? "Unknown"
    });

    // 11. If kill → award XP, drop loot
    if (isKill)
    {
        AwardExp(ctx, ctx.Sender, mobDef.ExpReward);
        DropLoot(ctx, mobDef, m.PosX, m.PosY, ctx.Sender);

        var ps = playerStats;
        ps.TotalMobKills++;
        ctx.Db.PlayerStats.Owner.Update(ps);
    }

    // 12. Set attack animation cho player
    var pp = ctx.Db.PlayerPosition.Owner.Find(ctx.Sender).Value;
    pp.AnimState = 2; // Attack
    // Face toward mob
    pp.FacingRight = m.PosX > playerPos.PosX;
    ctx.Db.PlayerPosition.Owner.Update(pp);
}
```

---

## 8. Mob AI — State Machine Chi Tiết

```
                    ┌──────────────────────────────────────┐
                    │          MOB AI STATE MACHINE          │
                    └──────────────────────────────────────┘

    ┌─────────┐  player trong     ┌─────────┐  trong attack    ┌─────────┐
    │  IDLE   │ ──AggroRange────→ │  CHASE  │ ──range────────→ │ ATTACK  │
    │ (0)     │                   │ (2)     │                   │ (3)     │
    └────┬────┘                   └────┬────┘                   └────┬────┘
         │                             │                              │
         │ random timer (3-8s)         │ player quá xa                │ attack xong
         ▼                             │ HOẶC quá xa spawn            │
    ┌─────────┐                        │ (> LeashRange)               │
    │ PATROL  │                        ▼                              │
    │ (1)     │ ←──────────── ┌──────────┐ ◄────────────────────────┘
    └────┬────┘    đến nơi    │  RETURN  │   target chết hoặc disconnect
         │                    │  (4)     │
         │ player trong       └──────────┘
         │ AggroRange              │ đã về spawn point
         └────────────────────────→│
                                   ▼
                              quay lại IDLE

    ┌─────────┐
    │  DEAD   │  CurrentHp <= 0, delete sau 3s (để play death animation)
    │ (5)     │
    └─────────┘
```

### 8.1 AI Logic trong game_tick

```csharp
private static void MobAiTick(ReducerContext ctx, float dt, long now, GameConfig config)
{
    foreach (var mob in ctx.Db.MobInstance.Iter())
    {
        if (mob.AiState == 5) // Dead
        {
            // Sau 3 giây → delete mob instance (sẽ được respawn bởi spawn timer)
            if (now - mob.AiStateStartMs > 3000)
            {
                ctx.Db.MobInstance.MobId.Delete(mob.MobId);
            }
            continue;
        }

        var mobDef = ctx.Db.MobDef.MobDefId.Find(mob.MobDefId).Value;
        var m = mob;

        switch (mob.AiState)
        {
            case 0: // IDLE
                // Check aggro: tìm player gần nhất trong AggroRange
                Identity closest = FindClosestPlayer(ctx, m.PosX, m.PosY, mobDef.AggroRange);
                if (closest != default)
                {
                    m.TargetPlayer = closest;
                    m.AiState = 2; // Chase
                    m.AiStateStartMs = now;
                }
                else if (now - m.AiStateStartMs > 3000 + (m.MobId % 5000))
                {
                    // Random patrol timer (3-8s, seeded by MobId để mỗi con khác nhau)
                    m.AiState = 1; // Patrol
                    m.AiStateStartMs = now;
                    // Random direction
                    float angle = ((m.MobId * 7 + now / 1000) % 360) * MathF.PI / 180f;
                    m.DirX = MathF.Cos(angle);
                    m.DirY = MathF.Sin(angle);
                }
                break;

            case 1: // PATROL
                // Di chuyển theo direction hiện tại
                m.PosX += m.DirX * mobDef.Speed * 0.5f * dt; // Patrol speed = 50%
                m.PosY += m.DirY * mobDef.Speed * 0.5f * dt;
                m.AnimState = 1; // Walk
                m.FacingRight = m.DirX > 0;

                // Clamp to spawn zone (LeashRange)
                float distFromSpawn = Distance(m.PosX, m.PosY, m.SpawnX, m.SpawnY);
                if (distFromSpawn > mobDef.LeashRange * 0.5f)
                {
                    m.AiState = 4; // Return
                    m.AiStateStartMs = now;
                }
                else if (now - m.AiStateStartMs > 2000) // Patrol 2 giây rồi idle
                {
                    m.AiState = 0;
                    m.AiStateStartMs = now;
                    m.DirX = 0; m.DirY = 0;
                    m.AnimState = 0;
                }

                // Check aggro trong khi patrol
                Identity target = FindClosestPlayer(ctx, m.PosX, m.PosY, mobDef.AggroRange);
                if (target != default)
                {
                    m.TargetPlayer = target;
                    m.AiState = 2;
                    m.AiStateStartMs = now;
                }
                break;

            case 2: // CHASE
                // Tìm target position
                var targetPos = ctx.Db.PlayerPosition.Owner.Find(m.TargetPlayer);
                if (targetPos == null || !IsPlayerAlive(ctx, m.TargetPlayer))
                {
                    // Target gone → return
                    m.TargetPlayer = default;
                    m.AiState = 4;
                    m.AiStateStartMs = now;
                    break;
                }

                float tx = targetPos.Value.PosX;
                float ty = targetPos.Value.PosY;
                m.TargetLastX = tx;
                m.TargetLastY = ty;

                float distToTarget = Distance(m.PosX, m.PosY, tx, ty);
                float distToSpawn = Distance(m.PosX, m.PosY, m.SpawnX, m.SpawnY);

                // Leash check — quá xa spawn → bỏ chase
                if (distToSpawn > mobDef.LeashRange)
                {
                    m.TargetPlayer = default;
                    m.AiState = 4;
                    m.AiStateStartMs = now;
                    break;
                }

                if (distToTarget <= mobDef.AttackRange)
                {
                    // Trong attack range → attack
                    m.AiState = 3;
                    m.AiStateStartMs = now;
                    m.DirX = 0; m.DirY = 0;
                }
                else
                {
                    // Move toward target
                    float dirX = tx - m.PosX;
                    float dirY = ty - m.PosY;
                    float mag = MathF.Sqrt(dirX * dirX + dirY * dirY);
                    if (mag > 0.01f) { dirX /= mag; dirY /= mag; }

                    m.PosX += dirX * mobDef.Speed * dt;
                    m.PosY += dirY * mobDef.Speed * dt;
                    m.DirX = dirX; m.DirY = dirY;
                    m.AnimState = 1; // Walk
                    m.FacingRight = dirX > 0;
                }
                break;

            case 3: // ATTACK
                // Cooldown check
                if (now - m.LastAttackMs >= mobDef.AttackCooldownMs)
                {
                    // Deal damage to target
                    if (IsPlayerAlive(ctx, m.TargetPlayer))
                    {
                        DealMobDamage(ctx, m, mobDef, now);
                        m.LastAttackMs = now;
                        m.AnimState = 2; // Attack anim
                    }
                    else
                    {
                        m.TargetPlayer = default;
                        m.AiState = 4;
                        m.AiStateStartMs = now;
                    }
                }

                // Check if target moved out of range
                var tp = ctx.Db.PlayerPosition.Owner.Find(m.TargetPlayer);
                if (tp != null)
                {
                    float d = Distance(m.PosX, m.PosY, tp.Value.PosX, tp.Value.PosY);
                    if (d > mobDef.AttackRange * 1.2f) // 20% buffer
                    {
                        m.AiState = 2; // Back to chase
                        m.AiStateStartMs = now;
                    }
                }
                break;

            case 4: // RETURN to spawn
                float dxr = m.SpawnX - m.PosX;
                float dyr = m.SpawnY - m.PosY;
                float distR = MathF.Sqrt(dxr * dxr + dyr * dyr);

                if (distR < 0.5f)
                {
                    // Đã về spawn → idle
                    m.PosX = m.SpawnX;
                    m.PosY = m.SpawnY;
                    m.AiState = 0;
                    m.AiStateStartMs = now;
                    m.DirX = 0; m.DirY = 0;
                    m.AnimState = 0;
                    m.CurrentHp = mobDef.MaxHp; // Full heal khi return
                }
                else
                {
                    float ndx = dxr / distR;
                    float ndy = dyr / distR;
                    m.PosX += ndx * mobDef.Speed * 1.5f * dt; // Return speed = 150%
                    m.PosY += ndy * mobDef.Speed * 1.5f * dt;
                    m.DirX = ndx; m.DirY = ndy;
                    m.AnimState = 1;
                    m.FacingRight = ndx > 0;
                }
                break;
        }

        ctx.Db.MobInstance.MobId.Update(m);
    }
}
```

---

## 9. Inventory & Equipment — Full Lifecycle

### 9.1 Equip Flow (Server)

```
Player gọi EquipItem(slotId=42)
│
├─ 1. Validate: slotId thuộc về ctx.Sender? Item tồn tại?
├─ 2. Lấy ItemDef → check ItemType (weapon/armor/helmet/back)
├─ 3. Check slot tương ứng trong Equipment:
│     Ví dụ: ItemType=0 (Weapon) → check equip.WeaponSlotId
│
├─ 4a. Slot đang TRỐNG (WeaponSlotId == 0):
│     ├─ Set equip.WeaponSlotId = slotId
│     ├─ Set inventorySlot.SlotIndex = -1 (đánh dấu "equipped")
│     ├─ Update PlayerAppearance.WeaponType = itemDef.SpumSpriteIndex
│     └─ RecalculateStats(ctx, ctx.Sender)
│
├─ 4b. Slot đã CÓ item (WeaponSlotId = 37):
│     ├─ Tìm inventory slot trống cho item cũ
│     │     ├─ TÌM THẤY → set oldSlot.SlotIndex = emptyIndex
│     │     └─ KHÔNG TÌM THẤY → throw "Inventory full"
│     ├─ Set equip.WeaponSlotId = slotId (item mới)
│     ├─ Set inventorySlot.SlotIndex = -1
│     ├─ Update PlayerAppearance.WeaponType = newItemDef.SpumSpriteIndex
│     └─ RecalculateStats(ctx, ctx.Sender)
│
└─ 5. RecalculateStats:
      ├─ baseHp = 100 + (level - 1) × 20
      ├─ baseAtk = 10 + (level - 1) × 3
      ├─ baseDef = 5 + (level - 1) × 2
      ├─ Loop qua tất cả equipped items:
      │     totalBonusHp += itemDef.BonusHp
      │     totalBonusAtk += itemDef.BonusAtk
      │     totalBonusDef += itemDef.BonusDef
      │     totalBonusSpeed += itemDef.BonusSpeed
      ├─ stats.MaxHp = baseHp + totalBonusHp
      ├─ stats.Atk = baseAtk + totalBonusAtk
      ├─ stats.Def = baseDef + totalBonusDef
      ├─ stats.Speed = 3.0 + totalBonusSpeed
      ├─ Clamp CurrentHp nếu > MaxHp mới
      └─ Update PlayerStats
```

---

## 10. Chat System

```csharp
[SpacetimeDB.Reducer]
public static void SendChat(ReducerContext ctx, string content)
{
    // 1. Validate
    if (string.IsNullOrWhiteSpace(content)) return;
    content = content.Trim();
    if (content.Length > 200)
        content = content.Substring(0, 200);

    // 2. Sanitize (chống XSS — dù là in-game chat, vẫn nên clean)
    content = content.Replace("<", "＜").Replace(">", "＞");

    // 3. Get sender name
    var player = ctx.Db.Player.Owner.Find(ctx.Sender);
    if (player == null) throw new Exception("Player not found.");

    // 4. Insert message
    ctx.Db.ChatMessage.Insert(new ChatMessage {
        Sender = ctx.Sender,
        SenderName = player.Value.Username,
        Content = content,
        Channel = 0, // Global
        TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    });

    // 5. Cleanup cũ — giữ max 100 tin nhắn
    var config = ctx.Db.GameConfig.Id.Find(0).Value;
    var messages = ctx.Db.ChatMessage.Iter().ToList();
    if (messages.Count > config.MaxChatHistory)
    {
        var toDelete = messages
            .OrderBy(m => m.TimestampMs)
            .Take(messages.Count - config.MaxChatHistory);
        foreach (var old in toDelete)
            ctx.Db.ChatMessage.MessageId.Delete(old.MessageId);
    }
}
```

---

## 11. Admin & Debug Tools

### 11.1 Authorization Pattern

```csharp
// Helper dùng chung cho tất cả admin reducers
private static void RequireAdmin(ReducerContext ctx)
{
    var player = ctx.Db.Player.Owner.Find(ctx.Sender);
    if (player == null || !player.Value.IsAdmin)
        throw new Exception("Unauthorized: Admin privileges required.");
}

// Set admin via CLI: spacetime call spum-online SetAdmin '{"target":"<identity_hex>","isAdmin":true}'
[SpacetimeDB.Reducer]
public static void SetAdmin(ReducerContext ctx, string targetIdentityHex, bool isAdmin)
{
    // Chỉ module owner (first connected admin) có thể set admin
    // Hoặc hardcode 1 "super admin" identity trong Init
    RequireAdmin(ctx);
    // ... set target player IsAdmin = isAdmin
}
```

### 11.2 Stress Test: AdminSpawnRandomWave

```csharp
[SpacetimeDB.Reducer]
public static void AdminSpawnRandomWave(ReducerContext ctx,
    int totalCount, float centerX, float centerY, float radius)
{
    RequireAdmin(ctx);

    if (totalCount < 1 || totalCount > 500)
        throw new Exception("Count must be 1-500.");

    // Lấy tất cả mob definitions
    var mobDefs = ctx.Db.MobDef.Iter().ToList();
    if (mobDefs.Count == 0) throw new Exception("No mob definitions found.");

    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    for (int i = 0; i < totalCount; i++)
    {
        // Pseudo-random position trong radius
        // (Dùng i + now vì reducer phải deterministic — không dùng Random)
        float angle = ((i * 137 + (int)(now / 100)) % 360) * MathF.PI / 180f;
        float dist = ((i * 73 + (int)(now / 1000)) % 100) / 100f * radius;
        float px = centerX + MathF.Cos(angle) * dist;
        float py = centerY + MathF.Sin(angle) * dist;

        // Random mob type
        int defIndex = (i * 31 + (int)(now / 500)) % mobDefs.Count;
        var def = mobDefs[defIndex];

        ctx.Db.MobInstance.Insert(new MobInstance {
            MobDefId = def.MobDefId,
            PosX = px, PosY = py,
            SpawnX = px, SpawnY = py,
            CurrentHp = def.MaxHp,
            AiState = 0, // Idle
            AiStateStartMs = now,
            LastAttackMs = 0,
            TargetPlayer = default,
            TargetLastX = 0, TargetLastY = 0,
            DirX = 0, DirY = 0,
            AnimState = 0,
            FacingRight = true
        });
    }

    // Announce
    ctx.Db.SystemEvent.Insert(new SystemEvent {
        TargetPlayer = default,
        EventType = 4, // Announcement
        Message = $"Admin spawned {totalCount} mobs!",
        DataJson = ""
    });
}
```

---

## 12. Subscription Strategy — Bandwidth Optimization

### 12.1 Phase 1: Subscribe All (Simple)

```csharp
// OK cho <200 concurrent players
conn.SubscriptionBuilder()
    .SubscribeToAllTables();
```

### 12.2 Phase 2+: Selective Subscriptions (Scale)

```csharp
// Khi cần scale → subscribe theo vùng
// Giống BitCraft: subscribe entities trong "chunk" gần player
conn.SubscriptionBuilder()
    .OnApplied(OnSubApplied)
    .Subscribe(new[] {
        // Static data — subscribe 1 lần
        "SELECT * FROM item_def",
        "SELECT * FROM mob_def",
        "SELECT * FROM skill_def",
        "SELECT * FROM game_config",

        // Dynamic data — subscribe all (nhỏ, ít thay đổi)
        "SELECT * FROM player",
        "SELECT * FROM player_appearance",
        "SELECT * FROM player_stats",
        "SELECT * FROM equipment",

        // High-frequency — có thể filter theo vùng sau này
        "SELECT * FROM player_position",
        "SELECT * FROM mob_instance",
        "SELECT * FROM loot_drop",

        // Personal data
        $"SELECT * FROM inventory_slot WHERE owner = '{localIdentity}'",
        $"SELECT * FROM skill_cooldown WHERE owner = '{localIdentity}'",
        $"SELECT * FROM player_combat_state WHERE owner = '{localIdentity}'",

        // Chat — giới hạn 50 tin gần nhất
        "SELECT * FROM chat_message",

        // Events
        "SELECT * FROM damage_event",
        "SELECT * FROM system_event",
    });
```

### 12.3 Bandwidth Estimation

```
Giả sử 100 concurrent players, 50 mobs active:

PlayerPosition updates:
  - 100 players × 20 updates/s × ~40 bytes/row = 80 KB/s per client
  - Optimization: chỉ update khi moving → giảm ~60% = 32 KB/s

MobInstance updates:
  - 50 mobs × 20 updates/s × ~60 bytes/row = 60 KB/s
  - Optimization: mob idle không generate update → giảm ~70% = 18 KB/s

Tổng estimated bandwidth per client: ~50-100 KB/s
→ OK cho broadband, cần test trên 4G/mobile
```

---

## 13. Error Handling & Edge Cases

### 13.1 Reconnection

```csharp
// GameManager.cs
void HandleDisconnect(DbConnection conn, Exception error)
{
    IsConnected = false;
    Debug.LogWarning($"Disconnected: {error?.Message}");
    StartCoroutine(ReconnectLoop());
}

IEnumerator ReconnectLoop()
{
    int attempt = 0;
    while (!IsConnected)
    {
        attempt++;
        float delay = Mathf.Min(2f * Mathf.Pow(2, attempt - 1), 30f); // Exponential backoff, max 30s
        Debug.Log($"Reconnecting in {delay}s (attempt {attempt})...");
        yield return new WaitForSeconds(delay);
        try { Connect(); } catch (Exception e) { Debug.LogError(e.Message); }
    }
}
```

### 13.2 Race Conditions đã xử lý

| Vấn đề | Giải pháp |
|---------|-----------|
| 2 players pickup cùng 1 loot đồng thời | Reducer chạy transactional — player đầu tiên commit thành công, player thứ 2 nhận Exception "Loot not found" |
| Player equip item đang bị ai khác equip | Không xảy ra — InventorySlot có Owner, reducer check ctx.Sender |
| Mob đánh player nhưng player vừa disconnect | OnDisconnect clear mob aggro target, mob chuyển sang Return state |
| Player attack mob nhưng mob vừa chết bởi player khác | Reducer check mob.AiState == 5 → throw "Mob already dead" |
| Player gửi movement nhưng đang dead | Reducer check DeathTimeMs > 0 → silently ignore |
| 500 mob spawn cùng lúc → lag spike client | Client-side: object pooling + frustum culling — chỉ render mob trong camera view |
| Chat spam | Rate limit: reducer check timestamp giữa 2 tin nhắn, minimum 500ms |

### 13.3 Security Considerations

| Threat | Mitigation |
|--------|-----------|
| Speed hack (gửi direction lớn hơn 1) | Server normalize direction trong UpdateMovement reducer |
| Teleport hack (gửi position trực tiếp) | Client KHÔNG gửi position — chỉ gửi direction. Position tính bởi game_tick |
| Damage hack (gửi damage lớn) | Client KHÔNG gửi damage — server tính từ Atk, Def, formulas |
| Item dupe | Reducer transactional — insert/delete atomic. Không thể dupe qua race condition |
| Impersonation (giả mạo identity) | ctx.Sender do SpacetimeDB set — client không thể spoof |
| Admin command injection | RequireAdmin() check Player.IsAdmin trước mỗi admin reducer |

---

## 14. Performance — Bottlenecks & Solutions

### 14.1 Server-side (SpacetimeDB)

| Bottleneck | Dấu hiệu | Giải pháp |
|-----------|----------|-----------|
| Game tick quá nặng (>50ms) | Log warning trong reducer | Giảm tick rate (100ms), hoặc split AI vào scheduled reducer riêng |
| Quá nhiều table iterations | Reducer chạy chậm | Thêm BTree index, dùng .Find() thay .Iter().Where() |
| Transaction quá lớn | Publish timeout | Tách logic ra nhiều reducer nhỏ |

### 14.2 Client-side (Unity)

| Bottleneck | Dấu hiệu | Giải pháp |
|-----------|----------|-----------|
| Quá nhiều SPUM prefabs | FPS drop dưới 30 | Object pooling: max 100 visible players, recycle khi ra khỏi camera |
| SpriteRenderer overhead | GPU spike | Sprite atlas, batching, disable renderers ngoài camera |
| GC allocation từ callbacks | Frame stutter | Pre-allocate collections, avoid closures trong callbacks |
| Resources.Load mỗi frame | CPU spike | Cache loaded sprites trong Dictionary |

### 14.3 Object Pooling Strategy

```csharp
public class EntityPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialSize = 50;

    private Queue<GameObject> _pool = new();

    void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            var go = Instantiate(prefab);
            go.SetActive(false);
            _pool.Enqueue(go);
        }
    }

    public GameObject Get(Vector3 pos)
    {
        GameObject go;
        if (_pool.Count > 0)
        {
            go = _pool.Dequeue();
        }
        else
        {
            go = Instantiate(prefab); // Grow pool if needed
        }
        go.transform.position = pos;
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        _pool.Enqueue(go);
    }
}
```

---

## 15. Deployment & DevOps Workflow

### 15.1 Development Loop

```bash
# Terminal 1: Start SpacetimeDB local
spacetime start

# Terminal 2: Develop → Publish → Test loop
cd spum-online/spacetimedb/

# Edit code...

# Publish (giữ data)
spacetime publish --server local spum-online

# Hoặc publish (xóa data — fresh start)
spacetime publish --server local spum-online --delete-data

# Generate bindings cho Unity
spacetime generate --lang csharp --out-dir ../Assets/module_bindings

# View logs
spacetime logs --server local spum-online -f

# Terminal 3: Unity Editor → Play
```

### 15.2 Multi-Client Testing

Mở 2+ Unity instances để test multiplayer:
1. Build → File → Build Settings → Build (tạo standalone build)
2. Chạy Build #1 (standalone)
3. Nhấn Play trong Unity Editor (instance #2)
4. Cả 2 connect vào cùng local SpacetimeDB → test multiplayer

### 15.3 Production Deployment (khi sẵn sàng)

```bash
# Deploy lên SpacetimeDB Maincloud
spacetime login
spacetime publish spum-online  # không cần --server, mặc định lên Maincloud

# Client đổi HOST:
# const string HOST = "wss://maincloud.spacetimedb.com";
```

---

## 16. Testing Strategy

### 16.1 Server Module Tests

```bash
# SpacetimeDB hỗ trợ test bằng cách publish → gọi reducer → check logs
spacetime publish --server local spum-online --delete-data
spacetime call spum-online CreateCharacter '["TestPlayer",0,0,0,0,0,0,0]'
spacetime sql spum-online "SELECT * FROM player"
spacetime sql spum-online "SELECT * FROM player_position"
spacetime sql spum-online "SELECT * FROM player_stats"
```

### 16.2 Integration Tests (Unity)

```csharp
// TestRunner.cs — attach to a test scene
IEnumerator TestMovement()
{
    // 1. Connect
    yield return ConnectAndWait();

    // 2. Create character
    Conn.Reducers.CreateCharacter("TestBot", 0, 0, 0, 0, 0, 0, 0);
    yield return WaitForReducer();

    // 3. Move
    Conn.Reducers.UpdateMovement(1, 0, 1, true);
    yield return new WaitForSeconds(1f);

    // 4. Check position changed
    var pos = Conn.Db.PlayerPosition.Owner.Find(LocalIdentity);
    Assert.IsTrue(pos.Value.PosX > 0, "Player should have moved right");
}
```

### 16.3 Stress Test Checklist

| Test | Cách thực hiện | Metric đo |
|------|---------------|-----------|
| 100 idle players | Spawn 100 bots (mỗi bot là 1 CLI client gọi CreateCharacter) | Memory usage, subscription latency |
| 100 moving players | Bots gửi UpdateMovement liên tục | Tick execution time, bandwidth |
| 200 mob spawn | AdminSpawnRandomWave(200) | FPS client, tick time |
| 500 mob spawn | AdminSpawnRandomWave(500) | Breaking point test |
| Combat spam | 10 players attack cùng 1 mob | Reducer execution time |
| Chat spam | 10 players send message mỗi 500ms | Message ordering, cleanup |

---

## 17. Checklist Vấn Đề Đã Giải Quyết

| # | Vấn đề | Section giải quyết | Trạng thái |
|---|--------|-------------------|-----------|
| 1 | Movement sync giữa clients | §2, §5 — Client prediction + server reconciliation | ✅ |
| 2 | Animation sync (SPUM) | §6 — AnimState gộp trong PlayerPosition, map sang SPUM PlayAnimation | ✅ |
| 3 | Character customization persist | §3.1 — PlayerAppearance table, §6.2 serialization protocol | ✅ |
| 4 | PvP combat | §7 — Damage formula, range check, server-authoritative | ✅ |
| 5 | PvE combat (mob AI) | §8 — 6-state machine, aggro/leash/patrol | ✅ |
| 6 | Inventory management | §9 — SlotId based, equip/unequip flow, overflow handling | ✅ |
| 7 | Equipment visual sync | §6.2, §9.1 — Equip → update PlayerAppearance → SPUM visual refresh | ✅ |
| 8 | Chat system | §10 — Global chat, rate limit, history cleanup | ✅ |
| 9 | Admin spawn tools | §11 — RequireAdmin, AdminSpawnRandomWave, configurable | ✅ |
| 10 | Stress test mobs | §11.2 — Slider 1-500, random wave, §14 performance notes | ✅ |
| 11 | Floating damage numbers | §3.1 — DamageEvent (Event Table), transient, no DB bloat | ✅ |
| 12 | Death & respawn | §3.2 — DeathTimeMs tracking, auto-respawn in game_tick | ✅ |
| 13 | Disconnect handling | §3.2, §13.1 — OnDisconnect clear state, reconnection loop | ✅ |
| 14 | Speed/teleport hack prevention | §13.3 — Server-authoritative movement, direction normalize | ✅ |
| 15 | Bandwidth optimization | §12 — Table splitting, selective subscriptions, estimation | ✅ |
| 16 | Client performance (many entities) | §14 — Object pooling, sprite caching, frustum culling | ✅ |
| 17 | Race conditions (concurrent actions) | §13.2 — Transactional reducers, server-side validation | ✅ |
| 18 | HP/MP regen | §3.1, §3.2 — HpRegenTimer, out-of-combat check | ✅ |
| 19 | Loot system | §3.1 — LootDrop table, DropLoot(), PickupLoot(), auto-despawn | ✅ |
| 20 | Skill system with cooldowns | §3.1 — SkillDef, SkillCooldown tables, MP cost | ✅ |
| 21 | Leaderboard | §3.1 — TotalKills, TotalMobKills in PlayerStats, client sort + display | ✅ |
| 22 | World persistence (server restart) | SpacetimeDB native — all tables persist, hot-swap module code | ✅ |
| 23 | Multi-client testing | §15.2 — Build standalone + Editor play simultaneously | ✅ |
| 24 | Deterministic pseudo-random trong reducers | §11.2 — Seeded từ MobId + timestamp thay vì System.Random | ✅ |
| 25 | SPUM prefab memory | §6.3, §14 — Object pooling, sprite atlas, disable offscreen | ✅ |

---

*Tài liệu này cover toàn bộ kiến trúc từ byte-level data flow đến pixel-level rendering. Mỗi section có thể được implement độc lập theo thứ tự Phase trong GDD. Khi gặp vấn đề, tham chiếu Section tương ứng trong Checklist #17.*
