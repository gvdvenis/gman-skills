# Review-loop stopping criteria

Type: grilling
Status: resolved
Assignee: @gvdvenis

## Answer

**Who performs the review**

Use the existing `code-review` skill via a dedicated review sub-agent, not the orchestrator inline.
Rationale: the orchestrator's purpose is routing and aggregation; inline review re-introduces the
broad-discovery cost the delegate decision was taken to avoid. Spinning a sub-agent per review
cycle keeps the orchestrator context clean and reuses tested review logic without duplication.

**What counts as actionable**

A finding is actionable when it meets **all** of the following:
1. It identifies a concrete defect — a bug, a broken build, a failing test, or a security issue.
2. It is specific to a file and, where possible, a line range.
3. It is not a style comment, naming preference, or refactor suggestion.

Findings that do not meet all three criteria are noise and do not trigger another cycle. The
review sub-agent must apply the same high-confidence bar used by the existing `code-review` skill:
report only high-confidence bugs, security vulnerabilities, and logic errors; ignore style and
trivial issues.

**Maximum cycles**

Hard cap of **2 fix-and-review cycles** after the initial specialist run (3 review passes total:
initial + 2 retry cycles). Rationale: a specialist that cannot produce a clean review after two
targeted fixes has likely hit a scope or context problem that a third cycle will not resolve, and
the orchestrator should surface that rather than loop indefinitely.

- Cycle 0 (initial): specialist runs and returns its report.
- Cycle 1: orchestrator dispatches review sub-agent; if actionable findings, specialist is sent a
  targeted fix prompt limited to those findings.
- Cycle 2: review sub-agent runs again; if still actionable findings, orchestration stops the loop.

**What the orchestrator reports at the stop point**

When actionable findings remain after the cycle cap:

1. Set the lane's final `status` to `"review_unresolved"` in the run artifact (a new terminal
   status alongside `success`, `blocked`, `failed`).
2. Include a `review_findings` array in `analysis.json` listing each unresolved finding
   (file, line range where available, severity, description) — so the user can act on them
   manually.
3. Append a `review_loop_stopped` event to `events.jsonl` (mandatory, same category as
   `completion`).
4. Surface the count of unresolved findings and the run directory path in the orchestrator's
   closing line to the user, so they are not silently swallowed.

When `--skip-code-review` is set, all of the above is bypassed and the lane status comes
directly from the specialist's own report.

## Comments

- Closed. Review is a dedicated sub-agent (not inline); cap is 2 retry cycles; actionable = concrete
  defect, file-specific, not style; stop-point surfaces `review_unresolved` status and unresolved
  findings in `analysis.json` and `events.jsonl`.


When does the code-review gate stop?

Decide:

- The maximum number of fix-and-review cycles.
- What counts as an actionable finding worth another cycle, versus noise.
- Who performs the review — the orchestrator inline, a dedicated review sub-agent, or the existing
  `code-review` skill.
- What the orchestrator reports when review findings remain unresolved at the stop point.
