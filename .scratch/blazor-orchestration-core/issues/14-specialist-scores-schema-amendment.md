# Specialist scores schema amendment

Type: grilling
Status: resolved
Assignee: @copilot

## Question

The self-improvement report input contract (decided in the sibling map) derives finding severity
from per-dimension scores supplied by the specialist. The current specialist report-back schema
(ticket 04) has `self_diagnosis.confidence` and freeform `issues`/`improvements` strings, but no
per-dimension scores.

Amend the specialist report-back schema to add an optional `self_diagnosis.scores` object, emitted
only when `--self-improve` is on:

- Lock the four dimensions: `tokens`, `quality`, `clarity`, `maintainability`.
- Decide the scale: `low | medium | high` (consistent with other self_diagnosis fields) vs a 1–3
  integer (easier to average across specialists).
- Decide whether all four dimensions are always required when `scores` is present, or whether
  individual dimensions may be omitted if the specialist has no signal.
- Decide what the orchestrator does when `scores` is absent or partially populated (fall back to
  `confidence` for severity, or mark severity as `unknown`).

## Answer

Add an optional `self_diagnosis.scores` object to the specialist report-back schema, emitted only
when `--self-improve` is on (same gate as the rest of `self_diagnosis`).

**Scale:** `"low" | "medium" | "high"` — consistent with the existing `confidence` field; keeps the
whole `self_diagnosis` object uniformly typed.

**Dimensions:** `tokens`, `quality`, `clarity`, `maintainability`. All four are optional within the
object — a specialist emits only the ones it has genuine signal for. An empty `scores: {}` is valid.

**Orchestrator fallback behaviour:**

| Situation | Orchestrator action |
|---|---|
| `self_diagnosis` absent | No severity derivation; `--self-improve` is off or specialist omitted it |
| `self_diagnosis` present, `scores` absent | Fall back to `confidence` for severity; non-breaking for pre-amendment specialists |
| `scores` present, individual dimensions missing | Use present dimensions; treat absent ones as `unknown` in the improvement report |

The amendment is backwards-compatible: a run never fails solely because `scores` is absent.

Amended `self_diagnosis` shape:

```json
"self_diagnosis": {
  "issues": ["..."],
  "improvements": ["..."],
  "confidence": "medium",
  "scores": {
    "tokens": "low",
    "quality": "high",
    "clarity": "medium"
    // maintainability omitted — no signal
  }
}
```

## Comments

- Resolved: `scores` uses `low | medium | high` scale (matches `confidence`); all four dimensions
  optional; orchestrator falls back to `confidence` when `scores` absent; missing dimensions treated
  as `unknown` in the improvement report.


- [Specialist report-back schema](04-specialist-report-back-schema.md) — this ticket amends that decision
