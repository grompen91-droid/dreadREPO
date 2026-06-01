# UI notifications and widgets

Agent guide for **DreadNotificationSystem** and **DreadWidgets** (Slate-style HUD helpers). Shipped on `master`; not separate registry rows beyond `DreadNotificationSystem`.

## DreadNotificationSystem

| Item | Detail |
|------|--------|
| Path | `Systems/Notifications/DreadNotificationSystem.cs` |
| Registry id | `notifications` / `DreadNotificationHost` |
| API | Static `Info`, `Warn`, `Bad` (thread-safe; marshals to main thread) |
| Use | Camp lure/snitch toasts, overlay-adjacent status, agent-visible events |

Prefer notifications over `DreadLogger` when the player or agent should see a short on-screen message. Severity is rail color only (no icons).

## DreadWidgets

| Item | Detail |
|------|--------|
| Path | `Systems/UI/DreadWidgets.cs` |
| Role | Reusable monochrome IMGUI widgets (loading bar, slider demo) |
| Consumer | Debug overlay "Kit" demo button |

New gameplay UI should reuse `DreadWidgets` patterns before adding ad-hoc IMGUI. Full overlay layout: `Systems/DebugOverlay/`.

## Related

- [debug-tooling.md](debug-tooling.md): overlay F10, `DreadRuntimeState`
- [camp-lure-and-snitch.md](camp-lure-and-snitch.md): lure/snitch toasts
- [mod-architecture.md](mod-architecture.md): registry table
