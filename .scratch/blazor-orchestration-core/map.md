# Map: Blazor orchestration core package

Labels: `wayfinder:map`

## Destination

Every unresolved architectural decision for the core Blazor orchestration package is locked and
written into the package contract, so an implementation session can build the orchestrator skill,
its specialists, and its telemetry without further architectural debate. This map produces
decisions and a spec — not working code.

## Notes

- Domain: agent/skill packaging for the Copilot CLI harness; Blazor UI work is the payload, not the subject.
- The orchestrator is a **skill**, not an agent. It routes, splits lanes, and is the *only* component
  that spawns sub-agents. Specialists consult skills; specialists never spawn further sub-agents.
- Optimize token usage before speed.
- Telemetry stays local. No repository content, prompts, or credentials leave the machine.
- `--self-improve` is opt-in; self-improvement stays recommendation-only.
- Avoid duplicating guidance between the routing skill and specialist agents.
- Skills every session should consult: `/grilling`, `/domain-modeling`, `/codebase-design`,
  `/writing-great-skills`, `/skill-creator`.
- Sibling map: [Map: Self-improvement report](../improvement-report/map.md) — consumes this map's
  telemetry and report-back decisions.

## Decisions so far

- [Charting session: destination and orchestrator form](issues/00-charting-session.md) — the map ends
  at locked decisions, not running code; the orchestrator is a skill that alone spawns sub-agents.
- [Harness packaging and loader format](issues/01-harness-packaging-and-loader-format.md) — Copilot CLI
  has a real plugin system rooted at `plugin.json` and installed with `copilot plugin install`;
  `.copilot/packages/` and `manifest.yaml` have no loader role. Agent `tools` allowlists genuinely
  restrict specialists, `$ARGUMENTS` is not a verified Copilot contract, and a skill cannot enforce
  exclusive delegation rights.
- [Enforcing orchestrator exclusivity](issues/10-enforcing-orchestrator-exclusivity.md) — keep the
  orchestrator as the routing skill, enforce specialist non-delegation with tool allowlists, and use
  hook-level auditing to detect direct-delegation drift that would otherwise degrade routing and telemetry consistency.
- [Plugin manifest and install contract](issues/09-plugin-manifest-and-install-contract.md) — standardize on
  `plugin.json` with plugin name `blazor-orchestration-core`, keep `manifest.yaml` as documentation only,
  and require reinstall-after-change because installed plugin content is cached.
- [Orchestrator invocation surface and flags](issues/02-orchestrator-invocation-surface-and-flags.md) — skill
  named `blazor-orchestrator`; coexists with `blazor-component-architect` via distinct trigger scope; options
  parsed from natural language (with shorthand aliases); direct specialist invocation is tolerated but unsupported;
  run ID always created at invocation, telemetry artifact written only when enabled.
- [Inline-versus-delegate decision rule](issues/03-inline-versus-delegate-decision-rule.md) — route inline
  only for narrow single-lane work (<=2 files, no broad discovery); otherwise delegate, with parallel fan-out
  only for independent lanes that each save at least one specialist turn.
- [Specialist report-back schema](issues/04-specialist-report-back-schema.md) — specialists return strict
  JSON with required outcome/file/validation fields, optional coarse token estimates, and retry-then-fail
  handling for malformed reports.
- [Review-loop stopping criteria](issues/06-review-loop-stopping-criteria.md) — dedicated review
  sub-agent (not inline); hard cap of 2 fix-and-review cycles; actionable = concrete defect, file-specific,
  not style; stop-point records `review_unresolved` status with unresolved findings in `analysis.json` and
  a mandatory `review_loop_stopped` event in `events.jsonl`.
- [Which further specialist lanes are justified](issues/07-which-further-specialist-lanes-are-justified.md) —
  add data-fetching as the only new dedicated lane now; keep shared state/auth/prerendering/JS interop as
  inline skill overlays; treat Fluent UI as a cross-lane constraint layer.
- [Telemetry artifact format](issues/05-telemetry-artifact-format.md) — JSON Lines event log +
  one JSON per specialist report + analysis.json, all written by the orchestrator to
  `~/.copilot/blazor-orchestration/runs/<run_id>/` (user-level, not the target repo); `dispatch`,
  `completion`, `analysis` are mandatory; `tool_missed` recorded only under `--self-improve` as advisory.
- [Validation and smoke-test definition](issues/08-validation-and-smoke-test-definition.md) — define
  "package works" as contract validation: a fixed checklist, one end-to-end delegated smoke path,
  shipped run-artifact fixtures, and semver + major-bump policy for breaking contract changes.
- [Specialist style-source strategy](issues/11-specialist-style-source-strategy.md) — use one shared
  house-style skill as the canonical conventions source; keep specialist triggers domain-intent
  based and centralize style maintenance to avoid drift.
- [Routing-explanation contract](issues/12-routing-explanation-contract.md) — one sentence to the
  user per delegated lane (silent on inline); rationale persisted as `routing_reason` string field
  in the existing `dispatch` event; no new artifact.
- [Blocked/partial outcome semantics](issues/13-blocked-partial-outcome-semantics.md) — run status
  merges lane outcomes deterministically (`failed` > `blocked` > `partial` > `success`) with
  required blocked/failure detail fields in `completion` events and `analysis.json`.
- [Specialist scores schema amendment](issues/14-specialist-scores-schema-amendment.md) — add optional
  `self_diagnosis.scores` (`low | medium | high` per dimension, matching `confidence` scale); all four
  dimensions optional; orchestrator falls back to `confidence` when `scores` absent, marks missing
  dimensions `unknown` in the improvement report; amendment is backwards-compatible.

## Not yet specified

None currently.

## Out of scope

- Interactive HTML report design, prompt compression, and provider integration — owned by
  [Map: Self-improvement report](../improvement-report/map.md).
- Implementing the orchestrator, specialists, or telemetry writer. This map plans; a later effort builds.
- Blazor implementation guidance itself. Existing skills already cover it.
