# Cross-run finding deduplication

Type: grilling
Status: resolved
Assignee: @copilot
Blocked by: 05

## Question

When the same weakness recurs across multiple runs, how are those findings deduplicated — and how
does deduplication interact with the suggestion history weights decided in ticket 05?

Decide:

- What makes two findings from different runs "the same finding".
- Where and when deduplication happens (CLI-side vs report-side).
- How duplicates are collapsed: which fields merge, which are overwritten, and what new derived
  fields emerge (e.g. recurrence count).
- How the recurrence count (or absence of it) feeds into ranking and the history weight system.
- Whether a dismissed `suggestion_key` that recurs in a new run is re-shown, suppressed, or shown
  with explicit "seen before" context.

## Answer

Findings across runs are the same finding when they share the same `suggestion_key` — the
deterministic key already established in ticket 05 (normalized intent + target surface + proposed
change shape, with volatile wording and scores excluded).

**Deduplication is CLI-side, at report-generation time.** The analyzer folds all per-run occurrences
of a key into a single finding record before writing the HTML. The report receives one record per
`suggestion_key`, never one per run-occurrence, so the browser artifact stays simple and stateless.

**Merge rules for the collapsed record:**

| Field | Rule |
|---|---|
| `title` / `summary` / `prompt_fragment` | Latest-run value wins |
| `severity` | Max across all occurrences |
| `expected_impact` | Latest-run value wins |
| `evidence` | Union — all evidence sources from all runs, each annotated with its run timestamp |
| `recurrence_count` | Count of distinct runs in which the key appeared |
| `first_seen` / `last_seen` | Min and max run timestamps |

**Recurrence feeds ranking as a bounded additive weight.** A finding with `recurrence_count > 1`
receives a mild, capped boost (e.g. +0.1 per additional run, ceiling at +0.3) to its base score.
This signals persistence without overwhelming the severity and impact signals. The boost is visible
in the score breakdown so ordering stays explainable.

**History weights from ticket 05 apply to the collapsed key, not to per-run instances.** Concretely:

- A key dismissed in a prior run stays deprioritized even if it recurs; the recurrence boost does
  not override the dismissal penalty — the net score is `base + recurrence_boost + history_weight`,
  and the dismissal penalty will ordinarily outweigh a small recurrence boost.
- A key with "never suggest again" set is excluded from the report entirely, regardless of recurrence.
- The "never suggest again" exclusion happens at the same CLI-side fold step, so it never reaches
  the HTML at all.

**A recurring, previously dismissed finding is not re-shown as new.** If it passes the dismissal
threshold (i.e. the cooldown has elapsed and the penalty is no longer dominant), it surfaces with a
`recurrence_count` badge and the evidence union — the reviewer can see it keeps coming back, without
being surprised it was suppressed before. The report does not surface the dismissal history itself
(that stays in the CLI history file), only the count and evidence.
