# Orchestrator invocation surface and flags

Type: grilling
Status: resolved
Assignee: @gvdvenis
Blocked by: 01, 10

## Question

How is the orchestrator skill invoked, and how do `--skip-code-review` and `--self-improve` reach it?

Decide:

- The skill name and its trigger description, and how it avoids colliding with the existing
  `blazor-component-architect` routing skill — supersede it, wrap it, or keep both with a clear boundary.
- Whether flags are parsed from free-form arguments, natural language, or both.
- What happens when the user invokes a specialist directly, bypassing the orchestrator.
- Whether a run identifier is always created, or only when telemetry is enabled.

## Answer

**Skill name and collision boundary**

The orchestrator skill is named **`blazor-orchestrator`** (plugin-scoped, no collision with
`blazor-component-architect`). The two coexist with distinct trigger surfaces:

- `blazor-orchestrator` triggers on full-request phrasing — "implement this feature", "review and
  refactor this page", "build this form end-to-end" — where lane-splitting and multi-specialist
  routing is the job.
- `blazor-component-architect` (user-level, external to this plugin) remains the authoring specialist
  skill that the orchestrator and its specialists may invoke as guidance; it is not wrapped or
  superseded.

The trigger description for `blazor-orchestrator` should explicitly exclude single-skill requests:
"Route a full Blazor work request across the appropriate specialist lane(s). Use when the request
spans more than one concern (authoring, data, auth, review) or when lane selection itself is
uncertain. For a single, narrow authoring task, invoke the component or data specialist directly."

**Flag and option parsing**

`$ARGUMENTS` is not a verified harness contract. Options are expressed in **natural language** as
the canonical form; the skill prompt also matches the shorthand aliases to reduce friction:

- "skip code review" or `--skip-code-review` → suppress the review lane
- "enable self-improvement" or `--self-improve` → opt in to the self-improve step (off by default)

The skill must not depend on structured flag-parsing infrastructure. If neither phrase is present,
code review runs and self-improvement is skipped.

**Direct specialist invocation**

Invoking a specialist directly is tolerated but unsupported. Each specialist opens its system prompt
with a notice: "This specialist is designed to be invoked by the `blazor-orchestrator` skill. Direct
invocation produces no run record and no telemetry." The specialist still does its work (its
allowlists already cap blast radius), but no run ID, routing log, or report envelope is emitted.
No recovery path is built in — the user accepts a partial result.

**Run identifier lifecycle**

A run ID is **always created** at orchestrator invocation, regardless of whether telemetry is
enabled. Format: a short timestamp slug, e.g. `run-20260731-1555`. The ID appears in the
orchestrator's opening line so the user has it in the conversation. The telemetry artifact is only
written to disk when telemetry is enabled — but because the ID is always surfaced, a user can opt
in retroactively without losing the correlation key.
