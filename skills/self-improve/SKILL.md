---
name: self-improve
user-invocable: false
description: Generates the self-improvement report for a blazor-architect run, shows it in the local report-server, and stages the reviewed report in git. Loaded only by blazor-architect when --self-improve is set.
---

# Self-improve skill

Runs after blazor-architect's review gate.

## Step 1 — Generate the improvement report

Follow "Generation algorithm" in `references/self-improve-generation.md`. Write
`improvement-report-data.json` to the run folder, matching
`references/improvement-report-data-schema.json` (example:
`references/improvement-report-data-example.json`). Set `decisions` to `{}` and `shipped_prompt`
to `null`; the server fills them in. No self-diagnosis issues: write an empty `findings` array.

## Step 2 — Launch the report-server

```
~/.copilot/gman-skills/bin/report-server[.exe] --report-path ~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json
```

Use `.exe` on Windows. When port 5173 is already in use, a server is already running: print a
warning and do not start a second one. Open `http://127.0.0.1:5173` in the browser and print:

```
[blazor-architect] Self-improvement report ready at http://127.0.0.1:5173
  report file: ~/.self-improve-reports/blazor-architect/runs/<run_id>/improvement-report-data.json
  server PID: <pid>
```

Binary missing: print a warning to run `/setup-gman-skills` and continue without the server.

## Step 3 — Stage after the server stops

The server stops on `GET /shutdown`, an idle timeout, or terminal exit. Then, when `decisions` is
non-empty or `shipped_prompt` is non-null, run `git add` on the report file. When the file is
already tracked and has local changes, run "Conflict flow" from
`references/self-improve-generation.md`. Ask once; when the user picks nothing, continue.
