# Contract: Conditional debug registry rows (012)

**Feature**: 012 | **Amends**: [002 extension-registry](../../002-arch-3-extensible-core/contracts/extension-registry.md)

## Rule

**Agent-only** `SystemRegistration` rows exist **only inside** `#if DREAD_DEBUG`:

| Id | Type | Group |
|----|------|-------|
| `debug-server` | `DebugServerSystem` | Debug |
| `debug-overlay` | `DebugOverlaySystem` | Debug |

**Always registered** (production + development):

| Id | Type | Group | Config |
|----|------|-------|--------|
| `test-crash` | `TestCrashSystem` | Debug | `11. Testing` |

Core rows (always present): unchanged (see [extension-registry.md](../../002-arch-3-extensible-core/contracts/extension-registry.md)).

## Ordering

When `DREAD_DEBUG` is defined: Core group runs before Debug group (unchanged ADR-0016).

When `DREAD_DEBUG` is undefined: agent debug hosts are not compiled; `test-crash` still registers.

## Static analysis (Tier 0)

`scripts/verify-dread.ps1`:

1. **Always** require all Core type names + `TestCrashSystem` in `DreadSystemRegistry.cs`.
2. **When** `-RequireDebugRegistry` **or** registry file contains `#if DREAD_DEBUG`: also require `DebugServerSystem` and `DebugOverlaySystem`.
3. **Production CI** uses `.github/scripts/verify-production-dll.sh` on Release `Dread.dll`.

## ADR-0016 note

**Amended for 012**: In production builds, agent debug hosts are not compiled. In development builds, prior behavior retained (config gates behavior inside systems).
