# 15 — Generate `improvement-report-data.json` and CLI staging signal

**What to build:** When a run completes with `--self-improve`, the CLI writes a valid schema v1.1 `improvement-report-data.json` to `~/.copilot/blazor-orchestration/runs/<run_id>/`. The file includes `origin` (skill_id, skill_scope, skill_path, repo_root, run_id), `findings[]` with all required fields (id, specialist, title, summary, category, severity, expected_impact, prompt_fragment, evidence), and an empty `decisions` map and null `shipped_prompt`. Before writing, the CLI applies cross-run dedup by `suggestion_key` — collapsing duplicates to one record (max severity, evidence union, recurrence count) and applying a bounded ranking boost for recurrence. Dismissed keys from cross-run history are deprioritized. Once the file exists and later accumulates `decisions` and/or `shipped_prompt`, the CLI treats it as its readiness signal to stage the file alongside implementation changes, presenting the developer with a continue/stash/discard conflict flow if the file is already tracked.

**Blocked by:** None — can start immediately.

**Status:** done

- [x] `--self-improve` run produces `improvement-report-data.json` at the correct run-directory path
- [x] File is schema version `1.1` and includes all required top-level and finding fields
- [x] `category` is one of: `tool_use`, `planning`, `output_quality`, `validation`, `communication`
- [x] Cross-run dedup folds by `suggestion_key`; max severity and evidence union are applied
- [x] Recurrence count increments; ranking boost is bounded and does not override dismissal penalty
- [x] Dismissed suggestion keys from history are excluded or deprioritized in findings
- [x] CLI detects presence of `decisions`/`shipped_prompt` and stages the file
- [x] Conflict flow (continue/stash/discard) is presented when file is already tracked and has changes
