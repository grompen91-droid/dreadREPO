"use client";
export default function Footer() {
  return (
    <footer
      className="py-12 px-6"
      style={{
        background: "var(--bg)",
        borderTop: "1px solid var(--border)",
      }}
    >
      <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="text-sm font-mono tracking-widest uppercase animate-flicker"
            style={{ color: "var(--crimson)" }}
          >
            DREAD
          </span>
          <span
            className="text-xs font-mono"
            style={{ color: "var(--text-dim)" }}
          >
            v1.5.0
          </span>
        </div>

        <div className="flex items-center gap-6">
          <a
            href="https://thunderstore.io/c/repo/p/elytraking/Dread/"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs font-mono tracking-wider uppercase transition-colors duration-200"
            style={{ color: "var(--text-dim)" }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.color = "var(--text-muted)")
            }
            onMouseLeave={(e) =>
              (e.currentTarget.style.color = "var(--text-dim)")
            }
          >
            Thunderstore
          </a>
          <a
            href="https://github.com/grompen91-droid/dreadREPO"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs font-mono tracking-wider uppercase transition-colors duration-200"
            style={{ color: "var(--text-dim)" }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.color = "var(--text-muted)")
            }
            onMouseLeave={(e) =>
              (e.currentTarget.style.color = "var(--text-dim)")
            }
          >
            GitHub
          </a>
        </div>

        <span
          className="text-xs font-mono"
          style={{ color: "var(--text-dim)" }}
        >
          MIT · elytraking
        </span>
      </div>
    </footer>
  );
}
