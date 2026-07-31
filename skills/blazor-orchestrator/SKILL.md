---
name: blazor-orchestrator
description: >
  Route a full Blazor work request across the appropriate specialist lane(s). Use when the request
  spans more than one concern (authoring, data, auth, review) or when lane selection itself is
  uncertain. For a single, narrow authoring task, invoke the component or data specialist directly.
  Triggers on full-request phrasing: "implement this feature", "review and refactor this page",
  "build this form end-to-end". Does NOT trigger for single-skill or narrow lane-specific requests.
---

# Blazor orchestrator skill

## Run ID

Generate a run ID at the very start of every invocation, regardless of telemetry settings.
Format: `run-YYYYMMDD-HHMM` (use current **local** date and time). Announce it in your first output line:

```
[blazor-orchestrator] run-20260731-2346 started
```

## Flag and option parsing

Parse options from the invocation text in natural language. Do **not** rely on `$ARGUMENTS`
substitution — it is not a verified Copilot contract.

| Option | Recognised phrases | Default |
|---|---|---|
| Skip code review | "skip code review", `--skip-code-review` | `false` (review runs) |
| Enable self-improvement | "enable self-improvement", `--self-improve` | `false` (off) |

If neither phrase appears, code review runs and self-improvement is skipped.

## Collision boundary with `blazor-component-architect`

These two skills coexist. Do not replace or wrap `blazor-component-architect`:

- **`blazor-orchestrator`**: full-request, multi-concern routing — lane-splitting and handoff is the job.
- **`blazor-component-architect`** (user-level, external): single-lane Blazor authoring guidance; may be
  invoked as guidance by the orchestrator or specialists.

## Execution

After parsing flags, apply the route classifier in `docs/routing-classifier.md`. The classifier decides
whether to execute inline or delegate to a specialist agent — do not force delegation for every
invocation. Inline execution (single lane, ≤ 2 files, no broad repo discovery) keeps the work in
this context window without spawning a sub-agent.

## Self-improvement report generation (`--self-improve`)

Run this step **after** all specialist work and the review loop have completed, and only when
`--self-improve` is active.

### What to generate

Write `improvement-report-data.json` to:
```
~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json
```

Consult `docs/self-improve-generation.md` for the complete ordered generation algorithm, including:
- `suggestion_key` derivation rules
- Cross-run dedup fold rules (evidence union, max severity, recurrence count)
- Ranking formula (`base_severity_weight + recurrence_boost + history_weight`)
- Dismissal and history-weight application (load `~/.copilot/blazor-orchestration/suggestion-history.json`)
- Full step-by-step generation algorithm

The file must conform to schema `1.1` in `telemetry/improvement-report-data-schema.json`.
The example at `telemetry/improvement-report-data-example.json` shows a valid populated instance.

### Initial file shape

```json
{
  "schema_version": "1.1",
  "generated_at": "<ISO timestamp>",
  "origin": {
    "skill_id": "<active skill id>",
    "skill_scope": "repo | user",
    "skill_path": "<absolute path to skill directory>",
    "repo_root": "<absolute git repository root>",
    "run_id": "<run_id>"
  },
  "findings": [ ... ],
  "decisions": {},
  "shipped_prompt": null
}
```

`decisions` is always an empty object at generation time. `shipped_prompt` is always `null` at
generation time. Both are written by the C# server after user actions.

### CLI staging readiness signal

After the C# server session ends (auto-launched by `--self-improve` on port 5173 by default),
check whether the file has a non-empty `decisions` object **or** a non-null `shipped_prompt`.
If either is true, stage the file:

```
git add ~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json
```

If the file is already git-tracked and has local modifications, present the conflict flow
before staging. See `docs/self-improve-generation.md § Conflict flow` for the three-option
(continue / stash / discard) prompt and the 30-second non-interactive default.

### Server auto-launch

When `--self-improve` is active, the C# server auto-launches on fixed port `5173` (loopback
`127.0.0.1` by default; use `0.0.0.0` when mobile access is preferred and the user has
configured that in skill preferences). If the port is already bound, assume the server is
already running, log a warning, and continue without launching a second instance.

The server stays alive until:
- `GET /shutdown` is called, or
- an idle timeout elapses after the browser first connects, or
- the terminal session ends.

API surface (for reference — not implemented in this skill):
`GET /api/report`, `GET /ping`, `POST /api/ship-prompt`, `POST /api/dismissals`, `GET /shutdown`.
