# Contract: Dread runtime system lifecycle

**Feature**: ARCH-3 | **Consumers**: contributors, agents

## Boot order (normative)

1. `Plugin.Awake`: config, logging, Harmony patches (config + compat gated).
2. `Plugin.Start`: optional REPOConfig compat attempt.
3. `SceneManager.sceneLoaded`: call `DreadSystemInitializer.TryInitialize()` until UI ready and init succeeds.
4. `DreadSystemInitializer`: for each [registry](./extension-registry.md) entry in order, if enabled, create host + `AddComponent`.

Patches MUST NOT be registered in the system registry.

## Adding a new system (checklist)

| Step | Location | Requirement |
|------|----------|-------------|
| 1 | `Systems/YourSystem.cs` | `MonoBehaviour`; scene/menu gating inside system |
| 2 | `Config/DreadConfig.cs` | New section/entries with descriptions |
| 3 | `Systems/DreadSystemRegistry.cs` | One `SystemRegistration` row |
| 4 | `CONTEXT.md` | Glossary entry if new domain term |
| 5 | `docs/agents/guides/*.md` | Feature guide if non-trivial |
| 6 | `DreadRuntimeState` | Fields for overlay/MCP only if needed |
| 7 | `DebugServerSystem` | `debugKey` if MCP should tune value |

**MUST NOT** add `TryAddSystem` calls in `Plugin.cs`.

## Enable predicates

Use `isEnabled` on registration when the system should not spawn:

- Debug-only: `() => DreadConfig.DebugServerEnabled.Value`
- Gameplay mutation disabled in compatibility mode: check inside system `Update` **or** predicate on registration (prefer one consistent pattern per subsystem; document in PR)

## Failure behavior

- Failed `AddComponent`: log error with type name; continue registry loop.
- Zero successes: log `All systems failed to initialize` (existing).
- Partial success: log `Systems initialized (N)` with N &lt; total attempted.

## Scene cleanup

Systems that subscribe to `SceneManager.sceneLoaded` MUST unsubscribe in `OnDestroy` on the system host.

## Verification

- Stub `dotnet build` passes.
- `verify-dread.ps1` ARCH-3 tier0 check passes.
- Optional probe system per [quickstart.md](../quickstart.md).
