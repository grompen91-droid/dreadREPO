# Dread

> Atmospheric horror overhaul for R.E.P.O.

![Version](https://img.shields.io/badge/version-1.4.1-crimson?style=flat-square)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.21+-blueviolet?style=flat-square)
![Game](https://img.shields.io/badge/game-R.E.P.O.-orange?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

Dread layers three independent systems on top of R.E.P.O.: spatial ambient audio, monster speed/detection/audio overhaul, and a proximity-driven tension system. Every feature is independently toggleable. The mod has no dependencies beyond BepInEx.

---

## For Users

### Features

#### Ambient Audio

Random positional sounds spawn in 3D space around the player during runs, with no visible source.

| Sound | Weight | Description |
|-------|--------|-------------|
| `scraping.ogg` | 1.0 (common) | Something dragging across the floor |
| `footsteps.ogg` | 1.0 (common) | Steps from a direction nobody is in |
| `breathing.ogg` | 0.6 (uncommon) | Close, slow breathing nearby |
| `whisper.ogg` | 0.25 (rare) | A voice at the edge of hearing |

- Sounds spawn 5-15m from the camera in a random direction
- Interval: 30-90 seconds (multiplied by config Frequency)
- Each sound is fully 3D spatialized with linear rolloff (1m min, 25m max)
- Disabled on menu screens
- `door_creak.ogg` ships with the mod but is not currently used by any system

#### Monster Overhaul

> Host must have Dread installed for speed and detection changes to apply. Clients without the mod still experience faster enemies when the host has it.

**Speed and Aggression** (host-authoritative)
- Enemy NavMeshAgent speed and acceleration multiplied by **1.2x** at spawn
- Cached default speed is also patched so ability-based speed resets stay boosted
- Affects all enemies including modded ones (Mimic, WesleysEnemies, etc.)

**Audio Overhaul** (per-client)
- Enemy sound pitch lowered to **0.72x** (clamped 0.3-1.0)
- Reverb zone mix set to **1.1** for spatial weight
- All enemy AudioSources set to fully 3D spatialized
- Applied dynamically every 4 seconds via a scan loop; works on newly spawned enemies
- `DreadAudioTweaked` marker component prevents double-patching

**Detection Radius** (host-authoritative)
- `EnemyDirector.SetInvestigate` radius multiplied by **1.5x**
- Voice and physics noise alerts enemies from further away
- Capped at 1.5x -- higher values cause too many simultaneous investigate events, overwhelming Photon enemy-position sync on clients

#### Tension System

Scans for the nearest enemy every 0.5 seconds and adjusts gameplay based on proximity (within 15m).

**Adrenaline**
- Sprint energy drain reduced by up to **70%** when an enemy is within 15m
- Scales linearly: closer enemy = slower drain
- Smooth lerp back to normal drain when threat clears

**Panic Sprint**
- Starting a sprint within 15m of an enemy triggers a **1.25x** speed burst for 2 seconds
- 20-second cooldown between triggers
- Uses `Traverse` to access private `SprintSpeedMultiplier` field; restores cleanly

**Out of Breath**
- Plays a gasp sound when the player stops sprinting with energy at or below **5** (flat threshold)
- 60-second cooldown
- Ships with `breathing.ogg`; code also checks for `breath2.ogg` and `breath3.ogg` if added

**Fake Footsteps**
- Plays footstep sounds behind the player at 2.5-5m distance, slightly offset to one side
- Triggers every 2-4 minutes with 35% chance per interval
- Low volume (0.55), short falloff (0.5m min, 8m max)

#### QOL

| Feature | Value | Notes |
|---------|-------|-------|
| Crouch Speed Boost | +30% | Patches the cached `playerOriginalCrouchSpeed` so tumbles don't reset it |

---

### Configuration

All settings are in `BepInEx/config/elytraking.dread.cfg`, generated on first run. Compatible with **REPOConfig** for in-game editing.

<details>
<summary>Full config reference</summary>

```
[1. Audio Dread]
Enabled = true
Frequency = 1.0       # multiplier; 2.0 = twice as often
Volume = 0.4          # 0.0 to 1.0

[2. Monster Overhaul]
AggressionEnabled = true   # HOST ONLY
AudioEnabled = true

[3. Tension]
AdrenalineEnabled = true
PanicSprintEnabled = true
LowStaminaSoundEnabled = true
FakeFootstepsEnabled = true

[4. QOL]
CrouchSpeedBoost = true
```

</details>

---

### Netcode Compatibility

| Feature | Who needs the mod |
|---------|-------------------|
| Monster speed and acceleration | Host only |
| Enemy detection radius | Host only |
| Ambient audio | Per client |
| Adrenaline and panic sprint | Per client |
| Out of breath sounds | Per client |
| Fake footsteps | Per client |
| Crouch speed boost | Per client |

Players without Dread **can join** modded lobbies. Monster changes apply only if the host has Dread. All atmospheric and tension effects are local and do not affect other players.

---

### Requirements

- [BepInEx 5.4.21+](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)

No other dependencies.

---

### Installation

#### Via Mod Manager (Recommended)
Install through r2modman or Thunderstore Mod Manager. Search for **Dread** under R.E.P.O.

#### Manual
1. Download the latest `.zip` from [Thunderstore](https://thunderstore.io/c/repo/p/elytraking/Dread/)
2. Extract into your r2modman profile directory or directly into your R.E.P.O. game folder
3. Folder structure must match:
```
BepInEx/
  plugins/
    elytraking-Dread/
      Dread.dll
      audio/
        breathing.ogg
        footsteps.ogg
        scraping.ogg
        whisper.ogg
```

---

## For Developers

### Project Structure

```
Dread.csproj                # .NET 4.8 project, references BepInEx/Harmony/Unity/Photon
Plugin.cs                   # BepInEx entry point: config init, Harmony patch all, spawn DreadHost
Config/
  DreadConfig.cs            # All ConfigEntry bindings (4 sections, 9 toggles)
Systems/
  AudioDreadSystem.cs       # Ambient audio loop: weighted random clip, 3D spawn, self-cleaning
  MonsterOverhaulSystem.cs  # Audio scan loop + 3 Harmony patches (speed, crouch, detection)
  TensionSystem.cs          # Proximity scan + 4 features (adrenaline, panic, breath, footsteps)
audio/                      # OGG Vorbis sound files shipped alongside the DLL
```

### Architecture

The mod launches at game startup:

1. **`Plugin.Awake()`** -- Initializes BepInEx config, calls `Harmony.PatchAll()` which applies all `[HarmonyPatch]`-annotated method patches
2. **`Plugin.Start()`** -- Creates a `DontDestroyOnLoad` GameObject named "DreadHost" and attaches three MonoBehaviour systems

#### Harmony Patches (in `MonsterOverhaulSystem.cs`)

| Patch | Method | Type | Effect |
|-------|--------|------|--------|
| `EnemyNavMeshAgentAwakePatch` | `EnemyNavMeshAgent.Awake` | Postfix | 1.2x speed/acceleration at spawn |
| `PlayerControllerAwakePatch` | `PlayerController.Awake` | Postfix | 1.3x crouch speed |
| `EnemyDirectorSetInvestigatePatch` | `EnemyDirector.SetInvestigate` | Prefix | 1.5x detection radius multiplier |

All patches check their respective config toggle before applying. Speed patches use `Traverse` to update cached private fields (`DefaultSpeed`, `DefaultAcceleration`, `playerOriginalCrouchSpeed`) so game-internal speed resets use boosted values.

#### System Lifecycles

- **AudioDreadSystem**: Coroutine loads 4 OGG clips from DLL-adjacent `audio/` folder via `UnityWebRequestMultimedia`, then enters a loop spawning weighted-random 3D audio sources every 30-90s
- **MonsterOverhaulSystem**: Coroutine scans `FindObjectsOfType<EnemyHealth>()` every 4s, applies pitch/reverb/spatial tweaks to child AudioSources. Marker component `DreadAudioTweaked` prevents re-patching
- **TensionSystem**: `Update()` proximity scan every 0.5s drives adrenaline, panic sprint, and out-of-breath logic. Separate coroutine handles fake footsteps. State resets on `SceneManager.sceneLoaded`

#### Audio Loading

Both `AudioDreadSystem` and `TensionSystem` load audio from `Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/audio/"`. This means the `audio/` folder must sit adjacent to `Dread.dll` at runtime.

### Building from Source

Requires .NET SDK and a local R.E.P.O. installation (for assembly references).

Update `GameDir` in `Dread.csproj` to match your R.E.P.O. install path:
```xml
<GameDir>C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed</GameDir>
```

```powershell
.\build.ps1 -Version "1.4.1"
```

Output: `dist\elytraking-Dread-<version>\` (folder) and `dist\elytraking-Dread-<version>.zip` (Thunderstore-ready).

**Post-build targets** (in `Dread.csproj`):
- `DeployToProfile` -- Copies DLL + audio to local r2modman profile (auto-runs if profile directory exists)
- `DeployToDist` -- Copies to `dist/` and re-zips (auto-runs if dist directory exists)

### Adding Audio

1. Place `.ogg` files in the `audio/` folder
2. To add ambient sounds: add the filename to `AudioDreadSystem.ClipNames` and optionally `ClipWeights`
3. To add breath variants: name them `breath2.ogg`, `breath3.ogg` -- the code already scans for these

### Versioning

Before releasing:
1. Update `Plugin.VERSION` in `Plugin.cs`
2. Update `manifest.json` `version_number`
3. Update `-Version` argument in `build.ps1` invocation

> **Note:** As of v1.4.1, `Plugin.cs` still says `1.4.0` -- ensure both files are bumped together.

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for full version history.

## License

MIT. Use, modify, and redistribute freely with attribution.
