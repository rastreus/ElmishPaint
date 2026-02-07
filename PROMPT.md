# PROMPT.md — Per-Iteration Agent Instructions

> This file is read by the agent at the start of each Ralph loop iteration.
> It complements AGENTS.md (project policy) with iteration-specific behavior.

---

## Context

You are running inside an autonomous loop. Each iteration:

1. You receive ONE story from prd.json to implement.
2. You have a fresh context window — no memory of previous iterations.
3. Your only memory is: git history, progress.txt, and prd.json status.

**Read progress.txt first.** It tells you what previous iterations accomplished
and any decisions or gotchas they discovered. This saves you from re-exploring
the entire codebase.

**Read `.agents/skills/feliz-v3/SKILL.md` before writing any Feliz code.**
This project uses Feliz v3 which has breaking changes from v2. Your training
data likely contains v2 patterns that WILL cause compilation errors. The skill
file documents every breaking change and the correct v3 syntax.

---

## Iteration Protocol

### Phase 1: Orient (do this before writing any code)

1. Read `progress.txt` for context from prior iterations.
2. Read `.agents/skills/feliz-v3/SKILL.md` for Feliz v3 syntax rules.
3. Read the assigned story in `prd.json` — understand all acceptance criteria.
4. Run `jj log --limit 10` to see recent commits.
5. Run `jj st` to check working copy state.
6. Examine relevant source files (Types, Model, Msg, the module you'll work in).
7. Examine relevant test files to understand testing conventions.
8. Check `.fsproj` file order.

### Phase 2: Plan

Write your TCR cycle plan before touching any code. List the sequence of
atomic changes you intend to make, e.g.:

```
TCR Plan for S08-pencil:
  1. Add Bresenham algorithm to Algorithms.fs — test with known inputs
  2. Add pencil polarity detection to Pencil.fs — test toggle behavior
  3. Handle CanvasMouseDown in update for Pencil tool — test state change
  4. Handle CanvasMouseMove with Bresenham interpolation — test gap-free line
  5. Handle CanvasMouseUp: commit stroke to canvas — test round-trip
  6. Wire pencil into CanvasView mouse events — integration verify
```

### Phase 3: Implement (TCR)

Follow the TCR loop from AGENTS.md exactly:

```
jj desc -m "type(scope): description"
# make small change
dotnet fable src -e fs.jsx --verbose
pnpm test
# green → jj new
# red   → jj restore
```

### Phase 4: Verify

Before marking the story complete, run ALL feedback loops:

```bash
dotnet fable src -e fs.jsx --verbose       # Fable transpilation, zero warnings
pnpm test                                  # Vitest unit tests
pnpm build                                 # Production build (Fable + Vite)
dotnet fantomas --check src/               # Code formatting
```

ALL must pass. If any fails, fix it before proceeding.

### Phase 5: Complete

1. Update `prd.json`: set your story's `"passes"` to `true`.
2. Append to `progress.txt`:

```
## Iteration N — Story ID — Title
Date: YYYY-MM-DDTHH:MM:SSZ
- What was implemented
- Key decisions and reasoning
- Files added/modified
- Gotchas or patterns discovered
- Any AGENTS.md updates made
```

3. Commit these tracking files:

```bash
jj desc -m "chore(ralph): complete story <ID>"
jj new
```

4. If you discover a pattern, gotcha, or convention that future iterations
   should know about, append it to AGENTS.md §9 (Discovered Patterns)
   and commit that too.

---

## Rules

- **One story per iteration.** Never work on a different story than assigned.
- **TCR is mandatory.** No large uncommitted changes. Small steps, always.
- **No stubs.** Everything you commit must work. No TODO comments, no
  placeholder functions, no "will implement later."
- **No skipping tests.** If you add a module, you add tests. If you modify
  behavior, you verify with tests.
- **Respect file order.** F# single-pass compilation means .fsproj order
  matters. When adding files, place them correctly.
- **Feliz v3 only.** Do not use Feliz v2 patterns. When in doubt, check
  `.agents/skills/feliz-v3/SKILL.md`.
- **Be concise in progress.txt.** Future iterations read it for context.
  Sacrifice grammar for density. No filler.

---

## Communication

Be direct. No preamble. No "Let me start by..." or "Great, I'll...".
Just orient, plan, execute, verify, complete.
