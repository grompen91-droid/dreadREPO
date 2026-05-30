# Contract: Error reporting first-run prompt

**Feature**: ERR-2 | **Consumers**: players, `ErrorReporterSystem`, agents

**Consent gate**: `ErrorReportingConsent.IsReportingAllowed()` in `Systems/ErrorReporting/ErrorReportingConsent.cs` (see [data-model.md](../data-model.md)).

## Normative rules

1. First-run UI MUST use `ErrorReportingPrivacyCopy.ShortSummary`, `DataBullets`, and `DisableInstructions` for player-visible text (no paraphrase of payload categories).
2. Dismissing the prompt MUST set `ErrorReportingPromptShown` to `true`.
3. **Keep reporting on** MUST set `ErrorReportingEnabled` to `true`.
4. **Turn off reporting** MUST set `ErrorReportingEnabled` to `false`.
5. Prompt MUST NOT appear when `ErrorReportingPromptShown` is `true`.
6. Prompt MUST NOT appear on menu levels (`SemiFunc.MenuLevel()` or project-equivalent guard).
7. While `ErrorReportingPromptShown` is `false`, error report enqueue/send MUST NOT occur (consent gate), regardless of `ErrorReportingEnabled` default.
8. After prompt is shown, runtime behavior MUST follow `ErrorReportingEnabled` only (existing ADR-0010 pipeline).

## UI contract (IMGUI)

| Element | Requirement |
|---------|-------------|
| Window | Centered, fixed min size, scroll view for bullets |
| Primary actions | Two buttons: "Keep reporting on", "Turn off reporting" (exact labels may vary if meaning preserved) |
| Input | Gameplay input SHOULD be blocked while visible (optional v1: overlay only) |
| Styling | Minimal `GUI.skin` usage; no new asset bundles |

## Registration contract

- Host type registered in `DreadSystemRegistry` with order after core init, always enabled (prompt self-gates on cfg).
- System added only via `DreadSystemInitializer` (ARCH-3).

## ERR-3 cross-reference

- Canonical copy: `specs/003-err-3-privacy-copy/contracts/privacy-copy.md`
- Implementation: `Systems/ErrorReporting/ErrorReportingPrivacyCopy.cs`
- ERR-2 PR MUST update privacy contract row 10 (default-on wording) when changing defaults.

## Review checklist (pre-merge)

- [ ] Fresh cfg: default `ErrorReportingEnabled` true, prompt once, both buttons work.
- [ ] Existing cfg `false`: prompt once, stays false until Keep on.
- [ ] No reports enqueued before `ErrorReportingPromptShown` true on first session.
- [ ] Menu level: no prompt.
- [ ] Grep: no duplicate disclosure strings outside `ErrorReportingPrivacyCopy`.
- [ ] ADR-0010, CHANGELOG, README updated; no version bump in manifest/Plugin.cs.
- [ ] Tier 0 + ErrorReportJson tests pass.
