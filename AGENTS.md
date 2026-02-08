# AGENTS.md — Agent Development Protocol

> This file governs how AI coding agents develop, test, and commit code in this
> repository. These are instructions, not suggestions. Follow them precisely.

---

## 0. Agent Communication Standards

Be direct. No preamble, no filler, no sycophancy. Do not say "Great question!"
or "Thanks for the context!" — just answer or act.

If the user's approach is wrong, say so and explain why. If a requested change
would degrade code quality, push back with a concrete reason. Mutual respect
means honest feedback, not compliance.

Output code, not commentary about code. When explaining is necessary, be concise.

---

## 1. Execution Context: The Ralph Loop

This project is developed through an **autonomous agent loop** (Ralph). Each
iteration spawns a fresh agent session with a clean context window. You have
no memory of previous iterations.

### 1.1 Your Memory Between Iterations

| Source           | What it tells you                                    |
|------------------|------------------------------------------------------|
| `progress.txt`   | What prior iterations did, decisions made, gotchas   |
| `prd.json`       | Which stories are done (`passes: true`) and pending  |
| `jj log`         | Commit history showing what code was actually changed |
| `AGENTS.md`      | May contain a "Discovered Patterns" section (§9)     |

**Read `progress.txt` first** at the start of every iteration. It is your
shortcut past re-exploring the entire codebase.

### 1.2 What You Update

At the end of each iteration:

1. **`prd.json`** — Set your story's `"passes"` to `true`.
2. **`progress.txt`** — Append a concise entry: what you did, decisions made,
   files changed, gotchas found.
3. **`AGENTS.md` §9** — If you discover a pattern or gotcha that future
   iterations need, add it to the Discovered Patterns section.

Commit these tracking files as a `chore(ralph):` commit.

### 1.3 Iteration Protocol

```
1. Orient    — Read progress.txt, prd.json, jj log, relevant source/test files
2. Plan      — Write your TCR cycle sequence before coding
3. Implement — Follow TCR (§3) for each step
4. Verify    — Run ALL feedback loops (§6)
5. Complete  — Update prd.json, progress.txt, commit
```

See `PROMPT.md` for detailed per-phase instructions.

---

## 2. Skills & Tooling Context

This project uses **Skills** (SKILL.md files) for domain-specific tool
expertise. AGENTS.md defines *project policy*. Skills define *tool knowledge*.

### 2.1 Required Skills

| Skill             | Purpose                       | When to load                          |
|-------------------|-------------------------------|---------------------------------------|
| `jj-vcs`          | Jujutsu version control       | Any VCS operation                     |
| `agent-browser`   | Headless browser verification | Runtime error checking (§6)           |

### 2.2 Skill Precedence

This file takes precedence when it conflicts with a skill's general guidance.
The jj skill may suggest freeform commit messages; this file mandates
Conventional Commits (§4). Load skills for *how* to use tools. Follow this
file for *when* and *why*.

---

## 3. Development Methodology: TCR (Test && Commit || Revert)

Every implementation change follows TCR without exception.

### 3.1 The TCR Loop (Jujutsu)

```
1. Describe intent:    jj desc -m "<conventional commit message>"
2. Make a small change (max ~20 lines of diff)
3. Run: dotnet build (compile check)
4. Run: dotnet test --filter "Relevant" (or full suite)
5. IF tests pass  → jj new   (finalize, start fresh)
6. IF tests fail  → jj restore   (discard changes)
7. Return to step 1
```

### 3.2 What Counts as "One Change"

A single TCR cycle does exactly **one** of:

- Add a new case to a discriminated union
- Add or modify one clause in `update` or `init`
- Add or modify one element in a `view` function
- Add one new test case
- Rename or move one definition
- Modify one type signature and fix resulting compiler errors

### 3.3 The Red-Green Exception (New Tests)

```
1. jj desc -m "test(scope): verify <expected behavior>"
2. Write the failing test
3. jj new   ← commit the red test
4. Resume normal TCR for implementation
```

### 3.4 On Revert — What To Do

1. **Decompose further.** Break the change into 2–3 smaller steps.
2. **Try a different angle.**
3. **Write a characterization test** if the failure was unexpected.

After **3 consecutive reverts** on the same goal: stop, write characterization
tests, read output carefully, reassess entirely.

---

## 4. Commit Convention: Conventional Commits

All commits follow **Conventional Commits 1.0.0**.

### 4.1 Format

```
<type>(<scope>): <description>
```

### 4.2 Types

| Type       | When to use                                                |
|------------|------------------------------------------------------------|
| `feat`     | New feature or capability                                  |
| `fix`      | Bug fix                                                    |
| `test`     | Adding or correcting tests                                 |
| `refactor` | Code change: no bug fix, no new feature                    |
| `docs`     | Documentation only                                         |
| `style`    | Formatting, whitespace                                     |
| `perf`     | Performance improvement                                    |
| `build`    | Build system or dependency changes                         |
| `ci`       | CI configuration                                           |
| `chore`    | Maintenance (includes Ralph tracking file updates)         |

### 4.3 Rules

- Lowercase description, imperative mood, no trailing period.
- Scope = Elmish module or domain concept: `feat(bitcanvas)`, `test(pencil)`,
  `fix(floodfill)`, `chore(ralph)`.
- Breaking changes: `feat(api)!: description` or `BREAKING CHANGE:` footer.

### 4.4 History Cleanup Before Push

```bash
jj log                                    # review micro-commits
jj squash --into <id> -m "msg"            # combine related commits
jj describe -r <id> -m "msg"              # fix a message
jj absorb                                 # auto-distribute changes
```

### 4.5 Changelog

```bash
jj git export
git-cliff --output CHANGELOG.md
```

---

## 5. Technology Stack & Architecture

**F#** with **Fable** compiler, **Elmish** (MVU), **Feliz** for React rendering.

### 5.1 Why This Stack (For The Agent)

- **Single-pass compilation**: top-to-bottom dependency order.
- **Exhaustive matching**: compiler errors = your task list.
- **Strong types**: catches mistakes before tests run.
- **Unidirectional data flow**: View → Msg → Update → Model → View.
- **No circular dependencies**: impossible by F# project structure.

### 5.2 Architecture Invariants (Never Violate)

1. `update` is pure. Side effects go through `Cmd<Msg>` only.
2. `view` is pure. Reads model, calls dispatch. No mutation.
3. `Model` is single source of truth. No mutable state outside it.
4. All external events enter through `Msg`.
5. `Msg` union is exhaustive. If not in `Msg`, it cannot happen.

### 5.3 File Organization

F# single-pass: lower files may reference upper. Never the reverse.

```
src/
  Types.fs          — Bit, Tool, Pattern, Modifiers, etc.
  Model.fs          — Model record type
  Msg.fs            — Msg discriminated union
  Canvas/
    BitCanvas.fs    — 512×342 packed bit array
    Algorithms.fs   — Bresenham, flood fill
    Dithering.fs    — Atkinson dithering
    Patterns.fs     — 8×8 pattern tiles
  Tools/
    Pencil.fs       — Pencil logic
    Eraser.fs       — Eraser logic
    Line.fs         — Line tool logic
    Rectangle.fs    — Rectangle logic
    FloodFill.fs    — Flood fill logic
    Marquee.fs      — Selection logic
  Update.fs         — Pure update function
  Components/
    CanvasView.fs   — Canvas rendering
    Toolbar.fs      — Tool selection
    PatternPalette.fs
    StatusBar.fs
    ImportDialog.fs
    ExportMenu.fs
    MacBezel.fs
  Interop/
    Canvas2D.fs     — Fable bindings for Canvas API
    FileApi.fs      — File/Blob/download bindings
    ShadcnUi.fs     — shadcn/ui bindings
  App.fs            — Root component, Feliz.UseElmish wiring
  Main.fs           — Entry point, mount React root
```

**When adding files: update `.fsproj` `<Compile Include="..."/>` in correct
dependency order.**

### 5.4 Adding a Feature — Canonical TCR Sequence

Each step = one TCR cycle = one Conventional Commit:

```
Step 1: Add Msg case(s) + minimal update handler → feat(scope): add X type
Step 2: Write failing test                       → test(scope): verify X
Step 3: Implement update logic, test goes green  → feat(scope): implement X
Step 4: Add view element dispatching Msg         → feat(scope): add X to view
Step 5: (if needed) Add Cmd + result Msg pair    → feat(scope): add X effect
```

---

## 6. Feedback Loops

Run ALL before marking a story complete. **Do not commit if any fails.**

```bash
# F# compilation check (catches type errors Fable may miss)
dotnet build

# F# compilation via Fable (zero warnings)
dotnet fable src -e fs.jsx

# Vitest JS-level tests (if applicable)
pnpm test

# Vite production build (must succeed)
pnpm build

# F# formatting check
dotnet fantomas --check src/

# Runtime verification (catches errors mocks/jsdom cannot)
# Load the agent-browser skill, then:
# Start dev server, open app, check `agent-browser errors` returns clean.
# Required for any story that changes rendering or browser API usage.
```

---

## 7. VCS Quick Reference (Jujutsu)

**Never use raw `git` commands. This is a jj repository.**

```bash
# === TCR Cycle ===
jj desc -m "type(scope): description"   # describe FIRST
# ... make changes ...
dotnet build                             # compile
pnpm test                                # test
jj new                                   # ON GREEN: finalize
jj restore                               # ON RED: discard

# === Recovery ===
jj undo                                  # reverse last operation
jj op log                                # operation history

# === History ===
jj log                                   # view commits
jj squash --into <id> -m "msg"           # combine commits
jj describe -r <id> -m "msg"             # fix message
jj absorb                                # auto-distribute

# === Push ===
jj bookmark move main --to @
jj git push -b main

# === Changelog ===
jj git export
git-cliff --output CHANGELOG.md
```

---

## 8. Agent Behavioral Rules

### 8.1 Before Writing Code

1. Read `progress.txt` (if in Ralph loop).
2. Read relevant `Model`, `Msg`, existing types.
3. Read existing tests for conventions.
4. Check `.fsproj` for file ordering.
5. Plan TCR cycle sequence. Write the plan, then execute.

### 8.2 During Development

- **Never skip the test run.**
- **Never combine changes.** One thing per TCR cycle.
- **Compile before testing.** `dotnet build` failure = revert.
- **Describe before coding.** `jj desc` is always first.
- **No stubs.** Everything committed must work. No TODOs.

### 8.3 Common Pitfalls

- Do not create mutable state outside the Elmish model.
- Do not use `obj` or `unbox` to circumvent the type system.
- Do not add files without updating `.fsproj` file order.
- Do not accumulate unrelated functions in utility modules.
- Do not import JS libraries without checking for F#/Fable bindings.
- Do not use raw `git` commands. This is a jj repository.
- Do not use `?` dynamic access when typed Fable bindings exist.

---

## 9. Discovered Patterns

> This section is appended by agents during development. If you discover a
> pattern, convention, or gotcha that future iterations need to know, add it
> here and commit as `docs(agents): add <pattern description>`.

- `dotnet fable clean` must include the same extension used by transpilation.
  For this repo (`-e fs.jsx`), use `dotnet fable clean src -e fs.jsx --yes`
  or stale `*.fs.jsx` test artifacts may remain and produce false-positive test
  runs.
- Sandbox configuration: `ralph-codex.sh` grants `--add-dir .git` so `jj desc`,
  `jj new`, `jj restore` all work inside the Codex sandbox. If jj operations
  fail with "Operation not permitted", check that `.git` is in the add-dir list.
  pnpm/corepack caches are also granted via `--add-dir`.
- Fable 5 in this repo does not support runtime creation helpers for `array2d`
  (`Array2D.create`, `Array2D.init`, `array2D`). Do not construct default
  `bool array2d` tiles in `init`; defer concrete pattern tile construction to
  the patterns story to avoid transpilation errors.
- `dotnet build` must be run as the first verification step; Fable transpilation alone does not catch all F# compilation errors (e.g., FS0247 namespace/module collisions).
- `Dom.ImageData.Create` requires a `Uint8ClampedArray` (not a plain `byte array`) and integer dimensions — passing floats causes a runtime `TypeError`.
- Unit tests with jsdom mocks can miss real browser API mismatches (e.g.,
  `ImageData` constructor signature differences). After any story that touches
  rendering or browser APIs, run `agent-browser errors` (if available) against the live dev
  server to catch runtime `TypeError`/`ReferenceError` that mocks hide.
- `agent-browser` cannot run inside the Codex sandbox due to macOS
  `mach_port_rendezvous` restrictions on Chromium process spawning. Runtime
  browser verification runs as a post-iteration step in `ralph-codex.sh`
  outside the sandbox. The agent should still write code that would pass
  runtime checks, and should use the strict `ImageData` shim in
  `vitest-setup.ts` to catch browser API mismatches during unit tests.
- Avoid `module A.B.C` declarations when `namespace A.B` is also used elsewhere in the assembly; use explicit `namespace` + nested `module` instead.
- `vite-plugin-fable` is required for the dev server (`pnpm start` / `vite dev`).
  It ensures fable_modules are compiled before Vite serves pages. Do not remove
  it. The agent's feedback loops (`pnpm test`, `pnpm build`) pre-transpile with
  `dotnet fable` explicitly and do not depend on the plugin.

---

## 10. Summary of Principles

1. **TCR is non-negotiable.** Every implementation change goes through the loop.
2. **Small steps always.** If a change feels big, decompose it.
3. **Describe first.** Conventional Commit message before any code change.
4. **Types are documentation.** `Msg` = what can happen. `Model` = what is known.
5. **Purity is power.** Pure `update` and `view` are trivially testable.
6. **The compiler is your ally.** Exhaustive matching catches mistakes early.
7. **Revert is information.** Step was too large or approach was wrong. Adjust.
8. **The operation log is your safety net.** `jj undo` recovers anything.
9. **Progress.txt is your memory.** Read it first, write to it last.
10. **One story per iteration.** Stay focused. Do not scope-creep.
