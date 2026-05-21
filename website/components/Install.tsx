"use client";
const steps = [
  {
    n: "01",
    title: "Install r2modman",
    desc: "Download the Thunderstore Mod Manager (r2modman). Free, open source, handles BepInEx automatically.",
    href: "https://thunderstore.io/package/ebkr/r2modman/",
    cta: "Get r2modman",
  },
  {
    n: "02",
    title: "Find Dread",
    desc: 'Open r2modman, select R.E.P.O. as your game, search for "Dread" by elytraking. One click install.',
    href: "https://thunderstore.io/c/repo/p/elytraking/Dread/",
    cta: "View on Thunderstore",
  },
  {
    n: "03",
    title: "Configure & Play",
    desc: "BepInEx generates elytraking.dread.cfg on first launch. Every feature is independently toggleable. Compatible with REPOConfig for live editing.",
    href: null,
    cta: null,
  },
];

export default function Install() {
  return (
    <section
      id="install"
      className="py-32 px-6"
      style={{ background: "var(--surface)" }}
    >
      <div className="max-w-6xl mx-auto">
        <div className="mb-16">
          <span
            className="text-xs font-mono tracking-widest uppercase"
            style={{ color: "var(--crimson)" }}
          >
            Installation
          </span>
          <h2
            className="text-4xl md:text-5xl font-black tracking-tight mt-2"
            style={{ color: "var(--text)" }}
          >
            Three steps.
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-px"
          style={{ background: "var(--border)" }}>
          {steps.map(({ n, title, desc, href, cta }) => (
            <div
              key={n}
              className="p-8 flex flex-col gap-4"
              style={{ background: "var(--surface)" }}
            >
              <span
                className="text-4xl font-black font-mono tracking-tighter"
                style={{ color: "var(--text-dim)" }}
              >
                {n}
              </span>
              <h3
                className="text-lg font-bold"
                style={{ color: "var(--text)" }}
              >
                {title}
              </h3>
              <p
                className="text-sm leading-relaxed flex-1"
                style={{ color: "var(--text-muted)" }}
              >
                {desc}
              </p>
              {href && cta && (
                <a
                  href={href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-2 text-xs font-mono tracking-wider uppercase transition-colors duration-200"
                  style={{ color: "var(--crimson)" }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.color = "#ef4444")
                  }
                  onMouseLeave={(e) =>
                    (e.currentTarget.style.color = "var(--crimson)")
                  }
                >
                  {cta} →
                </a>
              )}
            </div>
          ))}
        </div>

        {/* manual install */}
        <div
          className="mt-12 p-6 border"
          style={{ borderColor: "var(--border)" }}
        >
          <h4
            className="text-sm font-bold mb-3 tracking-wider uppercase"
            style={{ color: "var(--text-muted)" }}
          >
            Manual install
          </h4>
          <code
            className="text-xs font-mono leading-relaxed block"
            style={{ color: "var(--text-dim)" }}
          >
            BepInEx/plugins/elytraking-Dread/Dread.dll
            <br />
            BepInEx/plugins/elytraking-Dread/audio/*.ogg
          </code>
        </div>
      </div>
    </section>
  );
}
