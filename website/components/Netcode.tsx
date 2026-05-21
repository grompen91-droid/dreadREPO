const rows = [
  { feature: "Enemy speed, acceleration, detection", authority: "Host", who: "Host only" },
  { feature: "Enemy audio overhaul", authority: "Local", who: "Per client" },
  { feature: "Ambient audio", authority: "Local", who: "Per client" },
  { feature: "Adrenaline + panic sprint + breath", authority: "Local", who: "Per client" },
  { feature: "Fake footsteps", authority: "Local", who: "Per client" },
  { feature: "Crouch speed boost", authority: "Local", who: "Per client" },
];

export default function Netcode() {
  return (
    <section
      className="py-32 px-6"
      style={{ background: "var(--bg)" }}
    >
      <div className="max-w-6xl mx-auto">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-16">
          <div className="lg:col-span-1">
            <span
              className="text-xs font-mono tracking-widest uppercase"
              style={{ color: "var(--crimson)" }}
            >
              Multiplayer
            </span>
            <h2
              className="text-3xl font-black tracking-tight mt-2 mb-4"
              style={{ color: "var(--text)" }}
            >
              Netcode model.
            </h2>
            <p
              className="text-sm leading-relaxed mb-6"
              style={{ color: "var(--text-muted)" }}
            >
              R.E.P.O. uses Photon PUN. Monster changes are host-authoritative
              because the Harmony patches run on the host instance that owns the
              NavMesh sync. All audio and tension effects are client-local and
              invisible to other players.
            </p>
            <div
              className="p-4 border-l-2 text-sm"
              style={{
                borderColor: "var(--crimson)",
                background: "var(--crimson-glow)",
                color: "var(--text-muted)",
              }}
            >
              Players without Dread can join modded lobbies. Monster changes
              only require the host to have the mod.
            </div>
          </div>

          <div className="lg:col-span-2">
            <div
              className="border overflow-hidden"
              style={{ borderColor: "var(--border)" }}
            >
              <table className="w-full text-sm">
                <thead>
                  <tr style={{ background: "var(--surface2)" }}>
                    {["Feature", "Authority", "Requires mod"].map((h) => (
                      <th
                        key={h}
                        className="text-left px-5 py-3 text-xs font-mono tracking-wider uppercase"
                        style={{ color: "var(--text-dim)" }}
                      >
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {rows.map(({ feature, authority, who }, i) => (
                    <tr
                      key={feature}
                      style={{
                        background: i % 2 === 0 ? "var(--surface)" : "var(--bg)",
                        borderTop: "1px solid var(--border)",
                      }}
                    >
                      <td
                        className="px-5 py-3"
                        style={{ color: "var(--text)" }}
                      >
                        {feature}
                      </td>
                      <td
                        className="px-5 py-3 font-mono text-xs"
                        style={{
                          color:
                            authority === "Host"
                              ? "var(--crimson)"
                              : "var(--text-muted)",
                        }}
                      >
                        {authority}
                      </td>
                      <td
                        className="px-5 py-3 text-xs"
                        style={{ color: "var(--text-muted)" }}
                      >
                        {who}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
