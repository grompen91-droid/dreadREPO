# ADR-0009: Toggleable Harmony Patches with Runtime Lifecycle Management

**Date:** 2026-05-22
**Status:** Accepted

---

## Context

Issue #107 identified that all three Harmony patches were installed permanently via `_harmony.PatchAll()` in `Plugin.Awake()`. There was no mechanism to:

1. Conditionally skip installing a patch at load time based on config
2. Uninstall a patch at runtime when the user toggles a config option
3. Reinstall a patch at runtime when the user re-enables a config option

This meant patches were "all-or-nothing" and their effects could only be gated internally (via config checks inside the Postfix/Prefix method), leaving the IL permanently modified even when the feature was disabled.

---

## Decision

### 1. Replace PatchAll with Explicit Per-Patch Lifecycle

Each Harmony patch class exposes two static methods:

- `Apply(Harmony harmony)`: locates the target method via `AccessTools.Method` and calls `harmony.Patch()` with the appropriate prefix/postfix
- `Remove(Harmony harmony)`: calls `harmony.Unpatch()` to remove the specific patch from the target method

Each patch class stores a private `MethodInfo? _original` field to track whether the patch is currently applied.

### 2. Config-Gated Initial Application

`Plugin.Awake()` reads each config toggle at startup and only calls `Apply()` for patches whose feature is enabled. If a feature is disabled at startup, the patch is never installed -- zero IL impact.

### 3. Runtime Toggle via SettingChanged Handler

Each config entry registers a `SettingChanged` handler. When the user changes the toggle in BepInEx config:

- Enabled -> Disabled: `Remove()` is called, the patch is uninstalled from the target method
- Disabled -> Enabled: `Apply()` is called, the patch is installed on the target method

This allows full lifecycle management without requiring a game restart.

---

## Consequences

- Players can toggle monster aggression, crouch speed boost, and investigate radius at runtime with immediate effect
- Zero IL impact when a feature is disabled at startup -- no Harmony overhead for unused patches
- No `[HarmonyPatch]` or `[HarmonyPostfix]` attributes remain; all patching is explicit and self-documenting
- Slightly more verbose code per patch (Apply/Remove boilerplate) but each method is 4-6 lines
- `MonsterAggressionEnabled` controls two patches (EnemyNavMeshAgentAwake and EnemyDirectorSetInvestigate); both are applied/removed together
- If `AccessTools.Method` fails (e.g., the target class/method was removed in a game update), `Apply()` throws immediately at startup rather than failing silently in a Postfix -- faster detection of compatibility issues

---

## Rejected Alternatives

- **Keep PatchAll with internal config guards**: patches would still be installed even when disabled, wasting CPU cycles on every enemy spawn
- **Separate Harmony instances per patch**: unnecessary complexity; a single Harmony instance with selective Patch/Unpatch is cleaner
- **HarmonyManipulation.PatchAll with filter delegate**: Harmony 2.x supports passing a predicate to `PatchAll()` to skip certain classes, but this doesn't support runtime toggling
- **Two MonoBehaviours (Patcher + Unpatcher)**: adds game-object lifecycle management overhead for what is essentially a Harmony configuration concern
