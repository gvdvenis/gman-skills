# Self-improvement report generation

This document specifies the deterministic rules the CLI orchestrator follows to generate
`improvement-report-data.json` under `--self-improve`. It covers:

1. [Run directory and file path](#run-directory)
2. [suggestion_key derivation](#suggestion_key-derivation)
3. [Cross-run dedup fold rules](#cross-run-dedup-fold-rules)
4. [Ranking formula](#ranking-formula)
5. [Generation algorithm (ordered steps)](#generation-algorithm)
6. [Conflict flow](#conflict-flow)

Staging readiness lives in the `self-improve` SKILL.md, step 3.

---

## Run directory

```
~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json
```

Where `<run_id>` is the run identifier in `run-YYYYMMDD-HHMM` format.

The run directory is created by the orchestrator at the start of every run when `--self-improve`
is active. The data file is written once at run completion; all subsequent writes belong to the
C# server.

---

## suggestion_key derivation

A `suggestion_key` is the finding's fingerprint: the same weakness across runs, whatever the
wording or severity score.

**Derivation recipe:**

```
suggestion_key = "<category>:<target_surface>:<normalized_intent>"
```

| Part | Rule |
|---|---|
| `category` | Exact category enum value (`tool_use`, `planning`, `output_quality`, `validation`, `communication`) |
| `target_surface` | Lowercase kebab-case, max 32 chars. Strip file paths, variable names, and run-specific identifiers. Retain the structural concern (e.g. `read-before-edit`, `scope-check`, `token-efficiency`) |
| `normalized_intent` | Lowercase kebab-case, max 48 chars. Strip adjectives and phrasing variations. Distil to the smallest unit of actionable change (e.g. `always-view-target-file`, `confirm-lane-before-delegating`) |

**Excluded from the key:** title wording, summary prose, expected_impact, severity score, run_id, specialist name, evidence pointers.

**Examples:**

| Finding | suggestion_key |
|---|---|
| "Specialist did not call view before edit" | `tool_use:read-before-edit:always-view-target-file` |
| "Full-file rewrite used for 3-line change" | `output_quality:token-efficiency:avoid-full-file-rewrite` |
| "Lane boundary not confirmed before start" | `planning:scope-check:confirm-lane-before-delegating` |

---

## Cross-run dedup fold rules

Raw findings with the same `suggestion_key`, from this run and from the suggestion history at
`~/.self-improve-reports/blazor-architect/suggestion-history.json`, fold into one finding:

| Field | Rule |
|---|---|
| `title`, `summary`, `expected_impact`, `prompt_fragment` | Latest-run value wins |
| `severity` | Max across all occurrences (critical > high > medium > low) |
| `evidence` | Union — all evidence items from all runs, each annotated with `run_id` and `run_timestamp` |
| `recurrence_count` | Count of distinct `run_id` values in which the `suggestion_key` appeared |
| `first_seen` | Min `generated_at` timestamp across all occurrences |
| `last_seen` | Max `generated_at` timestamp across all occurrences (equals current run for new occurrences) |
| `id` | Assigned fresh per-run (e.g. `f-001`, `f-002`, sequential). Not stable across runs. |

---

## Ranking formula

```
ranking_score = base_severity_weight + recurrence_boost + history_weight
```

**base_severity_weight:**

| Severity | Weight |
|---|---|
| critical | 4.0 |
| high | 3.0 |
| medium | 2.0 |
| low | 1.0 |

**recurrence_boost:**

```
recurrence_boost = min((recurrence_count - 1) * 0.1, 0.3)
```

Maximum boost is 0.3 regardless of recurrence count. A first-seen finding (recurrence_count = 1)
gets no boost.

**history_weight:**

| History state | Weight |
|---|---|
| `accepted` in any prior run | +0.2 |
| No prior history | 0.0 |
| `dismissed` within cooldown window (30 days) | −1.5 |
| `dismissed` outside cooldown window | 0.0 |
| `never_again` | excluded — not ranked |

Only the most recent decision for a `suggestion_key` contributes to `history_weight`. Earlier
decisions for the same key are informational only. An active dismissal outweighs the largest
recurrence boost (−1.5 vs +0.3); the finding still keeps its recurrence count and evidence.

**Sort order:** by severity group (critical → high → medium → low; the HTML shows the groups
separately), then `ranking_score` descending, then `recurrence_count` descending, then
`first_seen` ascending (older issues first).

---

## Generation algorithm

Execute the following steps in order at the end of a `--self-improve` run, after all specialist
reports have been validated and the review loop has completed.

```
1.  Collect all self_diagnosis.issues entries from all specialist reports in the run.
2.  Map each issue to a raw finding: { specialist, issue_index, title, summary, category,
    severity, expected_impact, prompt_fragment, evidence: [{ specialist, issue_index }] }
    Severity comes from the self_diagnosis report; do NOT compute it here.
3.  Derive each raw finding's suggestion_key (see suggestion_key derivation).
4.  Load suggestion-history.json (see Cross-run dedup fold rules); skip silently if absent or
    unreadable.
5.  Drop every raw finding whose suggestion_key has a never_again entry. It never reaches the HTML.
6.  Fold the rest by suggestion_key (see Cross-run dedup fold rules).
7.  Compute ranking_score and sort (see Ranking formula).
8.  Assign sequential ids (f-001, f-002, ...) in sort order.
9.  Write improvement-report-data.json to the run directory in the shape shown in the
    self-improve SKILL.md, step 1: generated_at is now, origin comes from the current run,
    findings from step 8.
```

---

## Conflict flow

If `improvement-report-data.json` for the current run is already git-tracked (i.e. it was
previously staged or committed) and has local modifications (e.g. the user re-ran under
`--self-improve` for the same run_id), the CLI presents an explicit conflict resolution prompt
before staging:

```
improvement-report-data.json for run-YYYYMMDD-HHMM has local changes.
Choose an action:
  [c] Continue — keep existing file as-is, do not restage
  [b] Backup — copy existing file to improvement-report-data.json.bak before staging new
  [d] Discard — overwrite existing file with newly generated version
```

- **Continue**: no file operation; user manages the conflict manually.
- **Backup**: write existing file to `improvement-report-data.json.bak` in the same directory,
  then write the new file and stage it.
- **Discard**: overwrite and stage without preserving the existing file.

If the user picks nothing (for example in a non-interactive run), the CLI defaults to
**Continue** and logs the skipped staging decision in the run artifact.
