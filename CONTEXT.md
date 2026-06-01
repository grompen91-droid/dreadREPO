# Dread (R.E.P.O. mod)

Bounded context for **Dread**, a BepInEx horror-overhaul plugin for **R.E.P.O.**: ambient audio, host-side monster tweaks, client-side tension, rare **Psychotic Break** episodes, error reporting, and an optional debug bridge for agents.

This file is a **domain glossary** plus a short **file map** for onboarding. Definitions describe behavior; each top-level runtime system may name its implementing class once. Details live in [`docs/adr/`](docs/adr/) and code.

**Related docs:** [`docs/agents/domain.md`](docs/agents/domain.md) explains how agents should consume this file and ADRs.

## Language

### Mod shell and lifecycle

**Dread**:
The BepInEx horror-overhaul for **R.E.P.O.**: runtime patches plus persistent gameplay systems (audio, monsters, tension, psychotic break, reporting, debug).
_Avoid_: "the DLL", unnamed "horror mod"

**System host**:
Persistent in-game container for exactly one runtime system (for example tension lives on its own host). Survives scene changes.
_Avoid_: **DreadHost** (removed monolithic host; one host per system since v1.5.1)

**DreadConfig**:
Authoritative registry of mod toggles and tuning values in code. The generated BepInEx cfg on disk mirrors these entries after first run.
_Avoid_: "settings file" when you mean the on-disk cfg; editing cfg without knowing it maps to **DreadConfig**

### Logging and extension

**Mod logging**:
How chatty Dread is in the BepInEx log: None, Error, Debug, or Verbose. Debug is the normal default; Verbose is for diagnosis (per-system trace lines).
_Avoid_: confusing with global BepInEx log level; "enable logging" when you mean only Dread's **Mod logging**

**Compatibility mode**:
Config toggle (`10. Compatibility`) that keeps ambient audio while skipping monster Harmony patches, adrenaline/panic sprint mutation, and psychotic break. See [docs/agents/guides/compatibility.md](docs/agents/guides/compatibility.md) and ADR-0016.
_Avoid_: **Mod compatibility** (that term means working with modded enemies); using "compat" only for REPOConfig slider work

**Compat layer**:
Pattern for living next to other mods: detect optional dependencies after assemblies load, guard host-only behavior, apply Harmony only when safe. Shared helpers live in `Systems/Core/` (`Dread.Systems.Core`). Documented in [docs/adr/0016-arch-3-extension-model.md](docs/adr/0016-arch-3-extension-model.md).
_Avoid_: "compat mode" when you mean only REPOConfig or slider label work

**System initializer**:
`DreadSystemInitializer` spawns runtime systems from `DreadSystemRegistry` once Unity UI is available. Boot order and contracts: [docs/adr/0016-arch-3-extension-model.md](docs/adr/0016-arch-3-extension-model.md), [specs/002-arch-3-extensible-core/contracts/system-lifecycle.md](specs/002-arch-3-extensible-core/contracts/system-lifecycle.md).
_Avoid_: blaming **Plugin** alone when init was deferred or a single system failed in isolation

### Sessions and scenes

**Match**:
One play session from lobby through rounds until the group leaves or the run ends. Caps such as one **Psychotic Break** per match apply here.
_Avoid_: "run" when you mean session-wide limits (config keys use `OncePerMatch` on purpose)

**Run**:
Active horror gameplay: player is in a level and systems are allowed to fire ambient and tension effects (not main menu or idle lobby UI).
_Avoid_: "match" for ambient/tension timing; "level" when you mean menu vs play; "run" for psychotic break caps (use **Match**)

**In-level**:
Gameplay scene is active (not Menu/Main). Used for loops that should not run in menus, such as the monster audio scan.
_Avoid_: treating **In-level** and **Run** as synonyms; prefer **In-level** for scene gating, **Run** for player-facing feature behavior

### Runtime systems

Seven systems start from the plugin entry on typical builds (see **File map**). Each lives on its own **System host**.

**Audio Dread**:
During a **Run**, plays rare weighted 3D ambient horror sounds around the player; frequency and volume follow config.
_Implements:_ `AudioDreadSystem`
_Avoid_: "ambient mod", "background sounds"

**Monster Overhaul**:
Makes enemies feel harder to read and more threatening: host-side aggression tuning, periodic enemy audio treatment, and related monster-facing behavior.
_Implements:_ `MonsterOverhaulSystem`
_Avoid_: using the name for only **Monster aggression** or only audio tweaks

**Camp Lure**:
Host-only anti-camping: draws enemies toward a player who stays isolated too long, with escalation and a post-contact cooldown. Active only during **Extraction level** (`GameplayContext.AllowsHostMonsterFeatures`).
_Implements:_ `CampLureSystem`
_Avoid_: "lure mod", treating as client-local tension

**Snitch**:
Host-only: one random item per extraction level secretly triggers a 3D bang and enemy POI on first pickup. Active only during **Extraction level**.
_Implements:_ `SnitchSystem`
_Avoid_: calling every item dangerous; snitch is one hidden item per level

**Tension System**:
One shared **proximity scan** drives four client-local features: **Adrenaline**, **Panic sprint**, **Low stamina sound**, and **Fake footsteps**.
_Implements:_ `TensionSystem`
_Avoid_: treating each sub-feature as a separate top-level "system"

**Psychotic Break**:
Rare client-local **Episode** when the player is **Solo**, has **Recent threat**, has **LoS lost**, and is crouching.
_Implements:_ `PsychoticBreakSystem`
_Avoid_: "hallucination event", "cutscene"

**Error reporting**:
Captures serious game errors and can file deduplicated reports for the developer; subject to player opt-in and config. Payload JSON via `ErrorReportJson` (ADR-0010, ADR-0015).
_Implements:_ `ErrorReporterSystem`, `ErrorReportJson`
_Avoid_: "telemetry" without noting opt-in/config

**Test crash**:
Intentional crash path used to verify **Error reporting** end-to-end (config-driven, not a gameplay feature).
_Implements:_ `TestCrashSystem`
_Avoid_: production "panic button" wording in player-facing docs

**Debug server**:
Exposes a localhost JSON line protocol so tools (including the MCP bridge) can read state, config, logs, and patches while the game runs.
_Implements:_ `DebugServerSystem`
_Avoid_: in-game console, BepInEx log tailing, **Debug overlay**

**Debug overlay** (in development):
In-game IMGUI HUD for live mod state (tension, patches, config) while playing. Tracked under DBG roadmap issues; not wired on all branches yet.
_Avoid_: **Debug server** (TCP/MCP is out-of-process); calling it "the debug mod"

### Tension and proximity

**Proximity scan**:
Periodic check for the nearest enemy distance, shared by all **Tension System** features so tension stays coherent.
_Avoid_: separate enemy scans per sub-feature

**Adrenaline**:
While a nearby enemy is within about 15m, sprint energy drains more slowly (up to roughly 70% relief); effect fades when the area is clear.
_Avoid_: "stamina buff"

**Panic sprint**:
Starting a sprint while an enemy is within about 15m and off cooldown grants a brief speed surge, then returns to normal sprint behavior.
_Avoid_: generic "sprint boost"

**Low stamina sound**:
Gasp when sprint energy is low after sprinting. README may call this **Out of Breath**; use **Low stamina sound** in issues, triage, and config discussion.
_Avoid_: **Out of Breath** as the canonical term in new issues (marketing alias only)

**Fake footsteps**:
Rare 3D footsteps perceived behind the player during a **Run** (not **Psychotic Break** circling audio).
_Avoid_: **Audio Dread** ambient footsteps (similar sound, different feature)

### Psychotic Break (triggers and episode)

**Episode**:
Fixed-duration client-local horror beat (about 20s by default): screen overlay, phased audio, **Interaction lockdown**, flashlight off, stumble on exit. Only the affected player experiences it.
_Avoid_: "cutscene", network-synced event

**Recent threat**:
An enemy was within about 15m recently; that danger is remembered for about 30s for trigger checks.
_Avoid_: "aggro", "chase state"

**Solo** (trigger):
No other alive player is within about 30m.
_Avoid_: "singleplayer only", "alone in lobby"

**LoS lost**:
The player cannot see the threatening enemy anymore, but **Recent threat** memory is still active.
_Avoid_: vague "out of sight", "enemy despawned"

**Interaction lockdown**:
During an **Episode**, the player cannot move or act normally; controls return when the episode ends.
_Avoid_: pause menu freeze

**Phantom sound**:
A threatening 3D scream during the episode peak window (chance follows config). Same role as **Threat scream** in shipped audio.
_Avoid_: **Fake footsteps**, **Audio Dread** whispers

**Peak scream**:
Short intense burst when the episode hits its peak phase.
_Avoid_: **Shadow scream** (retired naming)

**Distant scream**:
Escalating ambience during buildup and crescendo before the peak.
_Avoid_: **Shadow scream** variants

**Threat scream**:
Longer fade-in threat used for **Phantom sound** moments (randomized pitch, 3D).
_Avoid_: **Shadow scream** variants

**Circling footsteps** (Psychotic Break):
Footsteps that pan around the player during the episode peak. Uses the shared footsteps bundled clip, not **Fake footsteps**.
_Avoid_: `phantom_footsteps` (README/doc name; file is not shipped under that name)

### Monster overhaul and network authority

**Host-authoritative** (monsters):
Monster movement and detection changes apply on the host; other clients experience the host's monsters. Lobby mates without Dread still face the host-tuned threat.
_Avoid_: "synced to all clients" when you mean per-client visual/audio layers

**Client-local**:
Effect only on the installing player's game: ambient audio, **Tension System**, **Psychotic Break**, per-enemy audio treatment, crouch speed boost.
_Avoid_: "singleplayer only" for multiplayer-safe client effects

**Monster aggression**:
Host-only tuning: faster enemies and wider detection. Distinct from the full **Monster Overhaul** (which also includes enemy audio treatment).
_Avoid_: **Monster Overhaul** when you mean only aggression tuning

**Enemy audio tweak**:
One-time scarier audio treatment per enemy (pitch, spatial feel, etc.), tracked so the same enemy is not processed repeatedly.
_Avoid_: **DreadAudioTweaked** in player-facing text (internal marker name); maintaining a manual per-mod enemy list

### Harmony, config, and compatibility

**Harmony patch**:
Dread changes game methods at runtime via Harmony. Patches tied to config can be applied or removed when related toggles change.
_Avoid_: "always-on patches", "IL rewrite" in issue titles

**Mod compatibility**:
Designed to work with modded enemy packs (Mimic, WesleysEnemies, etc.) by targeting shared enemy behavior, not a single mod ID. REPOLib is not required.
_Avoid_: **Compatibility mode** (future degrade/skip path); a single "compat" toggle (use specific **REPOConfig compat** or **Compat layer** instead)

**REPOConfig compat**:
Dread's BepInEx config can be edited live through the REPOConfig mod.

**REPOConfig slider label compat**:
Temporary workaround so REPOConfig float sliders show setting names when upstream passes blank descriptions (tracked for removal in DBG-4).
_Avoid_: assuming REPOConfig ships Dread defaults; blaming Dread keys for missing labels

### Audio assets and loading

**Weighted clip**:
Ambient sound pick uses a weight table: common events (scraping, footsteps) vs rare (whisper). Used by **Audio Dread**.
_Avoid_: uniform random ambient pick

**Bundled audio**:
OGG files shipped with the mod, loaded through a shared loader reused by multiple systems.
_Avoid_: duplicating load logic per system; "missing asset" without checking game path / Proton path issues

### Agent debugging and telemetry

**Fingerprint** (error reporting):
Stable identity for a crash or error so duplicate reports collapse into one GitHub issue.
_Avoid_: "crash ID" in player-facing copy

### Historical terms (do not use for new work)

**DreadHost**:
Removed monolithic host; replaced by per-**system host** objects.

**Shared enemy cache**:
Retired idea: one shared enemy list for all systems. Superseded by each system doing its own proximity scan (ADR-0008).

**Visual Corruption / Environmental Wrongness**:
Cut from shipping scope (ADR-0001); not part of current Dread.

**Shadow scream** / **shadow_scream_1-3**:
Retired doc names for episode screams. Shipped audio: **Peak scream**, **Distant scream**, **Threat scream** (`scream_peak`, `scream_distant`, `scream_threat` on disk).

**phantom_footsteps**:
Retired doc name for **Circling footsteps** (Psychotic Break). Shipped clip: `footsteps.ogg`.

## Flagged ambiguities

Open conflicts only. Resolved terms live in **Language** above.

| Topic | Notes |
|-------|--------|
| Proximity scan interval | Confirm live tuning vs CHANGELOG before changing behavior docs; do not hard-code seconds in this glossary. |

## Example dialogue

**Dev:** Psychotic Break fired: solo, recent threat, LoS lost, crouched. **Episode** ran **Interaction lockdown** and killed the flashlight until exit.

**Expert:** Others see the overlay?

**Dev:** No. **Psychotic Break** is **client-local**. **Host-authoritative** monsters are unchanged on the wire except existing Photon sync.

**Expert:** Config says once per **match**; the issue said **run**.

**Dev:** **Match** for `PsychoticBreakOncePerMatch`. **Run** means in-level play, not menu.

**Expert:** README "Out of Breath"?

**Dev:** Same as **Low stamina sound** (`LowStaminaSoundEnabled`). Use the glossary term in triage.

## File map

| Area | Path |
|------|------|
| Plugin entry, Harmony apply | `Plugin.cs` |
| Config bindings | `Config/DreadConfig.cs` |
| Runtime systems (flat) | `Systems/*.cs` (initializer, tension, audio, debug server, compat, etc.) |
| Harmony patches | `Systems/Patches/` |
| Psychotic break | `Systems/PsychoticBreak/` |
| Error reporting | `Systems/ErrorReporting/` (+ `ErrorReportJson.cs` at `Systems/` root) |
| Debug overlay | `Systems/DebugOverlay/` |
| OGG assets | `audio/` |
| Architecture decisions | `docs/adr/` |
| Agent orchestration hub | `docs/agents/README.md` |
| Agent implementation guides | `docs/agents/guides/README.md` (index of all systems) |
| Agent workflows (verify, PR, subagents) | `docs/agents/orchestration.md` |
| Agent domain consumption | `docs/agents/domain.md` |
| MCP bridge (debug protocol) | `dread-mcp-server/` |
| Error ingest worker | `workers/error-reporter/` |
| Build and Thunderstore package | `build.ps1`, `manifest.json` |
