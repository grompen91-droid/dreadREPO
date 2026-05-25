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
    inputSchema: z.object({}).strict(),
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
    inputSchema: z.object({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format: 'json' for structured data or 'text' for human-readable summary"),
    }).strict(),
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
    description: `Retrieve all BepInEx configuration entries for the Dread mod, organized by section.

Returns every config key with its current value, type, acceptable range (if applicable), and description.

Args:
  - response_format ('json' | 'text'): Output format (default: 'json')
  - section (string, optional): Filter to a specific config section (e.g., "8. Debug Server")

Returns:
  For JSON format: Object with sections as keys, each containing key-value entries with type/range/description

Examples:
  - Use when: "Show me all current configuration values"
  - Use when: "What debug server settings are configured?"
  - Use when: "What are the audio-related config options currently set to?"

Error Handling:
  - Returns error if server not reachable`,
    inputSchema: z.object({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format: 'json' for structured data or 'text' for human-readable summary"),
      section: z.string().optional().describe("Filter to a specific section (e.g., '8. Debug Server', '1. AudioDread')"),
    }).strict(),
    annotations: {
      readOnlyHint: true,
      destructiveHint: false,
      idempotentHint: true,
      openWorldHint: false,
    },
  },
  async ({ response_format, section }) => {
    return toolCall("get_config", {}, (response) => {
      const config = response.data as Record<string, Record<string, unknown>> ?? {};
      const sections = section
        ? Object.fromEntries(Object.entries(config).filter(([k]) => k.toLowerCase() === section.toLowerCase()))
        : config;

      if (Object.keys(sections).length === 0) {
        return { content: [{ type: "text", text: section ? `No config section found: '${section}'` : "No config entries found" }] };
      }

      if (response_format === "text") {
        const lines = ["# Dread Mod Configuration", ""];
        for (const [secName, entries] of Object.entries(sections)) {
          const entryMap = entries as Record<string, unknown>;
          lines.push(`## ${secName}`);
          for (const [key, val] of Object.entries(entryMap)) {
            const v = val as Record<string, unknown>;
            lines.push(`- **${key}**: \`${v.value ?? "N/A"}\``);
            if (v.type) lines.push(`  - Type: ${v.type}`);
            if (v.description) lines.push(`  - ${v.description}`);
          }
          lines.push("");
        }
        return { content: [{ type: "text", text: lines.join("\n") }] };
      }

      return {
        content: [{ type: "text", text: JSON.stringify(sections, null, 2) }],
      };
    });
  },
);

server.registerTool(
  "dread_set_config",
  {
    title: "Set Dread Mod Configuration",
    description: `Modify a BepInEx configuration entry at runtime. Changes take effect immediately (BepInEx fires SettingChanged events).

Args:
  - section (string, required): Config section name (e.g., "8. Debug Server")
  - key (string, required): Config entry key (e.g., "DebugServerEnabled")
  - value (string, required): New value as string (will be parsed to the correct type)
  - response_format ('json' | 'text'): Output format (default: 'text')

The value is parsed according to the entry's type (bool, float, int, string).

Examples:
  - Use when: "Enable verbose logging by setting LogVerbosity to 2"
  - Use when: "Disable the tension system"
  - Use when: "Change the debug server port to 15433"

Error Handling:
  - Returns error code -3 if the value cannot be parsed to the expected type
  - Returns error if the section/key is not found
  - Returns error if server not reachable`,
    inputSchema: z.object({
      section: z.string().min(1).describe("Config section name (e.g., '8. Debug Server')"),
      key: z.string().min(1).describe("Config entry key (e.g., 'DebugServerEnabled')"),
      value: z.string().describe("New value as string (parsed to the correct type automatically)"),
      response_format: z.enum(["json", "text"]).default("text").describe("Output format"),
    }).strict(),
    annotations: {
      readOnlyHint: false,
      destructiveHint: true,
      idempotentHint: false,
      openWorldHint: false,
    },
  },
  async ({ section, key, value, response_format }) => {
    return toolCall("set_config", { section, key, value }, (response) => {
      const msg = `Config updated: ${section}.${key} = ${value}`;
      if (response_format === "text") {
        return { content: [{ type: "text", text: msg }] };
      }
      return {
        content: [{ type: "text", text: JSON.stringify({ updated: true, section, key, value }) }],
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
    inputSchema: z.object({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format"),
    }).strict(),
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
    inputSchema: z.object({
      response_format: z.enum(["json", "text"]).default("json").describe("Output format"),
    }).strict(),
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
    inputSchema: z.object({}).strict(),
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
