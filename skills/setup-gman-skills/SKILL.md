---
name: setup-gman-skills
description: >
  First-time setup for the gman-skills package. Checks whether the dotnet-blazor plugin is installed
  and installs it when missing. Checks whether the report-server binary exists and downloads it
  from GitHub Releases when missing. Offers to add the plain-language session-start hook. Run once after `npx skills add gvdvenis/gman-skills`.
  Triggers on: "setup gman skills", "/setup-gman-skills", "install gman skills dependencies".
user-invocable: true
---

# setup-gman-skills

First-time setup for the gman-skills package. Run once after `npx skills add gvdvenis/gman-skills`.

## Step 1 — Check and install the dotnet-blazor plugin

Use the CLI of the agent running this skill: `copilot` in Copilot CLI, `claude` in Claude Code.
Run `<cli> plugin list` and look for `dotnet-blazor` in the output.

If **missing**, install it in two steps:
1. `<cli> plugin marketplace add dotnet/skills`
2. `<cli> plugin install dotnet-blazor@dotnet-agent-skills`

In Claude Code the plugin's skills load in the next session, not the current one; say so in the
summary.

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

## Step 3 — Offer the plain-language hook

The `use-plain-language` skill holds rules for clear writing and readable question rounds. A
session-start hook hands those rules to the agent at the start of every session, so they apply
without anyone invoking the skill.

Check first: if the hook config below already mentions `use-plain-language`, record `present`
and skip. Otherwise ask the user once: "Add the plain-language hook, so every session starts
with the clear-writing rules? (y/n)". On no, record `declined`.

On yes, resolve the absolute path of `../use-plain-language/scripts/` from this skill's folder,
then add the hook for the agent running this skill. Merge into existing config; keep every
hook already there.

- **Claude Code**: add a `SessionStart` entry without a matcher (so it also runs after `/clear`
  and compaction) to `~/.claude/settings.json`:
  ```json
  { "hooks": { "SessionStart": [ { "hooks": [ { "type": "command",
    "command": "<run-command> claude" } ] } ] } }
  ```
  `<run-command>` on Windows: `powershell -NoProfile -ExecutionPolicy Bypass -File "<scripts>/session-start.ps1"`;
  on Linux / macOS: `sh "<scripts>/session-start.sh"`.
- **Copilot CLI**: write `~/.copilot/hooks/use-plain-language.json`:
  ```json
  { "version": 1, "hooks": { "sessionStart": [ { "type": "command",
    "bash": "sh \"<scripts>/session-start.sh\" copilot",
    "powershell": "powershell -NoProfile -ExecutionPolicy Bypass -File \"<scripts>/session-start.ps1\" copilot",
    "timeoutSec": 10 } ] } }
  ```

Run the same command once by hand and check it prints the rules. Record `added` or `failed`.

**Done when:** the hook is `added`, `present` or `declined`, or it `failed` and the error is
recorded.

## Step 4 — Print the summary

Print a summary table so the user can see what happened. This is mandatory — the user needs
confirmation that setup worked:

```
[setup-gman-skills] Setup complete

  Component            Status
  ───────────────────────────────
  dotnet-blazor plugin  installed
  report-server binary  downloaded
  plain-language hook   added
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
