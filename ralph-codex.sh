#!/usr/bin/env bash
# ralph-codex.sh — Autonomous agent loop for ElmishPaint (Codex edition)
# Spawns a fresh Codex agent per iteration. Each iteration picks one
# prd.json story, implements it using TCR, and marks it complete.
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
# -o: capture final agent message to a file for completion-signal parsing
CODEX_OUTPUT_FILE=".ralph-codex-output.txt"
CODEX_FLAGS=(
  exec
  --full-auto
  --model gpt-5.3-codex
  -c model_reasoning_effort=high
  -c sandbox_workspace_write.network_access=true
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

# ── Main loop ────────────────────────────────────────────────────────
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║  Ralph Loop — ElmishPaint (Codex)                            ║"
echo "║  Max iterations: $MAX_ITERATIONS                                          ║"
echo "║  Remaining stories: $(remaining)                                       ║"
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
   - dotnet fable src -e fs.jsx --verbose (must compile, zero warnings)
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
  codex "${CODEX_FLAGS[@]}" "$ITERATION_PROMPT" 2>&1 || true

  # Check for completion signal in captured output
  if [[ -f "$CODEX_OUTPUT_FILE" ]]; then
    RESULT=$(cat "$CODEX_OUTPUT_FILE")
    echo "$RESULT"

    if [[ "$RESULT" == *"<promise>COMPLETE</promise>"* ]]; then
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
