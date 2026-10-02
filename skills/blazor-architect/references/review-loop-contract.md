# Review-loop contract

How the orchestrator runs code review, when it stops, and what it emits.

## Review sub-agent pattern

All review passes use a **dedicated review sub-agent** invoking the `code-review` skill.
Review is **never** performed inline in the orchestrator.

Rationale: isolating review in a sub-agent keeps orchestrator context clean and reuses the
battle-tested, high-confidence review logic already embedded in the `code-review` skill.

---

## Cycle-by-cycle description (hard cap: 2 fix-and-review cycles)

A maximum of **3 review passes** can run in total (initial + 2 fix-and-review cycles).

| Pass | Name | What happens |
|---|---|---|
| Pass 0 | Initial review | Review sub-agent runs against the specialist output. |
| Pass 1 | First fix cycle | If actionable findings exist, a targeted fix prompt is sent to the specialist (limited to those findings). Review sub-agent runs again. |
| Pass 2 | Second fix cycle | If actionable findings still exist, one more targeted fix and one more review run. Findings left after this review are unresolved; the loop **stops**. |

The loop also exits early when any review pass returns zero actionable findings.

---

## Actionable finding criteria (all three gates must be satisfied)

A finding is **actionable** only when it meets **all** of the following:

1. **Concrete defect** — must be a bug, broken build, failing test, or security issue.
   Vague concerns ("this could be better") do not qualify.
2. **File-specific** — must name the file, and include a line range where possible.
3. **Not stylistic** — style comments, naming preferences, and refactor suggestions are excluded
   regardless of confidence level.

The review sub-agent applies the same high-confidence bar as the `code-review` skill.

---

## Stop-point behavior and outputs

When the loop ends with **unresolved actionable findings** (either after cycle 2 or after the cap):

| Output | Details |
|---|---|
| `review_outcome` in `analysis.json` | `"review_unresolved"`. |
| Lane `status` in `analysis.json` | `"blocked"`, so `final_status` is at least `blocked`. |
| `review_findings` in `analysis.json` | Array of objects: `{ file, line_range?, severity, description }`. |
| Orchestrator closing line | Surfaces unresolved-findings count and run directory path. |

When the loop exits cleanly (no unresolved findings), the lane keeps its status from the
specialist's report.

---

## `--skip-code-review` bypass

When `--skip-code-review` is present, the entire review loop is bypassed.
Lane status is taken directly from the specialist's report.
`review_outcome` in the run summary is set to `"skipped"`.

---

## Valid `review_outcome` values

| Value | Meaning |
|---|---|
| `passed` | Review ran; no actionable findings in final pass. |
| `fixed` | Actionable findings were found and resolved within the cycle cap. |
| `review_unresolved` | Loop ended with unresolved findings after the cycle cap. |
| `skipped` | `--skip-code-review` was set; review was not run. |
