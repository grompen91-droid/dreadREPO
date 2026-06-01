# Dread R.E.P.O. Mod Constitution

Governing principles for Spec Kit workflows (`/speckit-plan`, `/speckit-tasks`, `/speckit-constitution`) on the **Dread** BepInEx horror-overhaul mod for **R.E.P.O.**

## Core Principles

### I. Stub-safe builds

CI and agents MUST compile without a local game install. Use generated stubs under `.github/stubs/refs/` and the stub build from `AGENTS.md` (`gen-stubs.ps1`, then `dotnet build` with `GameDir` and `BepInExDir` pointing at stubs). Regenerate stubs only when stub sources change. Do not add compile-time dependencies on game-only APIs without a `Systems/Core` compat path or documented stub gap.

### II. Manual in-game verification

Horror gameplay features are verified through **quickstart matrices** and Tier 1 MCP/TCP checks (`docs/agents/verify-dread.md`), not Unity play-mode unit tests, unless the spec explicitly requests automated tests (for example `Dread.ErrorReportJson.Tests`). Plans MUST list manual verification steps before claiming done.

### III. Host authority for monsters

Monster movement, speed, acceleration, and detection changes apply on the **host only** (ADR-0004). Use `HarmonyPatchCompat.IsMasterClient()` or equivalent at application sites. Client-local layers (ambient audio, tension, psychotic break, per-enemy audio treatment) run on every client. Never apply host NavMesh or director mutations on all clients in multiplayer.

### IV. Core compat seams

New game integration goes through **`Dread.Systems.Core`** reflection and compat helpers (`EnemyHealthCompat`, `PlayerControllerCompat`, `HarmonyPatchCompat`, etc.), not scattered reflection in feature systems (ADR-0016). Add systems via `DreadSystemRegistry` and `DreadSystemInitializer`; do not add `TryAddSystem<` in `Plugin.cs`. Harmony patches use explicit `Apply`/`Remove` lifecycle per ADR-0009, not blanket `PatchAll`.

### V. Versioning discipline

Never manually bump version strings in `manifest.json`, `Plugin.cs`, `README.md`, or `THUNDERSTORE_README.md`. CD pipeline owns releases (`vmajor` / `vminor` / `vpatch` tags). Record unreleased work under `CHANGELOG.md` **`[Unreleased]`**. Never change Thunderstore `"name"` in manifest (new listing, not update).

### VI. Simplicity

Prefer minimal diffs. No new public mod API (ARCH-4) without an approved spec. Avoid drive-by refactors, duplicate abstractions, and compatibility shims unless the feature spec requires them. Complexity MUST be justified in the plan **Constitution Check** table.

## Technology constraints

| Area | Requirement |
|------|-------------|
| Runtime | C# / .NET Framework 4.8 |
| Mod host | BepInEx 5.4.x (Thunderstore pack 5.4.2100) |
| Patching | Harmony 2, explicit per-patch Apply/Remove |
| Multiplayer | Photon PUN; respect host authority (ADR-0004) |
| Config | `DreadConfig` code registry; BepInEx cfg mirrors on disk |
| Docs style | No em dash in any repo markdown; use colon, comma, or rewrite |
| Build output | `dist\` Thunderstore layout per `AGENTS.md` when packaging locally |

## Quality gates

Plans and PRs SHOULD pass these gates before merge:

| Gate | Command / reference |
|------|---------------------|
| Tier 0 verify | `./scripts/verify-dread.ps1` (stub DLLs, Release build, grep analyze, MCP build, package layout) |
| Format | `dotnet format --verify-no-changes --no-restore` |
| CI analyze | `.github/workflows/ci.yml` `analyze` job (null-forgiving, Windows paths, whitespace, line length, BOM) |
| ARCH-3 registry | Tier 0 `arch3_try_add_system`, `arch3_registry_manifest` |
| Constitution Check | Plan table: stub build, no manual version bump, registry/compat rules, quickstart listed |

Optional: `dotnet test` for error-report JSON when touching `ErrorReportJson`.

## Governance

- This constitution **supersedes** generic Spec Kit template defaults (including mandatory TDD language) for the Dread repo.
- **Amendments**: update this file, bump **Last Amended**, document rationale in `CHANGELOG.md` `[Unreleased]` if user-facing, and re-run `/speckit-constitution` sync if templates reference principles.
- **Architecture decisions**: record in `docs/adr/` (especially host authority ADR-0004, Harmony lifecycle ADR-0009, extension model ADR-0016).
- **Agent orchestration**: start at [`docs/agents/README.md`](../../docs/agents/README.md); workflows in [`docs/agents/orchestration.md`](../../docs/agents/orchestration.md).
- **Domain language**: [`CONTEXT.md`](../../CONTEXT.md) and [`docs/agents/domain.md`](../../docs/agents/domain.md).
- **Active feature**: pinned in [`.specify/feature.json`](../feature.json); do not run `setup-plan.sh` on a filled `plan.md` (overwrites the plan).

**Version**: 1.0.0 | **Ratified**: 2026-05-26 | **Last Amended**: 2026-06-01
