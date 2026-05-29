# Security Policy

## Supported Versions

Security fixes target the latest release on Thunderstore and `master`. Older mod versions are not supported unless noted in a release advisory.

| Version | Supported |
| ------- | --------- |
| Latest Thunderstore release | Yes |
| `master` (pre-release) | Best effort |
| Older releases | No |

## Reporting a Vulnerability

**Do not open a public GitHub issue for undisclosed security problems.**

1. Open a [private security advisory](https://github.com/grompen91-droid/dreadREPO/security/advisories/new) on this repository, or email the maintainer via the contact on their GitHub profile.
2. Include steps to reproduce, affected versions, and impact (game client, Cloudflare worker, MCP server, etc.).
3. Allow up to 7 days for an initial response. Critical issues in the error-reporter worker or telemetry path get priority.

We will confirm receipt, assess severity, and coordinate a fix and release. Credit is given in the changelog when reporters want it.

## Scope

| Component | Notes |
| --------- | ----- |
| Dread BepInEx plugin (`Dread.dll`) | In-game mod behavior, config, patches |
| Error reporter worker (`workers/error-reporter`) | Cloudflare Worker proxy for opt-in telemetry |
| Dread MCP server (`dread-mcp-server`) | Local dev/debug tooling only |

Out of scope: vanilla R.E.P.O., BepInEx, other mods, and Thunderstore hosting.
