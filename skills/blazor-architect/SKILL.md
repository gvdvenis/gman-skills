---
name: blazor-architect
user-invokable: true
description: >
  Route a full Blazor work request across the appropriate specialist lane(s). Use when the request
  spans more than one concern (authoring, data, auth, review) or when lane selection itself is
  uncertain. Triggers on full-request phrasing: "implement this feature", "review and refactor this
  page", "build this form end-to-end". Distinct from blazor-component-architect (user-level, external,
  single-lane authoring guidance that may be invoked as a specialist resource).
---

# Blazor architect skill

## Step 1 — Announce the run

Generate a run ID (`run-YYYYMMDD-HHMM`, local time) and print it as the first output line:

```
[blazor-architect] run-20260802-1716 started
```

**Done when:** the run ID line is printed at the top of your response.

## Step 2 — Parse the work request and flags from the invocation text

The user's task description is embedded in the invocation text — the same line they typed to fire
the skill (e.g. `/blazor-architect implement a counter component in Components/Pages/Counter.razor --self-improve`).
Extract two things:

**The work request** — the task description, stripped of the skill name and flags. In the example
above, the work request is: "implement a counter component in Components/Pages/Counter.razor".

**Flags** — scanned from the invocation text:

| Flag | Recognised | Default |
|---|---|---|
| Skip code review | `--skip-code-review` or "skip code review" | off (review runs) |
| Self-improvement | `--self-improve` or "self improve" | off |

If no work request is found (the user typed just `/blazor-architect` with no task), ask what they
want to do and stop — do not proceed to routing.

**Done when:** you have identified the work request text and set `skip_code_review` and `self_improve` booleans.

## Step 3 — Route the work

Apply the route classifier in `references/routing-classifier.md` to the work request. The classifier
picks a primary lane and decides inline vs delegate. Print a one-line routing summary so the user
sees the decision:

```
Route: inline (component-author, 1 file)
```

or

```
Route: delegate (component-author → data-fetching-specialist, serial)
```

**Done when:** a lane is selected and the inline/delegate decision is made and announced.

## Step 4 — Execute the work

### Inline execution

For narrow tasks (single lane, ≤ 2 files, no broad repo discovery), do the work directly in this
context. Use the dotnet-blazor plugin skills as guidance — read the relevant skill's SKILL.md for
patterns and constraints before writing code.

### Delegated execution

For broader tasks, dispatch via the `task` tool. Each specialist lane receives a bounded task for
one lane. Specialist lanes:

| Lane | Scope | Guidance skill | Delegate? |
|---|---|---|---|
| component-author | Create a new component, parameters, lifecycle, CSS isolation | `dotnet-blazor:author-component` | Inline if ≤ 2 files |
| component-extractor | Extract sections from a page into reusable components | — | Always delegate |
| form-specialist | Forms, binding, validation, EditForm, @bind | `dotnet-blazor:collect-user-input` | Inline if 1–2 files |
| data-fetching-specialist | HttpClient, service abstractions, loading/error/empty states | `dotnet-blazor:fetch-and-send-data` | Always delegate |

Each specialist must return a structured report matching `references/feedback-report-template.json`.
On first validation failure, request one schema-repair retry; on second failure, mark the lane
`failed_report_schema` and preserve raw output.

### Fluent UI

Fluent UI is a cross-lane constraint, not a lane. When Fluent components are in scope, invoke the
`fluentui-blazor` skill as an overlay alongside the active specialist.

**Done when:** all specialist work is complete and reports are validated. Every specialist report
conforms to the feedback-report schema.

## Step 5 — Review gate

Check the `skip_code_review` flag that was parsed in step 2. This is a gate — make the decision
before dispatching any review sub-agent.

**When `skip_code_review` is true:** do not spawn a review sub-agent. Set `review_outcome` to
`"skipped"`. Proceed to step 6.

**When `skip_code_review` is false:** run the review loop — a dedicated review sub-agent via the
`task` tool with `agent_type: "code-review"`. The review loop:

- Maximum 3 passes (initial + 2 fix-and-review cycles).
- Stop early when a pass returns zero actionable findings.
- Actionable = concrete defect, file-specific, not stylistic. See `references/review-loop-contract.md`.

The decision is made here, not during execution — the review sub-agent is only dispatched when the
flag was not set, and only after all specialist work is complete.

**Done when:** review is complete or skipped. Either zero actionable findings remain, or
unresolved findings are recorded for the run summary.

## Step 6 — Write run artifacts

Write `analysis.json` to `~/.self-improve-reports/blazor-architect/runs/<run_id>/` containing:

- `run_id`, `final_status` (precedence: failed > blocked > partial > success)
- `status_counts`, `lane_outcomes[]`
- `review_outcome` (passed / fixed / review_unresolved / skipped)
- `follow_up_items[]` when status is partial or blocked

Create the run directory if it does not exist.

**Done when:** `analysis.json` is written to the run directory.

## Step 7 — Self-improvement (only when `--self-improve` is set)

When `--self-improve` is active, load the `self-improve` skill and execute its steps. The
self-improve skill generates the improvement report and launches the report-server. Follow
self-improve's steps exactly — do not inline its algorithm here.

When `--self-improve` is **not** set, skip this step entirely.

**Done when:** self-improve steps are complete (report generated, server launched) OR the flag
was not set.

## Step 8 — Print the run summary

Close every run with a summary block. This is the user's primary feedback — make it informative:

```
[blazor-architect] run-20260802-1716 complete
  lanes:     component-author
  status:    success
  review:    passed
  files:     src/Konqvist.Web/Components/Pages/Counter.razor (created)
  artifacts: ~/.self-improve-reports/blazor-architect/runs/run-20260802-1716/analysis.json
  follow-up: none
```

When `--self-improve` was active, also include:

```
  report:    ~/.self-improve-reports/blazor-architect/runs/run-20260802-1716/improvement-report-data.json
  server:    http://127.0.0.1:5173/api/report (running)
```

When `final_status` is `partial` or `blocked`, surface blocked/failed lane names and recommended
next actions.

**Done when:** the summary block is printed with all fields populated.
