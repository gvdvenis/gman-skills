# report-server

Minimal ASP.NET Core server that serves `improvement-report-data.json` for the
`--self-improve` flow. Auto-launched by the orchestrator skill — no separate CLI flag required.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/ping` | Liveness probe; returns `{"status":"ok"}` |
| `GET` | `/api/report` | Returns the run's `improvement-report-data.json` as `application/json` |
| `GET` | `/shutdown` | Graceful stop; returns `{"message":"shutting down"}` |

## CLI arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--report-path <path>` | *(required)* | Absolute path to `improvement-report-data.json` |
| `--port <n>` | `5173` | TCP port to listen on |
| `--bind <address>` | `127.0.0.1` | Bind address; use `0.0.0.0` for mobile/all-interfaces access |
| `--idle-minutes <n>` | `10` | Idle timeout after the browser first connects |

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Normal exit (shutdown requested or idle timeout) |
| `1` | Bad arguments (missing/invalid `--report-path`) |
| `2` | Port already bound; assumes server already running — caller should log a warning and continue |

## Lifecycle

1. `--self-improve` launches this process before opening the browser.
2. Port `5173` is probed first; if already bound, exit code `2` is returned and no second
   instance is started (the skill logs a warning and continues).
3. Default binding is loopback (`127.0.0.1`) — no Windows Firewall prompt.
4. All-interfaces (`0.0.0.0`) is used for mobile access when the user has configured that.
5. The server stays alive after the first `GET /api/report` until:
   - `GET /shutdown` is called, **or**
   - the idle timeout elapses (no requests for `--idle-minutes` minutes), **or**
   - the terminal session ends (parent process exits).

## Development preview

```sh
dotnet run
```

The Development launch profile uses an isolated temporary copy of
`Fixtures/improvement-report-data.json`, so the report renders immediately and actions such as
dismissal and prompt shipping never alter tracked fixture data.

## Running a specific report

```sh
dotnet run -- --report-path /path/to/improvement-report-data.json
```

Outside Development, `--report-path` is required.

## Build and test

```sh
dotnet build
dotnet test ReportServer.Tests/ReportServer.Tests.csproj
```

## Native AOT publish

The server is configured for self-contained native AOT publishing. Publish the Windows target
with:

```sh
dotnet publish -c Release -r win-x64
```

Native AOT requires the Visual Studio C++ build tools (including `vswhere.exe` and the x64 linker)
to be installed. If the Visual Studio Installer directory is not already on `PATH`, run this
PowerShell command before publishing:

```powershell
$env:Path = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer;$env:Path"
dotnet publish -c Release -r win-x64
```

Add one publish command and native-toolchain validation per additional target runtime identifier.
