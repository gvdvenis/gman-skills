## Problem Statement

Blazor work requests currently risk inconsistent routing, uneven specialist outputs, and missing run-level traceability when delegation happens ad hoc. The package needs a locked, implementation-ready orchestration contract that preserves token efficiency, keeps telemetry local, and produces deterministic outcomes across runs.

## Solution

Ship a plugin-scoped orchestration skill (`blazor-orchestrator`) that is the documented routing entrypoint, delegates through bounded specialist lanes, enforces deterministic routing/review/aggregation rules, and writes local run artifacts under `~/.copilot/blazor-orchestration/runs/<run_id>/` when telemetry is enabled.

## User Stories

1. As a Blazor developer, I want one orchestration entrypoint, so that multi-concern requests are routed consistently.
2. As a Blazor developer, I want narrow tasks handled inline when cheap, so that token usage stays low.
3. As a Blazor developer, I want broader tasks delegated automatically, so that context stays clean and work quality is stable.
4. As a Blazor developer, I want deterministic routing thresholds, so that similar requests route the same way across runs.
5. As a Blazor developer, I want delegated-lane rationale shown in one sentence, so that I understand why a lane was chosen.
6. As a Blazor developer, I want specialist lanes isolated by tool allowlists, so that delegation policy is enforceable.
7. As a Blazor developer, I want direct specialist use tolerated, so that I can still run focused work intentionally.
8. As a Blazor developer, I want unsupported direct specialist mode clearly disclosed, so that I understand telemetry limitations.
9. As a Blazor developer, I want a run ID surfaced at invocation, so that I can correlate outputs.
10. As a Blazor developer, I want strict specialist JSON report-back, so that aggregation is machine-reliable.
11. As a Blazor developer, I want malformed specialist reports retried once, so that transient formatting mistakes do not fail the run immediately.
12. As a Blazor developer, I want schema retry exhaustion handled explicitly, so that failures are visible and actionable.
13. As a Blazor developer, I want review to run in a dedicated sub-agent, so that orchestration context remains focused.
14. As a Blazor developer, I want review loops capped, so that orchestration does not spin indefinitely.
15. As a Blazor developer, I want unresolved review findings preserved, so that I can complete follow-up manually.
16. As a Blazor developer, I want deterministic multi-lane status merging, so that final outcomes are unambiguous.
17. As a Blazor developer, I want blocked and failed lanes reported distinctly, so that next actions are clear.
18. As a Blazor developer, I want telemetry artifacts outside the target repo by default, so that sensitive run data is not accidentally committed.
19. As a Blazor developer, I want `--self-improve` opt-in behavior, so that no self-improvement side effects happen unexpectedly.
20. As a Blazor developer, I want contract fixtures and smoke-path validation, so that package behavior is verifiable during implementation.

## Scope

- Plugin contract and packaging for the orchestration core.
- Invocation surface and natural-language option parsing for `--skip-code-review` and `--self-improve`.
- Route classifier thresholds and lane-admission rules.
- Specialist report-back schema and retry/fail semantics.
- Review-loop behavior and stop criteria.
- Telemetry artifacts, event requirements, and run-level status aggregation.
- Contract validation checklist, smoke scenario, fixture expectations, and semver policy.

## Out of Scope

- Building the orchestrator/specialists/telemetry writer implementation.
- Interactive self-improvement report UX and prompt-shipping behavior.
- New specialist lanes beyond extractor, author, form, and data-fetching.
- Any cloud-hosted telemetry or remote data export.

## Architecture and Component Boundaries

- **Orchestrator skill**: `blazor-orchestrator` is the routing and aggregation surface.
- **Specialists**: lane agents perform scoped work and return strict JSON; they do not spawn sub-agents.
- **Review lane**: dedicated `code-review` sub-agent performs review cycles.
- **Hooks**: plugin hook audits delegation drift (delegation outside orchestrator lane).
- **Style source**: one shared house-style skill is canonical; specialists consume it rather than duplicating convention logic.
- **Lane set**: extractor, author, form, data-fetching; Fluent UI is a cross-lane constraint layer, not its own lane.

## Data Contracts and Schemas

- **Plugin manifest**: `plugin.json` is loader contract; `manifest.yaml` is documentation-only.
- **Run artifact root**: `~/.copilot/blazor-orchestration/runs/<run_id>/`.
- **Artifacts**:
  - `events.jsonl`
  - `reports/<agent-id>.json`
  - `analysis.json`
  - `reports/self-improvement-report.html` only with `--self-improve`
- **Specialist report JSON (required fields)**:
  - `schema_version`, `specialist`, `status`, `summary`, `files_changed[]`, `validation`, `next_action`
- **Specialist report JSON (optional fields)**:
  - `token_estimate`
  - `self_diagnosis` (only when `--self-improve` is enabled)
- **Dispatch event required field**:
  - `routing_reason` (one-sentence, max 120 chars)
- **Completion/analysis required aggregation fields**:
  - lane completion: `lane`, `lane_status`, `merge_class`, and `blocking_reason` when blocked class
  - run completion: `final_status`, `status_counts`, `blocked_lanes`, `failed_lanes`
  - analysis: `final_status`, `status_counts`, `lane_outcomes[]`, `user_follow_up_required`, `follow_up_items[]` when needed

## Runtime Flow and Lifecycle

1. User invokes `blazor-orchestrator`; run ID is created and shown immediately.
2. Orchestrator parses natural-language options (including shorthand aliases).
3. Route classifier picks inline vs delegated lanes; delegated lanes emit one user-facing routing sentence each.
4. Delegated specialists run with bounded allowlists and return strict JSON.
5. Orchestrator validates reports; malformed report gets one schema-repair retry, then lane becomes `failed_report_schema`.
6. If review is enabled, review lane runs with max two fix-and-review retries after initial pass.
7. Orchestrator writes run artifacts (when telemetry enabled) and computes run-level `final_status` with precedence: failed > blocked > partial > success.
8. Orchestrator closes with lane-specific follow-up when final status is `partial` or `blocked`.

## Error Handling and Fallback Behavior

- Ambiguous route defaults to delegate.
- Malformed/missing specialist report: one retry, then explicit failed semantics; raw output retained in run artifacts.
- Review unresolved after cap: lane status `review_unresolved`, required unresolved findings persisted, `review_loop_stopped` event emitted.
- `tool_missed` is advisory-only and recorded only from explicit specialist self-diagnosis under `--self-improve`.
- Direct specialist invocation remains functional but produces no orchestrator run record or telemetry envelope.

## Security and Privacy Constraints

- Telemetry remains local only.
- Run artifacts are written under user-level Copilot path, not target repository.
- No automatic mutation of agents/skills from self-improvement outputs.
- Tool allowlists constrain specialist capability and prevent specialist-side delegation.

## Packaging, Install, and Update Model

- Plugin name: `blazor-orchestration-core`.
- Install with `copilot plugin install <package-path>`.
- Reinstall required after local plugin changes because installed content is cached.
- Keep plugin component names unique and non-colliding with external user-level assets such as `blazor-component-architect`.
- Include `agents`, `skills`, and `hooks` in plugin declaration; do not include `commands`/`mcpServers` for this effort.

## Validation and Acceptance Criteria

- `plugin.json` loads cleanly and exposes orchestrator + required specialists.
- Routing behavior matches locked classifier thresholds.
- Specialist report schema is enforced including retry-then-fail path.
- Review-loop cap and unresolved-handling behavior is honored.
- Artifact layout and mandatory events/fields match contract.
- `--self-improve` remains opt-in and advisory-only.
- A golden-path smoke run validates delegated flow and artifact emission.
- Fixtures exist in `fixtures/smoke-run/` with minimal valid examples for success, blocked, and aggregated analysis.

## Implementation Decisions

- Keep orchestrator as a skill (not promoted to standalone agent interface).
- Enforce delegation policy via specialist allowlists and hook-level auditing.
- Parse options from natural language; do not rely on `$ARGUMENTS`.
- Coexist with `blazor-component-architect` via distinct trigger boundary.
- Route classifier thresholds are fixed:
  - Inline only for single lane, <=2 files, no broad discovery.
  - Delegate when >1 lane, >2 files, broad reading, or isolation benefit.
  - Ambiguous defaults to delegate.
  - Parallel fan-out only for independent lanes with at least one specialist-turn savings each.
- Lane-admission rule requires repeated independent demand, clear failure-mode separation, and stable contract.
- Run-level aggregation classes are fixed: success class (`success`), blocked class (`blocked`, `review_unresolved`), failed class (`failed`, `failed_report_schema`).

## Testing Decisions

- Good tests assert external contract behavior, not internal implementation choices.
- Test seams:
  1. Orchestrator invocation contract and route classifier output.
  2. Specialist report schema validator + retry path.
  3. Review-loop controller and stop semantics.
  4. Artifact writer and status aggregation outputs.
- Prior art in this effort is fixture-based contract validation and deterministic smoke replay; this spec preserves that approach.

## Implementation Plan (Ordered Slices)

1. Establish `plugin.json` contract, naming, and component declarations.
2. Implement orchestrator invocation parsing and route classifier table.
3. Wire specialist lane contracts and strict report parser/validator.
4. Implement review-lane integration with capped fix-and-review loop.
5. Implement telemetry writer (`events.jsonl`, `reports/*.json`, `analysis.json`) and aggregation logic.
6. Add hook-level delegation drift audit.
7. Produce `fixtures/smoke-run/` contract fixtures and run checklist-driven smoke validation.
8. Finalize docs updates including semver/breaking-change policy.

## Risks and Open Assumptions

- Specialist trigger quality depends on clean domain-intent wording and shared-style maintenance.
- Cross-run telemetry retention has no purge policy in this scope.

## Further Notes

- `manifest.yaml` remains documentation and must explicitly state it is not the loader contract.
- Superseded decisions should remain documented historically, but implementation follows the final locked state above.
