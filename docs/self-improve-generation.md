# Self-improvement report generation

This document specifies the deterministic rules the CLI orchestrator follows to generate
`improvement-report-data.json` under `--self-improve`. It covers:

1. [Run directory and file path](#run-directory)
2. [suggestion_key derivation](#suggestion_key-derivation)
3. [Cross-run dedup fold rules](#cross-run-dedup-fold-rules)
4. [Ranking formula](#ranking-formula)
5. [Dismissal and history-weight application](#dismissal-and-history-weight-application)
6. [Generation algorithm (ordered steps)](#generation-algorithm)
7. [CLI staging readiness signal](#cli-staging-readiness-signal)
8. [Conflict flow](#conflict-flow)

---

## Run directory

```
~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json
```

Where `<run_id>` is the run identifier in `run-YYYYMMDD-HHMM` format.

The run directory is created by the orchestrator at the start of every run when `--self-improve`
is active. The data file is written once at run completion; all subsequent writes belong to the
C# server.

---

## suggestion_key derivation

A `suggestion_key` is a stable, normalized string that identifies the same weakness or gap across
multiple runs, independent of phrasing and severity score variation.

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

Before writing `findings[]`, the orchestrator folds all raw findings from the current run
against the cross-run suggestion history (if it exists at
`~/.copilot/blazor-orchestration/suggestion-history.json`).

**Fold merge rules:**

| Field | Rule |
|---|---|
| `title`, `summary`, `expected_impact`, `prompt_fragment` | Latest-run value wins |
| `severity` | Max across all occurrences (critical > high > medium > low) |
| `evidence` | Union — all evidence items from all runs, each annotated with `run_id` and `run_timestamp` |
| `recurrence_count` | Count of distinct `run_id` values in which the `suggestion_key` appeared |
| `first_seen` | Min `generated_at` timestamp across all occurrences |
| `last_seen` | Max `generated_at` timestamp across all occurrences (equals current run for new occurrences) |
| `id` | Assigned fresh per-run (e.g. `f-001`, `f-002`, sequential). Not stable across runs. |

**never_again exclusion:** Any `suggestion_key` with a `never_again` entry in history is excluded
from `findings[]` entirely before the fold step. It never reaches the HTML.

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
decisions for the same key are informational only.

**Sort order:** Findings are sorted descending by `ranking_score` within each severity group
(severity groups are shown separately in the HTML). Ties are broken by `recurrence_count`
descending, then by `first_seen` ascending (older issues first).

---

## Dismissal and history-weight application

1. Collect all `suggestion_key` values from the current run's raw findings.
2. Load `~/.copilot/blazor-orchestration/suggestion-history.json` (if it exists).
3. For each raw finding key: look up its most recent history entry.
4. Apply `never_again` exclusion first (hard exclude).
5. Apply `history_weight` to `ranking_score` (soft deprioritize for active dismissal cooldown).
6. The dismissal penalty will ordinarily outweigh the recurrence boost for an actively dismissed
   key (e.g. penalty −1.5 vs max boost +0.3 = net −1.2 from base). The recurrence count and
   evidence union are still preserved in the finding record for visibility.

---

## Generation algorithm

Execute the following steps in order at the end of a `--self-improve` run, after all specialist
reports have been validated and the review loop has completed.

```
1.  Collect all self_diagnosis.issues entries from all specialist reports in the run.
2.  Map each issue to a raw finding: { specialist, issue_index, title, summary, category,
    severity, expected_impact, prompt_fragment, evidence: [{ specialist, issue_index }] }
    Note: severity is pre-derived upstream and present in the self_diagnosis report;
    do NOT compute severity here.
3.  Derive suggestion_key for each raw finding using the derivation recipe above.
4.  Load suggestion-history.json from the user-level Copilot directory (if present; skip
    silently if absent or unreadable).
5.  Hard-exclude any raw finding whose suggestion_key has a never_again history entry.
6.  Group remaining raw findings by suggestion_key.
7.  For each group (same suggestion_key), fold into one finding record using the merge rules.
8.  Apply history_weight from the most recent decision for each suggestion_key.
9.  Compute ranking_score for each folded finding.
10. Sort by severity group (critical → high → medium → low), then by ranking_score descending
    within each group, applying tie-break rules.
11. Assign sequential ids (f-001, f-002, ...) in sort order.
12. Build the origin block from current run context.
13. Write improvement-report-data.json to the run directory with:
    - schema_version: "1.1"
    - generated_at: current ISO timestamp
    - origin: as built in step 12
    - findings: sorted, folded array from step 11
    - decisions: {} (empty object)
    - shipped_prompt: null
14. Emit an analysis event to events.jsonl referencing the generated file path.
```

---

## CLI staging readiness signal

The CLI checks the run's `improvement-report-data.json` after a server session ends (server
shutdown or idle timeout). The file is ready to stage when **either** of the following is true:

- `decisions` object is non-empty (user has taken at least one action via `POST /api/dismissals`
  or `POST /api/ship-prompt`)
- `shipped_prompt` is non-null (user has shipped a prompt via `POST /api/ship-prompt`)

When the readiness signal is detected, the CLI stages the file alongside any implementation
changes from the run:

```
git add ~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json
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
  [s] Stash — move existing file to improvement-report-data.json.bak before staging new
  [d] Discard — overwrite existing file with newly generated version
```

- **Continue**: no file operation; user manages the conflict manually.
- **Stash**: write existing file to `improvement-report-data.json.bak` in the same directory,
  then write the new file and stage it.
- **Discard**: overwrite and stage without preserving the existing file.

If the prompt receives no response within 30 seconds (e.g. non-interactive terminal), the CLI
defaults to **Continue** and logs the skipped staging decision in the run artifact.
