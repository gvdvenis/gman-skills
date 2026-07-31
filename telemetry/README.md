# Telemetry design

Telemetry is lightweight, structured, and local. All artifacts are written to the user-level
Copilot directory — never into the target repository.

## Artifact formats

Each run produces three complementary artifact types:

| File | Description |
| --- | --- |
| `events.jsonl` | Append-only JSON Lines streaming event log, written incrementally as the run progresses. |
| `reports/<agent-id>.json` | One JSON file per specialist, written by the orchestrator when it receives the specialist's report. Schema defined in `feedback-report-template.json`. |
| `analysis.json` | Orchestrator's end-of-run summary, written once at run end. Schema defined in `analysis-schema.json`. |

## Directory layout

All artifacts live under the user-level Copilot directory:

```
~/.copilot/blazor-orchestration/runs/<run_id>/
  events.jsonl
  reports/<agent-id>.json
  analysis.json
  reports/self-improvement-report.html   ← only with --self-improve
```

Artifacts are stored in the user-level Copilot directory, not the target repository. This means no
`.gitignore` entries are needed and there is no risk of accidentally committing run artifacts.

## Who writes what

**The orchestrator owns all writes.** Specialists never write directly to disk. A specialist returns
its report JSON in its response; the orchestrator receives it and writes it to
`reports/<agent-id>.json`. The orchestrator also writes every event to `events.jsonl` and writes
`analysis.json` at run end.

## Event categories

Event schemas are defined in `event-schema.json`. Events are divided into mandatory and best-effort
categories:

| Event | Status | Rationale |
| --- | --- | --- |
| `dispatch` | **Mandatory** | Every delegated lane must be traceable. |
| `completion` | **Mandatory** | Run outcome is uninterpretable without it. |
| `analysis` | **Mandatory** | End-of-run summary is the primary artifact. |
| `handoff` | **Mandatory when it occurs** | Any lane transition must be recorded. |
| `review_loop_stopped` | **Mandatory when it occurs** | Records that the review stop rule fired. |
| `tool_use` | Best-effort | Valuable but the harness does not always surface tool names. |
| `feedback` | Best-effort | Only emitted when a specialist raises an issue. |
| `tool_missed` | Conditional | See trustworthiness rule below. |

## `tool_missed` trustworthiness rule

`tool_missed` is emitted **only** when a specialist explicitly identifies a missed tool in
`self_diagnosis.issues` under `--self-improve`. It is never inferred from silence or from the
absence of a tool call in the event log.

When recorded, `severity` is always `"advisory"`. The orchestrator must never block on a
`tool_missed` event or treat it as a failure.

## Retention

There is no automated purge in the initial implementation. The orchestrator prints the run directory
path in its closing output line so the user can locate or clean up artifacts manually.
