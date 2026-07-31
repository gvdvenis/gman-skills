# 05 — Telemetry artifacts and run-status aggregation

**What to build:** Local run artifacts that capture orchestrator and lane outcomes (`events.jsonl`, lane reports, `analysis.json`) and deterministic final run status aggregation with explicit blocked/failed lane detail and required follow-up payloads.

**Blocked by:** 04 — Review gate with capped fix-and-review loop.

**Status:** ready-for-agent

- [ ] A completed orchestrator run writes the required artifact set in the defined local run directory structure with mandatory event/analysis fields.
- [ ] Mixed lane outcomes aggregate to the correct final status precedence and include the required blocked/failed lane reporting details.

