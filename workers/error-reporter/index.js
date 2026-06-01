const HOURLY_WINDOW = 3600000;
const MAX_REQUESTS_PER_IP = 5;
const MAX_COMMENTS_PER_ISSUE = 5;

// Per-isolate rate limiting. Provides burst protection but not global
// enforcement across Cloudflare edge nodes. Acceptable for low-volume
// mod error reporting. Upgrade to KV/Durable Objects if scale requires it.
const ipBuckets = new Map();
const commentBuckets = new Map();

function corsHeaders() {
  return {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Access-Control-Max-Age': '86400',
  };
}

function json(data, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders(), 'Content-Type': 'application/json' },
  });
}

function text(body, status = 200) {
  return new Response(body, { status, headers: corsHeaders() });
}

function checkLimit(buckets, key, max, windowMs = HOURLY_WINDOW) {
  const now = Date.now();
  const timestamps = (buckets.get(key) || []).filter(t => now - t < windowMs);
  if (timestamps.length >= max) return true;
  timestamps.push(now);
  buckets.set(key, timestamps);
  return false;
}

function ghHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
    Accept: 'application/vnd.github.v3+json',
    'Content-Type': 'application/json',
    'User-Agent': 'dread-error-reporter',
  };
}

async function gh(url, token, method = 'GET', body = null) {
  const opts = { method, headers: ghHeaders(token) };
  if (body) opts.body = JSON.stringify(body);
  const res = await fetch(url, opts);
  const data = await res.json();
  if (!res.ok) throw new Error(`GitHub API ${res.status}: ${data.message}`);
  return data;
}

function buildIssueBody(report, payload) {
  const lines = [];

  lines.push('## Error Report');
  lines.push('');
  lines.push('| Field | Value |');
  lines.push('|-------|-------|');
  lines.push(`| Mod Version | ${escapeTableCell(payload.ModVersion || 'N/A')} |`);
  lines.push(`| Game Version | ${escapeTableCell(payload.GameVersion || 'N/A')} |`);
  lines.push(`| Unity Version | ${escapeTableCell(payload.UnityVersion || 'N/A')} |`);
  lines.push(`| Scene | ${escapeTableCell(report.Scene || 'N/A')} |`);
  lines.push(`| Timestamp | ${escapeTableCell(report.Timestamp || 'N/A')} |`);
  lines.push(`| Type | ${escapeTableCell(report.Type || 'exception')} |`);
  lines.push('');

  lines.push('## Error Details');
  lines.push('');
  lines.push('```');
  lines.push(
    report.ExceptionType && report.Message
      ? `${report.ExceptionType}: ${report.Message}`
      : report.Message || report.ExceptionType || 'Unknown error'
  );
  lines.push('```');
  if (report.StackTrace) {
    lines.push('');
    lines.push('```');
    lines.push(report.StackTrace);
    lines.push('```');
  }
  lines.push('');

  if (report.ConsoleLog) {
    lines.push('## Console Log');
    lines.push('');
    lines.push('Recent Unity console output and BepInEx log tail from the reporting session.');
    lines.push('');
    lines.push('```');
    lines.push(truncateForGitHub(report.ConsoleLog, 52000));
    lines.push('```');
    lines.push('');
  }

  const systemInfo = report.SystemInfo;
  if (systemInfo && Object.keys(systemInfo).length > 0) {
    lines.push('## System Information');
    lines.push('');
    lines.push('| Component | Details |');
    lines.push('|-----------|---------|');
    if (systemInfo.Os) lines.push(`| OS | ${escapeTableCell(systemInfo.Os)} |`);
    if (systemInfo.Cpu) lines.push(`| CPU | ${escapeTableCell(systemInfo.Cpu)} |`);
    if (systemInfo.MemoryMB) lines.push(`| RAM | ${escapeTableCell(String(systemInfo.MemoryMB))} MB |`);
    if (systemInfo.Gpu) lines.push(`| GPU | ${escapeTableCell(systemInfo.Gpu)} |`);
    if (systemInfo.VramMB) lines.push(`| VRAM | ${escapeTableCell(String(systemInfo.VramMB))} MB |`);
    lines.push('');
  }

  const display = report.Display;
  if (display && Object.keys(display).length > 0) {
    lines.push('## Display');
    lines.push('');
    lines.push('| Setting | Value |');
    lines.push('|---------|-------|');
    if (display.Width) lines.push(`| Resolution | ${escapeTableCell(String(display.Width))}x${escapeTableCell(String(display.Height || ''))} |`);
    if (display.RefreshRate) lines.push(`| Refresh Rate | ${escapeTableCell(String(display.RefreshRate))} Hz |`);
    lines.push('');
  }

  const gameState = report.GameState;
  if (gameState && Object.keys(gameState).length > 0) {
    lines.push('## Game State');
    lines.push('');
    lines.push('| Property | Value |');
    lines.push('|----------|-------|');
    for (const [key, value] of Object.entries(gameState)) {
      lines.push(`| ${escapeTableCell(key)} | ${escapeTableCell(String(value))} |`);
    }
    lines.push('');
  }

  const config = report.Config;
  if (config && Object.keys(config).length > 0) {
    lines.push('## Configuration');
    lines.push('');
    lines.push('| Setting | Value |');
    lines.push('|---------|-------|');
    for (const [key, value] of Object.entries(config)) {
      lines.push(`| ${escapeTableCell(key)} | ${escapeTableCell(String(value))} |`);
    }
  } else {
    lines.push('## Configuration');
    lines.push('');
    lines.push('All default values');
  }
  lines.push('');

  lines.push('<details>');
  lines.push('<summary>Raw JSON Payload</summary>');
  lines.push('');
  lines.push('```json');
  lines.push(JSON.stringify(payload, null, 2));
  lines.push('```');
  lines.push('');
  lines.push('</details>');
  lines.push('');

  lines.push(`<!-- hash:${report.Hash} -->`);

  return lines.join('\n');
}

function escapeTableCell(str) {
  return String(str).replace(/\\/g, '\\\\').replace(/\|/g, '\\|').replace(/\n/g, ' ');
}

function truncateForGitHub(text, maxChars) {
  const s = String(text);
  if (s.length <= maxChars) return s;
  const notice = `...[truncated for GitHub issue size; showing last ${maxChars} characters]\n`;
  const keep = Math.max(0, maxChars - notice.length);
  return notice + s.slice(s.length - keep);
}

/** GitHub issue search for dedupe. Do not require labels: auto-reported is applied after create and may be missing on older issues. */
function buildDedupeSearchQuery(owner, repo, hash) {
  return `repo:${owner}/${repo} hash:${hash} type:issue`;
}

function dedupeKvKey(hash) {
  return `issue:${hash}`;
}

async function readDedupeKv(env, hash) {
  if (!env.DEDUP_KV) return null;
  try {
    const raw = await env.DEDUP_KV.get(dedupeKvKey(hash));
    if (!raw) return null;
    const n = Number.parseInt(raw, 10);
    return Number.isFinite(n) && n > 0 ? n : null;
  } catch {
    return null;
  }
}

async function writeDedupeKv(env, hash, issueNumber) {
  if (!env.DEDUP_KV) return;
  try {
    await env.DEDUP_KV.put(dedupeKvKey(hash), String(issueNumber), {
      expirationTtl: 60 * 60 * 24 * 90,
    });
  } catch {
    // KV optional; GitHub search remains fallback
  }
}

async function ensureIssueLabels(env, issueNumber) {
  try {
    await gh(
      `https://api.github.com/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPO}/issues/${issueNumber}/labels`,
      env.TOKEN,
      'POST',
      { labels: ['auto-reported', 'bug'] }
    );
  } catch {
    // Labels may already exist or token may lack permission; issue body still has hash marker
  }
}

async function findExistingIssue(env, hash) {
  const kvIssue = await readDedupeKv(env, hash);
  if (kvIssue != null) {
    return { number: kvIssue, state: 'open', source: 'kv' };
  }

  const q = encodeURIComponent(
    buildDedupeSearchQuery(env.GITHUB_OWNER, env.GITHUB_REPO, hash)
  );
  const searchResult = await gh(
    `https://api.github.com/search/issues?q=${q}&per_page=5&sort=created&order=asc`,
    env.TOKEN
  );

  if (!searchResult.total_count || !searchResult.items?.length) {
    return null;
  }

  const match = searchResult.items.find((item) => {
    const body = item.body || '';
    return body.includes(`hash:${hash}`) || body.includes(`<!-- hash:${hash} -->`);
  });

  if (!match) {
    return null;
  }

  return { number: match.number, state: match.state, source: 'search' };
}

async function handleReport(request, env) {
  const ip = request.headers.get('CF-Connecting-IP') || 'unknown';
  if (checkLimit(ipBuckets, ip, MAX_REQUESTS_PER_IP)) {
    return json({ error: 'Rate limit exceeded. Max 5 requests per hour per IP.' }, 429);
  }

  let payload;
  try {
    payload = await request.json();
  } catch {
    return json({ error: 'Invalid JSON body' }, 400);
  }

  if (!payload.Reports || !Array.isArray(payload.Reports) || payload.Reports.length === 0) {
    return json({ error: 'Missing or empty Reports array' }, 400);
  }

  const results = [];

  for (const report of payload.Reports) {
    if (!report.Hash) {
      results.push({ hash: null, status: 'skipped', reason: 'Report missing Hash' });
      continue;
    }

    try {
      const existing = await findExistingIssue(env, report.Hash);

      if (existing) {
        const issueNumber = existing.number;
        let wasClosed = existing.state === 'closed';

        if (wasClosed) {
          await gh(
            `https://api.github.com/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPO}/issues/${issueNumber}`,
            env.TOKEN,
            'PATCH',
            { state: 'open' }
          );
        }

        await ensureIssueLabels(env, issueNumber);
        await writeDedupeKv(env, report.Hash, issueNumber);

        const commentKey = `comment:${issueNumber}`;
        if (!checkLimit(commentBuckets, commentKey, MAX_COMMENTS_PER_ISSUE)) {
          await gh(
            `https://api.github.com/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPO}/issues/${issueNumber}/comments`,
            env.TOKEN,
            'POST',
            {
              body: [
                'Another occurrence of this error has been reported.',
                '',
                `| Field | Value |`,
                `|-------|-------|`,
                `| Exception | ${report.ExceptionType || 'Unknown'} |`,
                `| Message | ${(report.Message || 'N/A').slice(0, 200)} |`,
                `| Scene | ${report.Scene || 'N/A'} |`,
                `| Timestamp | ${report.Timestamp || 'N/A'} |`,
                '',
                `<!-- hash:${report.Hash} -->`,
              ].join('\n'),
            }
          );
        }

        results.push({
          hash: report.Hash,
          issueNumber,
          status: wasClosed ? 'reopened' : 'commented',
          dedupe: existing.source,
        });
      } else {
        const title = `[auto] ${report.ExceptionType || 'Unknown'} in ${report.Scene || 'Unknown'}`;
        const body = buildIssueBody(report, payload);
        const created = await gh(
          `https://api.github.com/repos/${env.GITHUB_OWNER}/${env.GITHUB_REPO}/issues`,
          env.TOKEN,
          'POST',
          { title, body, labels: ['auto-reported', 'bug'] }
        );
        await ensureIssueLabels(env, created.number);
        await writeDedupeKv(env, report.Hash, created.number);
        results.push({ hash: report.Hash, issueNumber: created.number, status: 'created' });
      }
    } catch (err) {
      results.push({ hash: report.Hash, status: 'error', error: err.message });
    }
  }

  return json({ processed: results.length, results });
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const method = request.method;

    if (method === 'OPTIONS') {
      return new Response(null, { status: 204, headers: corsHeaders() });
    }

    if (url.pathname === '/health' && method === 'GET') {
      return text('OK', 200);
    }

    if (url.pathname === '/api/report' && method === 'POST') {
      return handleReport(request, env);
    }

    return new Response(JSON.stringify({ error: 'not_found' }), {
      status: 404,
      headers: { 'Content-Type': 'application/json', ...corsHeaders() }
    });
  },
};
