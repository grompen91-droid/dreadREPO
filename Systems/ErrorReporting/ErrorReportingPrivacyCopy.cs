using System.Text;

namespace Dread.Systems
{
    /// <summary>
    /// Canonical player-facing disclosure for opt-in error reporting (ERR-3, issue #173).
    /// Maps to contract entity PrivacyDisclosure in specs/003-err-3-privacy-copy/data-model.md.
    /// </summary>
    /// <remarks>
    /// ERR-2 (#172) first-run prompt: import <see cref="ShortSummary"/>, <see cref="DataBullets"/>,
    /// and <see cref="DisableInstructions"/> for modal body. Do not paraphrase payload categories.
    /// Persist opt-in/out only via <c>ErrorReportingEnabled</c> (ERR-2 scope).
    /// Contract checklist: <c>specs/003-err-3-privacy-copy/contracts/privacy-copy.md</c>.
    /// </remarks>
    internal static class ErrorReportingPrivacyCopy
    {
        public const string ShortSummary =
            "Anonymous error reporting (on by default for new installs). When enabled, serious Unity errors "
            + "may be sent to the developer to fix bugs.";

        public const string DisableInstructions =
            "To disable: set ErrorReportingEnabled = false in BepInEx/config/elytraking.dread.cfg "
            + "(section 7. Error Reporting in elytraking.dread.cfg). REPOConfig lists the toggle only "
            + "(no per-toggle description API); use the cfg file or Configuration Manager (F1) for full text.";

        public static readonly string[] DataBullets =
        {
            "Exception type, message (length-capped), stack trace (length-capped), and a dedupe hash",
            "Active scene name and session play time",
            "Enemy counts (alive, total, nearby)",
            "Player HP, stamina, and world position when available",
            "OS, CPU, RAM, GPU, device model, and may include VRAM, GPU driver version, and shader level",
            "Screen resolution, refresh rate, DPI, and fullscreen mode",
            "Eleven named Dread settings (toggles plus audio frequency and volume), including this setting",
            "Not sent: your username, Steam profile, or deliberate PII",
            "Default on for new installs; turn off anytime via the first-run prompt or cfg",
        };

        public static readonly string FullDescription;

        static ErrorReportingPrivacyCopy()
        {
            var sb = new StringBuilder();
            sb.Append(ShortSummary);
            sb.Append(" Reports go to the developer through a Cloudflare Worker and may create or update ");
            sb.Append("public GitHub issues labeled auto-reported. Requires network; delivery is not ");
            sb.Append("instant and offline play may delay or drop batches. Does not intentionally include ");
            sb.Append("your account name, Steam ID, voice or chat, or files outside the game. When ");
            sb.Append("enabled, reports may include: ");
            for (var i = 0; i <= 6; i++)
            {
                if (i > 0)
                    sb.Append("; ");
                sb.Append(DataBullets[i]);
            }

            sb.Append(". ");
            sb.Append(DataBullets[7]);
            sb.Append(". ");
            sb.Append(DataBullets[8]);
            sb.Append(". Some fields may be omitted if capture fails. ");
            sb.Append(DisableInstructions);
            FullDescription = sb.ToString();
        }
    }
}
