# Feature Specification: Camp Lure and Snitch hardening (006)

**Feature Branch**: `006-lure-snitch-hardening`

**Status**: Planned

**Input**: Code review findings for Camp Lure and Snitch; user feedback that lure re-triggers instantly after mobs leave (needs cooldown); both features fire in shop/truck when they should only run during active extraction levels.

## Problem statement

Camp Lure and Snitch share host-only `EnemyLureCompat.Pull` but gate only on `SemiFunc.MenuLevel()`. R.E.P.O. keeps a non-menu scene active in the truck/shop between extraction runs, so both systems can arm, pull enemies, or attach snitch markers outside an active level.

Camp Lure also resets camp timers to zero the moment an enemy leaves safe distance, so with low config thresholds the lure immediately re-arms while the player is still hiding.

## User stories

### US1 - Gameplay phase gating (Priority: P1)

**As a** player in the truck/shop between runs  
**I want** Camp Lure and Snitch to stay inactive  
**So that** horror systems only affect active extraction gameplay.

**Acceptance criteria**:

- A single Core API (`GameplayContext` or successor) exposes at least: `Menu`, `TruckOrShop`, `ExtractionLevel` (names may differ; semantics must be documented).
- Camp Lure, Snitch, and future host monster features use the same gate: active only in `ExtractionLevel`.
- Overlay/debug block reasons reflect the current phase (e.g. `truck/shop`, `menu`).
- Gate degrades safely when REPO phase APIs are missing (default: inactive outside confirmed extraction level).

**Independent test**: Host in truck/shop with features enabled; overlay shows blocked reason; no `EnemyLureCompat.Pull`, no snitch arm attempt, no camp timer accumulation.

---

### US2 - Camp lure cooldown and contact reset (Priority: P1)

**As a** player who briefly escaped a lure pull  
**I want** a cooldown before the lure can target me again  
**So that** low-threshold settings do not instantly re-trigger while I am still hiding.

**Acceptance criteria**:

- New config key `LureCooldownSeconds` (default 60s, range 10-300) under `2. Monster Overhaul`.
- When a lure cycle ends because an enemy entered safe distance (contact), that player receives a per-player cooldown; camp timer does not immediately re-accumulate toward a new pull for that player until cooldown expires.
- While on cooldown, player is excluded from target selection even if still isolated.
- Overlay exposes cooldown remaining for current target or per-player (debug row extension acceptable).
- Contact clears active pull immediately (do not wait for next 1s evaluate tick).

**Independent test**: Min config thresholds; trigger lure; let enemy approach; confirm pull stops and lure does not re-arm for `LureCooldownSeconds`.

---

### US3 - Camp lure correctness (Priority: P1)

**As a** host during level load or with no enemies present  
**I want** camp lure to treat "no enemies" as "not isolated"  
**So that** the system does not pull toward campers when there is no threat.

**Acceptance criteria**:

- `ProximityScan.NearestDistance` returning `float.MaxValue` (or shared helper) does not increment camp timers.
- Target selection requires at least one valid enemy in scan.
- Pull stops when current target is no longer eligible (contact, cooldown, or no enemies).

**Independent test**: Empty enemy scan: no lure target, no pulls. Enemies spawn later: normal behavior resumes.

---

### US4 - Snitch reliability and hygiene (Priority: P2)

**As a** player  
**I want** the snitch to arm once per extraction level, detect real pickups, and stay silent in logs  
**So that** the feature feels intentional and does not spam or false-trigger.

**Acceptance criteria**:

- Remove temporary `AgentDebugLog086b84` instrumentation from production paths.
- Arm attempt logs at Verbose only (not Warning on every attempt).
- Failed arm after max retries uses explicit `Failed` state (not `_armed = true` with no marker).
- Snitch pickup detection ignores spawn-time kinematic/parent/moved signals (grace period or baseline snapshot after settle).
- Snitch only arms after extraction level is confirmed (uses US1 gate + existing `OnLevelGenDone` hook).
- Document multiplayer bang audio limitation in research; implement best-effort fix if REPO exposes a networked sound hook (otherwise ADR note + optional client-local fallback deferred).

**Independent test**: Level gen completes, one item armed; shop items do not false-trigger; return to truck disarms; next level re-arms.

---

### US5 - Shared investigate coordination (Priority: P2)

**As a** maintainer  
**I want** lure and snitch pulls to share one investigate dispatch path with predictable radius behavior  
**So that** aggression patch coupling and concurrent pulls are documented and safe.

**Acceptance criteria**:

- `EnemyLureCompat.Pull` documents interaction with `EnemyDirectorSetInvestigatePatch` (1.5x radius when aggression enabled).
- Optional: tag pull source (`CampLure` vs `Snitch`) in verbose logs for debug.
- No new coordinator required in v1 unless concurrent pulls cause observable bugs in manual test; document stacking behavior in research.

**Independent test**: Snitch POI + camp lure active simultaneously; enemies still receive investigate calls; no exceptions in log.

## Functional requirements (summary)

| ID | Requirement |
|----|-------------|
| FR-001 | Extend `GameplayContext` with phase detection and `AllowsHostMonsterFeatures` (or equivalent). |
| FR-002 | Camp Lure and Snitch gate on extraction level, not merely `!MenuLevel()`. |
| FR-003 | Add `LureCooldownSeconds` config and per-player cooldown state in `CampLureSystem`. |
| FR-004 | Camp Lure honors empty enemy scan; immediate pull stop on contact. |
| FR-005 | Remove agent debug log helper usage; downgrade snitch arm logs. |
| FR-006 | Snitch failed-arm explicit state; pickup grace period. |
| FR-007 | Update `CONTEXT.md`, `CHANGELOG.md`, reflection inventory, quickstart manual matrix. |

## Non-goals

- New public mod API (ARCH-4).
- Automated play-mode tests (constitution: manual verify).
- Rewriting snitch to Harmony pickup hooks (deferred unless heuristics fail in-game).

## Constitution alignment

| Gate | Requirement |
|------|-------------|
| Stub CI build | All changes must compile against `.github/stubs/refs` |
| No manual version bump | Do not edit manifest/Plugin/README version strings |
| Host authority | Monster features remain host-only (ADR-0004) |
| Core namespace | New seams in `Dread.Systems.Core` |
| Changelog | `[Unreleased]` entry required |
