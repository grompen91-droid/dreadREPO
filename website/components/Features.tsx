"use client";
const features = [
  {
    icon: "◈",
    title: "Ambient Audio",
    description:
      "Rare positional horror sounds placed in the world every 60–180 seconds. Scraping, breathing, whispers. Fully 3D spatialized, pitch randomized per spawn.",
    tag: "Per client",
  },
  {
    icon: "◉",
    title: "Monster Overhaul",
    description:
      "1.2× speed and acceleration. Randomized pitch on every enemy's audio, applied once per spawn. 1.5× detection radius. Harmony-patched at the IL level.",
    tag: "Host only",
  },
  {
    icon: "◎",
    title: "Tension System",
    description:
      "Proximity scan every 0.5s drives four systems: adrenaline drain reduction, panic sprint burst, out-of-breath audio, and fake footsteps behind you.",
    tag: "Per client",
  },
  {
    icon: "◇",
    title: "Crouch Speed",
    description:
      "30% faster crouch movement. Patched at Awake via Traverse so it persists through speed resets, tumbles, and scene transitions.",
    tag: "Per client",
  },
];

export default function Features() {
  return (
    <section
      id="features"
      className="relative py-32 px-6"
      style={{ background: "var(--bg)" }}
    >
      {/* separator line */}
      <div
        className="absolute top-0 left-1/2 -translate-x-1/2 w-px h-24"
        style={{
          background:
            "linear-gradient(to bottom, transparent, var(--crimson-dim))",
        }}
      />

      <div className="max-w-6xl mx-auto">
        <div className="mb-16">
          <span
            className="text-xs font-mono tracking-widest uppercase"
            style={{ color: "var(--crimson)" }}
          >
            What it does
          </span>
          <h2
            className="text-4xl md:text-5xl font-black tracking-tight mt-2"
            style={{ color: "var(--text)" }}
          >
            Four systems.
            <br />
            One persistent host.
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-px"
          style={{ background: "var(--border)" }}>
          {features.map(({ icon, title, description, tag }) => (
            <div
              key={title}
              className="group relative p-8 transition-all duration-300"
              style={{ background: "var(--bg)" }}
              onMouseEnter={(e) =>
                (e.currentTarget.style.background = "var(--surface)")
              }
              onMouseLeave={(e) =>
                (e.currentTarget.style.background = "var(--bg)")
              }
            >
              {/* hover accent bar */}
              <div
                className="absolute left-0 top-0 bottom-0 w-px opacity-0 group-hover:opacity-100 transition-opacity duration-300"
                style={{ background: "var(--crimson)" }}
              />

              <div className="flex items-start justify-between mb-4">
                <span
                  className="text-2xl"
                  style={{ color: "var(--crimson)" }}
                >
                  {icon}
                </span>
                <span
                  className="text-xs font-mono tracking-wider uppercase px-2 py-0.5 border"
                  style={{
                    color: "var(--text-muted)",
                    borderColor: "var(--border)",
                  }}
                >
                  {tag}
                </span>
              </div>

              <h3
                className="text-lg font-bold mb-3 tracking-tight"
                style={{ color: "var(--text)" }}
              >
                {title}
              </h3>
              <p
                className="text-sm leading-relaxed"
                style={{ color: "var(--text-muted)" }}
              >
                {description}
              </p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
