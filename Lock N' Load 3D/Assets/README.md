# Lock N' Load 3D - Developer Configuration Guide

This guide covers all configurable settings for weapons, enemies, audio, and wave management.

---

## Table of Contents

1. [Key Weapon System Overview](#key-weapon-system-overview)
2. [KeyWeaponData Assets vs Weapon Prefabs](#keyweapondata-assets-vs-weapon-prefabs)
3. [Configuring Weapon Damage & Stats](#configuring-weapon-damage--stats)
4. [Configuring Overheat System](#configuring-overheat-system)
5. [Configuring Weapon Audio](#configuring-weapon-audio)
6. [Master Audio Volume Settings](#master-audio-volume-settings)
7. [Wave Manager & Locklet Configuration](#wave-manager--locklet-configuration)
8. [Adding Enemies to Compass UI](#adding-enemies-to-compass-ui)
9. [File Locations Reference](#file-locations-reference)

---

## Key Weapon System Overview

The key weapon system uses a two-part system:

1. **KeyWeaponData Assets** (ScriptableObjects) - Define weapon identity, stats, and abilities
2. **Weapon Prefabs** - Define visual/audio behavior, overheat settings, and firing sounds

---

## KeyWeaponData Assets vs Weapon Prefabs

### KeyWeaponData Assets (`.asset` files)

**Location:** `Assets/ModAssets/KeyWeapons/*.asset`

These ScriptableObjects define **WHAT** a weapon is:

| Setting | Description | Example |
|---------|-------------|---------|
| `KeyName` | Display name | "Yellow Key" |
| `KeyColor` | UI/visual color | Yellow |
| `Damage` | Base damage per shot | 40 |
| `FireRate` | Time between shots (seconds) | 0.1 (fast) to 2.0 (slow) |
| `ProjectileSpeed` | How fast projectiles travel | 30-60 |
| `SpecialAbility` | Ability type (None, Lifesteal, Burn, Explosive, Teleport, BurnExplosive) | Lifesteal |
| `AbilityPower` | Ability-specific value (heal amount, burn duration, etc.) | 10 |
| `ExplosionRadius` | AOE radius for Explosive/BurnExplosive abilities | 5 |
| `ProjectilePrefab` | Reference to projectile prefab | Projectile_Yellow |
| `WeaponModelPrefab` | Reference to weapon model prefab | YellowKey_WeaponModel |
| `ProjectileTravelSound` | Audio clip for projectile in flight | |
| `ProjectileImpactSound` | Audio clip when projectile hits | |
| `TravelSoundVolume` | Per-weapon travel sound volume (0-5x) | 0.5 |
| `ImpactSoundVolume` | Per-weapon impact sound volume (0-5x) | 1.0 |

**Current Key Weapon Assets:**
- `YellowKey_Basic.asset` - Fast automatic fire, no special ability
- `OrangeKey_Multishot.asset` - Rapid fire automatic
- `PinkKey_Lifesteal.asset` - Heals player on hit
- `GreenKey_Pierce.asset` - Teleports player to impact point
- `PurpleKey_ShadowBall.asset` - Explosive AOE on impact
- `RedKey_FireTrail.asset` - BurnExplosive (AOE + burn over time)

---

### Weapon Prefabs (`.prefab` files)

**Location:** `Assets/ModAssets/Prefabs/Weapons/` or within weapon model folders

These prefabs define **HOW** a weapon behaves visually and audibly:

| Component | Settings | Description |
|-----------|----------|-------------|
| **WeaponController** | `ShootSfx` | Firing sound effect |
| | `FiringSoundVolume` | Per-weapon firing volume (0-5x) |
| | `RecoilForce` | Visual recoil amount |
| | `MuzzleFlashPrefab` | VFX for muzzle flash |
| **KeyWeaponOverheatBehavior** | `HeatPerShot` | Heat added per shot (0-1) |
| | `HeatDecayRate` | Heat lost per second |
| | `OverheatCooldownThreshold` | Heat level to resume firing |
| | `OverheatSound` / `CoolingSound` | Audio clips |
| | `OverheatSoundVolume` / `CoolingSoundVolume` | Per-weapon volumes |
| | `SteamVfxPrefab` | Steam particles when overheated |
| | `HotColor` | Emission color when hot |
| **KeyWeaponSwapAudio** | `PullOutSound` | Sound when drawing weapon |
| | `PullOutVolume` | Volume for pull-out sound |

---

### When to Edit What

| I want to change... | Edit this |
|---------------------|-----------|
| Weapon damage | KeyWeaponData asset → `Damage` |
| Fire rate | KeyWeaponData asset → `FireRate` |
| Projectile speed | KeyWeaponData asset → `ProjectileSpeed` |
| Special ability type | KeyWeaponData asset → `SpecialAbility` |
| Ability strength (heal, burn duration, etc.) | KeyWeaponData asset → `AbilityPower` |
| Explosion radius | KeyWeaponData asset → `ExplosionRadius` |
| Firing sound effect | Weapon Prefab → WeaponController → `ShootSfx` |
| Firing sound volume | Weapon Prefab → WeaponController → `FiringSoundVolume` |
| Heat per shot | Weapon Prefab → KeyWeaponOverheatBehavior → `HeatPerShot` |
| Overheat cooldown time | Weapon Prefab → KeyWeaponOverheatBehavior → `HeatDecayRate` |
| Impact/travel sounds | KeyWeaponData asset → `ProjectileImpactSound` / `ProjectileTravelSound` |

---

## Configuring Weapon Damage & Stats

### Step-by-Step: Change Weapon Damage

1. Open `Assets/ModAssets/KeyWeapons/` in Project window
2. Select the weapon asset (e.g., `YellowKey_Basic.asset`)
3. In Inspector, find **Weapon Stats** section
4. Adjust `Damage` value
5. Save (Ctrl+S)

### Weapon Stats Reference

```
Damage              - Base damage per projectile hit
FireRate            - Seconds between shots (lower = faster)
ProjectileSpeed     - Units per second projectile travels
MovementSpeedMultiplier    - Player speed while holding (1.0 = normal)
MovementSpeedWhileShooting - Player speed while firing (0.7 = 70%)
ProjectilesPerShot  - Number of projectiles per trigger pull
SpreadAngle         - Cone of fire spread in degrees
```

### Special Abilities

| Ability Type | AbilityPower Meaning | Other Settings |
|--------------|---------------------|----------------|
| `None` | Not used | - |
| `Lifesteal` | Heal amount per hit | - |
| `Burn` | Burn duration (seconds) | Burn DPS = Damage × 0.3 |
| `Explosive` | Not used (see ExplosionRadius) | `ExplosionRadius`, `ShakeIntensity`, `ShakeDuration` |
| `Teleport` | Not used | - |
| `BurnExplosive` | Burn duration (seconds) | `ExplosionRadius`, `ShakeIntensity`, `ShakeDuration` |

---

## Configuring Overheat System

The overheat system is configured **per-weapon prefab** via the `KeyWeaponOverheatBehavior` component.

### Overheat Settings Reference

```
Heat Settings:
├── HeatPerShot (0.12)         - Heat added per shot (0-1 scale, 1 = instant overheat)
├── HeatDecayRate (0.35)       - Heat lost per second when not shooting
└── OverheatCooldownThreshold (0.3) - Heat must drop below this to resume firing

Visual - Emission:
├── EmissionIntensity (1.0)    - Multiplier for glow effect
└── HotColor (Red)             - Color when overheated

Visual - Steam VFX:
├── SteamVfxPrefab             - Particle system for steam
└── SteamVfxEmissionRateMax    - Max particles when overheated

Sound:
├── OverheatSound              - Audio clip when overheating
├── CoolingSound               - Audio clip during cooldown
├── OverheatSoundVolume (0.5)  - Per-weapon overheat volume (0-5x)
└── CoolingSoundVolume (0.5)   - Per-weapon cooling volume (0-5x)

Animation:
├── OverheatLiftAmount (0.08)  - How much weapon lifts up
├── OverheatLiftSpeed (8)      - Animation speed
└── OverheatTiltAngle (-10)    - Tilt angle when overheated
```

### Overheat Behavior

1. Each shot adds `HeatPerShot` to heat level (0-1)
2. Heat decays at `HeatDecayRate` per second when not shooting
3. At heat = 1.0, weapon overheats and cannot fire
4. Heat must drop below `OverheatCooldownThreshold` to resume firing
5. UI bar shows remaining capacity (inverted heat level)

---

## Configuring Weapon Audio

### Audio Structure

```
Master Volume (KeyWeaponAudioSettings)
    ├── Firing Volume ────────→ WeaponController.FiringSoundVolume
    ├── Impact Volume ────────→ KeyWeaponData.ImpactSoundVolume
    ├── Travel Volume ────────→ KeyWeaponData.TravelSoundVolume
    ├── Pull-Out Volume ──────→ KeyWeaponSwapAudio.PullOutVolume
    ├── Overheat Volume ──────→ KeyWeaponOverheatBehavior.OverheatSoundVolume
    └── Cooling Volume ───────→ KeyWeaponOverheatBehavior.CoolingSoundVolume
```

### Per-Weapon Audio (on Prefabs)

**Firing Sound** - Set on `WeaponController` component:
- `ShootSfx` - The audio clip
- `FiringSoundVolume` - Volume multiplier (0-5x)

**Swap Sound** - Set on `KeyWeaponSwapAudio` component:
- `PullOutSound` - Audio clip when drawing weapon
- `PullOutVolume` - Volume multiplier (0-5x)

**Overheat/Cooling** - Set on `KeyWeaponOverheatBehavior`:
- `OverheatSound` / `CoolingSound` - Audio clips
- `OverheatSoundVolume` / `CoolingSoundVolume` - Volume multipliers (0-5x)

### Per-Weapon Audio (on KeyWeaponData Assets)

**Projectile Audio** - Set on the `.asset` file:
- `ProjectileTravelSound` - Looping sound while projectile flies
- `ProjectileImpactSound` - Sound on hit
- `TravelSoundVolume` / `ImpactSoundVolume` - Volume multipliers (0-5x)

---

## Master Audio Volume Settings

### Location

The `KeyWeaponAudioSettings` component must exist on a GameObject in your scene.

**Recommended:** Add to a "GameManager" or "AudioManager" GameObject.

### Settings (Inspector Only - Not In-Game)

| Slider | Range | Affects |
|--------|-------|---------|
| `Firing Volume` | 0-5x | All weapon firing sounds |
| `Projectile Travel Volume` | 0-5x | All projectile flight sounds |
| `Impact Volume` | 0-5x | All projectile hit sounds |
| `Pull Out Volume` | 0-5x | All weapon draw sounds |
| `Overheat Volume` | 0-5x | All overheat warning sounds |
| `Cooling Volume` | 0-5x | All cooling sounds |

### Persistence

Settings automatically save to `PlayerPrefs` and persist between sessions.

### Adding to Scene

1. Create empty GameObject named "KeyWeaponAudioSettings"
2. Add Component → `KeyWeaponAudioSettings`
3. Adjust sliders as needed during Play mode
4. Settings persist after stopping

---

## Wave Manager & Locklet Configuration

### Location

The `WaveManager` component should be on a GameObject in your scene.

### Wave Configuration

```
Wave Configuration:
└── Waves[] - Array of wave definitions
    ├── SpawnerCount    - Number of spawn points used
    ├── TotalEnemies    - Enemies spawned this wave
    └── SpawnInterval   - Seconds between spawns per spawner
```

**Default Waves:**
| Wave | Spawners | Enemies | Interval |
|------|----------|---------|----------|
| 1 | 4 | 20 | 0.67s |
| 2 | 6 | 45 | 0.67s |
| 3 | 8 | 80 | 0.67s |

### Spawn Settings

```
Spawner Positions:
└── SpawnPoints[] - World positions for spawn points (shown as gizmos in Scene view)

Spawn Settings:
├── SpawnRandomOffset (1.5) - Random position offset around spawn point
├── EnemyScale (0.33)       - Scale of spawned locklets (0.33 = 1/3 size)
├── InitialDelay (30)       - Seconds before first wave starts
└── DelayBetweenWaves (5)   - Seconds between waves
```

### Locklet Stats Override

These settings **override** the prefab defaults for all spawned Locklets:

```
Locklet Stats Override:
├── LockletDetectionRange (50)  - How far locklets can see player
├── LockletMoveSpeed (4)        - Movement speed while chasing
└── LockletHealth (100)         - Max health points

Locklet Audio Override:
├── LockletHitmarkerVolume (1.0)         - Hitmarker sound volume (0-10x)
└── LockletStatusIndicatorVolume (1.0)   - Death indicator sound volume (0-10x)
```

### Step-by-Step: Modify Wave Difficulty

1. Select WaveManager GameObject in Hierarchy
2. Expand `Waves` array in Inspector
3. Adjust individual wave settings:
   - Increase `TotalEnemies` for more enemies
   - Decrease `SpawnInterval` for faster spawning
   - Increase `SpawnerCount` to use more spawn points

### Step-by-Step: Adjust Locklet Stats

1. Select WaveManager GameObject
2. Find "Locklet Stats Override" section
3. Adjust:
   - `LockletHealth` - Higher = tankier enemies
   - `LockletMoveSpeed` - Higher = faster enemies
   - `LockletDetectionRange` - Higher = enemies spot player from farther

---

## Adding Enemies to Compass UI

### For Standard Enemies (EnemyController)

Already supported - enemies with `EnemyController` component automatically appear on compass.

### For Locklets (MeleeEnemyController)

To show Locklets on the compass:

1. Open Locklet prefab (e.g., `Assets/ModAssets/Prefabs/LockletPrefabs/YellowLocklet.prefab`)
2. Select root GameObject
3. Add Component → `CompassElement`
4. Assign `CompassMarkerPrefab` (use same marker as other enemies, found in `FPS/Prefabs/UI/`)
5. Save prefab

The compass will now show Locklets and change marker color when they detect the player.

---

## File Locations Reference

### Key Weapon Assets
```
Assets/ModAssets/KeyWeapons/
├── YellowKey_Basic.asset
├── OrangeKey_Multishot.asset
├── PinkKey_Lifesteal.asset
├── GreenKey_Pierce.asset
├── PurpleKey_ShadowBall.asset
└── RedKey_FireTrail.asset
```

### Audio Files
```
Assets/ModAssets/KeyWeapons/Audio/
├── Yellow Key/
│   ├── Yellow-firing-sfx.wav
│   ├── Yellow-projectile-impact-sfx.mp3
│   └── Yellow-weapon-pullout-sfx.mp3
├── Orange Key/
├── Pink Key/
├── Green Key/
├── Purple Key/
├── Red Key/
├── overheat-sfx.mp3
├── Metal-hitmarker.mp3
└── Status-indicator-sfx.mp3
```

### Projectile Prefabs
```
Assets/ModAssets/KeyWeapons/Projectiles/
├── Projectile_Yellow.prefab
├── Projectile_OrangeBlaster.prefab
├── Projectile_PinkKey.prefab
├── Projectile_PurpleSphere.prefab
├── Projectile_RedKey.prefab
└── (Green key uses same as another)
```

### Scripts
```
Assets/FPS/Scripts/Gameplay/
├── KeyWeaponData.cs              - ScriptableObject definition
├── KeyWeaponController.cs        - Main weapon system controller
├── KeyWeaponOverheatBehavior.cs  - Overheat logic
├── KeyWeaponAudioSettings.cs     - Master audio volume settings
├── KeyWeaponSwapAudio.cs         - Weapon swap sounds
└── WaveManager.cs                - Wave spawning system

Assets/FPS/Scripts/AI/
├── MeleeEnemyController.cs       - Locklet AI controller
└── LockletAudioController.cs     - Locklet audio (hitmarker, status)

Assets/FPS/Scripts/Game/Shared/
└── WeaponController.cs           - Base weapon controller
```

### Locklet Prefabs
```
Assets/ModAssets/Prefabs/LockletPrefabs/
└── YellowLocklet.prefab
```

---

## Quick Reference Card

### Change Weapon Damage
`KeyWeaponData asset → Damage`

### Change Fire Rate
`KeyWeaponData asset → FireRate` (lower = faster)

### Change Overheat Speed
`Weapon Prefab → KeyWeaponOverheatBehavior → HeatPerShot`

### Change Overheat Recovery
`Weapon Prefab → KeyWeaponOverheatBehavior → HeatDecayRate`

### Change Firing Sound
`Weapon Prefab → WeaponController → ShootSfx`

### Change Firing Volume
`Weapon Prefab → WeaponController → FiringSoundVolume`

### Change ALL Firing Volumes
`KeyWeaponAudioSettings → Firing Volume`

### Change Wave Enemy Count
`WaveManager → Waves[n] → TotalEnemies`

### Change Locklet Health
`WaveManager → LockletHealth`

### Change Locklet Audio
`WaveManager → LockletHitmarkerVolume / LockletStatusIndicatorVolume`

---

*Last Updated: December 2 2025*

