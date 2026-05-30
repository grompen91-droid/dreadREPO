# ERR-3 Data Model

Conceptual entities for privacy disclosure (not persisted types).

## PrivacyDisclosure

**Implementation**: `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs` (`ShortSummary`, `DataBullets`, `DisableInstructions`, `FullDescription`).

| Field | Description |
|-------|-------------|
| `title` | Short label (e.g. "Anonymous error reporting") |
| `summary` | One sentence purpose |
| `dataBullets` | Ordered list of what may be sent |
| `notCollected` | Explicit negatives (no account name, no chat logs) |
| `destination` | Worker to GitHub issues for developer triage |
| `disableSteps` | Set `ErrorReportingEnabled` false; cfg path |
| `optInNote` | Current default false until ERR-2 |

**Relationships**: Rendered into `ConfigDescription`; consumed by ERR-2 prompt (future).

**Validation**: Each `dataBullets` entry maps to [contracts/privacy-copy.md](./contracts/privacy-copy.md) checklist row.

## ConfigSurface

| Field | Description |
|-------|-------------|
| `section` | `11. Error Reporting` |
| `key` | `ErrorReportingEnabled` |
| `fileName` | `elytraking.dread.cfg` under `BepInEx/config/` |
| `defaultValue` | `false` (unchanged in ERR-3) |
| `description` | Full `PrivacyDisclosure` text for BepInEx |

**Relationships**: Bound in `DreadConfig.cs`; mirrored in REPOConfig when installed.

## PayloadCategory

| Field | Description |
|-------|-------------|
| `id` | `error` \| `gameState` \| `systemInfo` \| `display` \| `config` |
| `captureMethod` | `ErrorReportPayloadCapture` method name |
| `dtoType` | `ErrorReport`, `GameStateData`, etc. in `ErrorReportTypes.cs` |
| `optionalFailure` | Safe capture may omit fields on stub/API failure |

**Relationships**: Serialized in `ErrorReportJson.SerializePayload` per ADR-0015.

## CopyReviewRecord

| Field | Description |
|-------|-------------|
| `reviewer` | Human or agent |
| `date` | Review date |
| `payloadGitSha` | Commit hash of `ErrorReportPayloadCapture` reviewed |
| `checklistComplete` | All contract rows checked |

**Relationships**: Recorded in PR description per [quickstart.md](./quickstart.md); not stored in game.
