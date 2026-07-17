# BillGameCore v3 — Sample Games Design Document

---

# GAME 1: BILL FLAPPY (Flappy Bird Clone)

## 1.1 Overview

| Field | Value |
|-------|-------|
| Genre | Arcade / One-tap |
| Camera | 2D Side-scroll, fixed camera |
| Resolution | 1080×1920 (Portrait) |
| Target | Mobile (iOS/Android) |
| Session | 30s–2min |
| Core Loop | Tap → Fly → Avoid Pipes → Score → Die → Retry |

## 1.2 Game Flow (State Machine)

```
BootState → MenuState → GameplayState → GameOverState
                ↑            ↓ (pause)        ↓
                |        PauseState      MenuState
                ←────────────────────────────┘
```

### State Details

| State | Enter | Tick | Exit |
|-------|-------|------|------|
| **BootState** | Load configs, warm pools | — | → MenuState |
| **MenuState** | Show title + best score, idle bird bob animation | Wait for tap | Hide UI |
| **GameplayState** | Reset score, start pipe spawner, play BGM | Bird physics, pipe scroll, collision check | Stop spawner, stop BGM |
| **PauseState** | TimeScale=0, show pause panel | Wait for resume tap | TimeScale=1 |
| **GameOverState** | Death SFX, screen shake, show score/best, save highscore | Wait for retry/menu tap | Clean up pipes |

## 1.3 Bird Mechanics

### Physics
| Param | Value | Note |
|-------|-------|------|
| Gravity | -9.8 units/s² | Constant downward pull |
| Tap Force | 5.5 units/s | Instant upward velocity on tap |
| Max Fall Speed | -8 units/s | Clamp to prevent bullet-drop |
| Rotation | velocity-based | Nose up when rising (+30°), nose down when falling (-90°) |
| Rotation Speed | 8°/frame lerp | Smooth rotation transition |
| Hit Box | Circle r=0.35 | Slightly smaller than sprite for "fair feel" |

### Bird Animation States
```
Idle (menu)     → Flap (gameplay, on tap) → Glide (falling) → Hit (collision) → Fall (dead drop)
    ↑                                                                                    ↓
    ←──────────────────────── Retry ←───────────────────────────────────────────── Ground ┘
```

| Anim State | Frames | Speed | Trigger |
|------------|--------|-------|---------|
| **Idle** | 2 frames wing bob | 0.3s loop | MenuState enter |
| **Flap** | 3 frames wing down→mid→up | 0.15s total | On tap |
| **Glide** | 1 frame wings level | — | After flap finishes |
| **Hit** | 1 frame eyes X, wings tucked | — | On collision |
| **Fall** | Rotate to -90° + drop | 0.5s | After Hit, no input |

## 1.4 Pipe System

### Spawner Config
| Param | Value |
|-------|-------|
| Spawn Interval | 1.8s (Timer.Repeat) |
| Pipe Speed | 3.0 units/s (moves left) |
| Gap Size | 3.2 units (between top & bottom pipe) |
| Gap Y Range | [-1.5, 2.5] random |
| Pipe Width | 1.2 units |
| Despawn X | -3.0 (off-screen left) |
| Pool Warm Count | 8 (4 pairs) |

### Pipe Lifecycle (Pool)
```
Pool.Spawn("PipeTop") + Pool.Spawn("PipeBottom")
  → Set Y positions (gapCenter ± gapSize/2)
  → Move left each frame (speed × dt)
  → When bird.x passes pipe.x → Score++
  → When pipe.x < despawnX → Pool.Return(pipe)
```

### Difficulty Scaling (via Config)
| Score | Gap Size | Spawn Interval | Pipe Speed |
|-------|----------|----------------|------------|
| 0–10 | 3.2 | 1.8s | 3.0 |
| 11–20 | 3.0 | 1.6s | 3.3 |
| 21–30 | 2.8 | 1.5s | 3.5 |
| 31–50 | 2.6 | 1.4s | 3.8 |
| 51+ | 2.4 | 1.3s | 4.0 |

## 1.5 Scoring & Save

| Key | Type | Description |
|-----|------|-------------|
| `flappy_best` | int | All-time high score |
| `flappy_total_games` | int | Total games played |
| `flappy_total_score` | int | Cumulative score (for unlock) |

### Medal System
| Medal | Score |
|-------|-------|
| None | 0–9 |
| Bronze 🥉 | 10–19 |
| Silver 🥈 | 20–29 |
| Gold 🥇 | 30–49 |
| Platinum 💎 | 50+ |

## 1.6 Audio Design

| Key | Type | Trigger |
|-----|------|---------|
| `sfx_flap` | SFX | On tap |
| `sfx_score` | SFX | Pass pipe |
| `sfx_hit` | SFX | Collision |
| `sfx_die` | SFX | Hit ground |
| `sfx_swoosh` | SFX | UI transition |
| `sfx_medal` | SFX | Medal awarded (GameOver) |
| `bgm_menu` | Music | MenuState enter, fade 0.5s |

## 1.7 Tween Usage

| Where | Tween Call | Detail |
|-------|------------|--------|
| Score popup | `TweenScaleY(1.3f, 0.1f)` then back to 1 | Bounce on score++ |
| Game Over panel | `TweenLocalMoveY(from -800 to 0, 0.4f).SetEase(OutBack)` | Slide up |
| New Best flash | `TweenFade(0→1→0, loop)` | Blinking "NEW!" text |
| Bird death | `TweenMoveY(+1.5f, 0.3f)` then gravity drop | Pop up then fall |
| Screen shake | `TweenMoveX(±5px, 0.05f)` × 4 times | On hit |
| Medal stamp | `TweenScale(0→1.2→1, 0.3f).SetEase(OutElastic)` | Medal appear |

## 1.8 Event Bus Usage

| Event | Fired When | Subscribers |
|-------|-----------|-------------|
| `ScoreChangedEvent { int score }` | Pass pipe | HUD, DifficultyManager |
| `BirdDiedEvent` | Collision | GameManager, AudioManager |
| `GameStartEvent` | First tap | PipeSpawner, HUD |
| `NewBestEvent { int score }` | score > best | GameOverPanel |

---
---

# GAME 2: BILL RUNNER (Endless Runner)

## 2.1 Overview

| Field | Value |
|-------|-------|
| Genre | Endless Runner / Action |
| Camera | 2D Side-scroll, camera follows player |
| Resolution | 1920×1080 (Landscape) |
| Target | Mobile + PC |
| Session | 1–5min |
| Core Loop | Run → Jump/Slide → Collect Coins → Use Items → Die → Upgrade → Retry |

## 2.2 Game Flow (State Machine)

```
BootState → MenuState → ShopState (optional)
                ↓
         LoadingState → GameplayState ←→ PauseState
                              ↓
                        GameOverState → MenuState
```

### State Details

| State | What Happens |
|-------|-------------|
| **MenuState** | Character idle anim on a platform, parallax BG scrolling slowly. Show: Play, Shop, Leaderboard. Best distance displayed. |
| **ShopState** | Browse characters, upgrades. Uses Save for coin balance. |
| **LoadingState** | Generate first 3 chunks of map. Warm up pools. 0.5s min. |
| **GameplayState** | Player runs. Chunks spawn/despawn. Difficulty ramps. Items active. |
| **PauseState** | Freeze all. Show resume + quit. |
| **GameOverState** | Slow-mo death → score tally → coin earned → revive option (1 per run). |

## 2.3 Character Design

### Character Stats
| Stat | Base Value | Upgrade Max | Description |
|------|-----------|-------------|-------------|
| Run Speed | 6.0 u/s | 9.0 u/s | Auto-run speed, increases with distance |
| Jump Force | 12.0 | 14.0 | Initial jump velocity |
| Double Jump | No | Yes | Unlock in shop (500 coins) |
| HP | 1 | 3 | Hearts. Lose 1 per hit. Upgrade in shop. |
| Magnet Range | 0 | 3.0 units | Coin magnet radius. 0 = no magnet. |

### Character Animation States

```
                        ┌──────────────────────────────────────────┐
                        ↓                                          |
Idle ──(GameStart)──→ Run ──(Jump)──→ Jump_Up ──→ Jump_Peak ──→ Jump_Down ──(Land)──→ Run
                        |                ↓ (double jump input)
                        |           DoubleJump_Spin ──→ Jump_Down
                        |
                        ├──(Slide)──→ Slide ──(duration end)──→ Run
                        |
                        ├──(Hit, HP>0)──→ Hurt ──(0.5s invincible)──→ Run
                        |
                        └──(Hit, HP=0)──→ Death_Tumble ──→ Death_Ground
```

| Anim State | Frames | Duration | Detail |
|------------|--------|----------|--------|
| **Idle** | 4 frames breathing | 1.2s loop | Slight body bob, blink every 3s |
| **Run** | 8 frames full cycle | 0.5s loop (speed scales with runSpeed) | Arms pumping, dust particles at feet |
| **Jump_Up** | 3 frames | 0.2s | Legs tuck, arms up |
| **Jump_Peak** | 1 frame hold | Variable | Hang time at apex (velocity near 0) |
| **Jump_Down** | 2 frames | 0.15s | Arms spread, legs extend for landing |
| **DoubleJump_Spin** | 6 frames | 0.3s | Full 360° spin, sparkle trail VFX |
| **Slide** | 2 frames (enter + hold) | 0.6s fixed | Character drops to slide pose, hitbox shrinks 60% |
| **Hurt** | 3 frames | 0.5s | Knockback micro-pause, flash red 3× |
| **Death_Tumble** | 6 frames | 0.4s | Ragdoll-style tumble forward |
| **Death_Ground** | 2 frames | Hold | Face-plant on ground, stars VFX |

### Input
| Input | Action | Condition |
|-------|--------|-----------|
| Tap / Space | Jump | Grounded |
| Tap (airborne) | Double Jump | Has upgrade + hasn't used this jump |
| Swipe Down / S | Slide | Grounded, not already sliding |

## 2.4 Map Chunk System

### Architecture
Map is built from **pre-designed chunks** (prefab sections, each 20 units wide). Camera moves right; chunks spawn ahead and despawn behind.

```
[Chunk A][Chunk B][Chunk C][Chunk D] ...
         ↑ player here
Chunk A despawns when player.x > chunkA.endX + 10
Chunk E spawns when player.x > chunkD.startX - 30
```

### Chunk Categories (Pool keys)

| Category | Count | Description | Available From |
|----------|-------|-------------|----------------|
| **Flat_Easy** | 5 variants | Flat ground, few low obstacles, coins in lines | Distance 0m |
| **Flat_Medium** | 5 variants | Mix of crates + gaps, coins in arcs | 200m |
| **Elevated** | 4 variants | Multi-level platforms, requires jumping between | 400m |
| **Gap_Run** | 4 variants | Sequences of gaps requiring timed jumps | 600m |
| **Slide_Tunnel** | 3 variants | Low ceiling sections requiring slide | 300m |
| **Mixed_Hard** | 4 variants | Combination of gaps + obstacles + elevation | 800m |
| **Boss_Sprint** | 2 variants | Long dense obstacle run, high reward | 1200m (rare) |

### Chunk Selection Logic
```
1. Filter chunks by distance (only unlocked categories)
2. Weight by: 40% Medium, 30% Easy, 20% Hard, 10% Rare
3. No same chunk twice in a row
4. Every 5th chunk guaranteed coin-heavy (reward chunk)
```

## 2.5 Obstacles

| Obstacle | Pool Key | Size | Behavior | Counter |
|----------|----------|------|----------|---------|
| **Crate** | `obs_crate` | 1×1 | Static on ground | Jump over |
| **Double Crate** | `obs_crate2` | 1×2 | Static, 2 high | Jump over |
| **Spike** | `obs_spike` | 0.8×0.5 | Static on ground, instant kill (ignore HP) | Jump over |
| **Low Beam** | `obs_beam_low` | 3×0.5 | Static at y=1.5 (head height) | Slide under |
| **Bird Enemy** | `obs_bird` | 1×1 | Flies at y=2.5, sine wave ±0.5 | Slide under or time jump |
| **Rolling Barrel** | `obs_barrel` | 1×1 | Rolls toward player at 4 u/s | Jump over |
| **Gap** (no floor) | Map design | 2–4 units wide | Fall = death | Jump across |
| **Saw Blade** | `obs_saw` | 1.2×1.2 | Moves up/down between y=0.5–2.5, 2s cycle | Time passage |
| **Falling Platform** | `obs_falling` | 2×0.5 | Shakes 0.3s after player lands, then falls | Jump off quickly |

### Obstacle Damage Rules
| Type | Damage | Special |
|------|--------|---------|
| Crate/Barrel/Bird/Beam/Saw | 1 HP | 0.5s invincibility after hit |
| Spike | Instant Death | Ignores HP, ignores shield |
| Gap (fall) | Instant Death | Ignores HP, ignores shield |

## 2.6 Collectibles & Items

### Coins
| Type | Pool Key | Value | Spawn Pattern |
|------|----------|-------|---------------|
| **Bronze Coin** | `coin_bronze` | 1 | Lines of 5–8, common |
| **Silver Coin** | `coin_silver` | 3 | Arcs of 3–5, in tricky spots |
| **Gold Coin** | `coin_gold` | 10 | Single, hard to reach (high platforms, gap edges) |

### Power-Up Items (spawned in map chunks)
| Item | Pool Key | Duration | Effect | Spawn Rate |
|------|----------|----------|--------|------------|
| **Magnet** | `item_magnet` | 8s | All coins within 5 units fly to player | 1 per ~200m |
| **Shield** | `item_shield` | 1 hit | Absorb 1 hit without HP loss. Gold bubble VFX. | 1 per ~300m |
| **Speed Boost** | `item_speed` | 5s | 1.5× speed, invincible during boost, trail VFX | 1 per ~400m |
| **Coin Doubler** | `item_2x` | 10s | All coins worth 2× | 1 per ~350m |
| **Tiny Mode** | `item_tiny` | 6s | Character shrinks 50%, hitbox 50%, fits under beams | 1 per ~500m |

### Item Tween Effects
| Item | Idle Tween | Pickup Tween |
|------|-----------|-------------|
| All Items | `TweenMoveY(±0.3, 0.8s).SetEase(InOutSine).SetLoop(-1)` (float bob) | `TweenScale(1→1.5→0, 0.2s)` + particles |
| Coins | `TweenRotateZ(0→360, 1.2s).SetLoop(-1)` | `TweenMoveY(+1, 0.3s)` + fade out |
| Shield Active | — | Player gets `TweenScale(1→1.1→1, 0.5s).SetLoop(-1)` gold pulse |

## 2.7 Parallax Background

| Layer | Speed Ratio | Content |
|-------|-------------|---------|
| **Sky** (back) | 0.05× | Gradient sky + clouds |
| **Mountains** | 0.15× | Distant mountain silhouettes |
| **Trees Far** | 0.3× | Dark tree line |
| **Trees Near** | 0.5× | Detailed trees/buildings |
| **Ground Deco** | 0.8× | Grass tufts, flowers (near ground plane) |
| **Foreground** | 1.2× | Occasional leaf particles, dust |

Each layer auto-tiles: 2 copies side by side, when one scrolls off-screen left, it repositions to the right.

## 2.8 Difficulty Curve

| Distance | Run Speed | Obstacle Density | Chunk Pool | Item Frequency |
|----------|-----------|-------------------|------------|----------------|
| 0–200m | 6.0 | Low (1 per 8u) | Flat_Easy only | High (every 150m) |
| 200–400m | 6.8 | Medium (1 per 6u) | +Flat_Medium, +Slide_Tunnel | Normal (every 200m) |
| 400–600m | 7.5 | Medium-High (1 per 5u) | +Elevated | Normal |
| 600–800m | 8.0 | High (1 per 4u) | +Gap_Run | Lower (every 300m) |
| 800–1200m | 8.5 | High (1 per 3.5u) | +Mixed_Hard | Lower |
| 1200m+ | 9.0 (cap) | Very High (1 per 3u) | +Boss_Sprint | Lowest (every 400m) |

Speed formula: `speed = baseSpeed + (distance / 200) × 0.5` clamped to maxSpeed.

## 2.9 Scoring & Economy

### Distance Score
```
distanceScore = floor(player.x - startX)
finalScore = distanceScore + (coinsCollected × 2)
```

### Coin Economy

| Item | Cost | Effect |
|------|------|--------|
| Extra HP (+1) | 300 coins | Start with 2 HP (max 3) |
| Double Jump | 500 coins | Unlock double jump |
| Magnet Passive | 800 coins | Always-on small magnet (range 1.5) |
| Head Start 200m | 100 coins/use | Skip first 200m |
| Revive | 150 coins/use | Continue once on death |
| Skin: Robot | 200 coins | Cosmetic |
| Skin: Ninja | 400 coins | Cosmetic |
| Skin: Astronaut | 600 coins | Cosmetic |

### Save Keys
| Key | Type | Description |
|-----|------|-------------|
| `runner_coins` | int | Total coins owned |
| `runner_best_dist` | int | Best distance in meters |
| `runner_total_runs` | int | Total games played |
| `runner_hp_level` | int | HP upgrade level (0–2) |
| `runner_has_doublejump` | bool | Double jump unlocked |
| `runner_has_magnet` | bool | Passive magnet unlocked |
| `runner_skin` | string | Equipped skin key |
| `runner_skins_owned` | string | Comma-separated owned skin keys |

## 2.10 Audio Design

| Key | Type | Channel | Trigger |
|-----|------|---------|---------|
| `bgm_runner` | Music | Music | GameplayState, loop, fade in 1s |
| `bgm_menu` | Music | Music | MenuState |
| `sfx_jump` | SFX | SFX | Jump |
| `sfx_doublejump` | SFX | SFX | Double jump (higher pitch) |
| `sfx_slide` | SFX | SFX | Slide start |
| `sfx_land` | SFX | SFX | Landing from jump |
| `sfx_coin` | SFX | SFX | Coin pickup (pitch increases with combo) |
| `sfx_item` | SFX | SFX | Item pickup (power-up jingle) |
| `sfx_hurt` | SFX | SFX | Take damage |
| `sfx_death` | SFX | SFX | Death |
| `sfx_speed_loop` | SFX | SFX | Speed boost active (looping whoosh) |
| `sfx_button` | SFX | UI | UI button click |
| `sfx_purchase` | SFX | UI | Shop purchase |

## 2.11 Event Bus

| Event | Data | Fired When | Subscribers |
|-------|------|-----------|-------------|
| `DistanceChangedEvent` | `{ int meters }` | Every 10m | HUD |
| `CoinCollectedEvent` | `{ int value, Vector3 pos }` | Coin pickup | HUD, CoinFlyVFX |
| `ItemPickedUpEvent` | `{ string itemKey, float duration }` | Item pickup | HUD (timer bar), PlayerController |
| `ItemExpiredEvent` | `{ string itemKey }` | Item timer end | PlayerController, VFX |
| `PlayerHurtEvent` | `{ int remainingHP }` | Hit obstacle | HUD (hearts), CameraShake |
| `PlayerDiedEvent` | `{ int distance, int coins }` | HP=0 or fall | GameManager, GameOverPanel |
| `SpeedChangedEvent` | `{ float newSpeed }` | Speed ramp | ChunkSpawner, ParallaxLayers |
| `ChunkSpawnedEvent` | `{ string chunkKey }` | New chunk enters | Analytics |

## 2.12 UI Panels (IUIService)

| Panel | Content | When |
|-------|---------|------|
| **MenuPanel** | Play button, Shop button, best distance, coin count | MenuState |
| **HUDPanel** | Distance counter (top-center), coins (top-right), hearts (top-left), active item timer bar | GameplayState |
| **PausePanel** | Resume, Quit, sound toggle | PauseState |
| **GameOverPanel** | Score, distance, coins earned, best distance, retry/menu buttons, revive button | GameOverState |
| **ShopPanel** | Grid of items with prices, coin balance, equip/buy buttons | ShopState |

---
---

# GAME 3: BILL DEFENSE (Tower Defense)

## 3.1 Overview

| Field | Value |
|-------|-------|
| Genre | Tower Defense / Strategy |
| Camera | 2D Top-down, fixed per map |
| Resolution | 1920×1080 (Landscape) |
| Target | Mobile + PC |
| Session | 10–25min per map |
| Core Loop | Place Towers → Start Wave → Enemies Walk Path → Towers Shoot → Earn Gold → Upgrade/Build → Next Wave |

## 3.2 Game Flow (State Machine)

```
BootState → MenuState → MapSelectState → LoadingState
                                              ↓
                            ┌─── BuildPhaseState ←──────────────────┐
                            ↓                                       |
                      WaveActiveState ──(all enemies dead)──→ WaveCompleteState
                            ↓ (lives = 0)                          ↓ (next wave)
                      GameOverState                          BuildPhaseState
                            ↓                                      ↓ (final wave cleared)
                       MenuState                            VictoryState → MenuState
```

### State Details

| State | Duration | What Happens |
|-------|----------|-------------|
| **MenuState** | — | Map select, show stars per map |
| **MapSelectState** | — | Choose map + difficulty |
| **LoadingState** | ~1s | Load map, warm pools, init tower slots |
| **BuildPhaseState** | 15–30s (varies) | Player places/upgrades towers. Timer countdown. "Send Wave" button to skip wait. |
| **WaveActiveState** | Until all enemies dead/leaked | Enemies spawn. Towers auto-attack. Player can still build. |
| **WaveCompleteState** | 3s | "+Gold" tween, wave summary, bonus gold if no leak |
| **GameOverState** | — | "Defeated" screen, retry/menu |
| **VictoryState** | — | Stars awarded, total score, unlock next map |

## 3.3 Map Design

### Map Structure
Each map is a grid (20×12 tiles, each tile = 1 unit). Path is pre-defined. Towers can only be placed on **buildable tiles** (marked green on hover).

### Map 1: "Green Valley" (Easy, Tutorial)
```
S = Spawn, E = Exit, ░ = Path, ■ = Buildable, · = Blocked

· · · · · · · · · · · · · · · · · · · ·
· · · ■ ■ ■ · · · · · · · ■ ■ ■ · · · ·
· S ░ ░ ░ ░ ░ ░ · · · · ░ ░ ░ ░ ░ · · ·
· · · ■ ■ ■ · ░ · · · · ░ · ■ ■ ░ · · ·
· · · · · · · ░ · · · · ░ · · · ░ · · ·
· · · · · · · ░ ■ ■ ■ ■ ░ · · · ░ · · ·
· · · · · · · ░ ░ ░ ░ ░ ░ · · · ░ · · ·
· · · · · · · · · · · · · · · · ░ · · ·
· · · ■ ■ ■ ■ ░ ░ ░ ░ ░ ░ ░ ░ ░ ░ · · ·
· · · ■ ■ ■ ■ ░ · · · · · · · · · · · ·
· · · · · · · ░ ░ ░ ░ ░ ░ ░ E · · · · ·
· · · · · · · · · · · · · · · · · · · ·
```
- Single path, long snake
- Many buildable tiles
- 15 waves, teaches basics

### Map 2: "Desert Fork" (Medium)
```
· · · · · · · · · · · · · · · · · · · ·
· S ░ ░ ░ ░ ░ ░ ░ · · · · · · · · · · ·
· · · ■ ■ · · · ░ · · · · · · · · · · ·
· · · ■ ■ · · · ░ ░ ░ ░ ░ ░ · · · · · ·
· · · · · · · · · ■ ■ · · ░ · · · · · ·
· · · · · · · · · ■ ■ · · ░ ░ ░ ░ · · ·
· · · · · · · ░ ░ ░ ░ ░ · · · ■ ░ · · ·
· · · ■ · · · ░ · · · ░ · · · ■ ░ · · ·
· · · ■ · ░ ░ ░ · · · ░ ░ ░ ░ ░ ░ · · ·
· S2░ ░ ░ ░ · · · · · · · · · · ░ · · ·
· · · · · · · · · · · · · · · · ░ ░ E · ·
· · · · · · · · · · · · · · · · · · · ·
```
- 2 spawn points (enemies come from both!)
- Paths merge halfway
- Fewer buildable tiles
- 20 waves

### Map 3: "Frozen Pass" (Hard)
```
- 3 spawn points
- Very short paths (less time to kill)
- Minimal buildable tiles (force hard choices)
- 25 waves + Boss wave
```

## 3.4 Tower Design

### Tower Overview

| # | Tower | Cost | Range | DPS | Attack Speed | Target | Special |
|---|-------|------|-------|-----|-------------|--------|---------|
| 1 | **Arrow Tower** | 80g | 3.5u | 12 | 0.8s | Single (First) | None (basic) |
| 2 | **Cannon Tower** | 120g | 3.0u | 25 | 1.5s | AoE (1.2u radius) | Splash damage |
| 3 | **Ice Tower** | 100g | 3.0u | 8 | 1.0s | Single (First) | Slow 30% for 2s |
| 4 | **Lightning Tower** | 150g | 4.0u | 18 | 1.2s | Chain (3 targets) | Chains to nearby enemies |
| 5 | **Sniper Tower** | 200g | 6.0u | 45 | 2.5s | Single (Strongest) | Ignores armor, crit 20% (2× dmg) |
| 6 | **Poison Tower** | 130g | 3.0u | 5 | 1.0s | AoE (2.0u radius) | DoT: 4 dmg/s for 3s, stacks 3× |

### Tower Targeting Modes
| Mode | Description | Default On |
|------|-------------|-----------|
| **First** | Closest to exit | Arrow, Ice |
| **Last** | Furthest from exit | — |
| **Strongest** | Highest current HP | Sniper |
| **Weakest** | Lowest current HP | — |
| **Nearest** | Closest to tower | Cannon, Lightning, Poison |

Player can tap a tower to cycle targeting mode.

### Tower Upgrade System

Each tower has **3 upgrade levels** (Base → Lv2 → Lv3). Upgrade costs increase.

#### Arrow Tower Upgrades
| Level | Cost | Damage | Attack Speed | Range | Bonus |
|-------|------|--------|-------------|-------|-------|
| Lv1 (Base) | 80g | 12 | 0.8s | 3.5 | — |
| Lv2 | 100g | 20 | 0.7s | 3.8 | — |
| Lv3 | 180g | 35 | 0.5s | 4.0 | Double arrow (hits 2 targets) |
| **Total invest** | **360g** | | | | |

#### Cannon Tower Upgrades
| Level | Cost | Damage | Attack Speed | AoE Radius | Bonus |
|-------|------|--------|-------------|------------|-------|
| Lv1 | 120g | 25 | 1.5s | 1.2 | — |
| Lv2 | 150g | 40 | 1.3s | 1.5 | — |
| Lv3 | 250g | 65 | 1.1s | 2.0 | Stun 0.5s |
| **Total** | **520g** | | | | |

#### Ice Tower Upgrades
| Level | Cost | Damage | Slow % | Slow Duration | Bonus |
|-------|------|--------|--------|---------------|-------|
| Lv1 | 100g | 8 | 30% | 2s | — |
| Lv2 | 120g | 14 | 40% | 2.5s | — |
| Lv3 | 200g | 22 | 50% | 3s | Freeze: 15% chance to stun 1s |
| **Total** | **420g** | | | | |

#### Lightning Tower Upgrades
| Level | Cost | Damage | Chain Count | Chain Range | Bonus |
|-------|------|--------|------------|-------------|-------|
| Lv1 | 150g | 18 | 3 | 2.0u | — |
| Lv2 | 180g | 28 | 4 | 2.5u | — |
| Lv3 | 280g | 42 | 6 | 3.0u | Overcharge: 10% chain deals 3× |
| **Total** | **610g** | | | | |

#### Sniper Tower Upgrades
| Level | Cost | Damage | Crit % | Attack Speed | Bonus |
|-------|------|--------|--------|-------------|-------|
| Lv1 | 200g | 45 | 20% | 2.5s | Armor pierce |
| Lv2 | 250g | 75 | 25% | 2.2s | Armor pierce |
| Lv3 | 350g | 120 | 35% | 1.8s | Kill shot: instant kill if enemy <15% HP |
| **Total** | **800g** | | | | |

#### Poison Tower Upgrades
| Level | Cost | DoT/s | DoT Duration | Max Stacks | Bonus |
|-------|------|-------|-------------|------------|-------|
| Lv1 | 130g | 4/s | 3s | 3 | — |
| Lv2 | 160g | 7/s | 3.5s | 3 | — |
| Lv3 | 250g | 12/s | 4s | 5 | Plague: enemies spread poison on death |
| **Total** | **540g** | | | | |

### Sell Price
`sellPrice = totalInvested × 0.7` (70% refund)

## 3.5 Enemy Design

### Enemy Types

| # | Enemy | HP | Speed | Armor | Gold | Special | First Appears |
|---|-------|-----|-------|-------|------|---------|---------------|
| 1 | **Goblin** | 30 | 2.0 | 0 | 5g | None (basic) | Wave 1 |
| 2 | **Orc** | 80 | 1.5 | 2 | 10g | None (tanky) | Wave 3 |
| 3 | **Wolf** | 25 | 3.5 | 0 | 8g | Fast | Wave 4 |
| 4 | **Skeleton** | 50 | 2.0 | 0 | 8g | Resurrect once at 50% HP after 2s | Wave 6 |
| 5 | **Shield Orc** | 100 | 1.3 | 5 | 15g | Shield blocks first 3 hits (then armor=0) | Wave 8 |
| 6 | **Bat Swarm** | 15 | 2.5 | 0 | 3g | Flying (ignores ground path, goes straight to exit) | Wave 10 |
| 7 | **Dark Mage** | 60 | 1.8 | 1 | 12g | Heals nearby allies 5 HP/s (2u radius) | Wave 12 |
| 8 | **Golem** | 250 | 0.8 | 8 | 25g | Immune to slow, very high armor | Wave 15 |
| 9 | **Ghost** | 40 | 2.2 | 0 | 10g | 50% chance to dodge attacks | Wave 17 |
| 10 | **Dragon** (Boss) | 800 | 1.0 | 10 | 100g | Fires back at towers (20 dmg/3s), immune to poison | Boss waves |

### Armor Mechanic
```
actualDamage = max(1, rawDamage - armor)
```
Sniper tower ignores armor entirely.

### Enemy Scaling per Wave
```
enemyHP = baseHP × (1 + wave × 0.08)         // +8% HP per wave
enemyArmor = baseArmor + floor(wave / 10)      // +1 armor every 10 waves
goldReward = baseGold + floor(wave / 5)         // +1g every 5 waves
```

### Enemy Animation States
```
Walk → (hit) → Hurt flash → Walk
  ↓ (HP=0)
Death (fade + particles)
  ↓ (if Skeleton, first death)
Resurrect (glow) → Walk (50% HP)
```

| State | Detail |
|-------|--------|
| **Walk** | 4-frame walk cycle, direction follows path waypoints |
| **Hurt** | SpriteRenderer flash white for 0.1s (Tween) |
| **Slow** | Blue tint overlay, walk anim plays at reduced speed |
| **Poison** | Green bubbles particle, small green tint |
| **Shield** (Shield Orc) | Shield sprite visible in front, disappears after 3 hits |
| **Heal Aura** (Dark Mage) | Green circle pulse around mage |
| **Death** | TweenFade(0, 0.3s) + TweenScale(0.5, 0.3s) + coin particle |

## 3.6 Wave Design (Map 1: Green Valley)

### Wave Table

| Wave | Enemies | Count | Spawn Interval | Gold Bonus | Special |
|------|---------|-------|----------------|------------|---------|
| 1 | Goblin | 8 | 1.0s | 10g | Tutorial: "Place an Arrow Tower" |
| 2 | Goblin | 12 | 0.9s | 10g | Tutorial: "Upgrade your tower" |
| 3 | Goblin + Orc | 8+3 | 0.8s | 15g | Orc introduced |
| 4 | Wolf | 10 | 0.5s | 12g | Fast swarm |
| 5 | Goblin + Wolf + Orc | 6+4+2 | 0.7s | 20g | Mixed |
| 6 | Skeleton | 8 | 0.9s | 15g | Resurrect mechanic introduced |
| 7 | Orc + Skeleton | 5+6 | 0.8s | 18g | |
| 8 | Shield Orc + Goblin | 4+10 | 0.7s | 20g | Shield mechanic |
| 9 | Wolf + Goblin | 15+5 | 0.4s | 15g | Speed rush |
| 10 | Bat Swarm | 12 | 0.6s | 25g | Flying! Need towers near path end |
| 11 | Orc + Shield Orc + Skeleton | 5+4+4 | 0.8s | 22g | |
| 12 | Dark Mage + Orc | 3+8 | 0.7s | 25g | Kill mages first! |
| 13 | Wolf + Bat Swarm | 8+6 | 0.5s | 20g | Speed + Flying combo |
| 14 | Shield Orc + Dark Mage + Skeleton | 5+3+5 | 0.6s | 30g | Hard combo |
| 15 | **BOSS: Dragon** + Goblin | 1+20 | Dragon first, goblins 0.3s | 50g | Final wave! |

### Wave Spawn Pattern
```
foreach enemy in wave.enemies:
    Timer.Delay(spawnIndex × spawnInterval, () => {
        var mob = Pool.Spawn(enemy.poolKey, spawnPoint);
        mob.SetHP(enemy.hp × scaling);
        mob.SetPath(pathWaypoints);
    });
```

## 3.7 Economy & Balance

### Starting Resources per Difficulty

| Difficulty | Starting Gold | Lives | Towers Available | Wave Count |
|------------|-------------|-------|-----------------|------------|
| **Easy** | 300g | 20 | All 6 | Map waves |
| **Normal** | 250g | 15 | All 6 | Map waves |
| **Hard** | 200g | 10 | All 6 | Map waves + 20% more enemies |
| **Nightmare** | 150g | 5 | All 6 | Map waves + 50% more enemies + 30% more HP |

### Gold Income Sources
| Source | Amount |
|--------|--------|
| Kill enemy | Enemy gold value (scaled) |
| Complete wave (no leak) | 20g + wave × 2 |
| Complete wave (some leaked) | 10g + wave × 1 |
| Interest (between waves) | floor(currentGold / 100) × 2 (max 20g) |

### Build Phase Timer
| Phase | Time |
|-------|------|
| Before Wave 1 | 30s |
| Between waves (Normal) | 15s |
| Between waves (Hard/Nightmare) | 10s |
| "Send Wave" button | Skip timer, +5g bonus gold |

## 3.8 Star Rating (Victory)

| Stars | Condition |
|-------|-----------|
| ⭐ | Complete all waves |
| ⭐⭐ | Complete with ≥50% lives remaining |
| ⭐⭐⭐ | Complete with 0 lives lost (perfect) |

### Save Keys
| Key | Type | Description |
|-----|------|-------------|
| `td_map_{id}_stars` | int | Best star count per map (0–3) |
| `td_map_{id}_best_wave` | int | Highest wave reached |
| `td_maps_unlocked` | int | Number of maps unlocked |
| `td_difficulty` | int | Selected difficulty (0–3) |
| `td_total_kills` | int | All-time enemy kills |

## 3.9 Tower Placement UI Flow

```
1. Player taps "Build" button → Tower selection panel opens (bottom bar)
2. Player drags tower from panel onto map
3. Valid tile → Green highlight, show range circle
4. Invalid tile → Red highlight, cannot place
5. Release on valid tile → Deduct gold, spawn tower, play build SFX + scale tween
6. Release on invalid → Cancel, tower returns to panel

Tap existing tower:
1. Show radial menu: Upgrade (cost shown), Sell (refund shown), Target mode
2. Upgrade → Gold deducted, tower visually changes, stats update, tween pulse
3. Sell → Gold refunded, tower removed with shrink tween + poof particles
```

## 3.10 Audio Design

| Key | Type | Trigger |
|-----|------|---------|
| `bgm_td_build` | Music | BuildPhaseState, calm strategic music |
| `bgm_td_battle` | Music | WaveActiveState, intense, fade crossfade 1s |
| `bgm_td_victory` | Music | VictoryState, triumphant fanfare |
| `bgm_td_defeat` | Music | GameOverState |
| `sfx_tower_place` | SFX | Tower placed |
| `sfx_tower_upgrade` | SFX | Tower upgraded (ascending chime) |
| `sfx_tower_sell` | SFX | Tower sold |
| `sfx_arrow` | SFX | Arrow tower fires |
| `sfx_cannon` | SFX | Cannon fires (boom) |
| `sfx_ice` | SFX | Ice tower fires (frost) |
| `sfx_lightning` | SFX | Lightning chains (zap) |
| `sfx_sniper` | SFX | Sniper fires (crack) |
| `sfx_poison` | SFX | Poison tower fires (bubble) |
| `sfx_enemy_hit` | SFX | Enemy takes damage |
| `sfx_enemy_die` | SFX | Enemy killed |
| `sfx_boss_roar` | SFX | Boss spawn |
| `sfx_wave_start` | SFX | Wave begins horn |
| `sfx_wave_clear` | SFX | Wave cleared jingle |
| `sfx_life_lost` | SFX | Enemy reached exit |
| `sfx_coin` | SFX | Gold earned |

## 3.11 Event Bus

| Event | Data | Fired When |
|-------|------|-----------|
| `WaveStartEvent` | `{ int waveNum, int enemyCount }` | Wave begins |
| `WaveCompleteEvent` | `{ int waveNum, int bonusGold, bool perfect }` | All enemies dead |
| `EnemySpawnedEvent` | `{ string type, int hp }` | Enemy spawned |
| `EnemyKilledEvent` | `{ string type, int goldReward, Vector3 pos }` | Enemy dies |
| `EnemyLeakedEvent` | `{ string type, int livesLeft }` | Enemy reached exit |
| `TowerPlacedEvent` | `{ string towerType, Vector2Int tile }` | Tower built |
| `TowerUpgradedEvent` | `{ string towerType, int newLevel }` | Tower upgraded |
| `TowerSoldEvent` | `{ string towerType, int refund }` | Tower sold |
| `GoldChangedEvent` | `{ int newGold, int delta }` | Gold changes |
| `LivesChangedEvent` | `{ int livesLeft }` | Life lost |
| `BuildPhaseTimerEvent` | `{ float secondsLeft }` | Timer tick each second |
| `GameOverEvent` | `{ int wavesCompleted }` | Lives = 0 |
| `VictoryEvent` | `{ int stars, int totalKills }` | Final wave cleared |

## 3.12 Tween Usage

| Where | Tween | Detail |
|-------|-------|--------|
| Tower place | `TweenScale(0→1.2→1, 0.3s).SetEase(OutBack)` | Pop in |
| Tower upgrade | `TweenScale(1→1.3→1, 0.2s)` + white flash | Level up pulse |
| Tower sell | `TweenScale(1→0, 0.2s)` + particle burst | Poof away |
| Enemy death | `TweenFade(0, 0.3s)` + `TweenScale(0.5, 0.3s)` | Shrink & fade |
| Enemy hurt | SpriteRenderer → white for 0.1s | Damage flash |
| Gold popup | `TweenMoveY(+0.8, 0.5s)` + `TweenFade(0, 0.5s)` | "+5g" fly up |
| Life lost | HUD heart `TweenScale(1.5→0, 0.3s)` | Heart breaks |
| Wave banner | `TweenScale(0→1, 0.3s).SetEase(OutElastic)` | "WAVE 5" appear |
| Boss HP bar | `TweenFillAmount(current, 0.3s)` | Smooth HP drain |
| Build timer | `TweenFillAmount(0, totalTime)` | Countdown bar |

## 3.13 Pool Usage Summary

| Pool Key | Warm Count | Used For |
|----------|-----------|----------|
| `enemy_goblin` | 15 | Goblin mobs |
| `enemy_orc` | 8 | Orc mobs |
| `enemy_wolf` | 12 | Wolf mobs |
| `enemy_skeleton` | 8 | Skeleton mobs |
| `enemy_shield_orc` | 6 | Shield Orc |
| `enemy_bat` | 10 | Bat Swarm |
| `enemy_mage` | 4 | Dark Mage |
| `enemy_golem` | 3 | Golem |
| `enemy_ghost` | 6 | Ghost |
| `enemy_dragon` | 1 | Boss Dragon |
| `proj_arrow` | 20 | Arrow projectile |
| `proj_cannonball` | 10 | Cannon projectile |
| `proj_ice` | 10 | Ice shard |
| `proj_lightning` | 8 | Lightning bolt |
| `proj_sniper` | 5 | Sniper bullet |
| `proj_poison` | 10 | Poison blob |
| `vfx_explosion` | 8 | Cannon AoE |
| `vfx_frost` | 8 | Ice slow effect |
| `vfx_chain` | 6 | Lightning chain |
| `vfx_poison_cloud` | 8 | Poison AoE |
| `vfx_death` | 10 | Enemy death poof |
| `vfx_coin` | 15 | Gold earn particle |
| `ui_dmg_number` | 20 | Floating damage text |
| `ui_gold_popup` | 10 | "+5g" floating text |

---
---

# APPENDIX: BillGameCore Service Usage Matrix

| Service | Flappy | Runner | Tower Defense |
|---------|--------|--------|---------------|
| **Bill.State** | ✅ 5 states | ✅ 6 states | ✅ 8 states |
| **Bill.Pool** | ✅ Pipes | ✅ Obstacles, Coins, Items | ✅ Enemies, Projectiles, VFX |
| **Bill.Timer** | ✅ Pipe spawner | ✅ Item durations, speed ramp | ✅ Wave spawner, build timer |
| **Bill.Tween** | ✅ UI anim, death | ✅ Items, coins, UI | ✅ Towers, enemies, UI |
| **Bill.Audio** | ✅ SFX + Music | ✅ SFX + Music | ✅ SFX + Music (per tower) |
| **Bill.Save** | ✅ Highscore | ✅ Coins, upgrades, skins | ✅ Stars, map progress |
| **Bill.Events** | ✅ 4 events | ✅ 8 events | ✅ 12 events |
| **Bill.UI** | ✅ 3 panels | ✅ 5 panels | ✅ 6 panels |
| **Bill.Scene** | ❌ Single scene | ✅ Menu→Game | ✅ Menu→Map→Game |
| **Bill.Config** | ✅ Difficulty | ✅ Balance values | ✅ Tower/enemy stats |
| **Bill.Net** | ❌ | ❌ | ❌ (expandable) |
| **Bill.Cheat** | ✅ Set score | ✅ Add coins, godmode | ✅ Add gold, skip wave, kill all |
