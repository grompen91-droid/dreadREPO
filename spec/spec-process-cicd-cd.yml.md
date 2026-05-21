---
title: CI/CD Workflow Specification - Dread Mod Release Pipeline (CD)
version: 2.0
date_created: 2026-05-22
last_updated: 2026-05-22
owner: Grompen91
tags: [process, cicd, github-actions, automation, bepinex, unity-modding, release, deployment]
---

## Workflow Overview

**Purpose**: Build, version, and publish Dread mod releases to GitHub Releases on tag push (vmajor/vminor/vpatch), with a clear extension path to Thunderstore publishing.
**Trigger Events**: Tag push matching `vmajor`, `vminor`, or `vpatch` (literal tag names; NOT `v*.*.*` semver)
**Target Environments**: GitHub Releases (current), Thunderstore (future)

## Execution Flow Diagram

```mermaid
graph TD
    TRIG[Tag Push vmajor/vminor/vpatch] --> VAL[Validate Tag Name]
    VAL -- valid --> VER_READ[Read Version from manifest.json]
    VER_READ --> VER_CALC[Calculate New Version from Tag]
    VER_CALC --> BUMP[Create Local Bump Commit]
    BUMP --> CHLOG[Rename [Unreleased] in CHANGELOG.md + Recreate Empty Header]
    CHLOG --> TAG_FORCE[Force-Move Trigger Tag to Bump Commit]
    TAG_FORCE --> BUILD[Build Release DLL with New Version]
    BUILD -- success --> GH_RELEASE[Create GitHub Release at Tag with Changelog as Body]
    GH_RELEASE --> PUSH[Push Bump Commit to master]
    BUILD -- fails --> ABORT[Discard Local Changes + Exit Error]
    ABORT --> DONE_FAIL

    subgraph "Future Phase"
        TS_DEPLOY[Thunderstore Deploy]
        TS_DEPLOY --> TS_UPLOAD[Upload to Thunderstore]
    end

    PUSH --> TS_DEPLOY

    style TRIG fill:#e1f5fe
    style VER_READ fill:#fff3e0
    style VER_CALC fill:#fff3e0
    style BUMP fill:#fff3e0
    style CHLOG fill:#fff3e0
    style TAG_FORCE fill:#ffe0b2
    style BUILD fill:#f3e5f5
    style GH_RELEASE fill:#e8f5e8
    style PUSH fill:#e1f5fe
    style ABORT fill:#ffcdd2
    style DONE_FAIL fill:#ffcdd2
```

## Jobs & Dependencies

| Job Name | Purpose | Dependencies | Execution Context |
|----------|---------|--------------|-------------------|
| version | Validate tag name (vmajor/vminor/vpatch), read manifest.json, calculate new version, create local bump commit (manifest.json + Plugin.cs + CHANGELOG.md), force-move tag | None | ubuntu-latest, 3m timeout |
| build | Compile Dread.dll in Release mode with new version baked in | version | windows-latest, 15m timeout |
| commit-and-release | Create GitHub Release at tag with changelog as body; push bump commit to master | build | ubuntu-latest, 5m timeout |
| thunderstore | (Future) Upload zip to Thunderstore API | commit-and-release | ubuntu-latest, 5m timeout |

## Requirements Matrix

### Functional Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|-------------------|
| REQ-001 | Tag push vmajor/vminor/vpatch triggers the release pipeline | High | Pushing `git tag vminor && git push origin vminor` starts the workflow |
| REQ-002 | Version is read from manifest.json at runtime, not from the tag | High | Workflow reads manifest.json version, then bumps the segment matching the tag (vmajor=1.5.0>2.0.0, vminor=1.5.0>1.6.0, vpatch=1.5.0>1.5.1) |
| REQ-003 | CHANGELOG.md [Unreleased] section is renamed to the new version with date, and an empty [Unreleased] header is recreated above it | High | After CD runs, CHANGELOG.md shows `[X.Y.Z] - YYYY-MM-DD` replacing `[Unreleased]` with a fresh `[Unreleased]` above it |
| REQ-004 | GitHub Release is named after the actual version (e.g., "v1.6.0"), NOT after the trigger tag | High | Release title is `vX.Y.Z` where X.Y.Z is the bumped version, not `vmajor`/`vminor`/`vpatch` |
| REQ-005 | Release body comes from the [Unreleased] changelog content | Medium | Release body contains the changelog text that was under `[Unreleased]` |
| REQ-006 | On build failure, nothing is pushed to remote, local bump commit is discarded, and tag is not moved | High | Failed build leaves no remote changes; no stale commits or moved tags on failure |
| REQ-007 | The trigger tag is force-moved to the bump commit only after the local bump commit is created, before build; if build fails, the move is undone locally | High | Tag points at bump commit; if build fails, local branch/tag changes are discarded |

### Security Requirements

| ID | Requirement | Implementation Constraint |
|----|-------------|---------------------------|
| SEC-001 | GITHUB_TOKEN scoped to contents: write (release creation + push) | Minimal permissions: contents: write, metadata: read |
| SEC-002 | No external secrets exposed in logs | Redact any secrets in step output |
| SEC-003 | Tag push validation prevents non-owner tags from triggering releases | Use github.actor or github.repository_owner check |

### Performance Requirements

| ID | Metric | Target | Measurement Method |
|----|-------|--------|-------------------|
| PERF-001 | End-to-end runtime | < 10 min | Aggregate wall-clock |
| PERF-002 | Tag validation + version read + version bump | < 1 min | Single step timing |

## Input/Output Contracts

### Inputs

```yaml
# Trigger
ref_name: string          # git tag name ("vmajor", "vminor", or "vpatch")
ref_type: string          # must be "tag" (enforced by trigger filter)

# Repository content
manifest.json: file       # Current version in manifest (source of truth for version)
Plugin.cs: file           # Current version in C# constant
CHANGELOG.md: file        # Source for release notes ([Unreleased] section)
```

### Outputs

```yaml
# GitHub Release
release_url: string       # URL to the created GitHub Release (e.g., "https://github.com/.../releases/tag/v1.6.0")
release_tag: string       # The actual version tag (e.g., "v1.6.0"), NOT the trigger tag

# Artifacts
Dread.dll: file           # Compiled mod DLL (bin/Release/net48/Dread.dll)

# Commits
version_bump_commit: sha  # Commit hash of the manifest.json + Plugin.cs + CHANGELOG.md bump (pushed to master)
```

### Secrets & Variables

| Type | Name | Purpose | Scope |
|------|------|---------|-------|
| Token | GITHUB_TOKEN | Create release, push bump commit, force-move tag | Workflow (write) |
| Secret | THUNDERSTORE_TOKEN | (Future) Authenticate with Thunderstore API | Repository |
| Variable | THUNDERSTORE_OWNER | (Future) Thunderstore team/user name | Repository |

## Execution Constraints

### Runtime Constraints

- **Timeout**: 15m global max (build stage)
- **Concurrency**: Grouped by tag `release-${{ github.ref }}`; only one release per tag
- **Resource Limits**: Standard GitHub-hosted runners (2 vCPU, 7GB RAM for Windows)

### Environmental Constraints

- **Runner Requirements**: Windows for build (same MAUI workload requirement as CI), Linux for version/commit-and-release
- **Network Access**: GitHub API (release creation, push), NuGet.org (restore)
- **Permissions**: `contents: write` (manage releases + push), `metadata: read`

## Error Handling Strategy

| Error Type | Response | Recovery Action |
|------------|----------|-----------------|
| Invalid tag name (not vmajor/vminor/vpatch) | Hard fail with error message: "Tag must be one of: vmajor, vminor, vpatch" | User deletes tag, pushes a valid tag |
| [Unreleased] section missing from CHANGELOG.md | Hard fail with error: "[Unreleased] section not found in CHANGELOG.md" | User adds [Unreleased] section, re-pushes tag |
| Build failure | Discard all local changes (bump commit, tag move reverted), exit with error | User fixes build issue, re-pushes tag |
| Release creation failure (duplicate) | Fail with duplicate release error | Delete existing release for this tag, re-run |
| Version bump push rejected (diverged) | Fail with merge conflict error | User rebases master, re-pushes tag |
| manifest.json parse failure | Hard fail with parse error | User fixes manifest.json format, re-pushes tag |

## Quality Gates

### Gate Definitions

| Gate | Criteria | Bypass Conditions |
|------|----------|-------------------|
| Tag Validation | Tag name is exactly one of: vmajor, vminor, vpatch | None (hard fail) |
| [Unreleased] Presence | CHANGELOG.md contains an `[Unreleased]` section header | None (hard fail) |
| Build | dotnet build exits 0 | None (hard fail: on failure, discard all changes) |
| Version Consistency | New version calculated from manifest + tag matches the version written to all files | None (verification step in bump) |

## Monitoring & Observability

### Key Metrics

- **Release Frequency**: Count of successful releases per week/month
- **Success Rate**: Target > 95% release success rate
- **Build Time**: Release build should complete in < 8 min typical

### Alerting

| Condition | Severity | Notification Target |
|-----------|----------|-------------------|
| CD workflow failure | Medium | Repository owner (GitHub notification) |
| Failed push to master | Medium | Repository owner |
| Missing [Unreleased] section | Medium | Workflow log + notification |
| Duplicate release attempt | Low | Workflow log only |

## Integration Points

### External Systems

| System | Integration Type | Data Exchange | SLA Requirements |
|--------|------------------|---------------|------------------|
| GitHub Releases | REST API (octokit) | Release metadata changelog body + binary asset | Standard GitHub SLA |
| NuGet.org | Package restore | .NET packages | Standard NuGet SLA |

### Dependent Workflows

| Workflow | Relationship | Trigger Mechanism |
|----------|--------------|-------------------|
| CI pipeline | Independent | PR events (no dependency on CD) |

## Compliance & Governance

### Audit Requirements

- **Execution Logs**: Retained by GitHub Actions for 90 days
- **Approval Gates**: None (tag push is author's intent to release)
- **Change Control**: Workflow versioned in repo; spec updated before implementation changes

### Security Controls

- **Access Control**: GITHUB_TOKEN scoped to `contents: write` (release creation + push)
- **Secret Management**: THUNDERSTORE_TOKEN (future) stored as repository secret
- **Vulnerability Scanning**: None (out of scope for this workflow)

## Edge Cases & Exceptions

### Scenario Matrix

| Scenario | Expected Behavior | Validation Method |
|----------|-------------------|-------------------|
| Tag pushed from fork | Workflow does NOT trigger (tag push from fork is not an event) | Documentation only |
| Tag `vpatch` pushed while version is 1.5.0 | CD reads 1.5.0 from manifest, bumps patch to 1.5.1, creates bump commit, force-moves tag, builds, releases at tag v1.5.1, pushes to master | Push vpatch on master at 1.5.0 |
| Tag `vminor` pushed while version is 1.5.0 | CD bumps minor to 1.6.0 | Push vminor on master at 1.5.0 |
| Tag `vmajor` pushed while version is 1.5.0 | CD bumps major to 2.0.0 | Push vmajor on master at 1.5.0 |
| Tag `foobar` pushed | Hard fail: "Tag must be one of: vmajor, vminor, vpatch" | Push invalid tag |
| Tag `VPATCH` pushed (uppercase) | Hard fail: tag name must match exactly | Push VPATCH |
| CHANGELOG.md has no [Unreleased] section | Hard fail: "[Unreleased] section not found" | Remove [Unreleased], push tag |
| Build fails after tag force-move | Local bump commit and tag move discarded; nothing pushed | Simulate build failure |
| Force push to master between tag push and CD commit push | Push rejected; workflow fails | Manually resolve, re-push tag |
| Thunderstore upload (future) fails after GH release succeeds | Release exists but Thunderstore missing | Manual Thunderstore upload or re-run CD with same tag |

## Validation Criteria

### Workflow Validation

- **VLD-001**: Pushing tag `vpatch` when manifest.json shows `1.5.0` creates a GitHub Release titled `v1.5.1` with changelog body
- **VLD-002**: After CD completes, `manifest.json` shows `1.5.1`, `Plugin.cs` shows `1.5.1`, and `CHANGELOG.md` has `[1.5.1] - YYYY-MM-DD` replacing `[Unreleased]` with a fresh empty `[Unreleased]` above it
- **VLD-003**: Pushing tag `vmajor` when manifest.json shows `1.5.0` bumps manifest to `2.0.0` and Plugin.cs to `2.0.0`
- **VLD-004**: Pushing tag `foobar` fails workflow with tag validation error
- **VLD-005**: Pushing a valid tag when CHANGELOG.md is missing `[Unreleased]` fails workflow with missing section error

### Performance Benchmarks

- **PERF-001**: Full pipeline completes in < 10 min
- **PERF-002**: Version validation + read + bump in < 30 seconds

## Change Management

### Update Process

1. **Specification Update**: Modify this document first
2. **Review & Approval**: PR against master with description of change
3. **Implementation**: Apply changes to `.github/workflows/cd.yml` and/or scripts
4. **Testing**: Push a test tag to a feature branch and verify all jobs
5. **Deployment**: Merge to master

### Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2026-05-22 | Initial specification | [Author] |
| 2.0 | 2026-05-22 | Redesign: trigger changed from v*.*.* semver to vmajor/vminor/vpatch literal tags; version source changed from tag to manifest.json; version calculated by incrementing segment matching trigger tag; release named after actual version; CHANGELOG.md [Unreleased] rename step added; missing [Unreleased] is now a hard fail; build failure now discards all local changes; tag force-moved after bump commit, before build; sequencing updated | noxaur |

## Related Specifications

- [CI Pipeline Spec](spec-process-cicd-ci.yml.md): PR verification pipeline
- [build.ps1](../build.ps1): Package build script (reused by CD)
- [AGENTS.md](../AGENTS.md): Thunderstore package requirements and versioning rules
- [CHANGELOG.md](../CHANGELOG.md): Release notes source
