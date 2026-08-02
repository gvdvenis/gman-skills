---
name: self-improve
user-invokable: false
description: >
  Loaded by blazor-architect when --self-improve is active. Handles improvement report
  generation, C# server auto-launch, and CLI staging. Should not be invoked directly.
---

# Self-improve skill

This skill is activated by `blazor-architect` when the `--self-improve` flag is set. It runs
**after** all specialist work and the review loop have completed. It should never be invoked
directly by the user.

## Report generation algorithm

The complete ordered algorithm for generating `improvement-report-data.json` is specified in
`references/self-improve-generation.md`. Consult that document for:

- `suggestion_key` derivation recipe (`<category>:<target_surface>:<normalized_intent>`)
- Cross-run dedup fold rules (evidence union, max severity, recurrence count, first/last seen)
- Ranking formula (`base_severity_weight + recurrence_boost + history_weight`)
- Dismissal and history-weight application (`never_again` exclusion, cooldown penalty)
- Full step-by-step generation algorithm (14 ordered steps)

Do **not** inline the algorithm here — `references/self-improve-generation.md` is the authoritative
source.

## Report data file

Write `improvement-report-data.json` to:

```
~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json
```

The initial file shape written at generation time:

```json
{
  "schema_version": "1.1",
  "generated_at": "<ISO timestamp>",
  "origin": {
    "skill_id": "<active skill id>",
    "skill_scope": "repo | user",
    "skill_path": "<absolute path to skill directory>",
    "repo_root": "<absolute git repository root>",
    "run_id": "<run_id>"
  },
  "findings": [ ... ],
  "decisions": {},
  "shipped_prompt": null
}
```

`decisions` is always an empty object at generation time. `shipped_prompt` is always `null` at
generation time. Both are written by the C# server after user actions.

The file must conform to `references/improvement-report-data-schema.json` (schema version 1.1).
A valid populated example is at `references/improvement-report-data-example.json`.

## Server auto-launch

When `--self-improve` is active, the C# server auto-launches on fixed port `5173`:

- **Bind address**: loopback `127.0.0.1` by default; use `0.0.0.0` when mobile access is preferred
  and the user has configured that in skill preferences.
- **Port conflict**: if port 5173 is already bound, assume the server is already running, log a
  warning, and continue without launching a second instance.
- **Lifecycle**: the server stays alive until one of:
  - `GET /shutdown` is called, or
  - an idle timeout elapses after the browser first connects, or
  - the terminal session ends.
- **API surface**: `GET /api/report`, `GET /ping`, `POST /api/ship-prompt`,
  `POST /api/dismissals`, `GET /shutdown`.

The server is not implemented in this skill — it is a separate C# project. This skill describes
the launch contract and API surface for reference.

## CLI staging readiness signal

After the C# server session ends (server shutdown or idle timeout), check the run's
`improvement-report-data.json` for staging readiness:

- **Ready** when `decisions` is non-empty **OR** `shipped_prompt` is non-null.
- When ready, stage the file:

```
git add ~/.copilot/blazor-orchestration/runs/<run_id>/improvement-report-data.json
```

See `references/self-improve-generation.md § CLI staging readiness signal` for full details.

## Conflict flow

If `improvement-report-data.json` for the current run is already git-tracked (previously staged or
committed) and has local modifications (e.g. the user re-ran under `--self-improve` for the same
`run_id`), present an explicit conflict resolution prompt before staging:

```
improvement-report-data.json for run-YYYYMMDD-HHMM has local changes.
Choose an action:
  [c] Continue — keep existing file as-is, do not restage
  [s] Stash — move existing file to improvement-report-data.json.bak before staging new
  [d] Discard — overwrite existing file with newly generated version
```

- **Continue**: no file operation; user manages the conflict manually.
- **Stash**: write existing file to `.bak`, then write and stage the new file.
- **Discard**: overwrite and stage without preserving the existing file.

If the prompt receives no response within 30 seconds (non-interactive terminal), default to
**Continue** and log the skipped staging decision in the run artifact.

See `references/self-improve-generation.md § Conflict flow` for full details.

## Suggestion history

The cross-run suggestion history store lives at:

```
~/.copilot/blazor-orchestration/suggestion-history.json
```

This is a CLI-owned, append-only, local-machine-only file that records user decisions
(`accepted`, `dismissed`, `never_again`) for each `suggestion_key` across runs. It is never
committed to source control.

See `references/suggestion-history-schema.json` for the schema (version 1.0). The history is
loaded during report generation to apply dedup folding, ranking weights, and dismissal penalties.
