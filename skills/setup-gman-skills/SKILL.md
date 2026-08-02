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

## Workflow

### 1. Check dotnet-blazor plugin

Run `copilot plugin list` and look for `dotnet-blazor` in the output.

- **Missing**: Install it in two steps:
  1. `copilot plugin marketplace add dotnet/skills`
  2. `copilot plugin install dotnet-blazor@dotnet-agent-skills`
- **Present**: Skip — note that the plugin is already installed.

### 2. Check report-server binary

Check whether the report-server binary exists at `~/.copilot/gman-skills/bin/`.

- **Windows**: look for `report-server.exe`.
- **Linux / macOS**: look for `report-server` (no extension).

If the binary is **present**, skip the download and note it is already installed.

If the binary is **missing**, run the platform-appropriate download script:

| Platform | Script |
|---|---|
| Windows | `scripts/setup-report-server.ps1` |
| Linux / macOS | `scripts/setup-report-server.sh` |

The script queries the GitHub Releases API (`gvdvenis/gman-skills`), downloads the matching
asset (`report-server-{os}-{arch}.zip`), and extracts the binary to
`~/.copilot/gman-skills/bin/`.

### 3. Print summary

Print a summary table:

| Component | Status |
|---|---|
| dotnet-blazor plugin | `installed` / `was installed` / `failed` |
| report-server binary | `present` / `downloaded` / `failed` |

List any follow-up actions the user should take (e.g. restart the CLI, build from source).

## Preflight check

For a non-blocking dependency check (always exits 0), use the check-deps scripts:

- Windows: `scripts/check-deps.ps1`
- Linux / macOS: `scripts/check-deps.sh`

These print warnings for missing components but never fail.
