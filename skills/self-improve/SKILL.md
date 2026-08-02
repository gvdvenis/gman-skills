---
name: self-improve
user-invokable: false
description: >
  Loaded by blazor-architect when --self-improve is active. Not user-invokable — appears in the
    skills list but can only be loaded by blazor-architect, not called directly. Handles improvement
    report generation, C# server auto-launch, and CLI staging.
---

# Self-improve skill

Activated by `blazor-architect` when `--self-improve` is set. Runs after all specialist work and
the review loop have completed.

## Step 1 — Create the run directory

Create `~/.self-improve-reports/blazor-architect/runs/<run_id>/` if it does not exist.

**Done when:** the run directory exists on disk.

## Step 2 — Generate the improvement report

Consult `references/self-improve-generation.md` for the complete algorithm. The ordered steps:

1. Collect all `self_diagnosis.issues` entries from specialist reports in the run.
2. Map each to a raw finding (title, summary, category, severity, expected_impact, prompt_fragment, evidence).
3. Derive `suggestion_key` for each finding (`<category>:<target_surface>:<normalized_intent>`).
4. Load `~/.self-improve-reports/blazor-architect/suggestion-history.json` if present.
5. Hard-exclude findings with `never_again` history entries.
6. Group by `suggestion_key`, fold using merge rules (max severity, evidence union, recurrence count).
7. Apply `history_weight` from most recent decision per key.
8. Compute `ranking_score` (base_severity_weight + recurrence_boost + history_weight).
9. Sort by severity group, then ranking_score descending, then first_seen ascending.
10. Assign sequential ids (f-001, f-002, ...).
11. Build the origin block from current run context.
12. Write `improvement-report-data.json` to the run directory.

The file must conform to `references/improvement-report-data-schema.json` (schema version 1.1).
A valid example is at `references/improvement-report-data-example.json`.

Initial file shape:

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

`decisions` and `shipped_prompt` are always empty/null at generation time — the C# server writes
them after user actions.

**Done when:** `improvement-report-data.json` is written to the run directory with valid findings
(or an empty findings array if no self-diagnosis issues were collected).

## Step 3 — Launch the report-server

The report-server binary lives at `~/.copilot/gman-skills/bin/report-server.exe` (Windows) or
`~/.copilot/gman-skills/bin/report-server` (Linux/macOS). Launch it with the report file path:

**Windows:**
```
~/.copilot/gman-skills/bin/report-server.exe --report-path ~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json
```

**Linux / macOS:**
```
~/.copilot/gman-skills/bin/report-server --report-path ~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json
```

The server listens on `127.0.0.1:5173` by default. If the port is already bound, assume the server
is already running — log a warning and continue (do not launch a second instance).

After launching, open the browser to `http://127.0.0.1:5173` so the user can see the report.
Announce the URL clearly:

```
[blazor-architect] Self-improvement report ready at http://127.0.0.1:5173
  report file: ~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json
  server PID: <pid>
```

The server stays alive until `GET /shutdown` is called, an idle timeout elapses after the browser
connects, or the terminal session ends. API surface: `GET /api/report`, `GET /ping`,
`POST /api/ship-prompt`, `POST /api/dismissals`, `GET /shutdown`.

If the binary is missing, print a warning telling the user to run `/setup-gman-skills` and continue
without the server — the report file is still useful on its own.

**Done when:** the server is running and responding on port 5173, or the binary is missing and a
warning has been printed.

## Step 4 — Staging readiness (after server session ends)

After the server shuts down (user calls `GET /shutdown`, idle timeout, or terminal end), check the
report file for staging readiness:

- **Ready** when `decisions` is non-empty OR `shipped_prompt` is non-null.
- When ready, stage: `git add ~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json`

If the file is already git-tracked and has local modifications, present the conflict flow
(continue / stash / discard) from `references/self-improve-generation.md § Conflict flow`. Default
to "Continue" after 30 seconds of no response.

**Done when:** staging is complete or the file is not ready to stage.

## Suggestion history

The cross-run suggestion history store lives at
`~/.self-improve-reports/blazor-architect/suggestion-history.json`. It records user decisions
(`accepted`, `dismissed`, `never_again`) per `suggestion_key` across runs. It is never committed
to source control. See `references/suggestion-history-schema.json` for the schema.
