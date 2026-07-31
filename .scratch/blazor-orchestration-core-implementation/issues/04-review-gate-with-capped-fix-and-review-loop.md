# 04 — Review gate with capped fix-and-review loop

**What to build:** A review gate that uses a dedicated review sub-agent, applies the actionable-finding bar, runs no more than two fix-and-review retries after the initial pass, and produces explicit unresolved-review outcomes with concrete follow-up.

**Blocked by:** 03 — Expand lanes and routing explanations.

**Status:** ready-for-agent

- [ ] Review runs through the dedicated review path, respects skip-review option behavior, and enforces the fixed cycle cap.
- [ ] When actionable findings remain at cap, outcome is persisted and surfaced as unresolved review with actionable follow-up details.

