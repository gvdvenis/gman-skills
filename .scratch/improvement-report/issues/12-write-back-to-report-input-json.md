# Write-back to improvement-report-data.json

Type: grilling
Status: resolved
Assignee: @copilot
Blocked by: 08b, 09, 06

## Question

Ticket 09 deferred the shape and scope of any write-back to `report-input.json`.
Ticket 06 made it the durable committed artifact. Ticket 08b permitted user-initiated server
write-back via `POST /api/dismissals`. This ticket decides what is written back and where.

## Answer

**`report-input.json` is renamed to `improvement-report-data.json` and promoted to a living
document** — it starts as orchestrator output and accumulates user decisions and the shipped
prompt. It is staged after `POST /api/ship-prompt` as the committed decision document for the run.

### Why the rename

The file is no longer input-only. It spans the full lifecycle: diagnosis (orchestrator-written)
→ user decisions (server-written) → shipped prompt (server-written). `improvement-report-data.json`
reflects that scope without implying it is consumed read-only.

### Full schema (schema_version 1.1)

```json
{
  "schema_version": "1.1",
  "generated_at": "<ISO timestamp>",
  "origin": {
    "skill_id": "blazor-component-architect",
    "skill_scope": "repo | user",
    "skill_path": "C:/Users/g.vd.venis/.agents/skills/blazor-component-architect",
    "repo_root": "C:/Users/g.vd.venis/.copilot/packages/blazor-orchestration-package",
    "run_id": "2026-07-31T16:06:00Z-abc123"
  },
  "findings": [ ],
  "decisions": {
    "<finding-id>": {
      "action": "queued | dismissed | skipped",
      "dismissed_reason": "optional string",
      "decided_at": "<ISO timestamp>"
    }
  },
  "shipped_prompt": {
    "readable": "...",
    "transformed": "...",
    "shipped_at": "<ISO timestamp>"
  }
}
```

### Origin block

Written by the orchestrator at generation time. Disambiguates user-level vs repo-level installs
of the same skill (`skill_scope`), and records the absolute on-disk `skill_path` so that any
shipped prompt is self-contained — a new Copilot session started from an arbitrary location can
resolve the skill without relying on ambient context.

`skill_scope: "repo"` takes precedence over `"user"` when both exist at the same `skill_id`.

### Findings block

Immutable after orchestrator writes it. Schema is unchanged from ticket 01.

### Decisions block

Additive. Written by the C# server in response to user gestures:
- `POST /api/dismissals` → sets `action: "dismissed"` on the targeted finding.
- `POST /api/ship-prompt` → sets `action: "queued"` on all findings in the shipped queue
  (if not already decided).

The CLI-owned cross-run history file (ticket 05) remains a separate store. `POST /api/dismissals`
writes to **both**: the per-run `decisions` block here, and the CLI history for cross-run
`suggestion_key` deduplication. The two stores are complementary, not redundant.

### Shipped prompt block

Written by the server when `POST /api/ship-prompt` completes. Stores both the browser-assembled
readable prompt and the server-transformed output. Makes the file self-sufficient for re-parse:
a later run can restore the report to the exact state the user left it in, including the prompt
that was shipped.

### Self-contained prompts

The browser reads the `origin` block from `GET /api/report` and injects it as a preamble when
assembling the readable prompt in-browser. Both copy paths — "Copy readable" and "Copy for LLM"
(via `/api/ship-prompt`) — therefore produce location-aware, self-contained prompts usable as
drop-ins on any agent or Copilot instance.

### Write ownership and atomicity

The C# server owns all post-generation writes to `improvement-report-data.json`, using an atomic
temp-rename pattern. The CLI writes only the initial file at run time. No coordination layer needed.

### Machine-readable readiness signal

The presence of a non-empty `decisions` block or a `shipped_prompt` block is a deterministic
signal to the CLI that the user has acted on the report and the artifact is ready to stage and
commit. This resolves the ambiguity in the `--self-improve` working-tree-not-empty flow: the CLI
checks this file rather than prompting blindly.

## Depends on

- [CLI callback vs copy-out boundary](08-cli-callback-vs-copy-out-boundary.md) — POST /api/dismissals is the trigger
- [Local server delivery model](09-local-server-delivery-model.md) — server is the write path
- [Report scope: per-run vs rolling](06-report-scope-per-run-vs-rolling.md) — durable committed artifact contract


Type: grilling
Status: resolved
Assignee: @copilot
Blocked by: 08b, 09, 06

## Question

Ticket 09 explicitly deferred the shape and scope of any write-back to `report-input.json`.
Ticket 06 made `report-input.json` the durable committed artifact and the source of traceability.
Ticket 08b permitted user-initiated server write-back via `POST /api/dismissals`.

This ticket decides what, if anything, the server writes back into `report-input.json` itself
(as opposed to separate history/dismissal storage):

- Which user actions, if any, should mutate `report-input.json` in the run directory:
  dismissals, queue selections, notes, or none?
- Whether mutations are appended as an audit trail (e.g. `"decisions": [...]` on a finding) or
  rewrite the finding in place.
- How this interacts with the git staging model from ticket 06: if `report-input.json` is
  committed alongside implementation, does a post-decision write-back create a messy diff, or
  is that acceptable?
- Whether write-back to `report-input.json` is distinct from the dismissal history owned by
  the CLI layer (ticket 05) — i.e. are there two separate stores, or one?
- Whether the C# server is responsible for atomic file writes (temp-rename pattern) or whether
  the CLI layer mediates all file mutations.
- What the scope of write-back is for this effort: decision traceability only, full audit log,
  or nothing (history stays in CLI-only store).

## Depends on

- [CLI callback vs copy-out boundary](08-cli-callback-vs-copy-out-boundary.md) — POST /api/dismissals is the trigger; this ticket decides what it persists and where
- [Local server delivery model](09-local-server-delivery-model.md) — server is the write path
- [Report scope: per-run vs rolling](06-report-scope-per-run-vs-rolling.md) — report-input.json is the committed durable artifact; write-back must not destabilise that contract
