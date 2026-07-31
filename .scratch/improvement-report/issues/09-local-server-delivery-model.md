# Local server delivery model

Type: grilling
Status: resolved
Assignee: @copilot

## Question

The local-first delivery constraints decision (ticket 04) settled on a fully self-contained HTML
file with inline data. That constraint is now superseded: the report is served by a local C# server
running at `127.0.0.1`, and the HTML fetches `report-input.json` via `GET /api/report`.

This ticket locks the delivery model in full:

- The lifecycle of the local C# server: how it is started (CLI flag, auto-launched by
  `--self-improve`, or separate command), when it exits (user closes browser, explicit stop, timeout),
  and whether it binds a fixed port or an ephemeral one.
- Whether the server is the same C# tool used for suggestion history and scoring feedback (ticket 05),
  or a separate process.
- What the minimal API surface looks like: at minimum `GET /api/report`, but what else is in scope
  for this effort vs deferred.
- Whether the self-contained HTML fallback is kept for environments without .NET, or dropped entirely.

## Depends on

- [Local-first delivery constraints](04-local-first-delivery-constraints.md) — superseded by this ticket
- [Suggestion history and scoring feedback](05-suggestion-history-and-scoring-feedback.md) — may share the same server process

## Answer

**The local C# server is the sole delivery path. No self-contained fallback.**

### Server lifecycle

`--self-improve` auto-launches the server — no separate `--serve` flag required. Because ticket 01
dropped inline embedding, `GET /api/report` is the only data path; making the server optional would
leave the report blank.

- **Start**: auto-launched by `--self-improve` as part of the run.
- **Port**: fixed, configurable via config/env, defaulting to `5173`. Fixed port is required by the
  stable QR-code URL established in the mobile ticket.
- **Stop**: the server exits when the CLI run finishes *unless* the browser has opened (detected by
  the first `GET /api/report` request). If the browser opened, the server stays alive until: (a)
  `GET /shutdown`, (b) idle timeout (10 min of no requests), or (c) the terminal session ends.

### One server — no separate history process

Ticket 05 put history ownership in the CLI/analyzer layer; it needs no resident server. There is no
"history server" to merge with. The report server is the single resident local C# process.

### API surface in scope for this effort

| Endpoint | Purpose |
|---|---|
| `GET /api/report` | Serve `improvement-report-data.json` from the run directory |
| `GET /ping` | Lightweight liveness probe for auto-reload polling |
| `POST /clipboard` *(superseded by `POST /api/ship-prompt` — see ticket 13)* | ~~Write assembled prompt to the desktop machine's clipboard~~ |
| `GET /shutdown` | Graceful stop (CLI can also SIGTERM) |

**Deferred (remains fog):** port conflict handling and Windows firewall behaviour; optional
prompt-enhancement pipeline before clipboard write; write-back to `improvement-report-data.json` (the
CLI-callback NB stands — copy-out boundary from ticket 08b is not reopened here).

### No self-contained fallback

Ticket 01 explicitly dropped inline embedding. Maintaining two data-delivery paths is not justified.
The `file://` degraded-state banner (ticket 08a) communicates the limitation. Environments without
.NET are out of scope.

