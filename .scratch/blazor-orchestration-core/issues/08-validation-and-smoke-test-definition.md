# Validation and smoke-test definition

Type: grilling
Status: resolved
Assignee: @gvdvenis
Blocked by: 01, 02, 04, 05, 09

## Question

What does "the package works" mean, given nothing is executed in this map?

Decide:

- The validation checklist an implementation session must satisfy.
- The smoke scenario that exercises routing, delegation, reporting, and artifact creation end to end.
- The sample run artifacts shipped with the package as fixtures.
- How the package is versioned, and what a breaking contract change requires.

## Answer

Define package validity as a contract check, not runtime feature completeness:

1. **Validation checklist for implementation sessions**
   - `plugin.json` loads with no schema/loader errors and exposes the orchestrator skill plus required specialists.
   - The orchestrator enforces the locked routing rule from [Inline-versus-delegate decision rule](03-inline-versus-delegate-decision-rule.md).
   - Each delegated specialist returns the locked report schema from [Specialist report-back schema](04-specialist-report-back-schema.md), including required fields.
   - Review gating follows [Review-loop stopping criteria](06-review-loop-stopping-criteria.md), including cycle cap and unresolved-finding handling.
   - Telemetry output matches [Telemetry artifact format](05-telemetry-artifact-format.md): `events.jsonl`, per-lane report JSON, and `analysis.json` in the run directory.
   - `--self-improve` remains opt-in and advisory-only; no autonomous rewrites or hidden side effects.

2. **Required smoke scenario (single golden path)**
   - Run one orchestrator invocation with a prompt that forces at least one delegated lane.
   - Confirm routing decision output, specialist dispatch, specialist completion, and final orchestration summary all occur.
   - Confirm artifact directory creation and presence of mandatory files/events.
   - Confirm one deterministic failure-mode branch by replaying a malformed specialist report and verifying retry-then-fail behavior is recorded.

3. **Fixtures shipped with the package**
   - A `fixtures/smoke-run/` sample run directory containing:
     - `events.jsonl` (minimal but valid dispatch/completion/analysis progression),
     - one specialist report JSON example for success,
     - one specialist report JSON example for `blocked`,
     - one `analysis.json` showing aggregated final status.
   - A short fixture README stating these are contract fixtures for validation tooling/docs, not production telemetry snapshots.

4. **Versioning and breaking-change policy**
   - Use semantic versioning for the package contract.
   - Treat as **breaking (major)**: changes to required report fields, required event types, artifact path layout, orchestrator invocation contract, or review-stop terminal statuses.
   - Treat as **minor**: additive optional fields/events and backward-compatible new specialist lanes.
   - Any breaking contract change requires: (a) major version bump, (b) fixture refresh in the same change, and (c) explicit migration notes in package docs.

## Comments

- Closed. "Package works" is defined by a contract-validation checklist and a deterministic end-to-end smoke scenario with shipped fixtures; breaking contract changes require a major bump plus fixture and migration updates.
