"use client";
const tension = [
  {
    name: "Adrenaline",
    desc: "Sprint energy drains up to 70% slower when an enemy is within 15m. Scales linearly with distance. Fully restored on scene transition.",
  },
  {
    name: "Panic Sprint",
    desc: "Sprinting near an enemy triggers a 1.25× speed burst for 2 seconds. 20s cooldown. Uses Traverse to access the private SprintSpeedMultiplier field.",
  },
  {
    name: "Out of Breath",
    desc: "Plays a gasping breath clip when stamina drops below 5 after sprinting. 60s cooldown. Supports breath2.ogg and breath3.ogg variants.",
  },
  {
    name: "Fake Footsteps",
    desc: "Every 3–6 minutes, a 20% chance to spawn a 3D footstep sound 2.5–5m behind you. Pitch randomized 0.5×–1.5×. No source. Nothing is there.",
  },
];

const audio = [
  { clip: "scraping.ogg", weight: "0.6", rarity: "Common" },
  { clip: "footsteps.ogg", weight: "0.6", rarity: "Common" },
  { clip: "breathing.ogg", weight: "0.3", rarity: "Uncommon" },
  { clip: "whisper.ogg", weight: "0.1", rarity: "Rare" },
];

export default function Systems() {
  return (
    <section
      className="py-32 px-6"
      style={{ background: "var(--surface)" }}
    >
      <div className="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-20">
        {/* tension system */}
        <div>
          <span
            className="text-xs font-mono tracking-widest uppercase"
            style={{ color: "var(--crimson)" }}
          >
            Tension System
          </span>
          <h2
            className="text-3xl font-black tracking-tight mt-2 mb-8"
            style={{ color: "var(--text)" }}
          >
            One scan.
            <br />
            Four responses.
          </h2>

          <div className="flex flex-col gap-0" style={{ borderLeft: "1px solid var(--border)" }}>
            {tension.map(({ name, desc }, i) => (
              <div
                key={name}
                className="group relative pl-6 pb-8"
              >
                {/* timeline dot */}
                <div
                  className="absolute left-[-5px] top-1 w-2.5 h-2.5 border-2 transition-all duration-200 group-hover:border-0"
                  style={{
                    borderColor: "var(--crimson-dim)",
                    background: "var(--surface)",
                  }}
                  onMouseEnter={(e) => {
                    (e.currentTarget as HTMLElement).style.background = "var(--crimson)";
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLElement).style.background = "var(--surface)";
                  }}
                />
                <span
                  className="text-xs font-mono tracking-widest uppercase"
                  style={{ color: "var(--text-dim)" }}
                >
                  0{i + 1}
                </span>
                <h3
                  className="text-base font-bold mt-1 mb-2"
                  style={{ color: "var(--text)" }}
                >
                  {name}
                </h3>
                <p
                  className="text-sm leading-relaxed"
                  style={{ color: "var(--text-muted)" }}
                >
                  {desc}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* ambient audio */}
        <div>
          <span
            className="text-xs font-mono tracking-widest uppercase"
            style={{ color: "var(--crimson)" }}
          >
            Ambient Audio
          </span>
          <h2
            className="text-3xl font-black tracking-tight mt-2 mb-8"
            style={{ color: "var(--text)" }}
          >
            Weighted random.
            <br />
            Pitch randomized.
          </h2>

          <p
            className="text-sm leading-relaxed mb-6"
            style={{ color: "var(--text-muted)" }}
          >
            Every 60–180 seconds a clip is selected by weighted random draw,
            spawned at a random 3D position 5–15m from camera, pitch rolled
            0.5×–1.5×. The AudioSource self-destructs after playback.
          </p>

          <div
            className="overflow-hidden border"
            style={{ borderColor: "var(--border)" }}
          >
            <table className="w-full text-sm">
              <thead>
                <tr style={{ background: "var(--surface2)" }}>
                  {["Clip", "Weight", "Rarity"].map((h) => (
                    <th
                      key={h}
                      className="text-left px-4 py-3 text-xs font-mono tracking-wider uppercase"
                      style={{ color: "var(--text-dim)" }}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {audio.map(({ clip, weight, rarity }, i) => (
                  <tr
                    key={clip}
                    style={{
                      background: i % 2 === 0 ? "var(--surface)" : "var(--bg)",
                      borderTop: "1px solid var(--border)",
                    }}
                  >
                    <td
                      className="px-4 py-3 font-mono text-xs"
                      style={{ color: "var(--text)" }}
                    >
                      {clip}
                    </td>
                    <td
                      className="px-4 py-3 font-mono text-xs"
                      style={{ color: "var(--crimson)" }}
                    >
                      {weight}
                    </td>
                    <td
                      className="px-4 py-3 text-xs"
                      style={{ color: "var(--text-muted)" }}
                    >
                      {rarity}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <p
            className="text-xs mt-4 font-mono"
            style={{ color: "var(--text-dim)" }}
          >
            Spatial blend 1.0 · linear rolloff · 1m–25m falloff
          </p>
        </div>
      </div>
    </section>
  );
}
