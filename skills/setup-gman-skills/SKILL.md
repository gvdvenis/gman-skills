---
name: setup-gman-skills
description: First-time setup for gman-skills. Installs the dotnet-blazor plugin and downloads the report-server binary when either is missing.
disable-model-invocation: true
# written for: Opus 5.5 / Sonnet 5.5; not tested on Haiku
---

# setup-gman-skills

Check first; act only when something is missing.

## Step 1 — dotnet-blazor plugin

Use `copilot` in Copilot CLI and `claude` in Claude Code. When `<cli> plugin list` does not show
`dotnet-blazor`:

1. `<cli> plugin marketplace add dotnet/skills`
2. `<cli> plugin install dotnet-blazor@dotnet-agent-skills`

Status: `installed`, `present`, or `failed` with the error. In Claude Code, say in the summary
that the plugin's skills load in the next session.

## Step 2 — report-server binary

Look for `~/.copilot/gman-skills/bin/report-server.exe` (Windows) or `report-server`
(Linux/macOS). When missing, run the script from this skill's folder (shown when this skill
loaded):

- Windows: `powershell -NoProfile -ExecutionPolicy Bypass -File <skill-folder>/scripts/setup-report-server.ps1`
- Linux/macOS: `bash <skill-folder>/scripts/setup-report-server.sh` (needs `curl` and `unzip`)

When no release fits, both build from `src/report-server`, which needs the .NET SDK.
Status: `downloaded`, `present`, or `failed` with the error.

## Step 3 — Summary

Always print the table. The header says `Setup complete` when nothing failed. A failed row gets
the reason and a fix:

```
[setup-gman-skills] Setup complete with warnings

  Component             Status      Note
  ───────────────────────────────────────────────
  dotnet-blazor plugin  installed
  report-server binary  failed      No GitHub Release found. Build from source:
                                    cd src/report-server && dotnet publish -c Release -r win-x64
                                    Then copy the binary to ~/.copilot/gman-skills/bin/
```
