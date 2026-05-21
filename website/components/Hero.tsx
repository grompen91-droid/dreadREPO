"use client";
export default function Hero() {
  return (
    <section
      className="relative flex flex-col items-center justify-center min-h-screen text-center px-6 overflow-hidden"
      style={{ background: "var(--bg)" }}
    >
      {/* radial crimson vignette */}
      <div
        className="absolute inset-0 pointer-events-none"
        style={{
          background:
            "radial-gradient(ellipse 80% 60% at 50% 50%, rgba(220,38,38,0.07) 0%, transparent 70%)",
        }}
      />

      {/* top-left corner bleed */}
      <div
        className="absolute top-0 left-0 w-96 h-96 pointer-events-none"
        style={{
          background:
            "radial-gradient(circle at 0% 0%, rgba(220,38,38,0.06) 0%, transparent 70%)",
        }}
      />

      {/* bottom-right corner bleed */}
      <div
        className="absolute bottom-0 right-0 w-96 h-96 pointer-events-none"
        style={{
          background:
            "radial-gradient(circle at 100% 100%, rgba(220,38,38,0.04) 0%, transparent 70%)",
        }}
      />

      <div className="relative z-10 flex flex-col items-center gap-6 max-w-4xl">
        {/* version badge */}
        <span
          className="inline-flex items-center gap-2 text-xs font-mono tracking-widest uppercase px-3 py-1 border opacity-0 animate-fade-in"
          style={{
            color: "var(--crimson)",
            borderColor: "var(--crimson-dim)",
            background: "rgba(220,38,38,0.05)",
            animationFillMode: "forwards",
          }}
        >
          <span
            className="w-1.5 h-1.5 rounded-full animate-pulse-slow"
            style={{ background: "var(--crimson)" }}
          />
          v1.5.0 — R.E.P.O.
        </span>

        {/* title */}
        <h1
          className="text-8xl md:text-9xl font-black tracking-tighter leading-none opacity-0 animate-slide-up glow-text delay-200"
          style={{
            color: "var(--text)",
            fontFamily: "var(--font-geist-sans)",
            animationFillMode: "forwards",
          }}
        >
          DREAD
        </h1>

        {/* subtitle */}
        <p
          className="text-lg md:text-xl max-w-xl leading-relaxed opacity-0 animate-slide-up delay-300"
          style={{
            color: "var(--text-muted)",
            animationFillMode: "forwards",
          }}
        >
          Atmospheric horror overhaul for R.E.P.O. Ambient dread, scarier
          monsters, and a tension system that reads your proximity to danger in
          real time.
        </p>

        {/* CTA buttons */}
        <div
          className="flex flex-col sm:flex-row gap-4 mt-4 opacity-0 animate-slide-up delay-500"
          style={{ animationFillMode: "forwards" }}
        >
          <a
            href="https://thunderstore.io/c/repo/p/elytraking/Dread/"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center gap-2 px-8 py-3 text-sm font-semibold tracking-wider uppercase transition-all duration-200 glow-red"
            style={{
              background: "var(--crimson)",
              color: "#fff",
            }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.background = "#b91c1c")
            }
            onMouseLeave={(e) =>
              (e.currentTarget.style.background = "var(--crimson)")
            }
          >
            Install via Thunderstore
          </a>
          <a
            href="https://github.com/grompen91-droid/dreadREPO"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center gap-2 px-8 py-3 text-sm font-semibold tracking-wider uppercase border transition-all duration-200"
            style={{
              color: "var(--text-muted)",
              borderColor: "var(--border)",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = "var(--text)";
              e.currentTarget.style.borderColor = "#555";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = "var(--text-muted)";
              e.currentTarget.style.borderColor = "var(--border)";
            }}
          >
            View Source
          </a>
        </div>

        {/* stat strip */}
        <div
          className="flex gap-8 mt-10 pt-10 opacity-0 animate-fade-in delay-700"
          style={{
            borderTop: "1px solid var(--border)",
            animationFillMode: "forwards",
          }}
        >
          {[
            { value: "4", label: "Systems" },
            { value: "10+", label: "Features" },
            { value: "0", label: "Dependencies" },
            { value: "1.5.0", label: "Version" },
          ].map(({ value, label }) => (
            <div key={label} className="flex flex-col items-center gap-1">
              <span
                className="text-2xl font-black tracking-tight"
                style={{ color: "var(--crimson)" }}
              >
                {value}
              </span>
              <span
                className="text-xs tracking-widest uppercase"
                style={{ color: "var(--text-dim)" }}
              >
                {label}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* scroll hint */}
      <div
        className="absolute bottom-8 left-1/2 -translate-x-1/2 animate-drift"
        style={{ color: "var(--text-dim)" }}
      >
        <svg
          width="16"
          height="24"
          viewBox="0 0 16 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <rect
            x="1"
            y="1"
            width="14"
            height="22"
            rx="7"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <circle cx="8" cy="7" r="2" fill="currentColor" className="animate-drift" />
        </svg>
      </div>
    </section>
  );
}
