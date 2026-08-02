---
name: setup-gman-skills
description: >
  First-time setup for the gman-skills package. Checks whether the dotnet-blazor plugin is installed
  and installs it when missing. Checks whether the report-server binary exists and downloads it
  from GitHub Releases when missing. Run once after `npx skills add gvdvenis/gman-skills`.
  Triggers on: "setup gman skills", "/setup-gman-skills", "install gman skills dependencies".
user-invokable: true
---

# setup-gman-skills

First-time setup for the gman-skills package. Run once after `npx skills add gvdvenis/gman-skills`.

## Step 1 — Check and install the dotnet-blazor plugin

Run `copilot plugin list` and look for `dotnet-blazor` in the output.

If **missing**, install it in two steps:
1. `copilot plugin marketplace add dotnet/skills`
2. `copilot plugin install dotnet-blazor@dotnet-agent-skills`

If **present**, skip — it is already installed.

Record the result: `installed` (was just installed), `present` (was already there), or `failed`.

**Done when:** the dotnet-blazor plugin is installed or confirmed present, or the install failed
and the error is recorded.

## Step 2 — Check and download the report-server binary

Check whether the report-server binary exists at `~/.copilot/gman-skills/bin/`:
- **Windows**: `report-server.exe`
- **Linux / macOS**: `report-server` (no extension)

If **present**, skip — it is already installed. Record `present`.

If **missing**, run the platform-appropriate download script from this skill's `scripts/` directory:
- **Windows**: `scripts/setup-report-server.ps1`
- **Linux / macOS**: `scripts/setup-report-server.sh`

The script detects OS + architecture, downloads the matching asset from the `gvdvenis/gman-skills`
GitHub Releases, and extracts the binary to `~/.copilot/gman-skills/bin/`.

Record the result: `downloaded` (was just downloaded), `present` (was already there), or `failed`.

**Done when:** the binary exists at `~/.copilot/gman-skills/bin/` or the download failed and the
error is recorded.

## Step 3 — Print the summary

Print a summary table so the user can see what happened. This is mandatory — the user needs
confirmation that setup worked:

```
[setup-gman-skills] Setup complete

  Component            Status
  ───────────────────────────────
  dotnet-blazor plugin  installed
  report-server binary  downloaded
```

If any component failed, print the failure reason and a suggested fix:

```
[setup-gman-skills] Setup complete with warnings

  Component            Status     Note
  ───────────────────────────────────────────────
  dotnet-blazor plugin  present
  report-server binary  failed     No GitHub Release found. Build from source:
                                    cd src/report-server && dotnet publish -c Release -r win-x64
                                    Then copy the binary to ~/.copilot/gman-skills/bin/
```

**Done when:** the summary table is printed with every component's status visible.
