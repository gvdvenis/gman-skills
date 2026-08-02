---
name: blazor-architect
user-invokable: true
description: >
  Route a full Blazor work request across the appropriate specialist lane(s). Use when the request
  spans more than one concern (authoring, data, auth, review) or when lane selection itself is
  uncertain. Triggers on full-request phrasing: "implement this feature", "review and refactor this
  page", "build this form end-to-end". Does NOT trigger for single-skill or narrow lane-specific
  requests. Distinct from blazor-component-architect (user-level, external, single-lane authoring
  guidance that may be invoked as a specialist resource).
---

# Blazor architect skill

## Run ID

Generate a run ID at the very start of every invocation, regardless of telemetry settings.
Format: `run-YYYYMMDD-HHMM` (use current **local** date and time). Announce it in your first output line:

```
[blazor-architect] run-20260731-2346 started
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

- **`blazor-architect`** (this skill): full-request, multi-concern routing and aggregation —
  lane-splitting, handoff, report validation, and run-level status merge is the job. It delegates
  to dotnet-blazor plugin skills as specialist resources; it does not replace them.
- **`blazor-component-architect`** (user-level, external): single-lane Blazor authoring guidance;
  may be invoked as a specialist resource by this orchestrator or by specialists.

## Execution

After parsing flags, apply the route classifier in `references/routing-classifier.md`. The classifier
decides whether to execute inline or delegate to a specialist agent — do not force delegation for
every invocation. Inline execution (single lane, ≤ 2 files, no broad repo discovery) keeps the work
in this context window without spawning a sub-agent.

## Specialist delegation instructions

Each specialist lane is dispatched via the `task` tool. The five lanes below cover all supported
work categories. Each lane report must conform to `references/feedback-report-template.json`
(schema v1.0). On first validation failure, request one schema-repair retry; on second failure,
mark the lane `failed_report_schema` and preserve raw output as a sidecar.

### 1. component-author

- **Scope**: Create a new Blazor component (single component, parameters, lifecycle, CSS isolation).
- **Tool constraints**: Standard editing tools (view, edit, create). No sub-agent spawning.
- **In scope**: Single component authoring, parameter wiring, EventCallback, RenderFragment slots,
  lifecycle methods, code-behind.
- **Out of scope**: Forms/validation (route to form-specialist), data fetching (route to
  data-fetching-specialist), multi-component extraction (route to component-extractor).
- **Delegation guidance**: Use `dotnet-blazor:author-component` as guidance.
- **Inline**: Inline if ≤ 2 files expected and no broad repo discovery needed.
- **Report format**: `feedback-report-template.json` with `specialist: "component-author"`.
- **Completion criteria**: Component file created, build passes, validation evidence included.

### 2. component-extractor

- **Scope**: Extract sections from an existing page into reusable components.
- **Tool constraints**: Standard editing tools. Requires reading + editing several files.
- **In scope**: Identifying duplicated markup, extracting into parameterized components, updating
  parent pages to use new components.
- **Out of scope**: Creating entirely new components not derived from existing markup (route to
  component-author), form validation logic (route to form-specialist).
- **Delegation**: Always delegate — reading and editing several files benefits from isolation.
- **Report format**: `feedback-report-template.json` with `specialist: "component-extractor"`.
- **Completion criteria**: Extracted components created, parent pages updated, build passes.

### 3. form-specialist

- **Scope**: Add or fix forms, data binding, and validation in Blazor components.
- **Tool constraints**: Standard editing tools. No sub-agent spawning.
- **In scope**: EditForm, built-in input components, DataAnnotationsValidator, custom validation,
  @bind, SSR form patterns (SupplyParameterFromForm, FormName, AntiforgeryToken, Enhance).
- **Out of scope**: Component authoring without forms (route to component-author), HTTP data
  fetching (route to data-fetching-specialist), auth/authorization (separate concern).
- **Delegation guidance**: Use `dotnet-blazor:collect-user-input` as guidance.
- **Inline**: Inline if single form, 1–2 files. Delegate when page is complex.
- **Report format**: `feedback-report-template.json` with `specialist: "form-specialist"`.
- **Completion criteria**: Form renders, validation triggers, build passes.

### 4. data-fetching-specialist

- **Scope**: Fetch data from API, register HttpClient, build service abstractions, implement
  loading/error/empty states.
- **Tool constraints**: Standard editing tools. Reading + writing service layer files.
- **In scope**: HttpClient registration, service interfaces, loading/error/empty state UI,
  async lifecycle patterns, Auto/WebAssembly render-mode service abstractions.
- **Out of scope**: Form validation (route to form-specialist), component structure (route to
  component-author), auth configuration (separate concern).
- **Delegation**: Always delegate — service-layer work benefits from isolation.
- **Delegation guidance**: Use `dotnet-blazor:fetch-and-send-data` as guidance.
- **Report format**: `feedback-report-template.json` with `specialist: "data-fetching-specialist"`.
- **Completion criteria**: Service registered, data flows to component, loading/error/empty states
  implemented, build passes.

### 5. review

- **Scope**: Dedicated code-review sub-agent for adversarial review of specialist output.
- **Tool constraints**: Read-only review tools. Uses the `code-review` skill. Never performed
  inline in the orchestrator.
- **In scope**: Bug detection, broken builds, failing tests, security issues — high-confidence
  findings only. See `references/review-loop-contract.md` for actionable-finding criteria.
- **Out of scope**: Style comments, naming preferences, refactor suggestions.
- **Delegation**: Always delegate via the `task` tool with `code-review` agent type.
- **Report format**: Findings array with `{ file, line_range?, severity, description }`.
- **Completion criteria**: Review pass complete; either zero actionable findings or
  unresolved findings persisted for manual follow-up.

### Fluent UI constraint layer

Fluent UI is **not a lane**. It is a cross-lane constraint applied alongside the selected primary
lane whenever Fluent components, providers, or theming are in scope. Invoke the `fluentui-blazor`
skill as an overlay on top of whichever specialist is active.

## Aggregation and report validation

After all specialist lanes complete, validate each report against `references/feedback-report-template.json`:

1. **Schema validation**: Parse JSON, check required fields and types.
2. **Retry**: On first validation failure, send a schema-only JSON repair prompt to the specialist
   (one retry). On second failure, mark the lane `failed_report_schema`, preserve raw output as a
   sidecar file in run artifacts, and treat the lane as `failed`.
3. **Status merge**: Aggregate lane statuses into a run-level `final_status` with precedence:
   `failed` > `blocked` > `partial` > `success`. `failed_report_schema` is treated as `failed`.
   `review_unresolved` is treated as `blocked`.
4. **Run artifact**: Write `analysis.json` with `final_status`, `status_counts`, `lane_outcomes[]`,
   and follow-up items when status is `partial` or `blocked`.

## Review loop

When `--skip-code-review` is not set, the review lane runs with a capped fix-and-review loop.
See `references/review-loop-contract.md` for the complete contract:

- Maximum 3 review passes (initial + 2 fix-and-review cycles).
- Hard stop after cycle 2 or when a pass returns zero actionable findings.
- Unresolved findings set status to `review_unresolved` and emit a `review_loop_stopped` event.
- `--skip-code-review` bypasses the entire loop; `review_outcome` is set to `"skipped"`.

## Run summary

Close every run with a summary block:

```
[blazor-architect] <run_id> complete
  lanes:    <comma-separated lane names>
  status:   <final_status>
  review:   <review_outcome>
  follow-up: <follow-up items or "none">
```

When `final_status` is `partial` or `blocked`, surface blocked/failed lane names and recommended
next actions for the user.

## `--self-improve` hook

When `--self-improve` is active, load the `self-improve` skill after all specialist work and the
review loop have completed. The `self-improve` skill handles report generation (algorithm, dedup,
ranking, dismissal), C# server auto-launch on port 5173, and CLI staging readiness. Do **not**
duplicate the self-improvement algorithm, server lifecycle, or staging logic here — defer all
details to the `self-improve` skill and its reference files.
