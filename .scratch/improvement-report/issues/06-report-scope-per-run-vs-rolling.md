# Report scope: per-run vs rolling

Type: grilling
Status: closed
Assignee: @copilot

## Answer

The report is **per-run only** — one timestamped HTML file per `--self-improve` invocation, served
by the local C# server rather than opened directly as a `file://` document.

### Artifact shape

Each run produces two files in the run directory at
`~/.copilot/blazor-orchestration/runs/<run_id>/`:

- **`report.html`** — the HTML report shell. Contains no inline data; it fetches its findings from
  the local C# server via `GET /api/report`. This supersedes the earlier self-contained single-file
  constraint (ticket 04), which was already superseded by the report input contract decision
  (ticket 01).
- **`report-input.json`** — the durable data record written by the orchestrator. This file is the
  run's canonical artifact; the HTML file is volatile presentation on top of it.

**`report.html` is not version-controlled** — it is a thin shell that changes rarely and has no
run-specific data. **`report-input.json` is what gets committed** — it is the timestamped,
content-bearing record of the run.

### Retention

`report-input.json` files are retained indefinitely under the run directory and committed to source
control. The git history of these files is the longitudinal record across runs. No rolling HTML
file; each run's data is self-contained within its own `report-input.json`.

### CLI staging

The CLI **stages `report-input.json`** (`git add`) after generation. The developer commits manually,
alongside the implementation it informed, so the diff captures both recommendation and response.
`report.html` is not staged (it's a static shell).

### Conflict flow

If previous work is detected on entry, the CLI inspects `git status` and presents three options:

1. **Continue unfinished work** — re-surface the existing report if only staged; if implementation
   changes also exist, prompt "ready to commit?" and offer to commit; abort if user declines.
2. **Stash and continue** — full `git stash` (report-input.json + any implementation work), then
   start fresh.
3. **Discard and continue** — drop everything and start fresh.

The stash is a full stash; the timestamped `report-input.json` file remains on disk and is always
re-stageable.

### Rolling aggregate / cross-run diagnostics

Out of scope for this effort — a future layer on top of the committed per-run `report-input.json`
artifacts.

## Reconciliation note

The original answer referred to "one self-contained timestamped HTML file per run." This was
reconciled in the same session that resolved ticket 01 (report input contract), which superseded the
self-contained constraint: the HTML shell is served by the local C# server and fetches data at
runtime. The durable per-run artifact is `report-input.json`, not `report.html`. Both the
"per-run" and "retained in source control" conclusions stand; only the artifact shape and what gets
committed are corrected here.

## Question

Is the self-improvement report one self-contained HTML file per run, a single rolling aggregate
across runs, or both — and what are the retention and UX implications of each choice?

Decide:

- Whether a run produces exactly one HTML file, one ever-growing file, or both.
- Where old reports live after new runs (overwritten, named by timestamp, accumulated).
- How this choice interacts with the local-first, single-HTML constraint (no browser persistence).
- UX implications: how does a reviewer navigate findings across runs vs within a run?
- Retention implications: disk footprint, cleanup story, access to historical findings.
