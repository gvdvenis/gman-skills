# Route classifier

This is the fixed classifier table for the `blazor-orchestrator`. Each task category maps to the
same lane and execution mode on every run. Any override must be recorded in the run artifact so
behaviour stays auditable.

## Decision thresholds

Apply in order; first match wins.

| Priority | Condition | Decision |
|---|---|---|
| 1 | Single lane AND ≤ 2 files expected AND no broad repo discovery needed | **Inline** |
| 2 | More than one lane required | **Delegate** |
| 3 | Expected scope > 2 files | **Delegate** |
| 4 | Broad cross-module reading required | **Delegate** |
| 5 | Isolation materially protects main context (long logs, tool-heavy, independent reasoning) | **Delegate** |
| 6 | Ambiguous / cannot classify | **Delegate** (default) |

The ambiguous default is delegate because false-inline decisions are costlier than false-delegate
decisions — token-control and context isolation are core package goals.

## Parallel fan-out

Parallel fan-out is allowed only when **both** conditions are met:

1. The selected lanes are independent (no shared file writes, no ordering dependency).
2. Each lane is expected to save ≥ 1 full specialist turn compared to serial execution.

If either condition is not met, delegate serially.

## Classifier table by task category

| Task category | Primary lane | Default route | Notes |
|---|---|---|---|
| Create one new component | component-author | Inline if ≤ 2 files | Narrow authoring; context overhead low |
| Extract sections from an existing page | component-extractor | Delegate | Reading + editing several files |
| Add / fix form, binding, or validation | form-specialist | Inline if single form, 1–2 files | Delegate when page is complex |
| Implement a feature end-to-end | Depends on scope | Delegate | Multi-file; isolation benefit |
| Review and refactor a page | component-extractor + review | Delegate (serial) | Multi-concern; always delegate |
| Full feature spanning authoring + forms | component-author → form-specialist | Delegate serial or parallel | Parallel only if independent |
| Narrow file edit with clear boundary | Inline | Inline | Single lane, ≤ 2 files confirmed |
| Lane selection uncertain | — | Delegate | Apply ambiguous default |

## Override recording

When the orchestrator departs from the table (e.g. a category that doesn't appear above, or a
user-supplied flag that changes routing), record the override in the run artifact:

```json
{
  "run_id": "<run_id>",
  "classifier_override": {
    "reason": "<why the default was not followed>",
    "original_decision": "<inline|delegate|parallel>",
    "actual_decision": "<inline|delegate|parallel>"
  }
}
```
