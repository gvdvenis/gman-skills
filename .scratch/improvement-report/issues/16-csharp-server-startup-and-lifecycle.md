# 16 — Fixed-port C# server startup and lifecycle

**What to build:** `--self-improve` auto-launches a single C# server process on port 5173 (default). The server exposes `GET /api/report` (returns the run's `improvement-report-data.json`), `GET /ping`, and `GET /shutdown`. The server binds to loopback (`127.0.0.1`) by default; all-interfaces (`0.0.0.0`) when configured (e.g. for mobile access). If port 5173 is already bound, the CLI logs a warning and assumes the server is already running, then continues. The server stays alive after the first report load until `GET /shutdown` is called, an idle timeout expires, or the terminal process ends.

**Blocked by:** 15 — Generate `improvement-report-data.json` and CLI staging signal.

**Status:** done

- [x] `--self-improve` auto-launches the C# server; no separate flag required
- [x] `GET /api/report` returns the run's JSON payload
- [x] `GET /ping` returns a 200 response
- [x] `GET /shutdown` gracefully stops the server
- [x] Port-already-bound is detected, logged as a warning, and treated as server already running (exit code 2)
- [x] Default binding is loopback; all-interfaces binding is configurable via `--bind 0.0.0.0`
- [x] Server stays alive after first report load until idle timeout or explicit shutdown
