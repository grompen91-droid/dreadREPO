#!/usr/bin/env bash
# test-error-reporter.sh
#
# Smoke test for the live deployed Dread error reporter Cloudflare Worker.
# Tests the /health endpoint and submits a synthetic error report.
#
# Usage:
#   bash scripts/test-error-reporter.sh [--verify-issue]
#
# Options:
#   --verify-issue   After report submission, use the gh CLI to verify
#                    the corresponding GitHub issue exists.
#
# Requirements: curl, jq
# Optional:     gh (GitHub CLI, only for --verify-issue)

set -euo pipefail

# ── Configuration ──────────────────────────────────────────────────────────────

WORKER_URL="https://dread-error-reporter.nox-heights.workers.dev"
REPO="grompen91-droid/dreadREPO"
TEST_HASH="test_smoke_000001"

# ── Colors ─────────────────────────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
RESET='\033[0m'

# ── State ──────────────────────────────────────────────────────────────────────

PASS=0
FAIL=0
VERIFY_ISSUE=false

# ── Helpers ────────────────────────────────────────────────────────────────────

log_pass() {
  PASS=$((PASS + 1))
  echo -e "  ${GREEN}✓ PASS${RESET}: $1"
}

log_fail() {
  FAIL=$((FAIL + 1))
  echo -e "  ${RED}✗ FAIL${RESET}: $1"
}

log_info() {
  echo -e "  ${CYAN}ℹ${RESET} $1"
}

# ── Argument parsing ──────────────────────────────────────────────────────────

for arg in "$@"; do
  case "$arg" in
    --verify-issue) VERIFY_ISSUE=true ;;
    *) echo "Unknown argument: $arg"; exit 1 ;;
  esac
done

# ── Dependency checks ─────────────────────────────────────────────────────────

if ! command -v curl &>/dev/null; then
  echo -e "${RED}Error: curl is required but not found.${RESET}"
  exit 1
fi

if ! command -v jq &>/dev/null; then
  echo -e "${RED}Error: jq is required but not found.${RESET}"
  echo "Install with: sudo pacman -S jq (Arch) or sudo apt install jq (Debian)"
  exit 1
fi

if [ "$VERIFY_ISSUE" = true ] && ! command -v gh &>/dev/null; then
  echo -e "${YELLOW}Warning: gh CLI not found, skipping issue verification.${RESET}"
  VERIFY_ISSUE=false
fi

# ── Header ─────────────────────────────────────────────────────────────────────

echo ""
echo -e "${BOLD}═══════════════════════════════════════════════════${RESET}"
echo -e "${BOLD}  Dread Error Reporter: Live Smoke Test${RESET}"
echo -e "${BOLD}═══════════════════════════════════════════════════${RESET}"
echo -e "  Worker:  ${CYAN}${WORKER_URL}${RESET}"
echo -e "  Time:    $(date -u '+%Y-%m-%dT%H:%M:%SZ')"
echo ""

# ── Test 1: GET /health ────────────────────────────────────────────────────────

echo -e "${BOLD}[1/3] Health check (GET /health)${RESET}"

HEALTH_HTTP_CODE=$(curl -s -o /tmp/dread-health-body.txt -w "%{http_code}" \
  "${WORKER_URL}/health" 2>/dev/null || echo "000")
HEALTH_BODY=$(cat /tmp/dread-health-body.txt 2>/dev/null || echo "")

if [ "$HEALTH_HTTP_CODE" = "200" ]; then
  log_pass "HTTP 200 returned"
else
  log_fail "Expected HTTP 200, got ${HEALTH_HTTP_CODE}"
fi

if [ "$HEALTH_BODY" = "OK" ]; then
  log_pass "Body is 'OK'"
else
  log_fail "Expected body 'OK', got '${HEALTH_BODY}'"
fi

echo ""

# ── Test 2: POST /api/report ──────────────────────────────────────────────────

echo -e "${BOLD}[2/3] Submit synthetic report (POST /api/report)${RESET}"

TIMESTAMP=$(date -u '+%Y-%m-%dT%H:%M:%SZ')

PAYLOAD=$(cat <<EOF
{
  "ModVersion": "0.0.0-smoketest",
  "GameVersion": "0.0.0",
  "UnityVersion": "0.0.0",
  "Reports": [{
    "Hash": "${TEST_HASH}",
    "Timestamp": "${TIMESTAMP}",
    "Type": "exception",
    "ExceptionType": "SmokeTestException",
    "Message": "[Dread SmokeTest] Automated smoke test from test-error-reporter.sh",
    "StackTrace": "SmokeTest.Run() at test-error-reporter.sh:0",
    "Scene": "SmokeTest",
    "GameState": {
      "SceneName": "SmokeTest",
      "EnemiesAlive": 0,
      "EnemiesTotal": 0,
      "EnemiesNearby": 0,
      "PlayerHp": 100,
      "PlayerMaxHp": 100,
      "PlayerStamina": 100,
      "PlayerPosition": {"x": 0, "y": 0, "z": 0},
      "PlayTimeSeconds": 0
    },
    "SystemInfo": {
      "Os": "SmokeTest",
      "OsFamily": "Other",
      "Cpu": "SmokeTest",
      "CpuCores": 1,
      "CpuFrequencyMHz": 0,
      "MemoryMB": 0,
      "Gpu": "SmokeTest",
      "GpuVendor": "SmokeTest",
      "GpuDriverVersion": "0",
      "GpuShaderLevel": 0,
      "VramMB": 0,
      "DeviceType": "Desktop",
      "DeviceModel": "SmokeTest"
    },
    "Display": {
      "Width": 1920,
      "Height": 1080,
      "RefreshRate": 60,
      "Dpi": 96.0,
      "FullScreenMode": "FullScreenWindow"
    },
    "Config": {
      "AudioEnabled": true,
      "AudioFrequency": 120.0,
      "AudioVolume": 0.4,
      "AggressionEnabled": true,
      "AggressionAudioEnabled": true,
      "FakeFootsteps": true,
      "Adrenaline": true,
      "LowStaminaSound": true,
      "PanicSprint": true,
      "CrouchSpeedBoost": true,
      "ErrorReportingEnabled": true
    }
  }]
}
EOF
)

REPORT_HTTP_CODE=$(curl -s -o /tmp/dread-report-body.txt -w "%{http_code}" \
  -X POST \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD" \
  "${WORKER_URL}/api/report" 2>/dev/null || echo "000")
REPORT_BODY=$(cat /tmp/dread-report-body.txt 2>/dev/null || echo "")

if [ "$REPORT_HTTP_CODE" = "200" ]; then
  log_pass "HTTP 200 returned"
else
  log_fail "Expected HTTP 200, got ${REPORT_HTTP_CODE}"
fi

# Validate JSON structure: must contain "processed" and "results" keys
if echo "$REPORT_BODY" | jq -e '.processed' &>/dev/null; then
  log_pass "Response contains 'processed' field"
else
  log_fail "Response missing 'processed' field"
fi

if echo "$REPORT_BODY" | jq -e '.results' &>/dev/null; then
  log_pass "Response contains 'results' field"
else
  log_fail "Response missing 'results' field"
fi

log_info "Response body: $(echo "$REPORT_BODY" | jq -c . 2>/dev/null || echo "$REPORT_BODY")"

echo ""

# ── Test 3 (optional): Verify GitHub issue ─────────────────────────────────────

echo -e "${BOLD}[3/3] GitHub issue verification${RESET}"

if [ "$VERIFY_ISSUE" = true ]; then
  log_info "Searching for issue with hash: ${TEST_HASH}"

  # Search for issues containing the smoke test hash
  ISSUE_COUNT=$(gh issue list --repo "$REPO" \
    --search "hash:${TEST_HASH} in:body" \
    --label "auto-reported" \
    --json number --jq 'length' 2>/dev/null || echo "0")

  if [ "$ISSUE_COUNT" -gt 0 ]; then
    ISSUE_NUM=$(gh issue list --repo "$REPO" \
      --search "hash:${TEST_HASH} in:body" \
      --label "auto-reported" \
      --json number --jq '.[0].number' 2>/dev/null || echo "?")
    log_pass "Found auto-reported issue #${ISSUE_NUM}"
  else
    log_fail "No auto-reported issue found with hash ${TEST_HASH}"
  fi
else
  log_info "Skipped (pass --verify-issue flag and install gh CLI to enable)"
fi

echo ""

# ── Summary ────────────────────────────────────────────────────────────────────

echo -e "${BOLD}═══════════════════════════════════════════════════${RESET}"
TOTAL=$((PASS + FAIL))
echo -e "  ${GREEN}Passed: ${PASS}${RESET} / ${TOTAL}"
if [ "$FAIL" -gt 0 ]; then
  echo -e "  ${RED}Failed: ${FAIL}${RESET} / ${TOTAL}"
fi
echo -e "${BOLD}═══════════════════════════════════════════════════${RESET}"

# Cleanup temp files
rm -f /tmp/dread-health-body.txt /tmp/dread-report-body.txt

if [ "$FAIL" -gt 0 ]; then
  echo -e "\n${RED}SMOKE TEST FAILED${RESET}"
  exit 1
else
  echo -e "\n${GREEN}SMOKE TEST PASSED${RESET}"
  exit 0
fi
