# ADR-0007: CI Pipeline Optimization for Sub-30s Verification

**Date:** 2026-05-22
**Status:** Accepted

---

## Context

The CI pipeline for Dread took 5-10 minutes per PR. Most of this time was spent on infrastructure setup rather than actual verification:

- Windows runner startup (~15s)
- MAUI workload install for net48 targeting pack (~60-180s)
- Stub assembly generation compiling 14 assemblies from scratch (~60-120s)
- `dotnet-format` tool install (~20s)
- Multi-job overhead (4 jobs: relevance -> build -> analyze -> summary)

The actual compilation of 5 C# source files (~23KB total) takes under 4 seconds.

---

## Decision

Restructure the CI as a single job on `ubuntu-latest` with committed stub assemblies and instant grep-based checks.

### Specific Decisions

**1. Switch runner OS from `windows-latest` to `ubuntu-latest`**

Ubuntu runners start ~10s faster than Windows runners on GitHub Actions. The .NET SDK is pre-installed on both, but ubuntu-latest includes SDK 10+ out of the box, eliminating the `actions/setup-dotnet` step entirely.

**2. Commit pre-built stub assemblies instead of generating them per-run**

The `gen-stubs.ps1` script produces 14 small DLLs (~632KB total, including BepInEx). These are reference assemblies with no IL (metadata-only stubs). Committing them eliminates a 90s per-run generation step. They change only when the stub API surface changes, which is rare.

**3. Remove MAUI workload installation**

The `dotnet workload install maui-windows` step was a workaround for missing net48 targeting packs on GitHub's Windows runner. On ubuntu-latest, the `Microsoft.NETFramework.ReferenceAssemblies` NuGet package provides the same capability without a workload install. The package is small (~56KB extracted) and cached via `actions/cache`.

**4. Replace `dotnet-format` with grep-based checks**

`dotnet format --verify-no-changes` on .NET SDK 10 downloads Roslyn analyzer packages on first run (~18s). For a 5-file project, grep-based checks for trailing whitespace and tab characters catch the same class of issues instantly.

**5. Collapse 4 jobs into 1**

GitHub Actions multi-job pipelines have scheduling overhead per job. A single job avoids this and allows checks to run in the background during compilation via bash `&`.

**6. Use shallow checkout (`fetch-depth: 1`)**

Full git history is not needed for PR verification.

---

## Consequences

- Fresh PR CI completes in 15-21s (from 5-10 min). Subsequent runs with cached NuGet packages take 11-13s.
- Stub assemblies are tracked in git (~632KB, one-time cost). They need regeneration when the stub API changes (mod author runs `gen-stubs.ps1` locally and commits the updated DLLs).
- Format checks are less comprehensive than Roslyn analyzers but cover the two most common formatting issues (trailing whitespace, tabs). For deeper analysis, developers run `dotnet format` locally.
- CD pipeline (tag-triggered release) also uses ubuntu-latest + committed stubs, but keeps multi-job structure for clarity of version/bump/package/release stages.
- No cached NuGet files or build artifacts added to repo. NuGet packages are cached at the runner level via `actions/cache`.

---

## Rejected Alternatives

- **Docker-based build image**: would require maintaining a custom Docker image with pre-installed SDK and stubs, adding maintenance burden for marginal speed gain.
- **Self-hosted runner**: not practical for an open-source mod; contributors cannot access a self-hosted runner.
- **NuGet package committed to repo**: 20KB nupkg file would eliminate first-run NuGet download entirely, but clutters the repo with cached artifacts (rejected per maintainer preference).
- **Mono msbuild**: Ubuntu runners have Mono pre-installed, but Mono's msbuild does not support SDK-style `.csproj` files without additional tooling.
- **Windows runner with pre-cached stubs**: Windows runner startup is inherently slower (~15s vs ~5s). The cross-platform build approach (committed stubs + reference assemblies NuGet) works identically on both platforms.
