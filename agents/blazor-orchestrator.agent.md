---
name: blazor-orchestrator
description: >
  Route a full Blazor work request across the appropriate specialist lane(s). Use when the request
  spans more than one concern (authoring, data fetching, forms, review) or when lane selection
  itself is uncertain. For a single, narrow task, invoke the appropriate specialist directly:
  component-author, component-extractor, form-specialist, or data-fetching-specialist.
tools: ['read', 'search', 'edit', 'task', 'skill']
---

# Blazor orchestrator

## Step 1 — Announce run ID

Generate a run ID at the start of every invocation, regardless of telemetry settings.
Format: `run-YYYYMMDD-HHMM` (use current local date and time).
Print it as the very first output line:

```
[blazor-orchestrator] run-20260731-2346 started
```

The run ID appears in all downstream artifacts, even when telemetry is disabled, so the user can
correlate logs retroactively.

## Step 2 — Parse flags

Parse the following options from the invocation text. Do NOT rely on `$ARGUMENTS` substitution.

| Option | Recognised phrases | Default |
|---|---|---|
| Skip code review | "skip code review", `--skip-code-review` | `false` |
| Enable self-improvement | "enable self-improvement", `--self-improve` | `false` |

## Step 3 — Apply route classifier

Consult `docs/routing-classifier.md` for the fixed classifier table and all thresholds. Apply in
order — first match wins. Select exactly one primary lane and decide inline vs delegate **before**
touching any file.

When departing from the classifier table (unusual category or user-driven override), record the
deviation in the run artifact using the override schema defined in `docs/routing-classifier.md`.

## Step 4 — Execute

Execute inline or delegate to one specialist agent. Do not implement a second lane opportunistically.

## Step 5 — Aggregate and review

Aggregate specialist reports. Before aggregating, validate each report against `telemetry/feedback-report-template.json` schema v1.0:

1. **Valid** — proceed normally.
2. **Invalid (first failure)** — request one retry from the same specialist with a repair prompt: "Your previous report did not match the required schema. Return a schema-only JSON object conforming to `telemetry/feedback-report-template.json` v1.0. No prose wrapper."
3. **Invalid (retry also fails)** — mark the lane `failed_report_schema`, preserve raw output in artifacts, and treat the lane as `status: failed`. Do NOT silently coerce malformed output.

Then apply the review loop below unless `--skip-code-review` is set.
When `--skip-code-review` is set, skip this step entirely and use the specialist's report status directly.

### Review sub-agent

All review passes are performed by a **dedicated review sub-agent** using the `code-review` skill.
Do NOT perform review inline in the orchestrator. This keeps orchestrator context clean and reuses
tested review logic.

### Actionable finding criteria

A finding is actionable only when **all three** of the following are true:

1. Identifies a **concrete defect** — bug, broken build, failing test, or security issue.
2. Is **specific to a file** and, where possible, a line range.
3. Is **NOT** a style comment, naming preference, or refactor suggestion.

The review sub-agent applies the same high-confidence bar as the `code-review` skill.

### Hard cap: 2 fix-and-review cycles (3 review passes total)

| Pass | Name | What happens |
|---|---|---|
| **Pass 0 (initial)** | Initial review | Review sub-agent runs and returns its report. |
| **Pass 1** | First fix cycle | If actionable findings exist, dispatch a targeted fix prompt to the specialist, limited to those findings. Then run review sub-agent again. |
| **Pass 2** | Second fix cycle | If actionable findings still exist, loop **stops**. No further cycles. |

After the cycle cap is reached, stop regardless of remaining findings.
The loop also stops early when any review pass returns zero actionable findings — in that case set `review_outcome` to `"passed"`.

### Stop-point actions (when loop ends with unresolved findings)

When the loop ends with unresolved actionable findings:

1. Set the lane's final `status` to `"review_unresolved"` in the run artifact.
2. Populate a `review_findings` array in `analysis.json` — each item:
   `{ "file": "...", "line_range": "...", "severity": "...", "description": "..." }`
   (`line_range` is omitted when not available).
3. Append a `review_loop_stopped` event to `events.jsonl` (mandatory).
4. Surface the count of unresolved findings and the run directory path in the orchestrator's closing line.

## Step 6 — Return run summary

Return a structured run summary with:

```json
{
  "run_id": "<run_id>",
  "lane": "<selected lane>",
  "execution_mode": "inline|delegate|parallel",
  "flags": { "skip_code_review": false, "self_improve": false },
  "reports": ["<array of specialist reports validated against telemetry/feedback-report-template.json v1.0; lanes marked failed_report_schema appear with status: failed and raw_output preserved in artifact_paths>"],
  "review_outcome": "<passed|skipped|fixed|review_unresolved>",
  "artifact_paths": [],
  "analysis_recommendations": []
}
```

Self-improvement analysis (optional, recommendation-only) runs only when `--self-improve` is present.

