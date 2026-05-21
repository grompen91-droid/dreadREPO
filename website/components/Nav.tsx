"use client";
import { useState, useEffect } from "react";

export default function Nav() {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 40);
    window.addEventListener("scroll", onScroll);
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <nav
      className="fixed top-0 left-0 right-0 z-50 transition-all duration-300"
      style={{
        background: scrolled
          ? "rgba(8,8,8,0.95)"
          : "transparent",
        borderBottom: scrolled ? "1px solid var(--border)" : "1px solid transparent",
        backdropFilter: scrolled ? "blur(12px)" : "none",
      }}
    >
      <div className="max-w-6xl mx-auto px-6 h-14 flex items-center justify-between">
        <span
          className="text-sm font-mono tracking-widest uppercase animate-flicker"
          style={{ color: "var(--crimson)" }}
        >
          DREAD
        </span>
        <div className="flex items-center gap-6">
          <a
            href="#features"
            className="text-xs tracking-wider uppercase transition-colors duration-200"
            style={{ color: "var(--text-muted)" }}
            onMouseEnter={(e) => (e.currentTarget.style.color = "var(--text)")}
            onMouseLeave={(e) => (e.currentTarget.style.color = "var(--text-muted)")}
          >
            Features
          </a>
          <a
            href="#install"
            className="text-xs tracking-wider uppercase transition-colors duration-200"
            style={{ color: "var(--text-muted)" }}
            onMouseEnter={(e) => (e.currentTarget.style.color = "var(--text)")}
            onMouseLeave={(e) => (e.currentTarget.style.color = "var(--text-muted)")}
          >
            Install
          </a>
          <a
            href="https://thunderstore.io/c/repo/p/elytraking/Dread/"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs tracking-wider uppercase px-4 py-1.5 border transition-all duration-200"
            style={{
              color: "var(--crimson)",
              borderColor: "var(--crimson-dim)",
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = "var(--crimson-glow)";
              e.currentTarget.style.borderColor = "var(--crimson)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = "transparent";
              e.currentTarget.style.borderColor = "var(--crimson-dim)";
            }}
          >
            Download
          </a>
        </div>
      </div>
    </nav>
  );
}
