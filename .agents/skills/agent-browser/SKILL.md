---
name: agent-browser
description: "Headless browser automation for runtime verification. Use after pnpm build to check for runtime errors (console exceptions, rendering failures) that unit tests and Fable transpilation cannot catch."
allowed-tools: Bash(agent-browser *)
---

# agent-browser — Agent Reference

Headless Chromium CLI for verifying app renders without runtime errors.

## Core Workflow for Runtime Verification
```bash
# 1. Start dev server in background
pnpm start &
DEV_PID=$!
sleep 3  # wait for vite to be ready

# 2. Open the app
agent-browser open http://localhost:5173

# 3. Check for runtime errors (uncaught exceptions)
agent-browser errors

# 4. Check console for warnings/errors
agent-browser console

# 5. Take screenshot for visual verification (optional)
agent-browser screenshot /tmp/verify.png

# 6. Cleanup
agent-browser close
kill $DEV_PID
```

## Key Commands

| Command | Purpose |
|---------|---------|
| `agent-browser open <url>` | Navigate to URL |
| `agent-browser errors` | View uncaught JavaScript exceptions |
| `agent-browser console` | View console messages (log, error, warn, info) |
| `agent-browser snapshot -i` | Get interactive elements with refs |
| `agent-browser screenshot [path]` | Capture viewport |
| `agent-browser get title` | Get page title |
| `agent-browser get text <sel>` | Get element text content |
| `agent-browser eval <js>` | Run JavaScript in page context |
| `agent-browser close` | Close browser |

## Checking for Errors
```bash
# Uncaught exceptions (TypeError, ReferenceError, etc.)
agent-browser errors
# Returns nothing if clean, or lists exceptions with stack traces

# Console output (includes console.error calls)
agent-browser console
```

**A clean runtime verification means `agent-browser errors` returns no output.**

## Snapshot for Element Verification
```bash
agent-browser snapshot -i
# Output:
# - heading "ElmishPaint" [ref=e1]
# - canvas [ref=e2]
# - button "Pencil" [ref=e3]

# Verify specific element exists
agent-browser get text @e1
```

## Full Reference

Run `agent-browser --help` for all commands.
