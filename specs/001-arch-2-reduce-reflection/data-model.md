# ARCH-2 Data Model

Conceptual entities for build and reflection documentation (not runtime database types).

## BuildProfile

| Field | Description |
|-------|-------------|
| `id` | `stub` \| `full` |
| `gameDir` | Path to Unity/game Managed assemblies |
| `bepInExDir` | Path to BepInEx core for references |
| `ciDefault` | Whether CI uses this profile |
| `limitations` | Human-readable list (e.g. optional UI reflection still at runtime) |

**Relationships**: Produces `Dread.dll`; consumed by verify tiers.

## ReflectionSite

| Field | Description |
|-------|-------------|
| `id` | Stable slug (e.g. `repoconfig-slider-compat`) |
| `file` | Repo path |
| `method` | Method or hook name |
| `trigger` | `startup` \| `scene` \| `per-frame` \| `event` \| `on-demand` |
| `optionalModGate` | e.g. `REPOConfig loaded`, `none` |
| `stubBuild` | `required` \| `optional` \| `not-used` |
| `fullBuild` | Same enum |
| `disposition` | `keep` \| `reduce` \| `replace` |
| `rationale` | Why reflection exists or change plan |

**Validation**: Every `Systems/**/*.cs` reflection use must map to one row before ARCH-2 closes.

## CompileTimeRef

| Field | Description |
|-------|-------------|
| `typeName` | C# type |
| `assembly` | Stub or game assembly name |
| `replacesSiteId` | Optional link to retired `ReflectionSite` |

**State**: Added during ARCH-2 implementation when `replace` disposition merges.

## InventoryDocument

| Field | Description |
|-------|-------------|
| `path` | `docs/agents/guides/reflection-inventory.md` |
| `lastReviewed` | Date |
| `siteCount` | Total rows |
| `hotPathCount` | Subset where `trigger` = per-frame |
