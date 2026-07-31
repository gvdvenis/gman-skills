# Port conflict handling and Windows firewall

Type: grilling
Status: resolved
Assignee: @copilot

## Question

Ticket 09 locked the server at a fixed port (default `5173`, configurable via config/env). Two
operational edge cases were deferred to this ticket:

1. **Port conflict**: what happens when `5173` is already bound — another process, a stale server
   instance, or a second parallel `--self-improve` run? Who detects it, what error is shown, what
   options does the user have?
2. **Windows firewall**: on first bind, Windows may prompt the user to allow the app through the
   firewall. Is this expected, suppressed, or guided?

Decide:

- Detection and response strategy for a bound port (fail-fast vs retry on next free port vs
  configurable behaviour).
- Whether to attempt to detect and kill a stale instance of *this* server on the same port.
- What the user-facing message looks like on conflict.
- Whether `--self-improve` should validate the port is free before launching or let the server
   process fail and surface the OS error.
- How the Windows firewall prompt is handled: loopback-only binding (`127.0.0.1`) vs `0.0.0.0`, and
  whether to document the expected first-run prompt or try to avoid it.

## Answer

**Port already bound → assume our server is running; log a warning and continue.**

No fail-fast, no stale-instance detection. The stable QR-code URL is unaffected because the same
server is already serving it. The warning is emitted to the chat output.

**Binding mode is a skill-level preference, passed as a flag to the server tool.**

- Default: loopback-only (`127.0.0.1`) — no Windows firewall prompt.
- Mobile access: all-interfaces (`0.0.0.0`) — Windows may prompt on first bind.
- The skill (`self-improve-report`) accepts the preference as a natural language instruction or
  parameter. It persists the choice in the user temp folder (`Path.GetTempPath()`), so subsequent
  runs are silent. Re-asks only when temp has been cleared — intentional, matches user expectation
  (like a cleared session cookie).
- The skill passes the resolved binding address as a flag to the C# server tool on launch.
- No dedicated reset command; overriding via a natural language instruction to the skill overwrites
  temp.

**Windows firewall:** loopback-only binding never triggers the firewall prompt. All-interfaces
binding may trigger it on first use — this is expected and documented, not suppressed.

## Depends on

- [Local server delivery model](09-local-server-delivery-model.md) — fixed port, auto-launched server

