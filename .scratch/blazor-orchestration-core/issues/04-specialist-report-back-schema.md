# Specialist report-back schema

Type: prototype
Status: resolved
Assignee: @gvdvenis

## Question

What exactly does a specialist return to the orchestrator?

Produce a concrete example report for one realistic extraction task to react to, then lock the schema.

Decide:

- Required versus optional fields, and whether the report is JSON, fenced JSON in prose, or markdown.
- How files changed and validation performed are represented.
- Whether a token estimate is honest enough to be worth collecting.
- How `self_diagnosis` is gated so it costs nothing when `--self-improve` is off.
- What the orchestrator does with a malformed or missing report.

## Answer

Lock report-back to a strict, single JSON object (no prose wrapper) so the orchestrator can parse it
deterministically.

Required fields:

- `schema_version` (string, starts at `"1.0"`)
- `specialist` (string lane name)
- `status` (`"success" | "blocked" | "failed"`)
- `summary` (1-3 sentence plain-language result)
- `files_changed` (array; empty allowed) where each item has:
  - `path` (repo-relative string)
  - `change` (`"created" | "updated" | "deleted" | "none"`)
  - `reason` (short string)
- `validation` object:
  - `performed` (boolean)
  - `commands` (array of strings, empty when not performed)
  - `result` (`"pass" | "fail" | "not_run"`)
  - `evidence` (short string; include first failure signal when `fail`)
- `next_action` (string; explicit handoff or unblock step)

Optional fields:

- `token_estimate` object (`input`, `output`, `total` integers). Keep as coarse estimates (nearest
  100 tokens) and treat as advisory, not billing-grade.
- `self_diagnosis` object, emitted **only** when `--self-improve` is on:
  - `issues` (array of short strings)
  - `improvements` (array of short strings)
  - `confidence` (`"low" | "medium" | "high"`)

Malformed or missing report handling:

1. Orchestrator validates against this schema.
2. On first failure, request one retry from the same specialist with a "schema-only JSON" repair prompt.
3. If retry fails, mark lane `failed_report_schema`, keep raw output in local run artifacts, and continue
   orchestration using `status: failed` semantics (do not silently coerce).

Concrete example (extraction task):

```json
{
  "schema_version": "1.0",
  "specialist": "blazor-component-architect",
  "status": "success",
  "summary": "Extracted OrderSummaryCard and PaymentMethodPicker from Checkout.razor and replaced inline markup with parameterized components.",
  "files_changed": [
    {
      "path": "src/Web/Pages/Checkout.razor",
      "change": "updated",
      "reason": "Replaced duplicated markup with component usage."
    },
    {
      "path": "src/Web/Components/OrderSummaryCard.razor",
      "change": "created",
      "reason": "New reusable summary display component."
    },
    {
      "path": "src/Web/Components/PaymentMethodPicker.razor",
      "change": "created",
      "reason": "New reusable payment method selector."
    }
  ],
  "validation": {
    "performed": true,
    "commands": [
      "dotnet build src/Web/Web.csproj",
      "dotnet test tests/Web.Tests/Web.Tests.csproj --filter Checkout"
    ],
    "result": "pass",
    "evidence": "Build succeeded; 6/6 Checkout tests passed."
  },
  "next_action": "Orchestrator can proceed to telemetry artifact generation and final user handoff.",
  "token_estimate": {
    "input": 2200,
    "output": 900,
    "total": 3100
  }
}
```

## Comments

- Resolved: specialist report-back is strict JSON with retry-on-parse-failure, optional coarse token
  estimates, and `self_diagnosis` emitted only under `--self-improve`.
