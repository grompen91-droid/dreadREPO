# Tasks: Remote audio assets (014)

**Branch**: `014-remote-audio-assets`

## Phase 1: Core pipeline

- [x] T001 `audio-manifest.json` + category folders under `audio/`
- [x] T002 `AudioAssetSystem` + cache reconcile, download, decode
- [x] T003 `AudioAssetApi` + `AudioAssetPathResolver`
- [x] T004 Migrate AudioDread, Tension, PsychoticBreak, Snitch to `RequestClip`
- [x] T005 CD: no OGG in zip; `upload-audio-release-assets.ps1`
- [x] T006 ADR-0017, agent guides, CHANGELOG `[Unreleased]`

## Phase 2: Hardening

- [x] T007 In-flight download guard + retry before `FulfillPath(null)`
- [x] T008 Remove bundled `AudioClipLoader.LoadClip`; CI/verify guard
- [x] T009 `door_creak.ogg` in manifest + ambient weights
- [x] T010 Spec Kit: `specs/014-remote-audio-assets`, `feature.json`, quickstart (Debug `dotnet build` + `DeployToProfile`)

## Phase 3: Verify

- [x] T011 Tier 0 `verify-dread.ps1` green
- [ ] T012 Manual quickstart matrices (in-game, dread profile)
