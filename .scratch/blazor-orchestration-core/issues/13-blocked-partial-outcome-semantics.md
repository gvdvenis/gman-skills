# Blocked/partial outcome semantics

Type: grilling
Status: resolved
Assignee: @gvdvenis
Blocked by: 04, 05, 06

## Question

How should the orchestrator aggregate final status when one or more lanes return non-success
results, especially `blocked` and mixed outcomes across multiple lanes?

Decide:

- Run-level status values and precedence when lane statuses differ.
- Exact merge rules for multi-lane runs (including all-blocked versus mixed blocked/success).
- Required `events.jsonl` and `analysis.json` fields so partial outcomes are explicit and actionable.

## Answer

Lock run-level aggregation to four terminal statuses: `success`, `partial`, `blocked`, `failed`.

Lane-level statuses remain those already established by prior tickets (`success`, `blocked`,
`failed`, `review_unresolved`, plus `failed_report_schema` from schema retry exhaustion). For
aggregation, classify lane statuses into merge classes:

- **Success class:** `success`
- **Blocked class:** `blocked`, `review_unresolved`
- **Failed class:** `failed`, `failed_report_schema`

Run-level merge rules (applied in order):

1. If any lane is in **Failed class** -> run `final_status = "failed"`.
2. Else if all lanes are in **Blocked class** -> run `final_status = "blocked"`.
3. Else if there is at least one **Success class** lane and at least one **Blocked class** lane ->
   run `final_status = "partial"`.
4. Else (all lanes are **Success class**) -> run `final_status = "success"`.

This makes `partial` strictly "mixed success + blocked-like outcomes" and reserves `blocked` for
"nothing succeeded yet, but no hard failure occurred."

### Required telemetry fields

Do not add a new artifact file. Keep using `events.jsonl` and `analysis.json`, but require these
fields:

1. Every lane `completion` event includes:
   - `lane`
   - `lane_status`
   - `merge_class` (`success` | `blocked` | `failed`)
   - `blocking_reason` (required when `merge_class = "blocked"`, omitted otherwise)
2. The run-level `completion` event includes:
   - `final_status`
   - `status_counts` object with counts for `success`, `blocked`, `failed`
   - `blocked_lanes` array (lane names in Blocked class)
   - `failed_lanes` array (lane names in Failed class)
3. `analysis.json` includes:
   - `final_status`
   - `status_counts`
   - `lane_outcomes[]` (lane, lane_status, merge_class, summary, next_action)
   - `user_follow_up_required` (boolean; true for `partial`, `blocked`, or `failed`)
   - `follow_up_items[]` (required when `user_follow_up_required = true`)

### User-facing closing line

When `final_status` is `partial` or `blocked`, the orchestrator must state which lanes are blocked
and the next action for each, rather than only reporting counts.

## Comments

- Closed. Final status now merges lane outcomes deterministically (`failed` > `blocked` > `partial`
  > `success`) with explicit blocked/failure lane details required in `completion` and
  `analysis.json`.
