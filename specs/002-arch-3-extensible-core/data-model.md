# ARCH-3 Data Model

Conceptual entities for system registration and initialization (not persisted database types).

## SystemRegistration

| Field | Description |
|-------|-------------|
| `id` | Stable slug (e.g. `audio-dread`) |
| `systemType` | `typeof` MonoBehaviour system |
| `hostName` | DontDestroyOnLoad `GameObject` name |
| `orderGroup` | `core` \| `debug` |
| `isEnabled` | Optional predicate; null means always try when init runs |
| `rationale` | One line for ADR/registry review |

**Relationships**: Consumed by `DreadSystemInitializer` after UI gate passes; initializer sorts by `orderGroup` (Core before Debug), then list declaration order.

**Ordering**: `orderGroup` is enforced at runtime via `OrderBy(OrderGroup)`; declaration order within a group remains significant for reviewers.

**Validation**: Each concrete runtime system in [mod-architecture.md](../../docs/agents/guides/mod-architecture.md) table maps to exactly one registration.

## InitResult (conceptual)

Not a persisted C# type in v1. Per-system outcomes are implied by try/catch in `TryAddSystem(Type, hostName)` and aggregated only in the session log line.

| Field | Description |
|-------|-------------|
| `registrationId` | Links to `SystemRegistration.id` (conceptual) |
| `success` | Whether `AddComponent` succeeded |
| `errorDetail` | Exception message if failed |

**Relationships**: Shipped logging uses `(count)` or `(count/attempted)` summary only; see `DreadSystemInitializer` after the registry loop.

## CompatProfile

| Field | Description |
|-------|-------------|
| `compatibilityMode` | From `DreadConfig.CompatibilityMode` |
| `skipConflictingPatches` | From `DreadConfig.CompatibilitySkipConflictingPatches` |
| `isHost` | `HarmonyPatchCompat.IsMasterClient()` |
| `optionalMods` | Set of detected soft deps (REPOConfig, MenuLib, etc.) |

**Relationships**: Influences patch apply in `Plugin` and which registry entries use `isEnabled` predicates.

## ExtensionModelDocument

| Field | Description |
|-------|-------------|
| `path` | `docs/adr/0016-arch-3-extension-model.md` |
| `contracts` | Links to `contracts/system-lifecycle.md`, `contracts/extension-registry.md` |
| `arch4Boundary` | Explicit deferral of public mod API |

## VerifyGuard

| Field | Description |
|-------|-------------|
| `id` | e.g. `arch3_registry_single_path` |
| `tier` | `tier0` |
| `rule` | Human-readable rule enforced by `verify-dread.ps1` |
