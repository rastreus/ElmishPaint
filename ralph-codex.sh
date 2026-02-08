#!/usr/bin/env bash
# ralph-codex.sh — Autonomous agent loop for ElmishPaint (Codex edition)
# Spawns a fresh Codex agent per iteration. Each iteration picks one
# prd.json story, implements it using TCR, and marks it complete.
#
# After each iteration, runs post-iteration verification outside the
# sandbox (dotnet build + optional agent-browser runtime check). If
# verification fails, the story's passes flag is reverted so the next
# iteration picks it up automatically.
#
# Usage:
#   ./ralph-codex.sh [max_iterations]    # default: 10
#   ./ralph-codex.sh 30                  # run up to 30 iterations
#
# Prerequisites:
#   - codex CLI (npm install -g @openai/codex)
#   - jq (brew install jq / apt install jq)
#   - jj (jujutsu VCS)
#   - dotnet SDK with Fable tooling
#   - Node.js with pnpm
#   - agent-browser (optional, for runtime verification)
#
# Authentication:
#   codex login          # ChatGPT account (browser OAuth)
#   codex login --with-api-key   # or pipe OPENAI_API_KEY

set -euo pipefail

MAX_ITERATIONS="${1:-10}"
PROMPT_FILE="PROMPT.md"
PRD_FILE="prd.json"
PROGRESS_FILE="progress.txt"
AGENTS_FILE="AGENTS.md"

# ── Codex configuration ─────────────────────────────────────────────
# --full-auto: workspace-write sandbox + on-request approvals (no prompts)
# Network access enabled for pnpm install, dotnet restore, etc.
# --add-dir .git: allow jj to write commits (jj desc, jj new, jj restore)
# --add-dir cache paths: allow pnpm/corepack to use their caches
# -o: capture final agent message to a file for completion-signal parsing
CODEX_OUTPUT_FILE=".ralph-codex-output.txt"

# Resolve pnpm/corepack cache dirs (macOS defaults as fallback)
PNPM_STORE="${PNPM_HOME:-$HOME/Library/pnpm}"
COREPACK_CACHE="${COREPACK_HOME:-$HOME/Library/Caches/node/corepack}"

CODEX_FLAGS=(
  exec
  --full-auto
  --model gpt-5.3-codex
  -c model_reasoning_effort=xhigh
  -c sandbox_workspace_write.network_access=true
  --add-dir .git
  --add-dir "$PNPM_STORE"
  --add-dir "$COREPACK_CACHE"
  --add-dir "$HOME/.pyenv"
  --add-dir "$HOME/Library/Caches/ms-playwright"
  --json
  -o "$CODEX_OUTPUT_FILE"
)

# ── Preflight checks ────────────────────────────────────────────────
for cmd in codex jq jj dotnet node; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "ERROR: '$cmd' is not installed or not on PATH."
    exit 1
  fi
done

for f in "$PRD_FILE" "$AGENTS_FILE" "$PROMPT_FILE"; do
  if [[ ! -f "$f" ]]; then
    echo "ERROR: Required file '$f' not found in project root."
    exit 1
  fi
done

# ── Archive previous runs if branch changed ─────────────────────────
BRANCH_NAME=$(jq -r '.branchName' "$PRD_FILE")
if [[ -f ".ralph-branch" ]]; then
  PREV_BRANCH=$(cat .ralph-branch)
  if [[ "$PREV_BRANCH" != "$BRANCH_NAME" && -f "$PROGRESS_FILE" ]]; then
    ARCHIVE_DIR="archive/$(date +%Y-%m-%d)-${PREV_BRANCH//\//-}"
    mkdir -p "$ARCHIVE_DIR"
    cp "$PROGRESS_FILE" "$ARCHIVE_DIR/"
    cp "$PRD_FILE" "$ARCHIVE_DIR/"
    echo "" > "$PROGRESS_FILE"
    echo "Archived previous run to $ARCHIVE_DIR/"
  fi
fi
echo "$BRANCH_NAME" > .ralph-branch

# ── Ensure we're on the right bookmark ───────────────────────────────
CURRENT_BOOKMARKS=$(jj bookmark list 2>/dev/null || true)
if ! echo "$CURRENT_BOOKMARKS" | grep -q "$BRANCH_NAME"; then
  echo "Creating bookmark: $BRANCH_NAME"
  jj bookmark create "$BRANCH_NAME" -r @
fi

# ── Initialize progress file if missing ──────────────────────────────
if [[ ! -f "$PROGRESS_FILE" ]]; then
  echo "# ElmishPaint — Ralph Progress Log" > "$PROGRESS_FILE"
  echo "" >> "$PROGRESS_FILE"
  echo "Initialized: $(date -u +%Y-%m-%dT%H:%M:%SZ)" >> "$PROGRESS_FILE"
  echo "" >> "$PROGRESS_FILE"
fi

# ── Count remaining stories ──────────────────────────────────────────
remaining() {
  jq '[.userStories[] | select(.passes == false)] | length' "$PRD_FILE"
}

# ── Revert a story's passes flag to false ────────────────────────────
revert_story() {
  local story_id="$1"
  local tmp
  tmp=$(mktemp)
  jq --arg id "$story_id" '
    (.userStories[] | select(.id == $id)).passes = false
  ' "$PRD_FILE" > "$tmp" && mv "$tmp" "$PRD_FILE"
  echo "  [revert] Set $story_id passes=false in prd.json"
}

# ── Post-iteration verification (runs OUTSIDE sandbox) ───────────────
# Catches issues the agent's in-sandbox checks may have missed:
#   1. dotnet build (F# type checking that Fable transpilation can skip)
#   2. agent-browser runtime errors (real browser API mismatches)
#
# If verification fails, the story's passes flag is reverted so the
# next iteration automatically picks it up as a fix task.

verify_build() {
  local story_id="$1"
  echo "  [verify] dotnet build..."
  if ! dotnet build --nologo -v quiet 2>&1; then
    echo "  [FAIL] dotnet build failed for $story_id"
    revert_story "$story_id"
    return 1
  fi
  echo "  [OK] dotnet build clean."
  return 0
}

verify_runtime() {
  local story_id="$1"

  # Skip stories that don't touch rendering or browser APIs
  case "$story_id" in
    S01-*|S02-*|S03-*) return 0 ;;
  esac

  # Skip if agent-browser is not installed
  if ! command -v agent-browser &>/dev/null; then
    echo "  [skip] agent-browser not installed, skipping runtime check."
    return 0
  fi

  echo "  [verify] Runtime browser check for $story_id..."

  # Start dev server in background
  pnpm start &>/dev/null &
  local dev_pid=$!

  # Wait for vite to be ready (poll for up to 15 seconds)
  local retries=0
  while ! curl -s -o /dev/null http://localhost:5173 2>/dev/null; do
    sleep 1
    retries=$((retries + 1))
    if [[ $retries -ge 15 ]]; then
      echo "  [FAIL] Dev server did not start within 15 seconds."
      kill "$dev_pid" 2>/dev/null || true
      wait "$dev_pid" 2>/dev/null || true
      revert_story "$story_id"
      return 1
    fi
  done

  # Open the app and check for runtime errors
  agent-browser open http://localhost:5173 2>/dev/null || true
  sleep 2  # let React render and any errors fire

  local errors
  errors=$(agent-browser errors 2>/dev/null || echo "")

  # Capture console errors too (non-blocking)
  local console_errors
  console_errors=$(agent-browser console 2>/dev/null | grep -i "error" || echo "")

  # Cleanup
  agent-browser close 2>/dev/null || true
  kill "$dev_pid" 2>/dev/null || true
  wait "$dev_pid" 2>/dev/null || true

  if [[ -n "$errors" ]]; then
    echo "  [FAIL] Uncaught runtime errors:"
    echo "$errors"
    if [[ -n "$console_errors" ]]; then
      echo "  Console errors:"
      echo "$console_errors"
    fi
    revert_story "$story_id"
    return 1
  fi

  if [[ -n "$console_errors" ]]; then
    echo "  [warn] Console errors (non-blocking):"
    echo "$console_errors"
  fi

  echo "  [OK] No runtime errors."
  return 0
}

# ── Main loop ────────────────────────────────────────────────────────
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Ralph Loop — ElmishPaint (Codex)                          ║"
echo "║  Max iterations: $MAX_ITERATIONS                           ║"
echo "║  Remaining stories: $(remaining)                           ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

for ((i=1; i<=MAX_ITERATIONS; i++)); do
  STORIES_LEFT=$(remaining)

  if [[ "$STORIES_LEFT" -eq 0 ]]; then
    echo "════════════════════════════════════════════════"
    echo "  All stories pass. PRD complete."
    echo "════════════════════════════════════════════════"
    exit 0
  fi

  # Pick the next story (highest priority that hasn't passed)
  NEXT_STORY=$(jq -r '
    [.userStories[] | select(.passes == false)]
    | sort_by(.priority)
    | .[0].id
  ' "$PRD_FILE")

  NEXT_TITLE=$(jq -r --arg id "$NEXT_STORY" '
    .userStories[] | select(.id == $id) | .title
  ' "$PRD_FILE")

  echo "──────────────────────────────────────────────────"
  echo "  Iteration $i/$MAX_ITERATIONS"
  echo "  Story: $NEXT_STORY — $NEXT_TITLE"
  echo "  Remaining: $STORIES_LEFT stories"
  echo "──────────────────────────────────────────────────"

  # Build the prompt for this iteration.
  #
  # Unlike Claude Code's @file syntax, Codex reads the working directory
  # automatically. We tell it which files to read in the prompt itself.
  ITERATION_PROMPT=$(cat <<EOF
You are starting iteration $i of the Ralph loop for the ElmishPaint project.

## Context Files — Read These First

Before doing anything else, read these files from the project root:

1. **AGENTS.md** — Development methodology (TCR, jj, Conventional Commits, architecture).
2. **PROMPT.md** — Per-iteration agent instructions (orient/plan/implement/verify/complete).
3. **prd.json** — Full story backlog with acceptance criteria. Your assigned story is below.
4. **progress.txt** — What prior iterations accomplished. Read this to avoid re-exploring.

## Your Task

Implement story **${NEXT_STORY}**: "${NEXT_TITLE}"

1. Read AGENTS.md for development methodology (TCR, jj, Conventional Commits).
2. Read PROMPT.md for the five-phase iteration protocol.
3. Read prd.json to understand the full story and its acceptance criteria.
4. Read progress.txt to understand what has been done in prior iterations.
5. Explore the codebase to understand current state.
6. Implement the story following TCR discipline (small steps, test, commit or revert).
7. Run ALL feedback loops before marking complete:
   - dotnet build (must succeed with zero errors)
   - dotnet fable src -e fs.jsx (must compile, zero warnings)
   - pnpm test (Vitest tests must pass)
   - pnpm build (production build must succeed)
   - dotnet fantomas --check src/ (formatting must pass)
8. When all acceptance criteria are met:
   - Update prd.json: set the story "passes" field to true.
   - Append a concise progress entry to progress.txt.
   - Commit prd.json and progress.txt changes.
9. ONLY work on story ${NEXT_STORY}. Do not work on other stories.

## Important: Feliz v3

This project uses Feliz v3 (not v2). Read .agents/skills/feliz-v3/SKILL.md for
critical v3 syntax changes before writing any Feliz code. The v3 breaking changes
WILL cause compilation errors if you use old patterns.

If you complete the story and ALL stories in prd.json now have passes: true,
include exactly this text in your final output: <promise>COMPLETE</promise>
EOF
  )

  # Clean previous output
  rm -f "$CODEX_OUTPUT_FILE"

  # Run the agent
  echo "  [codex] Starting agent..."
  codex "${CODEX_FLAGS[@]}" "$ITERATION_PROMPT" 2>&1 || true

  # ── Post-iteration verification (outside sandbox) ──────────────────
  echo ""
  echo "  [post] Running post-iteration verification..."

  VERIFY_FAILED=false

  verify_build "$NEXT_STORY" || VERIFY_FAILED=true

  if [[ "$VERIFY_FAILED" == "false" ]]; then
    verify_runtime "$NEXT_STORY" || VERIFY_FAILED=true
  fi

  if [[ "$VERIFY_FAILED" == "true" ]]; then
    echo ""
    echo "  [post] Verification FAILED for $NEXT_STORY."
    echo "         Story reverted to passes=false."
    echo "         Next iteration will address it."
    echo ""
  else
    echo "  [post] Verification passed for $NEXT_STORY."
  fi

  # ── Check for completion signal in captured output ─────────────────
  if [[ -f "$CODEX_OUTPUT_FILE" ]]; then
    RESULT=$(cat "$CODEX_OUTPUT_FILE")
    echo "$RESULT"

    # Only honor completion signal if verification also passed
    if [[ "$RESULT" == *"<promise>COMPLETE</promise>"* && "$VERIFY_FAILED" == "false" ]]; then
      echo ""
      echo "════════════════════════════════════════════════"
      echo "  All stories complete. PRD fulfilled."
      echo "════════════════════════════════════════════════"
      exit 0
    fi
  else
    echo "  [warning] No output captured from Codex for this iteration."
  fi

  # Brief pause between iterations
  sleep 2
done

echo ""
echo "════════════════════════════════════════════════"
echo "  Max iterations ($MAX_ITERATIONS) reached."
echo "  Remaining stories: $(remaining)"
echo "════════════════════════════════════════════════"
exit 1
