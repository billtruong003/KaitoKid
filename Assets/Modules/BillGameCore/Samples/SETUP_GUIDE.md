# BillGameCore v3 — Sample Games Setup Guide

---

## Table of Contents

1. Project Structure & Namespace Rules
2. Two Ways to Run: Standalone vs Launcher
3. Required Unity Setup (Tags, Layers, Build Settings)
4. Game 1: Bill Flappy — Setup
5. Game 2: Bill Runner — Detailed Setup
6. Game 3: Bill Defense — Detailed Setup
7. Replacing Placeholder Art with Custom Assets
8. Troubleshooting

---

## 1. Project Structure & Namespace Rules

### Folder Layout

```
Assets/
├── BillGameCore/                     ← The framework (from BillGameCore_v3.zip)
│   ├── Runtime/
│   ├── Editor/
│   └── BillGameCore.asmdef
│
├── BillSamples/
│   ├── Launcher/                     ← Game switcher (optional)
│   │   ├── SampleGameLauncher.cs
│   │   └── BillSamples.Launcher.asmdef
│   │
│   ├── BillFlappy/                   ← Game 1
│   │   ├── Scripts/  (7 files)
│   │   └── BillSamples.Flappy.asmdef
│   │
│   ├── BillRunner/                   ← Game 2
│   │   ├── Scripts/  (7 files)
│   │   └── BillSamples.Runner.asmdef
│   │
│   └── BillDefense/                  ← Game 3
│       ├── Scripts/  (9 files)
│       └── BillSamples.TowerDefense.asmdef
```

### Assembly Definitions

Each game is compiled as a separate assembly. This ensures:
- No cross-references between games (Flappy can't accidentally use Runner classes)
- Each game only depends on BillGameCore
- The Launcher depends on all 3 + BillGameCore
- Faster incremental compilation

If BillGameCore has its own .asmdef, make sure the "name" field matches
what the sample asmdefs reference. If BillGameCore does NOT have an asmdef,
remove the `"references": ["BillGameCore"]` lines from all sample asmdefs
and they'll compile against the default Assembly-CSharp.

### Namespace Isolation

| Game | Namespace | State Types | Pool Key Prefix |
|------|-----------|-------------|-----------------|
| Flappy | `BillSamples.Flappy` | `FlappyMenuState`, `FlappyPlayState`, etc. | `Pipe` |
| Runner | `BillSamples.Runner` | `RunnerMenuState`, `RunnerPlayState`, etc. | `chunk_*`, `coin_*`, `vfx_dust` |
| Defense | `BillSamples.TowerDefense` | `TDMenuState`, `TDBuildPhaseState`, etc. | `enemy_*`, `proj_*`, `vfx_*`, `ui_*` |

All state types, event types, and pool keys are unique across games.
They will NOT conflict even when all 3 exist in the same project.

---

## 2. Two Ways to Run

### Option A: Standalone Mode (Separate Scenes)

Best for: testing one game at a time, simple setup.

1. Create 3 scenes: `Scene_Flappy`, `Scene_Runner`, `Scene_Defense`
2. In each scene, create an empty GameObject
3. Attach the corresponding Setup script (`FlappySetup`, `RunnerSetup`, `TDSetup`)
4. Leave `autoBootstrap = true` (default)
5. Press Play — the scene self-builds

Each scene is fully independent. The Setup script creates camera, canvas, 
pools, prefabs, and all game objects. You can switch between scenes using
File > Open Scene.

**Important:** BillGameCore's `BillStartup` prefab (or bootstrap scene) 
must run BEFORE these scenes. Either:
- Put `BillStartup` in every scene, OR
- Have a "Boot" scene that initializes BillGameCore then loads game scenes, OR  
- Use [RuntimeInitializeOnLoadMethod] in BillGameCore if it supports that

### Option B: Launcher Mode (Single Scene, Switch Games)

Best for: demo purposes, showing all 3 games in one build.

1. Create one scene: `Scene_Launcher`
2. Ensure BillGameCore bootstraps in this scene
3. Create an empty GO → attach `SampleGameLauncher.cs`
4. Optionally: create 3 prefabs from each Setup GO (with assets assigned)
   and drag them into the Launcher's prefab slots
5. If not using prefabs, the Launcher will create default Setups automatically
6. Press Play → menu appears with 3 game buttons
7. Press F12 during any game to return to launcher

The Launcher handles:
- `Bill.State.Cleanup()` between switches
- `Bill.Timer.CancelAll()` to clear dangling timers
- `Bill.Pool.ReturnAll()` to flush all object pools
- `Destroy()` of all previous game objects
- Camera reset

---

## 3. Required Unity Setup

### Tags (REQUIRED)

Go to **Edit > Project Settings > Tags and Layers**, add these tags:

| Tag | Used By | Purpose |
|-----|---------|---------|
| `Obstacle` | Flappy pipes, Runner obstacles | Trigger damage/death |
| `ScoreZone` | Flappy pipe gaps | Trigger score increment |
| `Coin` | Runner coins | Trigger coin collection |
| `PowerUp` | Runner items | Trigger power-up activation |
| `Spike` | Runner kill zones | Trigger instant death |

### Physics (RECOMMENDED)

For Flappy (2D mode):
- Edit > Project Settings > Physics 2D
- Set layer collision matrix if needed (default is fine)

For Runner and Tower Defense (3D mode):
- Edit > Project Settings > Physics
- Ensure triggers detect each other (default is fine)

### Build Settings

If using Launcher mode, only `Scene_Launcher` needs to be in Build Settings.
If using Standalone mode, add all scenes you want to test.

---

## 4. Game 1: Bill Flappy — Setup

Flappy is the simplest. The Setup script creates everything.

### Quick Start
1. New scene → empty GO → add `FlappySetup`
2. Play

### What Gets Created Automatically

| Object | Description |
|--------|-------------|
| Main Camera | Orthographic, size=5, portrait-oriented |
| Background | Quad with sky color |
| Ground | Cube at y=-5, tagged "Obstacle" |
| Ceiling | Invisible collider at y=6, tagged "Obstacle" |
| Bird | Yellow sphere + orange beak + white eye, with `FlappyBird` script |
| Pipe Prefab | Green cube pair with gap + score trigger, pooled (warm=8) |
| PipeSpawner | Spawns pipes via `Bill.Timer.Repeat` |
| FlappyCanvas | Full UI with menu, HUD, game over panel |
| FlappyGameManager | Wires everything, manages score + difficulty |

### Customizing Art

On the `FlappySetup` component in Inspector:

| Slot | What to Assign |
|------|---------------|
| Bird Model Prefab | Your bird 3D model or 2D sprite. Will be parented under the Bird GO. The script rotates `modelRoot`, so make sure your model faces right at rotation (0,0,0). |
| Top Pipe Prefab | Your pipe model/sprite for the top obstacle. Should be tall (10 units). |
| Bottom Pipe Prefab | Same as top, or reuse top pipe flipped. |
| Background Material | Material for the background quad. |
| Ground Material | Material for the ground strip. |

### Controls

| Input | Action |
|-------|--------|
| Tap / Space / Left Click | Flap (menu: start game) |
| Escape | Pause / Resume |

---

## 5. Game 2: Bill Runner — Detailed Setup

Runner is more complex. While the Setup script creates all gameplay objects,
you'll want to customize obstacles, coins, and map chunks for a proper game.

### Quick Start
1. New scene → empty GO → add `RunnerSetup`
2. Play (3D side-scroller with placeholder shapes)

### What Gets Created Automatically

| Object | Description |
|--------|-------------|
| Main Camera | Perspective, FOV=60, follows player with offset (5, 3, -10) |
| Player | Blue capsule + eye, with `RunnerPlayer` (jump/slide/damage/power-ups) |
| 6 Chunk Prefabs | Each 20 units wide, registered in pool (3 instances each) |
| Coin Prefab | Yellow cylinder, pooled (warm=20) |
| VFX_Dust | Particle system for jump dust (warm=5) |
| 4 Parallax Layers | Colored quads at different depths and scroll speeds |
| RunnerCanvas | Menu, HUD (distance, coins, hearts), Game Over panel |
| ChunkSpawner | Infinite spawning ahead of player, despawn behind |
| RunnerGameManager | Difficulty curve, speed ramping, scoring |

### Chunk System — How It Works

Map chunks are pre-built 20-unit sections with embedded obstacles and coins.
The spawner picks chunks based on distance traveled:

| Distance | Available Chunks |
|----------|-----------------|
| 0m+ | `chunk_flat_easy` — 1 crate, line of 5 coins |
| 200m+ | `chunk_flat_med` — crate + double crate + low beam, arc coins |
| 300m+ | `chunk_slide` — 2 low beams (must slide), low coins |
| 400m+ | `chunk_elevated` — raised platform with coins on top |
| 600m+ | `chunk_gap` — split ground with kill zone, coins luring over gap |
| 800m+ | `chunk_mixed_hard` — saw + beam + crates + power-up reward |

### Manual Customization: Creating Better Chunks

The auto-generated chunks are functional but basic. For a polished game,
create your own chunk prefabs:

**Step 1: Create a chunk prefab**
```
1. Create empty GO, name it "MyChunk_Forest_01"
2. Add ground: Cube at (10, -0.5, 0), scale (20, 1, 3)
3. Add obstacles:
   - Crate at (5, 0.5, 0), tag "Obstacle", add RunnerObstacle component
   - Low beam at (12, 1.5, 0), tag "Obstacle", add RunnerObstacle
4. Add coins:
   - Cylinder at (3, 1, 0), tag "Coin", add RunnerCollectible
   - Repeat at (4.2, 1, 0), (5.4, 1.5, 0)... (make coin arcs!)
5. Add power-up (optional):
   - Sphere at (18, 1.5, 0), tag "PowerUp", add RunnerPowerUp
   - Set itemKey = "item_shield", duration = 0
6. Save as prefab in Assets/BillSamples/BillRunner/Prefabs/
```

**Step 2: Register in pool**
In `RunnerSetup.cs`, inside `CreateChunkPrefabs()`, add:
```csharp
Bill.Pool.Register("chunk_forest_01", yourPrefab, 3);
```

**Step 3: Add to spawner**
In `RunnerChunkSpawner`, add your key to the appropriate category array.

### Obstacle Setup Reference

Every obstacle needs these components:

| Component | Tag | Fields |
|-----------|-----|--------|
| **RunnerObstacle** on the GO | `Obstacle` | `type` = enum, `instantKill` = true for spikes |
| **BoxCollider** (3D) or **BoxCollider2D** | — | `isTrigger = true` |

Obstacle behaviors:

| Type | Setup |
|------|-------|
| Static (crate, beam) | Just place it. No extra settings. |
| Moving (barrel) | Set `moveSpeed = 4`, `moveDirection = Vector3.left` |
| Sine wave (bird, saw) | Set `sinWave = true`, `sinAmplitude = 0.5`, `sinFrequency = 2` |
| Instant kill (spike) | Set `instantKill = true`, OR use tag `Spike` |
| Falling platform | (Requires custom script — not in base, extend RunnerObstacle) |

### Coin Setup Reference

| Component | Tag | Fields |
|-----------|-----|--------|
| **RunnerCollectible** | `Coin` | `coinType` = Bronze/Silver/Gold |
| **Collider** | — | `isTrigger = true`, radius ~0.3 |

Coin values: Bronze = 1, Silver = 3, Gold = 10.
Coins auto-rotate and bob. The `coinDoubler` power-up doubles values.

### Power-Up Setup Reference

| Component | Tag | Fields |
|-----------|-----|--------|
| **RunnerPowerUp** | `PowerUp` | `itemKey`, `duration` |
| **Collider** | — | `isTrigger = true`, radius ~0.4 |

Available power-ups:

| itemKey | Duration | Effect |
|---------|----------|--------|
| `item_magnet` | 8s | All coins in 5-unit range fly to player |
| `item_shield` | 0 (1 hit) | Absorb 1 hit. Set duration = 0. |
| `item_speed` | 5s | 1.5x speed + invincible |
| `item_2x` | 10s | All coins worth double |
| `item_tiny` | 6s | Character shrinks 50%, fits under beams |

### Parallax Background Customization

Replace the placeholder quads with your own sprites:

1. Create a wide sprite (at least 40 units wide, or tileable)
2. In `RunnerSetup.CreateBGLayer()`, replace the Quad with your sprite
3. Adjust `speedRatio`: 0.05 = barely moves (sky), 1.0 = moves with camera
4. Adjust `z` position: higher z = further back

### Controls

| Input | Action |
|-------|--------|
| Space / Left Click | Jump (grounded), Double Jump (airborne, if unlocked) |
| S / Down Arrow | Slide (0.6s, shrinks hitbox) |
| Escape | Pause |
| Tab | Shop (from menu only) |

---

## 6. Game 3: Bill Defense — Detailed Setup

Tower Defense has the most moving parts. The Setup creates a fully playable
15-wave game, but customizing tower/enemy visuals and adding maps requires
manual work.

### Quick Start
1. New scene → empty GO → add `TDSetup`
2. Play (top-down view, placeholder cubes/cylinders)

### What Gets Created Automatically

| Object | Description |
|--------|-------------|
| Main Camera | Orthographic, size=8, angled 70° top-down |
| Grid (20×12) | Tile cubes: brown=path, green=buildable, dark=blocked |
| Waypoints | 12 red sphere markers showing enemy path |
| SpawnPoint | Red cube at path start |
| ExitPoint | Blue cube at path end |
| 10 Enemy Prefabs | Colored capsules/cubes/spheres, each pooled |
| 6 Projectile Prefabs | Small colored spheres, pooled |
| VFX Prefabs | Explosion particles, line renderer, floating text |
| TDCanvas | Full UI: top bar, tower panel, info popup, game over |
| WaveManager | 15 pre-defined waves for Map 1 |
| TDGameManager | Economy, lives, build phase timer, state flow |

### Tower Placement — How It Works

1. Click a tower button on the bottom bar (Arrow, Cannon, Ice, etc.)
2. Hover over the grid — green tiles highlight valid, red = invalid
3. Click to place (gold is deducted)
4. Click an existing tower to see info panel: Upgrade, Sell, change Target mode
5. Right-click or Escape to cancel placement

### Tower Definitions (from TDData.cs)

| Tower | Cost | DPS | Range | Special |
|-------|------|-----|-------|---------|
| Arrow | 80g | 15/s | 3.5 | Lv3: double arrow |
| Cannon | 120g | 17/s | 3.0 | AoE splash 1.2→2.0 radius. Lv3: stun |
| Ice | 100g | 8/s | 3.0 | Slow 30→50%. Lv3: 15% freeze |
| Lightning | 150g | 15/s | 4.0 | Chain 3→6 targets. Lv3: overcharge |
| Sniper | 200g | 18/s | 6.0 | Ignores armor, crit 20→35%. Lv3: execute <15% HP |
| Poison | 130g | 5/s | 3.0 | AoE DoT 4→12/s, stacks 3→5. Lv3: plague spread |

All towers have 3 upgrade levels. Sell refund = 70% of total invested.

### Enemy Types (from TDData.cs)

| Enemy | HP | Speed | Armor | Gold | Special | First Wave |
|-------|-----|-------|-------|------|---------|------------|
| Goblin | 30 | 2.0 | 0 | 5 | None | 1 |
| Orc | 80 | 1.5 | 2 | 10 | Tanky | 3 |
| Wolf | 25 | 3.5 | 0 | 8 | Fast | 4 |
| Skeleton | 50 | 2.0 | 0 | 8 | Resurrects once at 50% HP | 6 |
| Shield Orc | 100 | 1.3 | 5 | 15 | Shield blocks 3 hits, then armor=0 | 8 |
| Bat | 15 | 2.5 | 0 | 3 | Flying (ignores path) | 10 |
| Dark Mage | 60 | 1.8 | 1 | 12 | Heals nearby allies 5 HP/s | 12 |
| Golem | 250 | 0.8 | 8 | 25 | Immune to slow | 15* |
| Ghost | 40 | 2.2 | 0 | 10 | 50% dodge chance | 17* |
| Dragon | 800 | 1.0 | 10 | 100 | Immune to poison, boss | 15 |

*Golem and Ghost appear in Map 2+ (not in Map 1's 15 waves, but prefabs are ready)

Scaling per wave: `HP × (1 + wave × 0.08)`, `Armor + floor(wave/10)`

### Manual Setup: Custom Enemy Models

**Step 1: Create enemy prefab**
```
1. Import your 3D model (e.g., goblin.fbx)
2. Create empty GO "Enemy_Goblin_Custom"
3. Add your model as child, name it "Body"
4. Add HP bar:
   a. Create cube child "HPBarBG" at y = (model height + 0.3)
   b. Scale: (0.6, 0.08, 0.08), material: black
   c. Create cube child "HPBarFill" under HPBarBG
   d. Scale: (0.9, 0.9, 0.9), material: green
5. Add SphereCollider, radius=0.4, isTrigger=true
6. Add TDEnemy component:
   - modelRoot = Body transform
   - hpBarFill = HPBarFill transform
7. Save as prefab
```

**Step 2: Assign to TDSetup**
In the TDSetup Inspector, drag your prefab into the matching slot:
`goblinPrefab`, `orcPrefab`, `wolfPrefab`, etc.

The setup script checks each slot — if assigned, uses your prefab;
if null, creates a placeholder.

### Manual Setup: Custom Tower Models

**Step 1: Create tower prefab**
```
1. Create empty GO "Tower_Arrow_Custom"
2. Add base mesh as child "Base" (cylinder or custom)
3. Add turret mesh as child "Turret" (this rotates to aim)
4. Set Turret to face +X at rotation (0,0,0) — it will auto-aim
5. Add a child empty "FirePoint" at the barrel tip
6. Add TDTower component:
   - modelRoot = root transform
   - turretPivot = Turret transform
   - firePoint = FirePoint transform
7. Add range indicator:
   - Flat cylinder child "RangeIndicator", y=0.02
   - Transparent material, alpha ~0.15
   - Set inactive by default
   - Assign to tower.rangeIndicator
8. Save as prefab
```

**Step 2: Assign to TDSetup**
Drag into the `towerPrefab` slot. This single prefab is used as the
base for ALL tower types — the Setup script colors them per type and
adds the TDTower component with correct stats.

If you want unique models per tower type, modify `TDGrid.PlaceTower()`
to accept a dictionary of prefabs per TowerType.

### Manual Setup: Custom Map

The map is defined in `TDSetup.BuildMap1()`. To create a new map:

**Step 1: Design on paper**
Use the grid: 20 wide × 12 tall. Mark:
- `P` = Path tiles (enemies walk here)
- `B` = Buildable tiles (player places towers)
- `·` = Blocked tiles (decoration)
- `S` = Spawn point
- `E` = Exit point

**Step 2: Code it**
```csharp
void BuildMap2()
{
    // Set path tiles
    _grid.SetTile(x, y, TileType.Path);
    
    // Set buildable tiles
    _grid.SetTile(x, y, TileType.Buildable);
    
    // Spawn and exit
    _grid.SetSpawn(1, 5);
    _grid.SetExit(18, 5);
    
    // Waypoints (turning points of the path, IN ORDER)
    _grid.AddWaypoint(_grid.GridToWorld(1, 5));
    _grid.AddWaypoint(_grid.GridToWorld(10, 5));
    // ... more waypoints at each bend
    
    _grid.BuildVisuals();
}
```

**Step 3: Create wave data**
In `TDData.cs`, add a new array like `Map2Waves` following the same
`WaveDefinition` format as `Map1Waves`. Can include new enemy types
(Golem, Ghost) for harder maps.

### Economy Reference

| Source | Amount |
|--------|--------|
| Starting gold | Easy: 300, Normal: 250, Hard: 200, Nightmare: 150 |
| Kill enemy | Base gold × scaling |
| Wave clear (perfect) | 20 + wave × 2 |
| Wave clear (leaked) | 10 + wave × 1 |
| Interest (between waves) | floor(gold / 100) × 2, max 20 |
| Send wave early | +5 bonus gold |
| Sell tower | 70% of total invested |

### Controls

| Input | Action |
|-------|--------|
| Left Click | Place tower / Select tower / UI buttons |
| Right Click | Cancel placement |
| Escape | Pause |
| F | Toggle 2x speed |

---

## 7. Replacing Placeholder Art

### General Workflow

1. Run the game once with placeholders — note the sizes and positions
2. Create your 3D model / 2D sprite matching those dimensions
3. Assign to the Setup script's Inspector slots
4. Play again — your art replaces placeholders automatically

### Size Reference

| Game | Object | Placeholder Size | Notes |
|------|--------|-----------------|-------|
| Flappy | Bird | Sphere r=0.3 | Face right at (0,0,0) rotation |
| Flappy | Pipe | Cube 1.2×10×1 | Origin at center, tall |
| Runner | Player | Capsule 0.5×1×0.5 | Standing height ~1 unit |
| Runner | Crate | Cube 1×1×1 | |
| Runner | Coin | Cylinder 0.4×0.05×0.4 | Flat disc |
| Runner | Power-up | Sphere r=0.3 | |
| TD | Enemy (small) | Capsule scale 0.3 | Goblin, Wolf, Bat |
| TD | Enemy (medium) | Capsule scale 0.4 | Orc, Skeleton, Mage |
| TD | Enemy (large) | Cube/Capsule 0.6–0.8 | Golem, Dragon |
| TD | Tower base | Cylinder 0.7×0.25 | |
| TD | Tower turret | Cube 0.3×0.3×0.6 | Points toward enemy |
| TD | Grid tile | Cube 0.95×0.1×0.95 | 1-unit grid |

---

## 8. Troubleshooting

### "Tag 'Obstacle' is not defined"
Add required tags in Edit > Project Settings > Tags and Layers.

### States not registering / "State not found"
Each GameManager calls `Bill.State.AddState()` in its `Start()`.
If using Launcher mode, the Launcher calls `Bill.State.Cleanup()`
before switching games. If running standalone, make sure only ONE
game's Setup script is in the scene.

### Pool objects not spawning
Check that `Bill.IsReady` is true before Setup runs. BillGameCore's
bootstrap must complete first. The Setup scripts check this internally.

### Multiple cameras rendering
Each Setup creates a camera. In Launcher mode, the previous game's
camera is destroyed with the game root. If you see double cameras,
ensure Teardown() is being called.

### Tower Defense: enemies walking through walls
Enemies follow waypoints, not navmesh. Make sure your waypoints
list in `BuildMap1()` follows the exact path. Each waypoint should
be at a turn/bend in the path.

### Tower Defense: towers not shooting
Towers use `Physics.OverlapSphereNonAlloc` to find targets. Enemies
need a `SphereCollider` (isTrigger=true). The Setup adds these
automatically, but custom enemy prefabs need them too.

### Runner: player falls through ground
The player doesn't use physics for ground detection — it checks
`position.y <= 0.5f` directly. Make sure chunk ground surfaces
are at y = 0 (the player stands at y = 0.5).

### Namespace errors after import
If you see "type or namespace not found" errors, check:
1. Assembly definitions reference BillGameCore correctly
2. The asmdef "name" fields match the reference strings
3. If NOT using asmdefs, delete all .asmdef files and everything
   compiles into Assembly-CSharp together

---

**Questions? Check BillSampleGame.cs in BillGameCore/Samples/ for
the framework's own usage examples, or look at the GDD document
for the full game design specifications.**
