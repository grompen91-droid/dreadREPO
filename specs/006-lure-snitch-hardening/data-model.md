# Data model: Camp Lure and Snitch hardening (006)

## GameplayPhase (Core)

| Value | Meaning | AllowsHostMonsterFeatures |
|-------|---------|---------------------------|
| `Menu` | Main menu / menu level | false |
| `TruckOrShop` | Lobby truck or shop between runs | false |
| `ExtractionLevel` | Active extraction floor with level gameplay | true |
| `Unknown` | Could not classify | false (safe default) |

**Source**: `GameplayContext.CurrentPhase` computed each frame or cached with invalidation on scene load / level gen done.

---

## GameplayPhaseLatch (Core, internal)

| Field | Type | Notes |
|-------|------|-------|
| `_extractionActive` | bool | Set true on `NotifyExtractionLevelStarted()` |
| Cleared on | Single scene load (truck return) | Same guard as snitch reset |

---

## CampLureSystem state

### Per-player record (key: roster label)

| Field | Type | Description |
|-------|------|-------------|
| `CampTimer` | float | Seconds isolated beyond safe distance |
| `CooldownUntil` | float | `Time.time` deadline; 0 = no cooldown |

### Session fields

| Field | Type | Description |
|-------|------|-------------|
| `_targetLabel` | string | Active lure target |
| `_targetPos` | Vector3 | Last known position |
| `_pullStep` | int | Escalation step |
| `_nextTick` | float | Evaluate interval |
| `_nextPull` | float | Pull re-issue interval |

### State transitions

```text
Idle → Accumulating: HasEnemies && nearest > safe && cooldown expired
Accumulating → Targeted: campTimer >= threshold && best candidate
Targeted → Contact: nearest <= safe for target
Contact → Cooldown: set cooldownUntil = now + LureCooldownSeconds; clear target
Cooldown → Accumulating: time >= cooldownUntil (still isolated)
Any → Idle: !AllowsHostMonsterFeatures || !HasEnemies
```

---

## SnitchSystem state

| Field | Type | Description |
|-------|------|-------------|
| `_armed` | bool | Marker attached successfully |
| `_armFailed` | bool | Max retries exhausted this level |
| `_triggered` | bool | Pickup fired |
| `_armCountdown` | float | Delay before arm attempt |
| `_armRetries` | int | Item scan retries |
| `_poiRemaining` | float | POI duration countdown |

### Snitch overlay states

| State | Condition |
|-------|-----------|
| `disarmed` | Not in extraction level or not yet armed |
| `arming` | Countdown / retries |
| `armed` | Marker live |
| `triggered` | POI active |
| `failed` | `_armFailed` |

---

## DreadRuntimeState additions

| Property | Type | Publisher |
|----------|------|-----------|
| `GameplayPhase` | string | GameplayContext or systems |
| `LureCooldownRemaining` | float | CampLureSystem |
| `LureBlockReason` | string | CampLureSystem (optional, mirror snitch) |

---

## Config keys

See [contracts/camp-lure-config.md](./contracts/camp-lure-config.md).

| Key | Type | Default |
|-----|------|---------|
| `LureCooldownSeconds` | float | 60 |

Existing keys unchanged: `LureEnabled`, `LureSafeDistance`, `LureCampSeconds`, `LureEscalateSeconds`, `SnitchEnabled`, `SnitchPOIDurationSeconds`.
