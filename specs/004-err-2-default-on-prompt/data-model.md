# ERR-2 Data Model

**Feature**: `004-err-2-default-on-prompt`

## Entities

### FirstRunPrompt (runtime)

| Field | Type | Notes |
|-------|------|-------|
| `state` | enum | `Pending`, `Visible`, `Dismissed` |
| `scrollPosition` | Vector2 | IMGUI scroll for long disclosure |
| `sceneShown` | string? | Debug: scene where prompt first rendered |

**Lifecycle**:

```text
Pending -> Visible (first non-menu scene, OnGUI)
Visible -> Dismissed (button click)
Dismissed -> (terminal; no re-entry unless cfg reset)
```

### ErrorReportingPromptShown (config)

| Attribute | Value |
|-----------|-------|
| BepInEx section | `7. Error Reporting` |
| Key | `ErrorReportingPromptShown` |
| Type | `bool` |
| Default | `false` |
| Description | One-time first-run error reporting disclosure acknowledged |

**Relationships**: When `false`, prompt may show and reporting gate may block sends (see research). When `true`, prompt never shows.

### ErrorReportingEnabled (config, existing)

| Attribute | Value |
|-----------|-------|
| Section | `7. Error Reporting` |
| Type | `bool` |
| Default (ERR-2) | `true` (new installs) |
| Description | `ErrorReportingPrivacyCopy.FullDescription` (updated for default-on) |

**Relationships**: Runtime gate for `ErrorReportLogQueue` and capture; set by prompt buttons and later cfg edits.

### ErrorReportingConsent (logical helper, implementation)

Static helper recommended:

```text
IsReportingAllowed():
  if !ErrorReportingEnabled: return false
  if !ErrorReportingPromptShown: return false  // first-run gate
  return true
```

**Relationships**: Used by queue/reporter; keeps FR-006 single source for enable flag while honoring FR-009.

### PrivacyDisclosure (ERR-3, reused)

| Source | Use in ERR-2 |
|--------|----------------|
| `ShortSummary` | Prompt header |
| `DataBullets[]` | Scrollable list |
| `DisableInstructions` | Footer |

No duplicate strings in prompt code.

## State transitions (config)

```text
[New profile]
  ErrorReportingEnabled=true (generated default)
  ErrorReportingPromptShown=false
  -> prompt -> user choice -> PromptShown=true, Enabled per button

[Upgraded profile, had false]
  ErrorReportingEnabled=false (retained)
  ErrorReportingPromptShown=false
  -> prompt -> Turn off -> false, PromptShown=true
  -> prompt -> Keep on -> true, PromptShown=true
```

## Validation rules

- Prompt MUST NOT set `ErrorReportingPromptShown` without a button click (no auto-dismiss on scene change).
- Changing `ErrorReportingEnabled` in cfg after prompt MUST NOT reset `ErrorReportingPromptShown`.
- Resetting cfg file MUST reset both keys (BepInEx default behavior).
