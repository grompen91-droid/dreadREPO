# Security Policy

**Dread** is a BepInEx mod for [R.E.P.O.](https://thunderstore.io/c/repo/) plus optional tooling in this monorepo. This policy covers security issues in our code, not the base game or Thunderstore.

## Supported Versions

Patches ship via Thunderstore (`elytraking/Dread`) and GitHub releases on `master`. Only the latest published mod version receives security fixes unless a release advisory says otherwise.

| Component | Supported |
| --------- | --------- |
| Latest Thunderstore / GitHub release of **Dread** | Yes |
| `master` (unreleased commits) | Best effort |
| Older mod versions | No |

Game target: R.E.P.O. builds compatible with BepInEx 5.4.x (see `manifest.json` dependency on `BepInEx-BepInExPack-5.4.2100`).

## Reporting a Vulnerability

**Do not file a public issue for undisclosed vulnerabilities.**

1. Open a [private security advisory](https://github.com/grompen91-droid/dreadREPO/security/advisories/new) on this repository.
2. Describe impact, affected component, reproduction steps, and mod version (from `manifest.json` / in-game log).
3. Expect an initial response within **7 days**. Issues affecting **opt-in telemetry** (`workers/error-reporter`) or remote ingestion are prioritized.

We will confirm, assess severity, prepare a fix on `master`, and coordinate a Thunderstore release when needed. Reporters who want credit are named in `CHANGELOG.md`.

## In Scope

| Path / artifact | Risk surface |
| --------------- | ------------ |
| `Systems/`, `Plugin.cs`, `Dread.dll` | Harmony patches, config, in-game behavior |
| `workers/error-reporter/` | Cloudflare Worker proxy; handles opt-in crash/telemetry payloads (see ADR-0010 in `docs/`) |
| `dread-mcp-server/` | Local MCP bridge for dev/debug; not shipped to players |
| `.github/workflows/` | CI/CD secrets and supply chain |

## Out of Scope

- R.E.P.O., Unity, Steam, or other mods
- BepInEx itself (report upstream)
- Thunderstore hosting and CDN
- Audio asset licensing (see mod docs; CC sources on freesound.org)

## Safe Disclosure

If you are unsure whether something is a vulnerability (e.g. tension/audio behavior that feels exploitable but is by design), open a **private advisory** anyway. We can reclassify or close it without public disclosure.
