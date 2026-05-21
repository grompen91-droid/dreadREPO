# ADR-0003: Weighted Random Selection for Ambient Audio

**Date:** 2026-05-21
**Status:** Accepted

---

## Context

The `AudioDreadSystem` plays ambient horror sounds at random intervals (30-90s, config-scaled). Early versions used uniform random selection from a flat list of clips. This meant the rarest intended sound (`whisper.ogg`) played as often as the most common (`scraping.ogg`), making it feel repetitive and reducing the impact of rare events.

---

## Decision

Assign each clip a weight. Use cumulative weighted random selection:

| Clip | Weight | Effective frequency |
|------|--------|-------------------|
| `scraping.ogg` | 1.0 | ~35% of plays |
| `footsteps.ogg` | 1.0 | ~35% of plays |
| `breathing.ogg` | 0.6 | ~21% of plays |
| `whisper.ogg` | 0.25 | ~9% of plays |

Whisper is 4x rarer than scraping. Weights are chosen by feel during testing, not derived from data.

---

## Consequences

- Rare sounds stay rare. Players hear whisper approximately once every 5-10 minutes instead of every 1-2 minutes.
- Common sounds fill the dead air more consistently, maintaining atmospheric pressure.
- Adding new sounds is straightforward: assign a weight, append to the list.
- Weights are hardcoded, not configurable. No request for configurable weights has emerged.

---

## Rejected Alternatives

- **Uniform random with exclusion timer**: ensures variety but doesn't produce the intended rarity distribution. A timer that blocks whisper for 5 minutes is the same as just making it rare.
- **Configurable per-sound weights**: over-engineered for four sounds. If the sound pool grows significantly, revisit.
