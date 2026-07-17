# BillGameCore v3 — 3 Game Mẫu

## Bắt Đầu Nhanh

Mỗi game có 1 **Mega Setup Script** — gắn vào empty GO, bấm Play → tạo toàn bộ scene.

### Bill Flappy (Clone Flappy Bird)
1. Tạo scene trống
2. Tạo empty GameObject → gắn `FlappySetup.cs`
3. Bấm Play
4. (Tuỳ chọn) Kéo model chim/pipe vào Inspector để thay placeholder

### Bill Runner (Endless Runner)
1. Tạo scene trống
2. Tạo empty GameObject → gắn `RunnerSetup.cs`
3. Bấm Play
4. (Tuỳ chọn) Kéo model nhân vật, chướng ngại vật vào Inspector

### Bill Defense (Tower Defense)
1. Tạo scene trống
2. Tạo empty GameObject → gắn `TDSetup.cs`
3. Bấm Play
4. (Tuỳ chọn) Kéo model quái/tower vào Inspector

## Tag Bắt Buộc

Thêm các tag sau trong Edit > Project Settings > Tags & Layers:
`Obstacle`, `ScoreZone`, `Coin`, `PowerUp`, `Spike`

## Cấu Trúc File

```
BillFlappy/Scripts/
├── FlappySetup.cs         ← MEGA SETUP (gắn vào GO)
├── FlappyBird.cs           Điều khiển chim + vật lý
├── FlappyPipe.cs           Pipe + spawner
├── FlappyGameManager.cs    Điểm, độ khó, flow
├── FlappyUI.cs             HUD + panel Game Over
├── FlappyStates.cs         Các state riêng
└── FlappyEvents.cs         Định nghĩa event

BillRunner/Scripts/
├── RunnerSetup.cs          ← MEGA SETUP (gắn vào GO)
├── RunnerPlayer.cs         Player: nhảy, slide, nhận sát thương, power-up
├── RunnerEntities.cs       Component: Obstacle, Coin, PowerUp
├── RunnerWorld.cs           Chunk spawner, parallax, camera follow
├── RunnerGameManager.cs    Điểm, độ khó, shop, flow + UI
├── RunnerStates.cs         Các state riêng
└── RunnerEvents.cs         Định nghĩa event

BillDefense/Scripts/
├── TDSetup.cs              ← MEGA SETUP (gắn vào GO)
├── TDData.cs               Bảng dữ liệu tower/quái/wave
├── TDEnemy.cs              Quái: di chuyển, HP, giáp, kỹ năng đặc biệt
├── TDTower.cs              Tower: ngắm bắn, nâng cấp, bán
├── TDGrid.cs               Map grid + đặt tower
├── TDGameManager.cs        Economy, wave, state flow
├── TDUI.cs                 HUD, panel tower, game over
├── TDStates.cs             Các state riêng
└── TDEvents.cs             Định nghĩa event

Launcher/
├── SampleGameLauncher.cs   Chuyển giữa 3 game (F12 quay về)
└── BillSamples.Launcher.asmdef
```

## Service BillGameCore Sử Dụng

| Service | Flappy | Runner | TD |
|---------|--------|--------|----|
| Bill.State | 5 state | 6 state | 9 state |
| Bill.Pool | Pipe ×8 | Chunk, Coin, VFX | Quái ×10, Đạn ×6, VFX ×3 |
| Bill.Timer | Pipe spawner | Item duration | Wave spawner, build timer |
| Bill.Tween | UI anim, death | Item, coin, UI | Tower, quái, UI |
| Bill.Audio | SFX + Music | SFX + Music | SFX riêng mỗi tower + Music |
| Bill.Save | Điểm cao | Coin, upgrade, skin | Sao, tiến trình map |
| Bill.Events | 5 event | 8 event | 13 event |
| Bill.Config | Độ khó | Balance | Chỉ số tower/quái |
| Bill.Cheat | Set điểm | Thêm coin, god mode | Thêm gold, skip wave |

## Chạy Cả 3 Game Trong 1 Project

Hai cách:

**Cách 1 — Standalone:** Mỗi game 1 scene riêng, `autoBootstrap = true`

**Cách 2 — Launcher:** 1 scene duy nhất, gắn `SampleGameLauncher.cs`,
chọn game từ menu. Bấm F12 quay về. Launcher tự cleanup state machine,
timer, pool, audio khi chuyển game.

Đọc `SETUP_GUIDE_VI.md` để xem hướng dẫn chi tiết cho từng game.

## Tuỳ Chỉnh Art

Tất cả Setup script nhận prefab tuỳ chọn qua Inspector.
Khi để null → dùng placeholder (cube, sphere, capsule).

**Thay art riêng:**
1. Chạy game 1 lần → xem placeholder
2. Tạo model 3D khớp kích thước
3. Kéo vào slot trong Inspector
4. Bấm Play → model thay thế placeholder

Tower Defense: tower cần child "Turret" (xoay ngắm quái) + child "FirePoint" (nơi đạn spawn).
Quái cần `SphereCollider` isTrigger + child "HPBarFill" (thanh máu).
Xem chi tiết trong SETUP_GUIDE_VI.md mục 6.
