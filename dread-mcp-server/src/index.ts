#!/usr/bin/env node

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { TextContent } from "@modelcontextprotocol/sdk/types.js";
import { z } from "zod";
import * as net from "node:net";

const DEBUG_SERVER_HOST = process.env.DREAD_HOST ?? "127.0.0.1";
const DEBUG_SERVER_PORT = parseInt(process.env.DREAD_PORT ?? "15432", 10);
const COMMAND_TIMEOUT = parseInt(process.env.DREAD_TIMEOUT ?? "15000", 10);

if (isNaN(DEBUG_SERVER_PORT) || DEBUG_SERVER_PORT < 1 || DEBUG_SERVER_PORT > 65535) {
  console.error(`Invalid DREAD_PORT: must be 1-65535, got "${process.env.DREAD_PORT ?? ""}"`);
  process.exit(1);
}
if (isNaN(COMMAND_TIMEOUT) || COMMAND_TIMEOUT < 100) {
  console.error(`Invalid DREAD_TIMEOUT: must be at least 100ms, got "${process.env.DREAD_TIMEOUT ?? ""}"`);
  process.exit(1);
}

interface DreadResponse {
  id: number;
  ok: boolean;
  data?: unknown;
  error?: string;
  code?: number;
}

async function sendCommand(cmd: string, data?: unknown): Promise<DreadResponse> {
  return new Promise((resolve, reject) => {
    const socket = new net.Socket();
    let buffer = "";
    let resolved = false;

    const timer = setTimeout(() => {
      socket.destroy();
      reject(new Error(`Command timed out after ${COMMAND_TIMEOUT}ms`));
    }, COMMAND_TIMEOUT);

    socket.connect(DEBUG_SERVER_PORT, DEBUG_SERVER_HOST, () => {
      const payload = JSON.stringify({
        id: 1,
        cmd,
        data: data ?? {},
      }) + "\n";
      socket.write(payload);
    });

    socket.on("data", (chunk) => {
      if (resolved) return;
      buffer += chunk.toString("utf-8");

      const idx = buffer.indexOf("\n");
      if (idx !== -1) {
        resolved = true;
        const line = buffer.slice(0, idx);
        clearTimeout(timer);
        socket.destroy();

        try {
          resolve(JSON.parse(line) as DreadResponse);
        } catch (e) {
          reject(new Error(`Failed to parse response: ${line}`));
        }
      }
    });

    socket.on("error", (err) => {
      if (resolved) return;
      resolved = true;
      clearTimeout(timer);
      reject(new Error(`Connection failed: ${err.message}. Is the Dread debug server running on ${DEBUG_SERVER_HOST}:${DEBUG_SERVER_PORT}?`));
    });

    socket.on("close", () => {
      if (!resolved) {
        resolved = true;
        clearTimeout(timer);
        reject(new Error("Connection closed without receiving a response"));
      }
    });
  });
}

async function toolCall(
  cmd: string,
  data: unknown,
  handler: (response: DreadResponse) => { content: TextContent[] }
): Promise<{ content: TextContent[]; isError?: boolean }> {
  try {
    const response = await sendCommand(cmd, data);
    if (!response.ok) {
      return { isError: true, content: [{ type: "text", text: `Error: ${response.error}` }] };
    }
    return handler(response);
  } catch (error) {
    return { isError: true, content: [{ type: "text", text: error instanceof Error ? error.message : String(error) }] };
  }
}

const server = new McpServer({
  name: "dread-mcp-server",
  version: "0.1.0",
});

server.registerTool(
  "dread_ping",
  {
    title: "Ping Dread Debug Server",
    description: `Check if the Dread mod debug server is reachable and responsive.

Returns the server version and bound port for session correlation.

This is the first command to run when connecting to confirm the debug server is alive.

Examples:
  - Use when: "Is the game running with the Dread mod loaded?"
  - Don't use when: You need game state (use dread_get_state instead)

Error Handling:
  - Returns "Connection failed" if the game is not running or the debug server is disabled
  - Returns an error response if the server encounters an internal issue`,
    inputSchema: z.strictObject({}),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async () => {
    return toolCall("ping", {}, (response) => {
      const data = response.data as Record<string, unknown> | undefined;
      const version = data?.version ?? "unknown";
      const port = data?.port ?? DEBUG_SERVER_PORT;
      return {
        content: [{ type: "text", text: `Dread debug server is alive (version ${version}, port ${port})` }],
      };
    });
  },
);

server.registerTool(
  "dread_get_state",
  {
    title: "Get Dread Mod State",
    description: `Capture a full snapshot of the mod's runtime state, including:
  - Scene name
  - Enemy count and nearest enemy distance
  - Player HP and stamina
  - Episode active status and timer

Some fields may be null or empty when on the main menu (not in a level).

Args:
  - response_format ('json' | 'text'): Output format (default: 'json')

Returns:
  For JSON format: The raw state snapshot with fields:
  {
    "version": string,       // Mod version
    "scene": string,         // Current scene name
    "enemyCount": number,     // Enemy count in scene
    "nearestEnemyDist": number, // Distance to nearest enemy
    "playerHp": number,      // Current player HP
    "playerStamina": number, // Current player stamina
    "playerHp": number,      // Current player HP
    "playerStamina": number  // Current player stamina
  }

Examples:
  - Use when: "What is the current game state?"
  - Use when: "How many enemies are nearby?"
  - Use when: "What is the player's HP?"

Error Handling:
  - Returns safely on main menu (null-guarded, no crash)
  - Returns error if server not reachable`,
    inputSchema: z.strictObject({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format: 'json' for structured data or 'text' for human-readable summary"),
    }),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format }) => {
    return toolCall("get_state", {}, (response) => {
      if (response_format === "text") {
        const d = response.data as Record<string, unknown> ?? {};
        const lines = [
          "# Dread Mod State",
          "",
          `- **Scene**: ${d.scene ?? "unknown"}`,
          `- **Version**: ${d.version ?? "unknown"}`,
          "",
          "## Enemies",
          `- **Count**: ${d.enemyCount ?? 0}`,
          `- **Nearest**: ${d.nearestEnemyDist != null ? `${d.nearestEnemyDist}m` : "N/A"}`,
          "",
          "## Player",
          `- **HP**: ${d.playerHp ?? "N/A"}`,
          `- **Stamina**: ${d.playerStamina ?? "N/A"}`,
        ].join("\n");
        return { content: [{ type: "text", text: lines }] };
      }

      return {
        content: [{ type: "text", text: JSON.stringify(response.data, null, 2) }],
      };
    });
  },
);

server.registerTool(
  "dread_get_config",
  {
    title: "Get Dread Mod Configuration",
    description: `Retrieve Dread mod configuration.

Returns flat camelCase keys (e.g. audioEnabled, debugServerEnabled) plus a grouped
\`sections\` array. Each section entry includes \`debugKey\` values used by set_config
(section/key pairs like section="audio", key="enabled").

Args:
  - response_format ('json' | 'text'): Output format (default: 'json')
  - section (string, optional): Filter grouped sections by name (e.g. "8. Debug Server")

Examples:
  - Use when: "Show me all current configuration values"
  - Use when: "What debug server settings are configured?"

Error Handling:
  - Returns error if server not reachable`,
    inputSchema: z.strictObject({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format: 'json' for structured data or 'text' for human-readable summary"),
      section: z.string().optional().describe("Filter grouped sections (e.g. '8. Debug Server', '1. Audio Dread')"),
    }),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format, section }) => {
    return toolCall("get_config", {}, (response) => {
      const config = response.data as Record<string, unknown> ?? {};
      const sections = (config.sections as Array<Record<string, unknown>> | undefined) ?? [];
      const filteredSections = section
        ? sections.filter((s) => String(s.section ?? "").toLowerCase() === section.toLowerCase())
        : sections;

      if (response_format === "text") {
        const lines = ["# Dread Mod Configuration", "", "## Flat keys"];
        for (const [key, val] of Object.entries(config)) {
          if (key === "sections") continue;
          lines.push(`- **${key}**: \`${val}\``);
        }
        lines.push("");
        for (const sec of filteredSections) {
          lines.push(`## ${sec.section ?? "unknown"}`);
          const keys = (sec.keys as Array<Record<string, unknown>> | undefined) ?? [];
          for (const entry of keys) {
            lines.push(`- **${entry.key}** (\`${entry.debugKey}\`): \`${entry.value}\``);
            if (entry.restartRequired) lines.push("  - Restart required");
          }
          lines.push("");
        }
        return { content: [{ type: "text", text: lines.join("\n") }] };
      }

      const payload = section
        ? { ...config, sections: filteredSections }
        : config;

      return {
        content: [{ type: "text", text: JSON.stringify(payload, null, 2) }],
      };
    });
  },
);

server.registerTool(
  "dread_set_config",
  {
    title: "Set Dread Mod Configuration",
    description: `Modify a Dread config entry at runtime via debug-key section/key pairs.

Use the \`debugKey\` from get_config sections, split on the first dot:
  - debugKey "audio.enabled"     -> section="audio", key="enabled"
  - debugKey "errorReporting"    -> section="errorReporting", key=""
  - debugKey "debugServer.enabled" -> section="debugServer", key="enabled"

Restart-required keys (debugServer.enabled, debugServer.port) return a warning in the response.

Args:
  - section (string, required): Debug key prefix (e.g. "audio", "psychoticBreak", "debugServer")
  - key (string, required): Debug key suffix (e.g. "enabled", "volume"). Use "" for bare keys like errorReporting.
  - value (string, required): New value as string (parsed to bool/float/int/LogLevel)

Examples:
  - section="audio", key="enabled", value="false"
  - section="overlay", key="enabled", value="true"
  - section="psychoticBreak", key="enabled", value="true"

Error Handling:
  - Returns error code -3 if value cannot be parsed or key is unknown
  - Returns error if server not reachable`,
    inputSchema: z.strictObject({
      section: z.string().describe("Debug key prefix (e.g. 'audio', 'debugServer', 'errorReporting')"),
      key: z.string().default("").describe("Debug key suffix (e.g. 'enabled'). Empty for bare keys."),
      value: z.string().describe("New value as string (parsed to the correct type automatically)"),
      response_format: z.enum(["json", "text"]).default("text").describe("Output format"),
    }),
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  },
  async ({ section, key, value, response_format }) => {
    return toolCall("set_config", { section, key, value }, (response) => {
      const data = response.data as Record<string, unknown> | undefined;
      const warning = data?.warning as string | undefined;
      const msg = warning
        ? `Config updated: ${section}.${key} = ${value}. ${warning}`
        : `Config updated: ${section}.${key} = ${value}`;
      if (response_format === "text") {
        return { content: [{ type: "text", text: msg }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify({ updated: true, section, key, value, warning: warning ?? null }) }],
      };
    });
  },
);

server.registerTool(
  "dread_get_patches",
  {
    title: "Get Dread Mod Harmony Patches",
    description: `List all Harmony patches applied by the Dread mod, grouped by patched method.

Shows which methods are patched, the patch type (Prefix, Postfix, Transpiler, Finalizer), and which mod owner applied each patch.

Args:
  - response_format ('json' | 'text'): Output format (default: 'json')

Returns:
  For JSON format: Array of patches, each with method name, patch types, and owners
  For text format: Human-readable grouped listing

Examples:
  - Use when: "Which Harmony patches are currently active?"
  - Use when: "Is the aggression patch applied?"
  - Use when: "Show me all patches by owner"

Error Handling:
  - Returns error if server not reachable`,
    inputSchema: z.strictObject({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format"),
    }),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format }) => {
    return toolCall("get_patches", {}, (response) => {
      const raw = response.data as Record<string, unknown> | Array<Record<string, unknown>> | undefined;
      const patches = Array.isArray(raw)
        ? raw
        : (raw?.patches as Array<Record<string, unknown>> | undefined) ?? [];

      if (response_format === "text") {
        if (patches.length === 0) {
          return { content: [{ type: "text", text: "No Harmony patches found." }] };
        }
        const lines = [`# Dread Mod Harmony Patches (${patches.length} total)`, ""];
        for (const patch of patches) {
          lines.push(`## ${patch.method ?? "unknown"}`);
          const types = patch.patchTypes as Record<string, number> ?? {};
          const owners = patch.owners as string[] ?? [];
          if (Object.keys(types).length > 0) {
            lines.push(`- **Types**: Prefix(${types.prefixes ?? 0}), Postfix(${types.postfixes ?? 0}), Transpiler(${types.transpilers ?? 0}), Finalizer(${types.finalizers ?? 0})`);
          }
          if (owners.length > 0) {
            lines.push(`- **Owners**: ${owners.join(", ")}`);
          }
          lines.push("");
        }
        return { content: [{ type: "text", text: lines.join("\n") }] };
      }

      return {
        content: [{ type: "text", text: JSON.stringify(patches, null, 2) }],
      };
    });
  },
);

server.registerTool(
  "dread_get_logs",
  {
    title: "Get Dread Mod Logs",
    description: `Retrieve recent BepInEx log entries buffered by the Dread debug server (up to 200 entries).

Args:
  - response_format ('json' | 'text'): Output format (default: 'json')

Returns:
  For JSON format: Array of log entries with level, source, message, and timestamp
  For text format: Human-readable formatted log lines

Examples:
  - Use when: "Did the tension system initialize correctly?"
  - Use when: "What is the most recent log entry?"

Error Handling:
  - Returns empty array if server returns no logs
  - Returns error if server not reachable`,
    inputSchema: z.strictObject({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format"),
    }),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format }) => {
    return toolCall("get_logs", {}, (response) => {
      const raw = response.data as Record<string, unknown> | Array<Record<string, unknown>> | undefined;
      const entries = Array.isArray(raw)
        ? raw
        : (raw?.logs as Array<Record<string, unknown>> | undefined) ?? [];

      if (response_format === "text") {
        if (entries.length === 0) {
          return { content: [{ type: "text", text: "No log entries found." }] };
        }
        const lines = [`# Recent Dread Mod Logs (${entries.length} entries)`, ""];
        for (const entry of entries) {
          const ts = entry.timestamp ?? "";
          const lvl = entry.level ?? "Info";
          const msg = entry.message ?? "";
          lines.push(`[${ts}] [${lvl}] ${msg}`);
        }
        return { content: [{ type: "text", text: lines.join("\n") }] };
      }

      return {
        content: [{ type: "text", text: JSON.stringify(entries, null, 2) }],
      };
    });
  },
);

server.registerTool(
  "dread_shutdown",
  {
    title: "Shutdown Dread Debug Server",
    description: `Gracefully shut down the Dread debug server.

After this command, the debug server stops accepting connections. The game continues running without debug capabilities.

This is useful when cleaning up after a debugging session to release the port.

Examples:
  - Use when: "Stop debugging, shut down the debug server"
  - Use when: "Release the debug port so another instance can use it"

Error Handling:
  - Returns error if server not reachable
  - After success, subsequent commands will fail with "connection refused"`,
    inputSchema: z.strictObject({}),
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  },
  async () => {
    return toolCall("shutdown", {}, () => {
      return { content: [{ type: "text", text: "Debug server shut down gracefully." }] };
    });
  },
);

server.registerTool(
  "dread_verify",
  {
    title: "Verify Dread Mod Health",
    description: `Run automated health checks against the live mod.

Returns { checks: [{ id, ok, message }] } covering version, debug server, systems count,
audio clips, psychotic break clips, debug overlay host, and Harmony patch count.

Use after game launch with DebugServerEnabled=true to confirm the mod loaded correctly.

Examples:
  - Use when: "Did Dread initialize correctly in this session?"
  - Use when: "Autonomous agent verify loop after launching REPO"`,
    inputSchema: z.strictObject({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format"),
    }),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format }) => {
    return toolCall("verify", {}, (response) => {
      const data = response.data as { checks?: Array<{ id: string; ok: boolean; message: string }> } | undefined;
      const checks = data?.checks ?? [];

      if (response_format === "text") {
        const lines = ["# Dread Verify", ""];
        for (const check of checks) {
          lines.push(`- [${check.ok ? "OK" : "FAIL"}] **${check.id}**: ${check.message}`);
        }
        const failed = checks.filter((c) => !c.ok).length;
        lines.push("", failed === 0 ? "All checks passed." : `${failed} check(s) failed.`);
        return { content: [{ type: "text", text: lines.join("\n") }] };
      }

      return { content: [{ type: "text", text: JSON.stringify(data, null, 2) }] };
    });
  },
);

server.registerTool(
  "dread_trigger_test_crash",
  {
    title: "Trigger Test Crash",
    description: `Deliberately crash the game to verify error reporting (same path as config "Crash Game" button).

DESTRUCTIVE: The game process will terminate. Only works when DebugServerEnabled=true.

Examples:
  - Use when: "Verify error telemetry end-to-end"`,
    inputSchema: z.strictObject({}),
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  },
  async () => {
    return toolCall("trigger_test_crash", {}, (response) => {
      return { content: [{ type: "text", text: JSON.stringify(response.data, null, 2) }] };
    });
  },
);

server.registerTool(
  "dread_force_psychotic_break",
  {
    title: "Force Psychotic Break Episode",
    description: `Force-start a Psychotic Break episode for testing (bypasses trigger conditions).

Only works when DebugServerEnabled=true and PsychoticBreakSystem is loaded.

Examples:
  - Use when: "Test psychotic break visuals and audio in a live run"`,
    inputSchema: z.strictObject({}),
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  },
  async () => {
    return toolCall("force_psychotic_break", {}, (response) => {
      return { content: [{ type: "text", text: JSON.stringify(response.data, null, 2) }] };
    });
  },
);

server.registerTool(
  "dread_get_runtime_state",
  {
    title: "Get Dread Runtime State",
    description: `Return live mod runtime snapshot from DreadRuntimeState (tension, psychotic break, audio, overlay).

Lighter than get_state: no scene/player HP queries. Ideal for feature-level autonomous testing.

Args:
  - response_format ('json' | 'text'): Output format (default: 'json')`,
    inputSchema: z.strictObject({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format"),
    }),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format }) => {
    return toolCall("get_runtime_state", {}, (response) => {
      if (response_format === "text") {
        const d = response.data as Record<string, unknown> ?? {};
        const lines = ["# Dread Runtime State", ""];
        for (const [key, val] of Object.entries(d)) {
          lines.push(`- **${key}**: ${val}`);
        }
        return { content: [{ type: "text", text: lines.join("\n") }] };
      }
      return { content: [{ type: "text", text: JSON.stringify(response.data, null, 2) }] };
    });
  },
);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error(`dread-mcp-server running via stdio (target: ${DEBUG_SERVER_HOST}:${DEBUG_SERVER_PORT})`);
  console.error(`Use DREAD_HOST and DREAD_PORT env vars to change the target, DREAD_TIMEOUT for command timeout (default: ${COMMAND_TIMEOUT}ms)`);
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});
