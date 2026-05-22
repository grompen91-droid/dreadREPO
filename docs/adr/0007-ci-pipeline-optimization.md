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

Restructure the CI as a single job on `ubuntu-latest` with auto-generated stubs (cached) and instant grep-based checks.

### Specific Decisions

**1. Switch runner OS from `windows-latest` to `ubuntu-latest`**

Ubuntu runners start ~10s faster than Windows runners on GitHub Actions. The .NET SDK is pre-installed on both, but ubuntu-latest includes SDK 10+ out of the box, eliminating the `actions/setup-dotnet` step entirely.

**2. Auto-generate stubs in CI with caching**

The `gen-stubs.ps1` script generates 14 reference assemblies (~632KB total, including BepInEx). Rather than committing these to the repo (which breaks automation and clutters diffs), they are generated on every CI run. The output is cached via `actions/cache` with a key based on the script's content hash, so subsequent runs (same PR, same script) skip generation entirely.

The script itself was optimized for speed:
- Real stubs (UnityEngine.dll with actual type stubs) are built with `dotnet build` once
- Empty stub assemblies (13 of them) are built with `dotnet build` sequentially (each restore resolves the built-in netstandard2.0 targeting pack from the SDK, no NuGet download needed)
- BepInEx is downloaded and extracted from GitHub Releases
- Total generation time: ~16s on first run (uncached), ~0s on subsequent runs (cached)

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

- Fresh PR CI (no caches) completes in ~20-30s (from 5-10 min). Subsequent runs (NuGet + stubs cached) take ~11-13s.
- Stub assemblies are generated at runtime, not committed to git. No DLLs in PR diffs. The `.github/stubs/refs/` directory is gitignored.
- Developer workflow unchanged: `gen-stubs.ps1` is available locally for development, and `dotnet build` auto-recovers if stubs are missing by calling the script.
- Format checks are less comprehensive than Roslyn analyzers but cover the two most common formatting issues (trailing whitespace, tabs). For deeper analysis, developers run `dotnet format` locally.
- CD pipeline (tag-triggered release) also uses ubuntu-latest with auto-generated stubs and caching.
- No cached NuGet files, stub assemblies, or build artifacts added to repo. All generated content lives in `~/.nuget/packages` (cached) or `.github/stubs/refs/` (gitignored, generated).

---

## Rejected Alternatives

- **Committed stub DLLs**: would eliminate generation time entirely, but clutters PR diffs with binary files, requires manual regeneration + commit when stubs change, and breaks the automation principle (developers should not need to perform manual steps for CI to work).
- **Docker-based build image**: would require maintaining a custom Docker image with pre-installed SDK and stubs, adding maintenance burden for marginal speed gain.
- **Self-hosted runner**: not practical for an open-source mod; contributors cannot access a self-hosted runner.
- **NuGet package committed to repo**: 20KB nupkg file would eliminate first-run NuGet download entirely, but clutters the repo with cached artifacts (rejected per maintainer preference).
- **Mono msbuild**: Ubuntu runners have Mono pre-installed, but Mono's msbuild does not support SDK-style `.csproj` files without additional tooling.
- **Windows runner with stubs caching**: Windows runner startup is inherently slower (~15s vs ~5s). The cross-platform build approach (auto-generated stubs + reference assemblies NuGet) works identically on both platforms.
