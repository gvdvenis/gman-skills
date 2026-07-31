# Telemetry artifact format

Type: grilling
Status: resolved
Assignee: @gvdvenis
Blocked by: 04

## Answer

**Format: two formats, complementary roles**

Use **JSON Lines** (`events.jsonl`) for the streaming event log and **one JSON file per specialist** (`reports/<agent-id>.json`) for structured reports. A third file, `analysis.json`, holds the orchestrator's end-of-run summary. These are not alternatives — they serve different consumers:

- `events.jsonl` is the append-only audit trail, written incrementally as the run progresses.
- `reports/<agent-id>.json` is the specialist's structured handoff (the schema from ticket 04), written by the orchestrator when it receives the report.
- `analysis.json` is written once at run end by the orchestrator.

**Directory layout: user-level, not target repository**

Artifacts live in the user-level Copilot directory, not in the target repository:

```text
~/.copilot/blazor-orchestration/runs/<run_id>/
  events.jsonl
  reports/<agent-id>.json
  analysis.json
  reports/self-improvement-report.html   ← only with --self-improve
```

Rationale: keeping artifacts out of the target repo eliminates `.gitignore` maintenance, prevents accidental commits of potentially sensitive run data, and means the same run directory structure works regardless of which repository the orchestrator is operating on. The trade-off — runs from different projects intermix under the same user directory — is acceptable because `run_id` already encodes the timestamp and the `dispatch` event records the repository context.

**Who writes what**

The orchestrator owns all writes. Specialists never write directly to the telemetry directory — they return their report JSON in their response (as per ticket 04), and the orchestrator writes it to `reports/<agent-id>.json`. The orchestrator appends to `events.jsonl` on every meaningful state transition. This keeps file-write responsibility in one place and avoids races when lanes run in parallel.

**Mandatory versus best-effort events**

| Category | Status | Rationale |
|---|---|---|
| `dispatch` | Mandatory | Every delegated lane must be traceable; no dispatch = no telemetry |
| `completion` | Mandatory | Run outcome is uninterpretable without it |
| `analysis` | Mandatory | End-of-run summary is the primary artifact for self-improvement |
| `handoff` | Mandatory when it occurs | Any lane transition must be recorded; omit only if no handoff happened |
| `tool_use` | Best-effort | Valuable for token analysis but the harness doesn't always surface tool names in a parseable form |
| `feedback` | Best-effort | Only emitted when a specialist explicitly raises an issue; no issue = no event |
| `tool_missed` | Conditional (see below) | |

**`tool_missed` trustworthiness**

Record `tool_missed` only when the specialist explicitly identifies it in `self_diagnosis.issues` under `--self-improve`. Do not infer it from silence or from observed tool choices. When recorded, mark `severity` as `"advisory"` and never block orchestration or fail the run on it. Rationale: the hypothesis is derived from the specialist's own assessment, not from ground-truth observation, so it carries inherent uncertainty. Labelling it advisory keeps it visible for self-improvement analysis without overstating confidence.

**Retention and cleanup**

No automated purge in the initial implementation. Runs accumulate under `~/.copilot/blazor-orchestration/runs/`. The orchestrator prints the run directory path in its closing line so the user can locate and delete runs manually. A cleanup command (`--purge-runs-older-than <days>`) is explicitly deferred to a later effort; recording it here as a known gap rather than an out-of-scope ruling, because the self-improvement sibling map may depend on querying accumulated runs.

## Question

Is telemetry JSON Lines, one JSON artifact per run, or both — and who writes it?

Decide:

- The artifact format and directory layout, including whether artifacts live in the target repository
  or in a user-level location.
- Whether the orchestrator writes all events, or specialists append their own.
- Which of the seven event categories are mandatory versus best-effort.
- Whether the `tool_missed` hypothesis is trustworthy enough to record, and on what evidence.
- The retention and cleanup story.
