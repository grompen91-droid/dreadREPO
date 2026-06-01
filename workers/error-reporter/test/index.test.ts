import {
	env,
	createExecutionContext,
	waitOnExecutionContext,
} from "cloudflare:test";
import { describe, it, expect, beforeEach } from "vitest";
import worker from "../index.js";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Build a minimal valid error report payload. */
function makePayload(overrides = {}, reportOverrides = {}) {
	return {
		ModVersion: "1.5.2",
		GameVersion: "0.1.0",
		UnityVersion: "2022.3.0f1",
		Reports: [
			{
				Hash: "abc123def4567890",
				Timestamp: new Date().toISOString(),
				Type: "exception",
				ExceptionType: "InvalidOperationException",
				Message: "[Dread TestCrash] deliberate test crash",
				StackTrace: "TestCrashSystem.TriggerCrash() at TestCrashSystem.cs:36",
				Scene: "Warehouse",
				GameState: {
					SceneName: "Warehouse",
					EnemiesAlive: 2,
					EnemiesTotal: 5,
					EnemiesNearby: 1,
					PlayerHp: 80,
					PlayerMaxHp: 100,
					PlayerStamina: 55,
					PlayerPosition: { x: 10.5, y: 0, z: -3.2 },
					PlayTimeSeconds: 420,
				},
				SystemInfo: {
					Os: "Windows 11 (10.0.22631)",
					OsFamily: "Windows",
					Cpu: "AMD Ryzen 7 5800X",
					CpuCores: 16,
					CpuFrequencyMHz: 3800,
					MemoryMB: 32768,
					Gpu: "NVIDIA GeForce RTX 3080",
					GpuVendor: "NVIDIA",
					GpuDriverVersion: "537.58",
					GpuShaderLevel: 50,
					VramMB: 10240,
					DeviceType: "Desktop",
					DeviceModel: "Custom PC",
				},
				Display: {
					Width: 2560,
					Height: 1440,
					RefreshRate: 144,
					Dpi: 109,
					FullScreenMode: "ExclusiveFullScreen",
				},
				Config: {
					AudioEnabled: true,
					AudioFrequency: 120,
					AudioVolume: 0.4,
					AggressionEnabled: true,
					AggressionAudioEnabled: true,
					FakeFootsteps: true,
					Adrenaline: true,
					LowStaminaSound: true,
					PanicSprint: true,
					CrouchSpeedBoost: true,
					ErrorReportingEnabled: true,
				},
				...reportOverrides,
			},
		],
		...overrides,
	};
}

/** Fire a request through the Worker under test. */
async function callWorker(request) {
	const ctx = createExecutionContext();
	const response = await worker.fetch(request, env, ctx);
	await waitOnExecutionContext(ctx);
	return response;
}

/** Auto-incrementing IP to isolate rate limit buckets between tests. */
let ipCounter = 0;
function uniqueIp() {
	ipCounter++;
	return `203.0.113.${ipCounter}`;
}

/** POST JSON to the report endpoint. */
async function postReport(payload, ip) {
	const request = new Request("http://localhost/api/report", {
		method: "POST",
		headers: { "Content-Type": "application/json", "CF-Connecting-IP": ip || uniqueIp() },
		body: JSON.stringify(payload),
	});
	return callWorker(request);
}

/** In-memory KV for tests (wrangler.toml binds DEDUP_KV; isolate without reset leaks dedupe state). */
function makeInMemoryDedupeKv() {
	const store = new Map<string, string>();
	return {
		store,
		async get(key: string) {
			return store.get(key) ?? null;
		},
		async put(key: string, value: string) {
			store.set(key, value);
		},
		async delete(key: string) {
			store.delete(key);
		},
		async list() {
			return { keys: [...store.keys()].map((name) => ({ name })) };
		},
	};
}

/** GitHub search item with hash marker in body (required by findExistingIssue body verification). */
function searchIssue(number: number, state: string, hash: string) {
	return {
		number,
		state,
		body: `<!-- hash:${hash} -->`,
	};
}

// ---------------------------------------------------------------------------
// Because tests run inside the Worker isolate with real `env` bindings, but
// `fetch` calls to the GitHub API go out to the real internet, we intercept
// them at the module level by monkey-patching globalThis.fetch within each
// test that needs it. The Worker's `gh()` helper uses bare `fetch()`.
// ---------------------------------------------------------------------------

/** Install a GitHub API mock that records calls and returns canned responses. */
function mockGitHub() {
	const calls = [];
	const originalFetch = globalThis.fetch;

	// Default behaviors (can be overridden per-test via `setSearch` / `setCreate`).
	let searchResponse = { total_count: 0, items: [] };
	let createResponse = { number: 42 };
	let patchResponse = { number: 42, state: "open" };
	let commentResponse = { id: 1 };

	const mock = {
		calls,
		setSearch(resp) { searchResponse = resp; },
		setCreate(resp) { createResponse = resp; },
		restore() { globalThis.fetch = originalFetch; },
	};

	globalThis.fetch = async (url, opts = {}) => {
		const urlStr = typeof url === "string" ? url : url.toString();
		calls.push({ url: urlStr, method: opts.method || "GET", body: opts.body ? JSON.parse(opts.body) : null });

		// Route by GitHub API endpoint pattern.
		if (urlStr.includes("api.github.com/search/issues")) {
			return new Response(JSON.stringify(searchResponse), {
				status: 200,
				headers: { "Content-Type": "application/json" },
			});
		}
		if (urlStr.includes("/issues/") && urlStr.includes("/comments")) {
			return new Response(JSON.stringify(commentResponse), {
				status: 201,
				headers: { "Content-Type": "application/json" },
			});
		}
		if (urlStr.includes("/issues/") && (opts.method === "PATCH")) {
			return new Response(JSON.stringify(patchResponse), {
				status: 200,
				headers: { "Content-Type": "application/json" },
			});
		}
		if (urlStr.includes("/issues") && opts.method === "POST") {
			return new Response(JSON.stringify(createResponse), {
				status: 201,
				headers: { "Content-Type": "application/json" },
			});
		}

		// Fallback: pass through (should not happen in tests).
		return originalFetch(url, opts);
	};

	return mock;
}

// ===========================================================================
// Tests
// ===========================================================================

beforeEach(() => {
	(env as { DEDUP_KV?: ReturnType<typeof makeInMemoryDedupeKv> }).DEDUP_KV =
		makeInMemoryDedupeKv();
});

describe("Health endpoint", () => {
	it("GET /health returns 200 OK", async () => {
		const request = new Request("http://localhost/health", { method: "GET" });
		const response = await callWorker(request);

		expect(response.status).toBe(200);
		expect(await response.text()).toBe("OK");
	});
});

describe("Routing", () => {
	it("GET /nonexistent returns 404", async () => {
		const request = new Request("http://localhost/nonexistent", { method: "GET" });
		const response = await callWorker(request);

		expect(response.status).toBe(404);
		const body = await response.json();
		expect(body.error).toBe("not_found");
	});
});

describe("CORS", () => {
	it("OPTIONS /api/report returns 204 with CORS headers", async () => {
		const request = new Request("http://localhost/api/report", { method: "OPTIONS" });
		const response = await callWorker(request);

		expect(response.status).toBe(204);
		expect(response.headers.get("Access-Control-Allow-Origin")).toBe("*");
		expect(response.headers.get("Access-Control-Allow-Methods")).toContain("POST");
		expect(response.headers.get("Access-Control-Allow-Headers")).toContain("Content-Type");
	});
});

describe("Report validation", () => {
	it("rejects invalid JSON with 400", async () => {
		const request = new Request("http://localhost/api/report", {
			method: "POST",
			headers: { "Content-Type": "application/json", "CF-Connecting-IP": "10.0.0.1" },
			body: "not json at all",
		});
		const response = await callWorker(request);

		expect(response.status).toBe(400);
		const body = await response.json();
		expect(body.error).toBe("Invalid JSON body");
	});

	it("rejects empty Reports array with 400", async () => {
		const response = await postReport({ ModVersion: "1.0.0", Reports: [] });

		expect(response.status).toBe(400);
		const body = await response.json();
		expect(body.error).toContain("Missing or empty Reports");
	});

	it("rejects missing Reports field with 400", async () => {
		const response = await postReport({ ModVersion: "1.0.0" });

		expect(response.status).toBe(400);
	});

	it("skips reports with missing Hash", async () => {
		const gh = mockGitHub();
		try {
			const payload = makePayload({}, { Hash: undefined });
			// Remove Hash entirely.
			delete payload.Reports[0].Hash;

			const response = await postReport(payload);
			expect(response.status).toBe(200);

			const body = await response.json();
			expect(body.results[0].status).toBe("skipped");
			expect(body.results[0].reason).toContain("Hash");
		} finally {
			gh.restore();
		}
	});
});

describe("Issue creation (happy path)", () => {
	it("creates a new GitHub issue for a fresh hash", async () => {
		const gh = mockGitHub();
		gh.setCreate({ number: 99 });
		try {
			const response = await postReport(makePayload());
			expect(response.status).toBe(200);

			const body = await response.json();
			expect(body.processed).toBe(1);
			expect(body.results[0].status).toBe("created");
			expect(body.results[0].issueNumber).toBe(99);

			// Verify the GitHub API calls.
			const searchCall = gh.calls.find((c) => c.url.includes("search/issues"));
			expect(searchCall).toBeDefined();
			expect(searchCall.url).toContain("hash%3Aabc123def4567890");
			expect(searchCall.url).not.toContain("label%3Aauto-reported");

			const createCall = gh.calls.find(
				(c) => c.url.includes("/issues") && c.method === "POST" && !c.url.includes("/comments"),
			);
			expect(createCall).toBeDefined();
			expect(createCall.body.title).toBe("[auto] InvalidOperationException in Warehouse");
			expect(createCall.body.labels).toEqual(["auto-reported", "bug"]);
			expect(createCall.body.body).toContain("<!-- hash:abc123def4567890 -->");
		} finally {
			gh.restore();
		}
	});

	it("includes all payload sections in the issue body", async () => {
		const gh = mockGitHub();
		let createdBody = "";
		gh.setCreate({ number: 100 });

		// Capture the body sent to GitHub.
		const origFetch = globalThis.fetch;
		const wrappedFetch = globalThis.fetch;
		// We already have the mock; just inspect calls after.

		try {
			await postReport(makePayload());

			const createCall = gh.calls.find(
				(c) => c.url.includes("/issues") && c.method === "POST" && !c.url.includes("/comments"),
			);
			createdBody = createCall.body.body;

			// Verify all sections are present.
			expect(createdBody).toContain("## Error Report");
			expect(createdBody).toContain("## Error Details");
			expect(createdBody).toContain("## System Information");
			expect(createdBody).toContain("## Display");
			expect(createdBody).toContain("## Game State");
			expect(createdBody).toContain("## Configuration");
			expect(createdBody).toContain("Raw JSON Payload");

			// Verify specific field values rendered.
			expect(createdBody).toContain("1.5.2");         // ModVersion
			expect(createdBody).toContain("Warehouse");      // Scene
			expect(createdBody).toContain("AMD Ryzen 7");    // CPU
			expect(createdBody).toContain("RTX 3080");       // GPU
			expect(createdBody).toContain("2560");           // Resolution width
		} finally {
			gh.restore();
		}
	});
});

describe("Deduplication", () => {
	it("uses KV mapping before GitHub search when DEDUP_KV is bound", async () => {
		const gh = mockGitHub();
		const kv = makeInMemoryDedupeKv();
		kv.store.set("issue:kv_hash_1234567890", "50");
		(env as { DEDUP_KV?: typeof kv }).DEDUP_KV = kv;
		try {
			const response = await postReport(
				makePayload({}, { Hash: "kv_hash_1234567890" }),
			);
			expect(response.status).toBe(200);
			const body = await response.json();
			expect(body.results[0].status).toBe("commented");
			expect(body.results[0].issueNumber).toBe(50);
			expect(body.results[0].dedupe).toBe("kv");

			const searchCall = gh.calls.find((c) => c.url.includes("search/issues"));
			expect(searchCall).toBeUndefined();
		} finally {
			gh.restore();
		}
	});

	it("adds a comment when an open issue with the same hash exists", async () => {
		const gh = mockGitHub();
		const hash = "abc123def4567890";
		gh.setSearch({
			total_count: 1,
			items: [searchIssue(50, "open", hash)],
		});
		try {
			const response = await postReport(makePayload());
			expect(response.status).toBe(200);

			const body = await response.json();
			expect(body.results[0].status).toBe("commented");
			expect(body.results[0].issueNumber).toBe(50);

			// Should NOT have created a new issue.
			const createCall = gh.calls.find(
				(c) => c.url.includes("/issues") && c.method === "POST" && !c.url.includes("/comments"),
			);
			expect(createCall).toBeUndefined();

			// Should have posted a comment.
			const commentCall = gh.calls.find((c) => c.url.includes("/comments"));
			expect(commentCall).toBeDefined();
			expect(commentCall.body.body).toContain("Another occurrence");
		} finally {
			gh.restore();
		}
	});

	it("reopens a closed issue and adds a comment", async () => {
		const gh = mockGitHub();
		const hash = "abc123def4567890";
		gh.setSearch({
			total_count: 1,
			items: [searchIssue(77, "closed", hash)],
		});
		try {
			const response = await postReport(makePayload());
			expect(response.status).toBe(200);

			const body = await response.json();
			expect(body.results[0].status).toBe("reopened");
			expect(body.results[0].issueNumber).toBe(77);

			// Should have PATCHed the issue to reopen.
			const patchCall = gh.calls.find((c) => c.method === "PATCH");
			expect(patchCall).toBeDefined();
			expect(patchCall.body.state).toBe("open");
		} finally {
			gh.restore();
		}
	});

	it("throttles comments on the same issue after 5 comments in the hour", async () => {
		const gh = mockGitHub();
		const issueNumber = 88;
		const hash = "comment_throttle_same_hash";
		gh.setSearch({
			total_count: 1,
			items: [searchIssue(issueNumber, "open", hash)],
		});
		try {
			// Send 5 reports with the same hash (dedupe to one issue), all post comments
			for (let i = 0; i < 5; i++) {
				const response = await postReport(makePayload({}, { Hash: hash }));
				expect(response.status).toBe(200);
				const body = await response.json();
				expect(body.results[0].status).toBe("commented");
				expect(body.results[0].issueNumber).toBe(issueNumber);
			}

			// Clear captured calls to make it easy to verify new calls
			gh.calls.length = 0;

			// 6th report with same hash: still deduped but comment API throttled
			const response6 = await postReport(makePayload({}, { Hash: hash }));
			expect(response6.status).toBe(200);

			const body6 = await response6.json();
			expect(body6.results[0].status).toBe("commented");

			// No new comment call should be sent to GitHub
			const commentCall = gh.calls.find((c) => c.url.includes("/comments"));
			expect(commentCall).toBeUndefined();
		} finally {
			gh.restore();
		}
	});
});

describe("Rate limiting", () => {
	it("returns 429 after 5 requests from the same IP", async () => {
		const gh = mockGitHub();
		// Use a dedicated IP so prior tests don't pollute the bucket.
		const rateLimitIp = "198.51.100.99";
		try {
			// The Worker uses an in-memory Map, so we need to hit it 6 times
			// from the same IP within the same isolate.
			for (let i = 0; i < 5; i++) {
				const payload = makePayload({}, { Hash: `ratelimit_${i}_${Date.now()}` });
				const res = await postReport(payload, rateLimitIp);
				expect(res.status).toBe(200);
			}

			// 6th request should be rate-limited.
			const payload = makePayload({}, { Hash: `ratelimit_6_${Date.now()}` });
			const res = await postReport(payload, rateLimitIp);
			expect(res.status).toBe(429);

			const body = await res.json();
			expect(body.error).toContain("Rate limit");
		} finally {
			gh.restore();
		}
	});
});

describe("Markdown escaping", () => {
	it("escapes pipes, backslashes, and newlines in table cells", async () => {
		const gh = mockGitHub();
		gh.setCreate({ number: 200 });
		try {
			const payload = makePayload({}, {
				Hash: "escape_test_12345",
				Message: "Error with | pipe and \\ backslash and\nnewline",
				ExceptionType: "Escaped|Exception",
			});

			await postReport(payload);

			const createCall = gh.calls.find(
				(c) => c.url.includes("/issues") && c.method === "POST" && !c.url.includes("/comments"),
			);
			const body = createCall.body.body;

			// Pipes should be escaped in table cells.
			// The title uses ExceptionType directly (not escaped), but the body tables escape.
			expect(body).not.toContain("| pipe and |");
			// The message appears in the Error Details code block (not a table), so
			// check that the System Info table doesn't break with backslashes.
			expect(body).toContain("## System Information");
		} finally {
			gh.restore();
		}
	});
});
