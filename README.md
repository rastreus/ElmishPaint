# ElmishPaint — an agentic weekend build

ElmishPaint is a tiny 1-bit monochrome pixel editor (512×342) built in **F#** with **Fable + Elmish + Feliz v3**, inspired by classic Mac-era paint apps and targeting the Macintosh Classic II’s display constraints.

This repo’s real purpose wasn’t “ship a paint editor” — it was to learn modern **agent-based coding workflows**. I used OpenAI Codex (model: `gpt-5.3-codex`) and a simple local automation loop (“Ralph Loop”) to implement the project as a sequence of small, test-verified stories.

## Why this exists

I’d previously used AI (ChatGPT, Claude) primarily through the “chat window” interface and had experience with prompting / context engineering. This weekend project was a hands-on way to explore:

- how to structure work into tight, testable increments for an agent
- how to keep the agent grounded with project policy + skills docs
- how to maintain continuity via progress logs and a PRD backlog

## Tech stack

- **F#** application logic
- **Fable 5** → JS transpilation (this repo uses `.fs.jsx` output)
- **Elmish** update loop
- **Feliz v3** (breaking changes vs v2 are documented in the Feliz skill)
- **Vite** for dev/build
- **Vitest** for unit/integration tests

## Features (MVP)

- 512×342 1-bit canvas (packed bit storage)
- Canvas render + mouse coordinate mapping
- Zoom levels (1× / 2× / 4× / 8×) + optional pixel grid at higher zoom
- Core tools implemented as small stories (pencil/eraser/line/rectangle/fill/etc. as present in the repo)

> The PRD ([`prd.json`](./prd.json)) is the canonical feature/backlog list.

## Local development

### Requirements
- .NET SDK (see [`global.json`](./global.json))
- Node.js (LTS recommended)
- pnpm (repo uses `pnpm-lock.yaml`)

### Install
```bash
dotnet tool restore
dotnet paket install
dotnet build
pnpm install
```

## The “Ralph Loop” workflow (how the agent work was managed)

This repo includes a lightweight agentic workflow:

* [ralph-codex.sh](./ralph-codex.sh) — runs one iteration
* [prd.json](./prd.json) — backlog / user stories with acceptance criteria
* [progress.txt](./progress.txt) — append-only log of what each iteration did
* [AGENTS.md](./AGENTS.md) — project policy (TCR, commit conventions, verification steps)
* [PROMPT.md](./PROMPT.md) — per-iteration instructions for the agent
* .agents/skills/**/SKILL.md — tool/framework-specific guardrails (e.g. Feliz v3 syntax, jj usage)

The loop is intentionally simple:

1.	Pick exactly one story from prd.json
2.	Implement it in small TCR steps
3.	Run the full verification suite
4.	Mark the story as passing + append to progress.txt
5.	Commit

This kept the agent productive without letting the project sprawl beyond “weekend demo” scope.

## License

MIT (see [LICENSE](./LICENSE)).
