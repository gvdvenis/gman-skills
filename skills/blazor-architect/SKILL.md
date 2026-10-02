---
name: blazor-architect
user-invocable: true
description: >
  Routes a full Blazor work request across the appropriate specialist lane(s). Use when a Blazor
  request spans more than one concern (authoring, data, auth, review) or when lane selection itself
  is uncertain. Triggers on full-request phrasing in a Blazor project: "implement this Blazor
  feature", "review and refactor this page", "build this form end-to-end". For a single lane,
  invoke that lane's guidance skill directly.
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
context. **Before writing any Blazor code**, invoke the plugin skill that matches the active lane
(see the specialist lane table below).

### Delegated execution

For broader tasks, dispatch sub-agents (the `task` tool in Copilot CLI, the `Agent` tool in
Claude Code). Each specialist lane receives a bounded task for
one lane. Specialist lanes:

| Lane | Scope | Guidance skill |
|---|---|---|
| component-author | Create a new component, parameters, lifecycle, CSS isolation | `author-component` |
| component-extractor | Extract sections from a page into reusable components | `blazor-component-architect` |
| form-specialist | Forms, binding, validation, EditForm, @bind | `collect-user-input` |
| data-fetching-specialist | HttpClient, service abstractions, loading/error/empty states | `fetch-and-send-data` |

Whether a lane runs inline or delegated comes from the classifier in step 3.

#### How to invoke the guidance skills

When the active lane matches a guidance skill in the table above, invoke it by name before writing
or delegating code. For inline execution, invoke the skill in this context. For delegated
execution, include the skill name in the specialist's task prompt so the sub-agent invokes it in
its own context.

The guidance skills come from the `dotnet-blazor` plugin. Copilot CLI lists them as
`author-component`, `collect-user-input`, `fetch-and-send-data`; Claude Code lists them with the
plugin prefix, e.g. `dotnet-blazor:author-component`. The
`blazor-component-architect` skill (user-level) covers extraction/refactoring work. The
`fluentui-blazor` skill (user-level) covers Fluent UI component usage. Invoke them the same way —
by name.

Why this matters: each guidance skill encodes lifecycle ordering, parameter constraints,
render-mode boundaries and anti-patterns. Without it, code tends to look right but break at
runtime.

If a guidance skill is not in the list of available skills, do not guess its content: continue
without it, and end the final output with this line for each missing `dotnet-blazor` skill:

```
[blazor-architect] Guidance skill <name> not available. Run /setup-gman-skills to install the dotnet-blazor plugin.
```

Each specialist must return a structured report matching `references/feedback-report-template.json`
(filled-in example: `references/feedback-report-example.json`).
On first validation failure, request one schema-repair retry; on second failure, mark the lane
`failed_report_schema` and preserve raw output.

### Fluent UI

Fluent UI is a cross-lane constraint, not a lane. When Fluent components are in scope, invoke the
`fluentui-blazor` skill as an overlay alongside the active specialist. Invoke it the same way as
the lane guidance skills — by name, before writing Fluent component markup. The skill covers
provider setup, component-specific properties, JS interop pitfalls, and theming — all of which
are easy to get wrong from memory.

**Done when:** all specialist work is complete and reports are validated. Every specialist report
conforms to the feedback-report schema.

## Step 5 — Review gate

Run this step only after all specialist work is complete.

**When `skip_code_review` is true:** spawn no review sub-agent. Set `review_outcome` to
`"skipped"` and go to step 6.

**Otherwise:** run the review loop in `references/review-loop-contract.md`. It caps the passes,
defines an actionable finding, and sets `review_outcome`. The review sub-agent runs the
`code-review` skill: Claude Code lists it as `mattpocock-skills:code-review`.

**Done when:** review is complete or skipped. Either zero actionable findings remain, or
unresolved findings are recorded for the run summary.

## Step 6 — Write run artifacts

Write `analysis.json` to `~/.self-improve-reports/blazor-architect/runs/<run_id>/`, conforming to
`references/analysis-schema.json`, containing:

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
  files:     Components/Pages/Counter.razor (created)
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
