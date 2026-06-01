# Contract: Camp lure config

**Feature**: 006-lure-snitch-hardening  
**Section**: `2. Monster Overhaul`

## New key

| Key | Type | Default | Range | Description |
|-----|------|---------|-------|-------------|
| `LureCooldownSeconds` | float | 60 | 10-300 | Seconds after a lure cycle ends (enemy reached safe distance) before the same player can be targeted again |

## Bind text (DreadConfig)

```text
Seconds of immunity after enemies reach you and the lure resets. Prevents instant re-lure while hiding. HOST ONLY.
```

## Null-check array

Add `LureCooldownSeconds` to `allFields` in `Config/DreadConfig.cs`.

## Related existing keys (unchanged)

| Key | Default |
|-----|---------|
| `LureEnabled` | true |
| `LureSafeDistance` | 20 |
| `LureCampSeconds` | 90 |
| `LureEscalateSeconds` | 30 |
