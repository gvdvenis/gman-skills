# CLI callback vs copy-out boundary

Type: grilling
Status: resolved
Assignee: @copilot
Blocked by: 04, 09

## Question

Can the self-improvement HTML report trigger actions back in the CLI, or is it strictly copy-out only?

Decide:

- Whether the report is allowed to reach back into the CLI at all (e.g. write dismissals, apply a
  chosen prompt directly, launch an agent run).
- If callbacks are permitted, what the mechanism is (local HTTP server, named pipe, CLI watch loop,
  or other) and what the security surface looks like.
- If callbacks are not permitted, what the copy-out model covers and whether it is sufficient.
- Complexity cost: does allowing callbacks change the delivery model decided in ticket 04 (single
  self-contained HTML file, no network calls)?

## Answer

**User-initiated server write-back is permitted via the local C# server. Automatic mutations remain
forbidden.**

### What changed

The original answer ruled out all callbacks because any callback required a resident server —
incompatible with the `file://` single-file delivery model (ticket 04). That premise is gone:
ticket 09 made the local C# server the sole delivery path, auto-launched on every `--self-improve`
run. A server is now always present.

### The boundary

The standing Notes rule — "nothing the report emits mutates agents or skills *automatically*" —
remains the governing constraint. That word does the work. The new rule is:

> **User-initiated write-back via the server API is permitted. Automatic or background mutations
> are not.**

A user clicking "Dismiss" or "Copy to LLM" is a deliberate act. The server recording that act is
not automatic mutation; it is the mechanism of a user gesture. An agent run silently triggered
after copying a prompt *is* automatic, and is still out of scope.

### Permitted callback surface (minimal)

These are the only in-scope server endpoints that constitute write-back:

| Endpoint | Purpose | Already decided? |
|---|---|---|
| `POST /clipboard` *(superseded — see ticket 13; replaced by `POST /api/ship-prompt`)* | Write assembled prompt to desktop clipboard on user click | Yes — ticket 09 |
| `POST /api/dismissals` | Record a dismissal for a `suggestion_key` when user clicks Dismiss | New — this ticket |

`POST /api/dismissals` replaces the `--dismiss <key>` CLI command as the primary dismissal path in
`--serve` mode. The CLI command remains valid as a fallback for `file://` mode (degraded banner) or
scripted use.

### What remains out of scope

- Applying a prompt to an agent run automatically or on user click.
- Any write-back that executes code, modifies agents or skills, or triggers side effects beyond
  recording a user decision.
- Endpoints beyond the two above — the server API surface is locked at ticket 09's table plus
  `POST /api/dismissals`.

### Security surface

The security surface is unchanged from ticket 09's assessment: `127.0.0.1` only, no auth, any
local process can call these endpoints. Acceptable given the tool is a local dev aid, not a
multi-user system. Dismissal writes are idempotent and low-stakes.

## Comments

Reopened after ticket 09 (Local server delivery model) established the local C# server as the sole
delivery path, invalidating the original "no resident server" rationale. Ticket 09 explicitly noted
the CLI-callback NB and deferred re-resolution here. Resolved in the same session as the reopen.
