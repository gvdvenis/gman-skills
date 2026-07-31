# Decision-log normalization pass

Type: task
Status: resolved
Assignee: @copilot

## Question

The decision log has accrued superseded wording as later tickets overturned earlier ones. A reader
following the log from top to bottom will hit contradictions: ticket 04 says self-contained
`file://` HTML with no network calls; ticket 09 says the opposite. `POST /clipboard` is named in
tickets 08b and 09 but replaced by ticket 13. `--serve` in ticket 08a conflicts with
`--self-improve` auto-launch in ticket 09. `report-input.json` is named throughout but renamed to
`improvement-report-data.json` by ticket 12.

This ticket:
- Traces the supersession chain in chronological order.
- Identifies every ticket body and map entry carrying stale wording.
- Records the authoritative current state for each contested point.
- Updates map Decisions-so-far summaries that carry the stale wording.
- Adds supersession notices to the affected ticket answers so they remain legible as history.

## Answer

### Supersession chain (chronological)

**Delivery model** (`file://` → C# server):
- Ticket 04 settled: self-contained single HTML file, inline data, `file://`, no network calls.
- Ticket 01 superseded inline embedding: HTML fetches `report-input.json` via `GET /api/report` on local C# server.
- Ticket 09 superseded 04 fully: C# server is the sole delivery path, no self-contained fallback; `file://` mode is the degraded-banner path only.

**CLI flag** (`--serve` → `--self-improve` auto-launch):
- Ticket 08a introduced `--serve` as the server-launch flag.
- Ticket 09 superseded: `--self-improve` auto-launches the server — no separate `--serve` flag.

**Clipboard endpoint** (`POST /clipboard` → `POST /api/ship-prompt`):
- Ticket 09 defined `POST /clipboard`.
- Ticket 08b referenced `POST /clipboard` as the dismissal-adjacent write-back path.
- Ticket 13 replaced `POST /clipboard` with `POST /api/ship-prompt` (adds context enrichment, middleware pipeline, response body).
- Ticket 11 elaborated the pipeline behind `POST /api/ship-prompt`; noted `POST /clipboard` superseded.

**Data file name** (`report-input.json` → `improvement-report-data.json`):
- Tickets 01, 06, 08b, 09, 11 refer to `report-input.json`.
- Ticket 12 renamed it to `improvement-report-data.json` and promoted it to a living document.

**Dismissal path** (`--dismiss <key>` CLI command → `POST /api/dismissals` primary):
- Ticket 05 established CLI-owned suggestion history; dismissal was CLI-side.
- Ticket 08b introduced `POST /api/dismissals` as the primary dismissal path in `--serve` (now `--self-improve`) mode; `--dismiss <key>` demoted to `file://`-mode fallback.

### Authoritative current state

| Point | Current answer | Decided by |
|---|---|---|
| Delivery model | Local C# server (`--self-improve` auto-launch, port 5173). No self-contained HTML fallback. `file://` mode shows a degraded-state banner only. | Tickets 09, 01 |
| CLI launch flag | `--self-improve` auto-launches the server. No `--serve` flag. | Ticket 09 |
| Clipboard/LLM copy endpoint | `POST /api/ship-prompt` — runs middleware pipeline, writes to desktop clipboard, returns transformed prompt. | Ticket 13 |
| `POST /clipboard` | **Superseded** by `POST /api/ship-prompt` (ticket 13). Not part of the active API surface. | Ticket 13 |
| Data file name | `improvement-report-data.json` (living document: orchestrator output + user decisions + shipped prompt). | Ticket 12 |
| Dismissal path | `POST /api/dismissals` (primary, server mode); `--dismiss <key>` CLI (fallback, `file://` mode). | Ticket 08b |
| "Copy readable" | Browser clipboard API directly — no server call. | Ticket 13 |
| "Copy for LLM" | Browser POSTs assembled readable prompt to `POST /api/ship-prompt`; server enriches, runs pipeline, writes clipboard, returns result. | Tickets 13, 11 |

### Stale wording corrected in this pass

**Map — Decisions-so-far summaries updated:**
- *Local-first delivery constraints* gist updated: no longer implies self-contained `file://` is primary — it records the constraints that shaped the decision, and notes the model was later superseded by the server model (ticket 09).
- *Local server delivery model* gist updated: `POST /clipboard` removed from the gist; `POST /api/ship-prompt` is the correct endpoint.

**Ticket bodies — supersession notices added:**
- Ticket 04 answer: notice that the single-file `file://` delivery model was superseded by ticket 09.
- Ticket 08a answer: notice that `--serve` flag was superseded; `--self-improve` auto-launches the server (ticket 09).
- Ticket 08b answer: `POST /clipboard` reference in the permitted-surface table annotated as superseded by `POST /api/ship-prompt` (ticket 13).
- Ticket 09 answer: `POST /clipboard` row in API-surface table annotated as superseded by `POST /api/ship-prompt` (ticket 13); data file name updated to `improvement-report-data.json`.
- Ticket 11 answer: data file name updated to `improvement-report-data.json`.

*(Ticket answers are historical record — corrections are additive notices, not rewrites.)*
