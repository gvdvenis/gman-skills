## Unresolved Contradictions in Normative Sources

1. `01-report-input-contract.md` says finding `severity` is derived from per-dimension `self_diagnosis.scores` and points to a core schema amendment, but ticket 14 is excluded as non-normative input for this effort. The report contract therefore depends on a severity value being present in `improvement-report-data.json`, while the upstream derivation mechanism remains outside this spec's normative scope.

## Problem Statement

Users need a local, privacy-safe way to review run weaknesses, choose which improvements to act on, and ship a ready prompt in both human-readable and LLM-compressed forms. Current decisions must be normalized into one implementation-ready report system contract without introducing auto-mutation behavior.

## Solution

Provide a per-run local C# server experience auto-launched by `--self-improve`, serving report data from `~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json`, enabling severity-grouped queue-based prompt assembly in HTML, and supporting explicit user-triggered write-back via `POST /api/dismissals` and `POST /api/ship-prompt`.

## User Stories

1. As a developer, I want self-improvement only when requested, so that normal runs stay unchanged.
2. As a developer, I want findings grouped by severity, so that I can prioritize quickly.
3. As a developer, I want one-click add/remove queue actions, so that prompt assembly is fast.
4. As a developer, I want a live readable prompt workspace, so that I can inspect what will be shipped.
5. As a developer, I want explicit edit mode with rebuild control, so that manual edits are intentional and reversible.
6. As a developer, I want both readable and compressed outputs, so that I can use the right copy for each context.
7. As a developer, I want compressed output to preserve semantics, so that I do not lose improvement intent.
8. As a developer, I want deterministic compression in MVP, so that behavior is stable and local.
9. As a developer, I want dismissals remembered across runs, so that noisy repeats are reduced.
10. As a developer, I want stable suggestion identity, so that history weighting is explainable.
11. As a developer, I want cross-run duplicates collapsed, so that recurring issues are visible without clutter.
12. As a developer, I want recurrence influence bounded, so that one weak signal does not dominate ranking.
13. As a developer, I want mobile support at >=375px, so that I can triage from a phone.
14. As a developer, I want a stable URL for phone access, so that I do not rescan each run.
15. As a developer, I want server-side desktop clipboard write for LLM copy, so that second-device flow is practical.
16. As a developer, I want browser-side readable copy, so that fast local copy remains simple.
17. As a developer, I want per-run durable report data committed with code changes, so that intent and implementation stay linked.
18. As a developer, I want user actions written atomically, so that committed report state is reliable.
19. As a developer, I want explicit server lifecycle rules, so that report access is predictable.
20. As a developer, I want local-only delivery and no automatic execution side effects, so that privacy and control are preserved.

## Scope

- `--self-improve` report generation and local server delivery model.
- Report data contract (`improvement-report-data.json`) and HTML interaction model.
- Dual-output prompt behavior (readable + compressed).
- Suggestion history weighting and cross-run dedup by `suggestion_key`.
- Mobile/small-screen behavior and fixed-port server usage.
- User-initiated write-back API boundaries and persistence responsibilities.

## Out of Scope

- Automatic prompt application to agents/skills.
- Any non-local hosting path.
- Model-assisted semantic compression in MVP.
- Rolling cross-run dashboard beyond per-run committed artifacts.
- Environments without required local .NET runtime.

## Architecture and Component Boundaries

- **Orchestrator/CLI layer**:
  - Generates initial `improvement-report-data.json`.
  - Owns cross-run suggestion history store and deterministic scoring weights.
  - Stages the per-run data file after actionable user decisions/shipped prompt.
- **C# server layer (single process)**:
  - Serves API and static report shell.
  - Owns all post-generation writes to `improvement-report-data.json` (atomic temp-rename).
  - Runs prompt-shipping middleware chain for LLM copy path.
- **HTML/JS report layer**:
  - Displays findings and queue interactions.
  - Assembles readable prompt in-browser.
  - Uses browser clipboard for readable copy.
  - Calls server for dismissals and LLM copy/ship flow.

## Data Contracts and Schemas

- **Run directory**: `~/.copilot/blazor-orchestration/runs/<run_id>/`
- **Primary report data file**: `improvement-report-data.json` (schema version `1.1`)
- **Top-level shape**:
  - `schema_version`
  - `generated_at`
  - `origin` (`skill_id`, `skill_scope`, `skill_path`, `repo_root`, `run_id`)
  - `findings[]`
  - `decisions` keyed by finding id (`action`, optional `dismissed_reason`, `decided_at`)
  - `shipped_prompt` (`readable`, `transformed`, `shipped_at`)
- **Finding shape**:
  - `id`, `specialist`, `title`, `summary`, `category`, `severity`, `expected_impact`, `prompt_fragment`, `evidence`
- **Category enum**:
  - `tool_use`, `planning`, `output_quality`, `validation`, `communication`
- **Evidence**:
  - Pointer form (`specialist`, `issue_index`) and/or merged evidence unions from dedup flow.
- **API endpoints (final names)**:
  - `GET /api/report`
  - `GET /ping`
  - `POST /api/ship-prompt`
  - `POST /api/dismissals`
  - `GET /shutdown`

## Runtime Flow and Lifecycle

1. User enables `--self-improve`; server auto-launches on fixed port (default `5173`).
2. Browser opens report shell and fetches data from `GET /api/report`.
3. User triages findings (severity-grouped cards) and builds queue.
4. Browser assembles readable prompt (including origin preamble).
5. `Copy prompt (readable)` writes via browser clipboard.
6. `Copy for LLM` posts readable prompt to `POST /api/ship-prompt`.
7. Server runs middleware chain: deterministic syntactic compression, persist result, clipboard write, response return.
8. User dismissals call `POST /api/dismissals`, updating per-run decisions and CLI history linkage.
9. Server stays alive after first report load until `GET /shutdown`, idle timeout, or terminal end.

## Error Handling and Fallback Behavior

- Port already bound: assume server already running; warn and continue.
- Fixed-port binding preference:
  - default loopback (`127.0.0.1`)
  - optional all-interfaces (`0.0.0.0`) for mobile access preference.
- Pipeline step failure in `POST /api/ship-prompt`: pass-through unchanged prompt, continue persist + clipboard, return warnings metadata.
- `file://` usage is degraded fallback with banner guidance; full integrated flow requires server path.
- Dismissal and ship writes are additive and idempotent at record level.

## Security and Privacy Constraints

- Local-first: data stays on local machine.
- No automatic mutation of agents/skills.
- User action required for all write-back effects.
- Server surface is local-development scoped (loopback by default; optional broader bind for mobile).
- No browser-to-external-model calls in this scope.

## Packaging, Install, and Update Model

- Report system is delivered as part of package runtime behavior under `--self-improve`.
- Server process and HTML shell are coupled; no separate standalone report binary in this scope.
- Per-run data file is durable and intended for staging/commit alongside implementation changes.
- Superseded endpoint/name contracts are normalized to:
  - `improvement-report-data.json` (not `report-input.json`)
  - `POST /api/ship-prompt` (not `POST /clipboard`)
  - no `--serve` flag; auto-launch via `--self-improve`.

## Validation and Acceptance Criteria

- `--self-improve` run produces accessible report via fixed-port server.
- Report renders severity-grouped findings and supports queue add/remove behavior.
- Readable and compressed copy paths are both available when queue is non-empty.
- `POST /api/ship-prompt` persists `shipped_prompt`, writes clipboard, and returns transformed payload.
- `POST /api/dismissals` updates per-run decisions and integrates with cross-run history behavior.
- `improvement-report-data.json` accumulates origin, decisions, and shipped prompt deterministically.
- Mobile path is fully interactive at >=375px with polling-driven auto-reload behavior.

## Implementation Decisions

- Canonical interaction model is severity-grouped card list plus live prompt workspace (Variant A).
- Prompt factory remains primary purpose; review is navigation to prompt selection.
- Per-run artifact model is preserved; no rolling aggregate in scope.
- Cross-run dedup happens CLI-side by `suggestion_key` before report presentation.
- Recurrence produces bounded additive ranking boost and does not override dismissal penalty.
- C# server is the sole delivery and post-generation write surface.
- `Copy readable` stays browser-side; `Copy for LLM` is server pipeline path.

## Testing Decisions

- Good tests verify user-visible behavior and persisted contract outputs rather than DOM internals.
- Test seams:
  1. CLI report-data generation and dedup/ranking transforms.
  2. C# API contract behavior for `/api/report`, `/api/dismissals`, `/api/ship-prompt`.
  3. Atomic write behavior of `improvement-report-data.json`.
  4. HTML queue/edit/copy flows and responsive behavior at >=375px.
  5. Port-conflict and lifecycle behavior around startup/shutdown/idle timeout.
- Prior-art-aligned approach is contract-first validation with deterministic payload inspection.

## Implementation Plan (Ordered Slices)

1. Finalize `improvement-report-data.json` generation (including `origin` and finding contract fields).
2. Implement fixed-port server startup path under `--self-improve` with lifecycle and `/ping`.
3. Implement report HTML shell with severity grouping, queue, edit mode, and dual copy controls.
4. Implement `POST /api/dismissals` and atomic decisions write-back.
5. Implement `POST /api/ship-prompt` middleware chain, persistence, clipboard write, and warning return.
6. Wire CLI staging/readiness signal from `decisions`/`shipped_prompt`.
7. Validate mobile/small-screen interactions and stable QR-based access flow.
8. Normalize docs and contract fixtures to final endpoint/file naming.

## Risks and Open Assumptions

- Severity derivation algorithm upstream remains dependent on excluded ticket-14 scope; this spec assumes severity arrives correctly in report data.
- All-interfaces binding may trigger first-run firewall prompts on Windows by design.

## Further Notes

- Superseded names and flags remain historical context only; implementation must follow the normalized final contract in this spec.
