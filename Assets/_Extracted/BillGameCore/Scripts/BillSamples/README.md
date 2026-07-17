# BillGameCore v3 — Sample Games

## Quick Start

Each game has a **Mega Setup Script** — one component that builds the ENTIRE scene from scratch.

### Bill Flappy (Flappy Bird Clone)
1. Create empty scene
2. Add empty GameObject → Attach `FlappySetup.cs`
3. Press Play
4. (Optional) Assign your bird/pipe models in the Inspector

### Bill Runner (Endless Runner)
1. Create empty scene
2. Add empty GameObject → Attach `RunnerSetup.cs`
3. Press Play
4. (Optional) Assign character model, obstacle prefabs in Inspector

### Bill Defense (Tower Defense)
1. Create empty scene
2. Add empty GameObject → Attach `TDSetup.cs`
3. Press Play
4. (Optional) Assign enemy/tower 3D models in Inspector

## Required Tags

Add these tags in Edit > Project Settings > Tags & Layers:
- `Obstacle`
- `ScoreZone`
- `Coin`
- `PowerUp`
- `Spike`

## File Structure

```
BillFlappy/Scripts/
├── FlappySetup.cs         ← MEGA SETUP (drop this on GO)
├── FlappyBird.cs           Bird controller + physics
├── FlappyPipe.cs           Pipe behavior + spawner
├── FlappyGameManager.cs    Score, difficulty, flow
├── FlappyUI.cs             HUD + GameOver panel
├── FlappyStates.cs         Custom game states
└── FlappyEvents.cs         Event definitions

BillRunner/Scripts/
├── RunnerSetup.cs          ← MEGA SETUP (drop this on GO)
├── RunnerPlayer.cs         Player: jump, slide, damage, power-ups
├── RunnerEntities.cs       Obstacle, Coin, PowerUp components
├── RunnerWorld.cs           Chunk spawner, parallax, camera
├── RunnerGameManager.cs    Score, difficulty, shop, flow + UI
├── RunnerStates.cs         Custom game states
└── RunnerEvents.cs         Event definitions

BillDefense/Scripts/
├── TDSetup.cs              ← MEGA SETUP (drop this on GO)
├── TDData.cs               All tower/enemy/wave data tables
├── TDEnemy.cs              Enemy: pathing, HP, armor, specials
├── TDTower.cs              Tower: targeting, shooting, upgrades
├── TDGrid.cs               Map grid + tower placement
├── TDGameManager.cs        Economy, waves, state flow
├── TDUI.cs                 HUD, tower panel, game over
├── TDStates.cs             Custom game states
└── TDEvents.cs             Event definitions
```

## BillGameCore Services Used

| Service | Flappy | Runner | TD |
|---------|--------|--------|----|
| Bill.State | 5 states | 6 states | 9 states |
| Bill.Pool | Pipes ×8 | Chunks, Coins, VFX | Enemies ×10, Projectiles ×6, VFX ×3 |
| Bill.Timer | Pipe spawner | Item durations | Wave spawner, build timer |
| Bill.Tween | UI anims, death | Items, coins, UI | Tower place/sell, enemy death |
| Bill.Audio | SFX + Music | SFX + Music | Per-tower SFX + Music |
| Bill.Save | Highscore | Coins, upgrades, skins | Stars, map progress |
| Bill.Events | 5 events | 8 events | 13 events |
| Bill.Config | Difficulty | Balance | Tower/enemy stats |
| Bill.Cheat | Set score | Add coins, godmode | Add gold, skip wave |

## Customizing Visuals

All Setup scripts accept optional prefab references in the Inspector.
When left null, placeholder primitives (cubes, spheres, capsules) are used.

**To use your own art:**
1. Run the scene once to see placeholders
2. Create your 3D model prefab
3. Drag it into the corresponding slot on the Setup component
4. Press Play again — your model replaces the placeholder

Tower Defense towers are created via `TDGrid.CreatePlaceholderTower()` — 
override by assigning `towerPrefab` on `TDSetup`. Your prefab should have
a child named "Turret" (for aiming) and the `TDTower` component will be
added automatically.
