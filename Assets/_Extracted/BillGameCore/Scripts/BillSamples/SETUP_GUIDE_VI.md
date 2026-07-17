# BillGameCore v3 — Hướng Dẫn Setup 3 Game Mẫu

---

## Mục Lục

1. Cấu Trúc Project & Quy Tắc Namespace
2. Hai Cách Chạy: Standalone vs Launcher
3. Cài Đặt Unity Bắt Buộc (Tags, Layers, Build Settings)
4. Game 1: Bill Flappy — Hướng Dẫn Setup
5. Game 2: Bill Runner — Hướng Dẫn Setup Chi Tiết
6. Game 3: Bill Defense — Hướng Dẫn Setup Chi Tiết
7. Thay Thế Placeholder Bằng Art Riêng
8. Xử Lý Lỗi Thường Gặp

---

## 1. Cấu Trúc Project & Quy Tắc Namespace

### Cây Thư Mục

```
Assets/
├── BillGameCore/                     ← Framework gốc (giải nén từ BillGameCore_v3.zip)
│   ├── Runtime/
│   ├── Editor/
│   └── BillGameCore.asmdef
│
├── BillSamples/
│   ├── Launcher/                     ← Bộ chuyển game (tuỳ chọn, không bắt buộc)
│   │   ├── SampleGameLauncher.cs
│   │   └── BillSamples.Launcher.asmdef
│   │
│   ├── BillFlappy/                   ← Game 1: Flappy Bird
│   │   ├── Scripts/  (7 file .cs)
│   │   └── BillSamples.Flappy.asmdef
│   │
│   ├── BillRunner/                   ← Game 2: Endless Runner
│   │   ├── Scripts/  (7 file .cs)
│   │   └── BillSamples.Runner.asmdef
│   │
│   ├── BillDefense/                  ← Game 3: Tower Defense
│   │   ├── Scripts/  (9 file .cs)
│   │   └── BillSamples.TowerDefense.asmdef
│   │
│   ├── README.md
│   ├── SETUP_GUIDE.md                ← File này
│   └── GDD_BillGameCore_Samples.md   ← Game Design Document
```

### Assembly Definition (asmdef)

Mỗi game được biên dịch thành 1 assembly riêng biệt. Tác dụng:

- Flappy KHÔNG THỂ vô tình dùng class của Runner hay TowerDefense (và ngược lại)
- Mỗi game chỉ phụ thuộc vào BillGameCore, không phụ thuộc lẫn nhau
- Launcher phụ thuộc cả 3 game + BillGameCore (vì nó cần gọi Build/Teardown)
- Khi sửa code 1 game, Unity chỉ recompile assembly đó → nhanh hơn

Nếu BillGameCore có file .asmdef riêng, đảm bảo trường `"name"` trong asmdef
của nó khớp với giá trị trong `"references"` của các game. Nếu BillGameCore
KHÔNG có asmdef, xoá tất cả file .asmdef trong BillSamples — mọi thứ sẽ
compile chung vào Assembly-CSharp mặc định, vẫn hoạt động bình thường.

### Bảng Namespace & Key Isolation

| Game | Namespace | Prefix State | Prefix Pool Key |
|------|-----------|--------------|-----------------|
| Flappy | `BillSamples.Flappy` | `Flappy*` | `Pipe` |
| Runner | `BillSamples.Runner` | `Runner*` | `chunk_*`, `coin_*`, `vfx_dust` |
| Defense | `BillSamples.TowerDefense` | `TD*` | `enemy_*`, `proj_*`, `vfx_*`, `ui_*` |

Tất cả state, event, pool key đều unique — đã audit, không có bất kỳ
xung đột nào khi cả 3 game nằm chung project.

---

## 2. Hai Cách Chạy

### Cách A: Standalone — Mỗi Game Một Scene Riêng

Phù hợp khi: test từng game, setup đơn giản nhất.

```
Bước 1:  Tạo 3 scene: Scene_Flappy, Scene_Runner, Scene_Defense
Bước 2:  Trong mỗi scene, tạo 1 empty GameObject
Bước 3:  Gắn script Setup tương ứng vào (FlappySetup, RunnerSetup, TDSetup)
Bước 4:  Để autoBootstrap = true (đây là mặc định)
Bước 5:  Bấm Play → scene tự build hết: camera, UI, pool, prefab, mọi thứ
```

Mỗi scene hoạt động hoàn toàn độc lập. Chuyển game = chuyển scene.

**Lưu ý quan trọng:** BillGameCore cần được khởi tạo TRƯỚC khi Setup chạy.
Có 3 cách đảm bảo điều này:

- Cách 1: Đặt prefab `BillStartup` vào MỖI scene
- Cách 2: Có 1 scene "Boot" khởi tạo BillGameCore rồi load scene game
- Cách 3: Nếu BillGameCore dùng `[RuntimeInitializeOnLoadMethod]` thì tự động OK

### Cách B: Launcher — Một Scene, Chuyển Giữa 3 Game

Phù hợp khi: demo, trình bày, hoặc muốn chạy cả 3 game trong 1 build.

```
Bước 1:  Tạo 1 scene duy nhất: Scene_Launcher
Bước 2:  Đảm bảo BillGameCore bootstrap trong scene này (BillStartup prefab)
Bước 3:  Tạo empty GO → gắn SampleGameLauncher.cs
Bước 4:  (Tuỳ chọn) Tạo 3 prefab từ mỗi Setup GO (đã gán art assets)
         → kéo vào 3 slot prefab trên Launcher
Bước 5:  Nếu không dùng prefab, Launcher tự tạo Setup mặc định (placeholder)
Bước 6:  Bấm Play → menu hiện 3 nút chọn game
Bước 7:  Bấm F12 bất cứ lúc nào để quay về menu Launcher
```

Launcher xử lý tự động khi chuyển game:

| Bước | Hành động |
|------|-----------|
| 1 | Gọi `Teardown()` trên game hiện tại → unsubscribe events, stop timers |
| 2 | `Bill.State.Cleanup()` → xoá toàn bộ state machine cũ |
| 3 | `Bill.Timer.CancelAll()` → huỷ timer còn sót |
| 4 | `Bill.Pool.ReturnAll()` → trả hết object về pool |
| 5 | `Bill.Audio.StopMusic(0f)` → tắt nhạc |
| 6 | `Time.timeScale = 1f` → reset speed |
| 7 | `Destroy()` toàn bộ game root cũ (camera, canvas, mọi GO) |
| 8 | Tạo game root mới → gọi `Build()` trên Setup script mới |

---

## 3. Cài Đặt Unity Bắt Buộc

### Tags — BẮT BUỘC PHẢI TẠO

Vào **Edit > Project Settings > Tags and Layers**, thêm các tag sau:

| Tag | Game nào dùng | Mục đích |
|-----|--------------|----------|
| `Obstacle` | Flappy + Runner | Khi chạm = nhận sát thương hoặc chết |
| `ScoreZone` | Flappy | Khi chim bay qua khe pipe = +1 điểm |
| `Coin` | Runner | Khi chạm = thu coin |
| `PowerUp` | Runner | Khi chạm = kích hoạt power-up |
| `Spike` | Runner | Khi chạm = chết ngay (bỏ qua HP, shield) |

Nếu thiếu tag, Unity sẽ báo lỗi `"Tag 'Obstacle' is not defined"` khi Play.

### Physics

Flappy (chế độ 2D):
- Dùng Physics 2D, collision matrix mặc định là OK
- `FlappySetup` có tuỳ chọn `use2DPhysics = true` (mặc định)

Runner và Tower Defense (chế độ 3D):
- Dùng Physics 3D, trigger detection mặc định là OK
- Runner có tuỳ chọn `use2DPhysics = false` (mặc định)

### Build Settings

- Standalone mode: thêm từng scene game vào Build Settings
- Launcher mode: chỉ cần thêm `Scene_Launcher`

---

## 4. Game 1: Bill Flappy — Hướng Dẫn Setup

Flappy là game đơn giản nhất — Setup script tạo hết mọi thứ.

### Chạy Nhanh

```
1. Tạo scene mới
2. Tạo empty GO → gắn FlappySetup
3. Bấm Play
4. Xong! Game chạy ngay với placeholder (chim vàng, pipe xanh, nền trời)
```

### Những Gì Được Tạo Tự Động

| Object | Mô tả |
|--------|-------|
| Main Camera | Orthographic, size=5, hướng portrait |
| Background | Quad phẳng với màu trời xanh |
| Ground | Cube ở y=-5, tag "Obstacle" → chim rơi chạm = chết |
| Ceiling | Collider vô hình ở y=6 → chim bay quá cao = chết |
| Bird | Sphere vàng + hình beak cam + mắt trắng, có script FlappyBird |
| Pipe Prefab | 2 cube xanh (trên + dưới) với khe ở giữa + trigger ghi điểm, đã register pool (8 cặp) |
| PipeSpawner | Spawn pipe tự động qua `Bill.Timer.Repeat` |
| FlappyCanvas | Canvas đầy đủ: màn hình menu, HUD điểm, panel Game Over |
| FlappyGameManager | Quản lý điểm, độ khó, chuyển state |

### Tuỳ Chỉnh Art

Trên component `FlappySetup` trong Inspector:

| Slot | Gán cái gì | Ghi chú |
|------|-----------|---------|
| Bird Model Prefab | Model 3D con chim hoặc sprite 2D | Hướng mặt sang phải ở rotation (0,0,0). Script xoay `modelRoot` theo velocity. |
| Top Pipe Prefab | Model/sprite pipe trên | Cao khoảng 10 unit, origin ở giữa |
| Bottom Pipe Prefab | Model/sprite pipe dưới | Tương tự top, hoặc flip top pipe |
| Background Material | Material cho quad nền | Null = dùng màu skyColor |
| Ground Material | Material cho mặt đất | Null = dùng màu groundColor |

### Điều Khiển

| Input | Hành động |
|-------|-----------|
| Tap / Space / Chuột trái | Vỗ cánh (ở menu: bắt đầu game) |
| Escape | Pause / Resume |

### Hệ Thống Điểm & Medal

| Medal | Điểm cần |
|-------|----------|
| Không có | 0–9 |
| Đồng 🥉 | 10–19 |
| Bạc 🥈 | 20–29 |
| Vàng 🥇 | 30–49 |
| Kim cương 💎 | 50+ |

Điểm cao nhất tự động lưu qua `Bill.Save` với key `flappy_best`.

---

## 5. Game 2: Bill Runner — Hướng Dẫn Setup Chi Tiết

Runner phức tạp hơn Flappy nhiều. Setup script tạo được game chơi được ngay,
nhưng để đẹp và hay cần tuỳ chỉnh thủ công: obstacle, coin, chunk, parallax.

### Chạy Nhanh

```
1. Tạo scene mới
2. Tạo empty GO → gắn RunnerSetup
3. Bấm Play (3D side-scroller với hình khối placeholder)
```

### Những Gì Được Tạo Tự Động

| Object | Mô tả |
|--------|-------|
| Main Camera | Perspective, FOV=60, tự follow player với offset (5, 3, -10) |
| Player | Capsule xanh + mắt, có script RunnerPlayer đầy đủ (jump/slide/hurt/death/power-ups) |
| 6 Chunk Prefab | Mỗi cái rộng 20 unit, register pool (3 instance mỗi loại) |
| Coin Prefab | Cylinder vàng nhỏ, pool warm=20 |
| VFX_Dust | Particle bụi khi nhảy, pool warm=5 |
| 4 Parallax Layer | Quad màu ở các depth khác nhau, scroll speed khác nhau |
| RunnerCanvas | Menu (best + coins), HUD (distance, coins, hearts), Game Over panel |
| ChunkSpawner | Spawn infinite phía trước player, despawn phía sau |
| RunnerGameManager | Difficulty curve, speed ramping, scoring, shop |

### Hệ Thống Chunk — Cách Hoạt Động

Map được tạo từ các "chunk" (đoạn map) rộng 20 unit, nối đuôi nhau vô hạn.
Spawner chọn chunk dựa vào khoảng cách đã chạy:

| Khoảng cách | Chunk khả dụng | Mô tả |
|-------------|---------------|-------|
| 0m+ | `chunk_flat_easy` | Mặt đất phẳng, 1 crate, hàng 5 coin |
| 200m+ | `chunk_flat_med` | Crate + double crate + low beam, coin arc |
| 300m+ | `chunk_slide` | 2 beam thấp (phải slide), coin thấp |
| 400m+ | `chunk_elevated` | Platform nâng cao, coin trên đỉnh |
| 600m+ | `chunk_gap` | Hố giữa đường, coin nhử qua hố |
| 800m+ | `chunk_mixed_hard` | Saw + beam + crate + power-up thưởng |

### Tạo Chunk Tuỳ Chỉnh (Thủ Công)

Chunk tự generate là cơ bản. Muốn game hay phải tạo chunk riêng:

**Bước 1: Tạo prefab chunk**

```
1. Tạo empty GO, đặt tên "Chunk_Forest_01"

2. Thêm mặt đất:
   - Cube con, vị trí (10, -0.5, 0), scale (20, 1, 3)
   - Gán material đất

3. Thêm chướng ngại vật:
   - Tạo Cube tên "Crate", vị trí (5, 0.5, 0)
   - Tag = "Obstacle"
   - Add Component: RunnerObstacle
     → type = Crate
     → instantKill = false
   - Add Component: BoxCollider → isTrigger = true

   - Tạo Cube tên "LowBeam", vị trí (12, 1.5, 0), scale (3, 0.3, 2)
   - Tag = "Obstacle"
   - Add Component: RunnerObstacle → type = LowBeam
   - Add Component: BoxCollider → isTrigger = true

4. Thêm coin:
   - Tạo Cylinder tên "Coin_0", vị trí (3, 1, 0), scale (0.4, 0.05, 0.4)
   - Tag = "Coin"
   - Add Component: RunnerCollectible → coinType = Bronze
   - Add Component: SphereCollider → isTrigger = true, radius = 0.3
   - Copy thêm coin ở (4.2, 1, 0), (5.4, 1.5, 0), (6.6, 2, 0)
     → tạo hình arc vòng cung lên

5. Thêm power-up (tuỳ chọn, nên đặt ở vị trí khó lấy):
   - Tạo Sphere tên "PowerUp_Shield", vị trí (18, 2, 0), scale (0.6, 0.6, 0.6)
   - Tag = "PowerUp"
   - Add Component: RunnerPowerUp
     → itemKey = "item_shield"
     → duration = 0  (shield là single-use, không có thời gian)
   - Add Component: SphereCollider → isTrigger = true, radius = 0.4

6. Lưu thành Prefab vào Assets/BillSamples/BillRunner/Prefabs/
```

**Bước 2: Đăng ký vào Pool**

Mở `RunnerSetup.cs`, trong hàm `CreateChunkPrefabs()`, thêm:

```csharp
// Kéo prefab vào field public hoặc load từ Resources
public GameObject chunkForest01Prefab;

// Trong CreateChunkPrefabs():
if (chunkForest01Prefab != null)
    Bill.Pool.Register("chunk_forest_01", chunkForest01Prefab, 3);
```

**Bước 3: Thêm vào danh sách spawner**

Mở `RunnerChunkSpawner.cs`, thêm key mới vào mảng chunk phù hợp:

```csharp
public string[] mediumChunkKeys = { "chunk_flat_med", "chunk_forest_01" };
```

### Bảng Setup Obstacle

Mỗi obstacle CẦN những component sau:

| Component | Giá trị | Bắt buộc |
|-----------|---------|----------|
| Tag trên GameObject | `"Obstacle"` hoặc `"Spike"` | ✅ |
| `RunnerObstacle` script | Chọn `type`, set `instantKill` | ✅ |
| `BoxCollider` (3D) hoặc `BoxCollider2D` | `isTrigger = true` | ✅ |

Bảng hành vi theo loại:

| Loại | Cách setup |
|------|-----------|
| Đứng yên (crate, beam) | Đặt vào vị trí, không cần setting thêm |
| Di chuyển (barrel) | Set `moveSpeed = 4`, `moveDirection = Vector3.left` |
| Bay lên xuống (bird, saw) | Set `sinWave = true`, `sinAmplitude = 0.5`, `sinFrequency = 2` |
| Chết ngay (spike) | Set `instantKill = true` HOẶC dùng tag `"Spike"` |

### Bảng Setup Coin

| Component | Giá trị | Bắt buộc |
|-----------|---------|----------|
| Tag | `"Coin"` | ✅ |
| `RunnerCollectible` script | Chọn `coinType` | ✅ |
| `SphereCollider` | `isTrigger = true`, radius ~0.3 | ✅ |

Giá trị coin: Bronze = 1, Silver = 3, Gold = 10.
Coin tự xoay và nhấp nhô. Power-up `item_2x` nhân đôi giá trị.

### Bảng Setup Power-Up

| Component | Giá trị | Bắt buộc |
|-----------|---------|----------|
| Tag | `"PowerUp"` | ✅ |
| `RunnerPowerUp` script | Set `itemKey` và `duration` | ✅ |
| `SphereCollider` | `isTrigger = true`, radius ~0.4 | ✅ |

Danh sách power-up:

| itemKey | Thời gian | Hiệu ứng |
|---------|-----------|-----------|
| `item_magnet` | 8 giây | Mọi coin trong bán kính 5 unit tự bay đến player |
| `item_shield` | 0 (1 hit) | Chặn 1 lần nhận sát thương. Duration set = 0. |
| `item_speed` | 5 giây | Tốc độ ×1.5 + bất tử trong lúc boost |
| `item_2x` | 10 giây | Mọi coin thu được ×2 giá trị |
| `item_tiny` | 6 giây | Nhân vật thu nhỏ 50%, hitbox nhỏ, chui được dưới beam |

### Tuỳ Chỉnh Parallax Background

Thay quad placeholder bằng sprite riêng:

```
1. Chuẩn bị sprite rộng (ít nhất 40 unit, hoặc tileable)
2. Trong RunnerSetup.CreateBGLayer(), thay Quad bằng sprite của bạn
3. Chỉnh speedRatio:
   - 0.05 = gần như đứng yên (trời xa)
   - 0.15 = chuyển động nhẹ (núi xa)
   - 0.30 = vừa (cây xa)
   - 0.50 = rõ (cây gần)
   - 0.80 = gần bằng camera (cỏ, hoa)
   - 1.20 = nhanh hơn camera (particle foreground)
4. Chỉnh vị trí z: số lớn hơn = ở xa hơn phía sau
```

### Hệ Thống Shop & Economy

| Vật phẩm | Giá | Hiệu ứng |
|---------|-----|-----------|
| Extra HP (+1) | 300 coin | Bắt đầu với 2 HP (tối đa 3) |
| Double Jump | 500 coin | Mở khoá nhảy đôi |
| Magnet Passive | 800 coin | Nam châm mini luôn bật (range 1.5) |
| Head Start 200m | 100 coin/lần | Bỏ qua 200m đầu |
| Revive | 150 coin/lần | Hồi sinh 1 lần khi chết |
| Skin: Robot | 200 coin | Thay đổi ngoại hình |
| Skin: Ninja | 400 coin | Thay đổi ngoại hình |
| Skin: Astronaut | 600 coin | Thay đổi ngoại hình |

Tất cả lưu qua `Bill.Save`. Key format: `runner_coins`, `runner_hp_level`, 
`runner_has_doublejump`, `runner_skin`, v.v.

### Điều Khiển

| Input | Hành động |
|-------|-----------|
| Space / Chuột trái | Nhảy (đang đứng), Nhảy đôi (đang bay, nếu đã mua) |
| S / Mũi tên xuống | Slide (0.6 giây, hitbox thu nhỏ 60%) |
| Escape | Pause |
| Tab | Mở Shop (chỉ từ menu) |

---

## 6. Game 3: Bill Defense — Hướng Dẫn Setup Chi Tiết

Tower Defense có nhiều thành phần nhất. Setup script tạo game chơi được ngay
(15 wave, 6 tower, 10 loại quái), nhưng muốn đẹp cần thay model thủ công.

### Chạy Nhanh

```
1. Tạo scene mới
2. Tạo empty GO → gắn TDSetup
3. Bấm Play (top-down view, các khối placeholder)
```

### Những Gì Được Tạo Tự Động

| Object | Mô tả |
|--------|-------|
| Main Camera | Orthographic, size=8, nghiêng 70° nhìn xuống |
| Grid 20×12 | Tile cube: nâu = đường đi, xanh = đặt tower, đen = bị chặn |
| 12 Waypoint marker | Sphere đỏ nhỏ đánh dấu đường đi quái |
| SpawnPoint | Cube đỏ ở đầu path |
| ExitPoint | Cube xanh ở cuối path |
| 10 Enemy Prefab | Capsule/Cube/Sphere màu khác nhau theo loại, mỗi loại pool riêng |
| 6 Projectile Prefab | Sphere nhỏ nhiều màu, pool sẵn |
| VFX Prefab | Particle nổ, line renderer (lightning/sniper), floating text |
| TDCanvas | UI đầy đủ: thanh trên, panel tower, popup info, game over, victory |
| WaveManager | 15 wave cho Map 1 "Green Valley" |
| TDGameManager | Economy, lives, build phase timer, state flow |

### Cách Chơi — Flow Cơ Bản

```
1. BUILD PHASE (15-30 giây):
   - Click nút tower ở thanh dưới (Arrow, Cannon, Ice, Lightning, Sniper, Poison)
   - Di chuột trên grid → ô xanh = đặt được, ô đỏ = không được
   - Click ô xanh để đặt tower (trừ gold)
   - Click tower đã đặt → hiện popup: Upgrade / Sell / đổi Target Mode
   - Click nút "SEND WAVE" để bỏ qua chờ (bonus +5 gold)

2. WAVE ACTIVE:
   - Quái spawn từ SpawnPoint, đi theo waypoint đến ExitPoint
   - Tower tự bắn quái trong tầm
   - Quái đến exit = mất 1 mạng
   - Giết hết quái = wave complete

3. WAVE COMPLETE:
   - Nhận bonus gold (wave clear + interest)
   - Quay lại Build Phase

4. VICTORY (clear 15 wave) hoặc GAME OVER (hết mạng)
```

### Bảng Tower — Chỉ Số Đầy Đủ

| Tower | Giá | Sát thương | Tốc bắn | Tầm | Đặc biệt |
|-------|-----|-----------|---------|-----|-----------|
| Arrow | 80g | 12 → 20 → 35 | 0.8 → 0.7 → 0.5s | 3.5 → 4.0 | Lv3: bắn 2 mũi tên |
| Cannon | 120g | 25 → 40 → 65 | 1.5 → 1.3 → 1.1s | 3.0 | AoE splash 1.2→2.0. Lv3: stun 0.5s |
| Ice | 100g | 8 → 14 → 22 | 1.0s | 3.0 | Slow 30→40→50%. Lv3: 15% đóng băng 1s |
| Lightning | 150g | 18 → 28 → 42 | 1.2s | 4.0 | Chain 3→4→6 mục tiêu. Lv3: 10% overcharge ×3 |
| Sniper | 200g | 45 → 75 → 120 | 2.5 → 2.2 → 1.8s | 6.0 | Xuyên giáp, crit 20→25→35%. Lv3: hạ gục <15% HP |
| Poison | 130g | 5 → 7 → 12 | 1.0s | 3.0 | AoE DoT 4→7→12/s, stack 3→3→5. Lv3: lan độc khi chết |

Chi phí nâng cấp đầy đủ:

| Tower | Lv1 (mua) | Lv2 (nâng) | Lv3 (nâng) | Tổng đầu tư | Bán lại (70%) |
|-------|-----------|------------|------------|-------------|---------------|
| Arrow | 80g | 100g | 180g | 360g | 252g |
| Cannon | 120g | 150g | 250g | 520g | 364g |
| Ice | 100g | 120g | 200g | 420g | 294g |
| Lightning | 150g | 180g | 280g | 610g | 427g |
| Sniper | 200g | 250g | 350g | 800g | 560g |
| Poison | 130g | 160g | 250g | 540g | 378g |

### Bảng Quái — Chỉ Số Đầy Đủ

| Quái | HP | Speed | Giáp | Gold | Đặc biệt | Wave đầu tiên |
|------|-----|-------|------|------|-----------|---------------|
| Goblin | 30 | 2.0 | 0 | 5 | Không có (basic) | Wave 1 |
| Orc | 80 | 1.5 | 2 | 10 | Trâu, giáp cao | Wave 3 |
| Wolf | 25 | 3.5 | 0 | 8 | Nhanh | Wave 4 |
| Skeleton | 50 | 2.0 | 0 | 8 | Hồi sinh 1 lần ở 50% HP sau 2s | Wave 6 |
| Shield Orc | 100 | 1.3 | 5 | 15 | Khiên chặn 3 đòn đầu, sau đó giáp = 0 | Wave 8 |
| Bat | 15 | 2.5 | 0 | 3 | Bay (bỏ qua đường đi, bay thẳng đến exit) | Wave 10 |
| Dark Mage | 60 | 1.8 | 1 | 12 | Hồi máu đồng đội 5 HP/s trong bán kính 2 unit | Wave 12 |
| Golem | 250 | 0.8 | 8 | 25 | Miễn nhiễm slow | Sẵn sàng cho Map 2+ |
| Ghost | 40 | 2.2 | 0 | 10 | 50% né đòn | Sẵn sàng cho Map 2+ |
| Dragon | 800 | 1.0 | 10 | 100 | Boss: miễn poison, bắn trả tower 20 dmg/3s | Wave 15 |

Công thức scale theo wave:
```
HP thực tế = baseHP × (1 + wave × 0.08)       → Wave 10: HP ×1.8
Giáp thực tế = baseArmor + floor(wave / 10)    → Wave 10: +1 giáp
Gold thực tế = baseGold + floor(wave / 5)       → Wave 10: +2 gold
```

Công thức tính sát thương vào giáp:
```
Sát thương thực = max(1, sát thương gốc - giáp)
Sniper bỏ qua giáp hoàn toàn.
```

### Tạo Model Quái Tuỳ Chỉnh (Thủ Công)

**Bước 1: Chuẩn bị prefab quái**

```
1. Import model 3D (ví dụ: goblin.fbx từ Mixamo, AssetStore, v.v.)

2. Tạo empty GO, đặt tên "Enemy_Goblin_Custom"

3. Kéo model 3D làm con, đặt tên "Body"
   - Đảm bảo model đứng, hướng mặt sang phải (+X)
   - Scale sao cho cao khoảng 0.5-0.8 unit (tuỳ loại quái)

4. Tạo thanh HP:
   a. Tạo Cube con tên "HPBarBG"
      - Vị trí: (0, chiều cao model + 0.3, 0)
      - Scale: (0.6, 0.08, 0.08)
      - Material: màu đen
   b. Tạo Cube con TRONG HPBarBG tên "HPBarFill"
      - Scale: (0.9, 0.9, 0.9)
      - Material: màu xanh lá
      - Thanh này sẽ bị scale X theo tỷ lệ HP

5. Thêm SphereCollider lên GO gốc:
   - Radius = 0.4
   - isTrigger = true
   - CẦN THIẾT để tower phát hiện quái trong tầm bắn

6. Thêm TDEnemy component lên GO gốc:
   - modelRoot = kéo transform "Body" vào
   - hpBarFill = kéo transform "HPBarFill" vào

7. Lưu thành Prefab
```

**Bước 2: Gán vào TDSetup**

Trong Inspector của TDSetup, kéo prefab vào slot tương ứng:

| Slot | Gán model quái nào |
|------|-------------------|
| `goblinPrefab` | Goblin (nhỏ, xanh lá) |
| `orcPrefab` | Orc (vừa, nâu) |
| `wolfPrefab` | Wolf (nhỏ, xám) |
| `skeletonPrefab` | Skeleton (vừa, trắng) |
| `shieldOrcPrefab` | Shield Orc (vừa, có khiên) |
| `batPrefab` | Bat (nhỏ, tím) |
| `magePrefab` | Dark Mage (vừa, tím đậm) |
| `golemPrefab` | Golem (to, nâu) |
| `ghostPrefab` | Ghost (vừa, trong suốt) |
| `dragonPrefab` | Dragon boss (rất to, đỏ) |

Bất kỳ slot nào để null → Setup tự tạo placeholder (capsule/cube màu).

### Tạo Model Tower Tuỳ Chỉnh (Thủ Công)

**Bước 1: Chuẩn bị prefab tower**

```
1. Tạo empty GO, đặt tên "Tower_Custom"

2. Thêm mesh "Base" làm con:
   - Phần đế tower (cylinder, khối hộp, hoặc model đế)
   - Vị trí: (0, 0.25, 0), scale tuỳ

3. Thêm mesh "Turret" làm con:
   - Phần nòng/đầu tower (sẽ xoay hướng về quái)
   - Hướng mặt sang +X ở rotation (0,0,0)
   - Script sẽ tự xoay transform này

4. Thêm empty child "FirePoint":
   - Đặt ở đầu nòng súng (vị trí đạn sẽ spawn ra)

5. Thêm TDTower component lên GO gốc:
   - modelRoot = kéo root transform vào
   - turretPivot = kéo "Turret" transform vào
   - firePoint = kéo "FirePoint" transform vào

6. Thêm range indicator (vòng tròn tầm bắn):
   - Tạo Cylinder con "RangeIndicator"
   - Vị trí: (0, 0.02, 0) → nằm sát đất
   - Material: transparent, alpha ~0.15
   - Set inactive (sẽ bật khi click tower)
   - Kéo vào tower.rangeIndicator

7. Lưu thành Prefab
```

**Bước 2: Gán vào TDSetup**

Kéo prefab vào slot `towerPrefab`. Prefab này được dùng làm base cho
TẤT CẢ loại tower — Setup script sẽ đổi màu theo từng loại và add
TDTower component với chỉ số phù hợp.

Nếu muốn model riêng cho từng loại tower (Arrow model khác Cannon),
sửa `TDGrid.PlaceTower()` để nhận dictionary prefab theo TowerType.

### Tạo Map Tuỳ Chỉnh (Thủ Công)

Map được define trong code (`TDSetup.BuildMap1()`). Tạo map mới:

**Bước 1: Thiết kế trên giấy**

Dùng grid 20 rộng × 12 cao. Ký hiệu:
```
P = Path (đường quái đi)
B = Buildable (ô đặt tower)
· = Blocked (trang trí, không tương tác)
S = Spawn (nơi quái xuất hiện)
E = Exit (nơi quái thoát = mất mạng)
```

Ví dụ map đơn giản:
```
· · · · · · · · · · · · · · · · · · · ·
· S P P P P P · · · · · · · · · · · · ·
· · B B · · P · · · · · · · · · · · · ·
· · B B · · P P P P P · · · · · · · · ·
· · · · · · · B B · P · · · · · · · · ·
· · · · · · · B B · P P P P P P · · · ·
· · · · · · · · · · · · B B · P · · · ·
· · · · · · · · · · · · B B · P P P E ·
· · · · · · · · · · · · · · · · · · · ·
```

**Bước 2: Code map mới**

Thêm hàm `BuildMap2()` trong TDSetup.cs:

```csharp
void BuildMap2()
{
    // Set tile path
    _grid.SetTile(1, 1, TileType.Path);
    _grid.SetTile(2, 1, TileType.Path);
    // ... lặp cho tất cả ô P

    // Set tile buildable
    _grid.SetTile(2, 2, TileType.Buildable);
    _grid.SetTile(3, 2, TileType.Buildable);
    // ... lặp cho tất cả ô B

    // Điểm spawn và exit
    _grid.SetSpawn(1, 1);
    _grid.SetExit(17, 7);

    // Waypoint = các điểm BẺ GÓC trên đường đi, THEO THỨ TỰ
    // Quái đi thẳng giữa các waypoint
    _grid.AddWaypoint(_grid.GridToWorld(1, 1));   // Spawn
    _grid.AddWaypoint(_grid.GridToWorld(6, 1));   // Rẽ xuống
    _grid.AddWaypoint(_grid.GridToWorld(6, 3));   // Rẽ phải
    _grid.AddWaypoint(_grid.GridToWorld(10, 3));  // Rẽ xuống
    // ... thêm waypoint ở mỗi chỗ rẽ
    _grid.AddWaypoint(_grid.GridToWorld(17, 7));  // Exit

    _grid.BuildVisuals();
}
```

**Bước 3: Tạo wave data cho map mới**

Trong `TDData.cs`, thêm mảng wave mới:

```csharp
public static readonly WaveDefinition[] Map2Waves = new[]
{
    new WaveDefinition {
        waveNumber = 1,
        spawnInterval = 0.8f,
        bonusGold = 12,
        entries = new[] {
            new WaveEntry { enemyType = EnemyType.Goblin, count = 10 },
            new WaveEntry { enemyType = EnemyType.Wolf, count = 5 },
        }
    },
    // ... thêm wave
};
```

Sau đó trong `TDSetup`, đổi `_waveManager.Init(TDDatabase.Map2Waves, ...)`.

### Hệ Thống Economy

| Nguồn gold | Số lượng |
|-----------|----------|
| Gold khởi đầu | Easy: 300, Normal: 250, Hard: 200, Nightmare: 150 |
| Giết quái | Gold cơ bản × scaling theo wave |
| Clear wave (không lọt quái) | 20 + wave × 2 |
| Clear wave (có lọt) | 10 + wave × 1 |
| Lãi suất (giữa các wave) | floor(gold hiện tại / 100) × 2, tối đa 20 |
| Gửi wave sớm | +5 gold bonus |
| Bán tower | 70% tổng gold đã đầu tư |

Mạng sống theo độ khó:

| Độ khó | Gold đầu | Mạng | Quái thêm |
|--------|---------|------|-----------|
| Easy | 300 | 20 | Chuẩn |
| Normal | 250 | 15 | Chuẩn |
| Hard | 200 | 10 | +20% số quái |
| Nightmare | 150 | 5 | +50% quái, +30% HP |

### Hệ Thống Sao (Victory)

| Sao | Điều kiện |
|-----|-----------|
| ⭐ | Clear hết tất cả wave |
| ⭐⭐ | Clear với ≥50% mạng còn lại |
| ⭐⭐⭐ | Clear không mất mạng nào (perfect) |

### Điều Khiển

| Input | Hành động |
|-------|-----------|
| Chuột trái | Đặt tower / Chọn tower / Click nút UI |
| Chuột phải | Huỷ đặt tower |
| Escape | Pause |
| F | Bật/tắt tốc độ 2x |

---

## 7. Thay Thế Placeholder Bằng Art Riêng

### Workflow Chung

```
1. Chạy game 1 lần với placeholder → ghi nhận kích thước và vị trí
2. Tạo model 3D / sprite 2D khớp kích thước đó
3. Kéo vào slot tương ứng trên component Setup trong Inspector
4. Bấm Play lại → art của bạn tự thay thế placeholder
```

### Bảng Kích Thước Tham Chiếu

| Game | Object | Placeholder | Kích thước | Ghi chú |
|------|--------|-------------|-----------|---------|
| Flappy | Chim | Sphere | r=0.3 | Hướng mặt phải ở rot (0,0,0) |
| Flappy | Pipe | Cube | 1.2 × 10 × 1 | Origin ở giữa, cao dọc |
| Flappy | Mặt đất | Cube | 20 × 1 × 1 | |
| Runner | Player | Capsule | 0.5 × 1 × 0.5 | Chiều cao đứng ~1 unit |
| Runner | Crate | Cube | 1 × 1 × 1 | |
| Runner | Double Crate | Cube | 1 × 2 × 1 | |
| Runner | Low Beam | Cube | 3 × 0.3 × 2 | |
| Runner | Coin | Cylinder | 0.4 × 0.05 × 0.4 | Đĩa phẳng |
| Runner | Power-up | Sphere | r=0.3 | Mỗi loại 1 màu khác nhau |
| TD | Quái nhỏ | Capsule | scale 0.3 | Goblin, Wolf, Bat |
| TD | Quái vừa | Capsule | scale 0.4 | Orc, Skeleton, Mage, Ghost |
| TD | Quái to | Cube/Capsule | scale 0.6–0.8 | Golem, Dragon |
| TD | Tower đế | Cylinder | 0.7 × 0.25 | |
| TD | Tower nòng | Cube | 0.3 × 0.3 × 0.6 | Xoay hướng về quái |
| TD | Grid tile | Cube | 0.95 × 0.1 × 0.95 | Lưới ô vuông 1 unit |

---

## 8. Xử Lý Lỗi Thường Gặp

### "Tag 'Obstacle' is not defined"

**Nguyên nhân:** Chưa tạo tag trong Unity.
**Cách fix:** Edit > Project Settings > Tags and Layers → thêm tag: 
`Obstacle`, `ScoreZone`, `Coin`, `PowerUp`, `Spike`.

### State machine báo lỗi "State not registered"

**Nguyên nhân:** GameManager chưa kịp chạy `AddState()`, hoặc state từ game trước chưa được cleanup.
**Cách fix (Standalone):** Đảm bảo chỉ có 1 Setup script trong scene.
**Cách fix (Launcher):** Launcher tự gọi `Bill.State.Cleanup()` — nếu vẫn lỗi, check xem `Bill.IsReady` đã true chưa.

### Pool.Spawn trả về null

**Nguyên nhân:** Pool chưa register, hoặc BillGameCore chưa khởi tạo xong.
**Cách fix:** Đảm bảo `BillStartup` chạy trước Setup. Trong Setup, pool register ở `Awake()` / `Build()`.

### Hiện 2 camera chồng nhau

**Nguyên nhân:** Mỗi Setup tạo camera riêng. Nếu chuyển game mà không destroy game cũ.
**Cách fix (Launcher):** Launcher tự destroy game root cũ. Nếu lỗi, check `Teardown()`.
**Cách fix (Standalone):** Mỗi scene chỉ nên có 1 Setup script.

### Tower Defense: quái đi xuyên tường

**Nguyên nhân:** Quái đi theo waypoint, KHÔNG dùng navmesh. Nếu waypoint sai thứ tự hoặc thiếu.
**Cách fix:** Check danh sách waypoint trong `BuildMap1()` — mỗi chỗ đường rẽ phải có 1 waypoint. Thứ tự phải đúng từ spawn đến exit.

### Tower Defense: tower không bắn

**Nguyên nhân:** Tower dùng `Physics.OverlapSphereNonAlloc` tìm quái. Quái cần `SphereCollider` (isTrigger=true).
**Cách fix:** Kiểm tra prefab quái đã có SphereCollider chưa. Setup tự thêm cho placeholder, nhưng custom prefab cần tự thêm.

### Runner: player rơi xuyên đất

**Nguyên nhân:** Player không dùng physics cho ground detection — kiểm tra `position.y <= 0.5f` trực tiếp.
**Cách fix:** Đảm bảo mặt đất chunk ở y = 0 (player đứng ở y = 0.5).

### Lỗi namespace / "type not found" sau khi import

**Nguyên nhân:** Assembly definition không khớp.
**Cách fix:**
1. Kiểm tra asmdef `"references"` trỏ đúng tên asmdef của BillGameCore
2. Trường `"name"` trong asmdef phải khớp với giá trị reference
3. Nếu không muốn dùng asmdef: **xoá tất cả file .asmdef** → mọi thứ compile vào Assembly-CSharp chung, vẫn chạy bình thường (chỉ mất isolation)

### Chuyển game bằng Launcher bị lag/crash

**Nguyên nhân:** Pool object hoặc timer chưa được cleanup sạch.
**Cách fix:** Launcher đã gọi `Bill.Pool.ReturnAll()` và `Bill.Timer.CancelAll()`. Nếu vẫn lỗi, kiểm tra xem có coroutine hoặc async operation nào đang chạy ngoài tầm quản lý của Bill.Timer.

---

## Tổng Kết File

| Thư mục | Số file .cs | Dòng code | Mô tả |
|---------|------------|-----------|-------|
| BillFlappy/Scripts/ | 7 | ~1,400 | Flappy Bird clone |
| BillRunner/Scripts/ | 7 | ~1,900 | Endless Runner |
| BillDefense/Scripts/ | 9 | ~2,900 | Tower Defense |
| Launcher/ | 1 | ~280 | Bộ chuyển game |
| **Tổng** | **24** | **~6,500** | |

Ngoài ra còn: 4 file .asmdef, 1 README.md, 1 GDD.md, 1 SETUP_GUIDE.md (file này).

---

**Hỏi gì thêm thì check file `BillSampleGame.cs` trong BillGameCore/Samples/ để 
xem cách framework dùng các service, hoặc đọc GDD_BillGameCore_Samples.md để 
xem full game design cho cả 3 game.**
