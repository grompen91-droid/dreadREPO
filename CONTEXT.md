# Dread - Domain Context

Dread is a BepInEx mod for R.E.P.O. that adds atmospheric horror systems on top of the base game without breaking vanilla compatibility.

---

## Glossary

| Term | Definition |
|------|------------|
| **Ambient Audio** | Positional horror sounds placed in 3D space around the player during runs. No gameplay effect, purely atmospheric. |
| **AudioDreadSystem** | MonoBehaviour responsible for the ambient audio loop. Picks weighted random clips and spawns temporary AudioSource GameObjects. |
| **TensionSystem** | MonoBehaviour that scans for nearby enemies every 0.5s and drives adrenaline, panic sprint, low stamina sounds, and fake footsteps. |
| **MonsterOverhaulSystem** | MonoBehaviour that patches enemy speed, audio pitch, and detection radius. Runs every 4s to catch newly spawned enemies. |
| **Adrenaline** | Sprint energy drains slower (up to 70% reduction) when an enemy is within `ProximityRange` (15m). |
| **Panic Sprint** | A 1.25x speed burst lasting 2 seconds, triggered when the player starts sprinting within `ProximityRange`. 20s cooldown. |
| **Low Stamina** | Plays a breath/gasp clip when the player stops sprinting because energy ran out. 60s cooldown. |
| **Fake Footsteps** | Spawns a temporary AudioSource behind the player at random intervals (2-4 min) with 35% chance. No enemy is actually there. |
| **ProximityRange** | 15m radius around the player camera used by all TensionSystem proximity checks. |
| **AudioLoader** | (Planned) Shared static cache to prevent duplicate loading of the same .ogg file across systems. |
| **DreadAudioTweaked** | Marker MonoBehaviour attached to enemies after audio tweaks are applied, preventing double-patching. |
| **Host-authoritative** | Feature that only applies when the lobby host has the mod installed. Monster speed and detection are host-authoritative. |
| **Per-client** | Feature that applies independently for each player who has the mod installed. Ambient audio and tension effects are per-client. |
| **Thunderstore package** | The `.zip` containing `icon.png`, `manifest.json`, `README.md`, and `BepInEx/plugins/elytraking-Dread/` uploaded to Thunderstore. |
| **elytraking-Dread** | The canonical plugin folder name used in BepInEx plugin paths and Thunderstore package naming. |

---

## System Boundaries

```
Plugin.cs
  Awake()  -- initializes DreadConfig, patches Harmony
  Start()  -- spawns DreadHost GameObject with:
               AudioDreadSystem   (ambient horror audio loop)
               MonsterOverhaulSystem (enemy speed/audio/detection patches)
               TensionSystem      (proximity-reactive gameplay effects)

HarmonyPatches (in MonsterOverhaulSystem.cs):
  EnemyNavMeshAgentAwakePatch    -- +20% speed/acceleration on enemy spawn
  PlayerControllerAwakePatch     -- +30% crouch speed on player spawn
  EnemyDirectorSetInvestigatePatch -- 1.5x detection radius
```

---

## Key Constraints

- **No REPOLib dependency** as of v1.4.0. Host Options networking was removed because REPOLib GUID resolution was unreliable.
- **No PostProcessing patches.** R.E.P.O. uses a non-standard PP setup that does not expose `VolumeProfile` access patterns.
- **Detection radius capped at 1.5x.** Higher multipliers overwhelm Photon enemy-position sync on clients.
- **Audio loaded via UnityWebRequest**, not `Resources.Load`, because audio files ship alongside the DLL in `BepInEx/plugins/elytraking-Dread/audio/`.
