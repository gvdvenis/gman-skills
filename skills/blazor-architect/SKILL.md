---
name: blazor-architect
description: Routes a Blazor work request that spans more than one concern (authoring, data, auth, review) across specialist lanes, then reviews the result.
disable-model-invocation: true
argument-hint: "<task> [--skip-code-review] [--self-improve]"
---

# Blazor architect skill

## Step 1 — Announce the run

Print this as the first line, with a run ID in local time:

```
[blazor-architect] run-20260802-1716 started
```

## Step 2 — Parse the invocation

The work request is the invocation text minus the skill name and flags. Flags:

| Flag | Recognised | Default |
|---|---|---|
| Skip code review | `--skip-code-review` or "skip code review" | off (review runs) |
| Self-improvement | `--self-improve` or "self improve" | off |

No work request: ask what the user wants to do and stop.

## Step 3 — Route the work

Apply `references/routing-classifier.md`, then print the decision:

```
Route: inline (component-author, 1 file)
Route: delegate (component-author → data-fetching-specialist, serial)
```

## Step 4 — Execute the work

Inline: do the work in this context. Delegate: one sub-agent per lane (the `task` tool in
Copilot CLI, the `Agent` tool in Claude Code).

Before writing any code for a lane, invoke its guidance skill by name: in this context when
inline, or by naming it in the sub-agent's prompt when delegated.

| Lane | Scope | Guidance skill |
|---|---|---|
| component-author | Create a new component, parameters, lifecycle, CSS isolation | `author-component` |
| component-extractor | Extract sections from a page into reusable components | `blazor-component-architect` |
| form-specialist | Forms, binding, validation, EditForm, @bind | `collect-user-input` |
| data-fetching-specialist | HttpClient, service abstractions, loading/error/empty states | `fetch-and-send-data` |

When Fluent UI components are in scope, also invoke `fluentui-blazor`, whatever the lane.
Claude Code lists the dotnet-blazor skills with a prefix: `dotnet-blazor:author-component`.

When a guidance skill is not in the list of available skills, do not guess its content. Continue
without it, and end the final output with this line for each missing skill:

```
[blazor-architect] Guidance skill <name> not available. Run /setup-gman-skills to install the dotnet-blazor plugin.
```

Each specialist returns a report matching `references/feedback-report-template.json` (example:
`references/feedback-report-example.json`). On a schema failure, ask for one repair. On a second
failure, mark the lane `failed_report_schema` and keep the raw output.

## Step 5 — Review gate

After all lanes finish. With `skip_code_review`: start no review agent and set `review_outcome`
to `"skipped"`. Otherwise run the loop in `references/review-loop-contract.md`. The review
sub-agent runs `code-review` (`mattpocock-skills:code-review` in Claude Code).

## Step 6 — Write analysis.json

Write `~/.self-improve-reports/blazor-architect/runs/<run_id>/analysis.json` to match
`references/analysis-schema.json`. Create the folder when missing.

## Step 7 — Self-improvement

Only with `--self-improve`: load the `self-improve` skill and follow its steps.

## Step 8 — Print the run summary

```
[blazor-architect] run-20260802-1716 complete
  lanes:     component-author
  status:    success
  review:    passed
  files:     Components/Pages/Counter.razor (created)
  artifacts: ~/.self-improve-reports/blazor-architect/runs/run-20260802-1716/analysis.json
  follow-up: none
```

With `--self-improve`, add:

```
  report:    ~/.self-improve-reports/blazor-architect/runs/run-20260802-1716/improvement-report-data.json
  server:    http://127.0.0.1:5173/api/report (running)
```

When `final_status` is `partial` or `blocked`, name the blocked or failed lanes and the next
actions.
