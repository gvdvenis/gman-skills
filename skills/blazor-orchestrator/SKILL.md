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
