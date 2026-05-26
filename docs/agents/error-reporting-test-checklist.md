# Error reporting test checklist (ERR-1)

Manual test matrix for verifying the Dread error reporting pipeline end-to-end.
Covers: TestCrash button, MCP trigger, real exceptions, opt-out, deduplication,
rate limiting, spam filter, and payload shape.

Issue: [#171](https://github.com/grompen91-droid/dreadREPO/issues/171)

---

## Prerequisites

- [ ] R.E.P.O. running with Dread installed (local or Thunderstore build)
- [ ] BepInEx ConfigurationManager installed (optional but recommended for button tests)
- [ ] `DebugServerEnabled=true` in config (section 8. Debug Server)
- [ ] `ErrorReportingEnabled=true` in config (section 5. Error Reporting)
- [ ] MCP server configured in `.cursor/mcp.json` (for MCP tests in section B)

---

## Test Matrix

### A. TestCrash via ConfigurationManager button

- [ ] Enable `ErrorReportingEnabled` in config
- [ ] Open ConfigurationManager (F1)
- [ ] Click "Crash Game" button under section 7. Testing
- [ ] Confirm game crashes with `[Dread TestCrash]` in BepInEx log
- [ ] Check GitHub repo issues: new issue with label `auto-reported` + `bug`, title `[auto] InvalidOperationException in <Scene>`
- [ ] Verify issue body has: Error Report table, Error Details code block, System Information, Display, Game State, Configuration tables, raw JSON payload in details block
- [ ] Verify `<!-- hash:XXXXXXXXXXXXXXXX -->` comment is present at bottom of issue body

### B. TestCrash via MCP (debug server)

- [ ] Use `dread_set_config` section=`errorReporting` key=`` value=`true`
- [ ] Call `dread_trigger_test_crash`
- [ ] Confirm issue created (same validations as section A)

### C. Real exception path

- [ ] With error reporting enabled, cause a real error (e.g. rename an audio .ogg file while game is running to trigger a load failure)
- [ ] Confirm the error is captured and reported
- [ ] Verify the issue title and body reflect the actual exception, not TestCrash

### D. Opt-out verification

- [ ] Set `ErrorReportingEnabled=false`
- [ ] Trigger TestCrash
- [ ] Confirm NO `[ErrorReporter] Sending report` appears in BepInEx log
- [ ] Confirm no new GitHub issue created

### E. Deduplication

- [ ] With error reporting on, trigger TestCrash
- [ ] Note the GitHub issue number created
- [ ] Restart game, trigger TestCrash again (same crash = same hash)
- [ ] Confirm second occurrence adds a COMMENT to the existing issue, not a new issue
- [ ] Comment should contain: Exception, Message, Scene, Timestamp fields

### F. Reopen on closed duplicate

- [ ] Close the auto-reported issue from step E on GitHub
- [ ] Trigger same TestCrash again
- [ ] Confirm the closed issue is REOPENED (state changes to open)
- [ ] Confirm a comment is also added

### G. Rate limiting

- [ ] Trigger 6+ crashes rapidly (within 1 hour window)
- [ ] After 5th report, confirm Worker returns 429 status
- [ ] Check BepInEx log for `Error report failed` warning message

### H. Spam filter

- [ ] Confirm errors containing `DebugConsoleUI` in message or stack trace are NOT reported
- [ ] Confirm errors containing `DebugTester` / `SemiFunc.DebugTester` are NOT reported

### I. Payload shape verification

- [ ] Open any auto-reported issue on GitHub
- [ ] Verify all sections present:
  - Error Report (table with Mod Version, Game Version, Unity Version, Scene, Timestamp, Type)
  - Error Details (exception + stack trace)
  - System Information (OS, CPU, RAM, GPU, VRAM)
  - Display (Resolution, Refresh Rate)
  - Game State (SceneName, enemies, player stats, playtime)
  - Configuration (all 11 config values)
- [ ] Expand "Raw JSON Payload" details block and verify valid JSON

---

## Results

| Date | Tester | Mod Version | Pass/Fail | Notes |
|------|--------|-------------|-----------|-------|
| | | | | |
